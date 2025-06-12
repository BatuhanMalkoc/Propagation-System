using System;
using UnityEditor;
using UnityEngine;
using PropagationSystem;
using Random = UnityEngine.Random;
using PropagationSystem.Editor;

#if UNITY_EDITOR
public class PropagationBrushModel
{
    #region Brush
    public Action<StrokeData> OnStroke;

    private PropagationBrushSettingsModel Brush;
    private BrushDrawerModel brushDrawerModel;
    private bool isBrushActive = false;
    private SceneData sceneData;
    #endregion

    #region Scene State
    static bool isPressing;
    #endregion

    #region Constructor
    public PropagationBrushModel(PropagationBrushSettingsModel brush)
    {
        SceneView.duringSceneGui += OnSceneGUI;
        Brush = brush;
        brushDrawerModel = new BrushDrawerModel();
    }
    #endregion

    public void ToggleBrush(bool isActive)
    {
        isBrushActive = isActive;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0 && e.control)
        {
            isPressing = true;
            e.Use();
        }

        if (isBrushActive)
        {
            brushDrawerModel.DrawBrush(sceneView.camera, Brush);
            sceneView.Repaint();
        }
    }

    static bool IsValidQuaternion(Quaternion q)
    {
        float magnitude = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        return magnitude > 0.0001f && !float.IsNaN(magnitude) && !float.IsInfinity(magnitude);
    }

    // Refactor notes:
    /*
    private static int count;
    private static Mesh brushMesh;
    private static Material brushMaterial;
    public static PropagationBrushWindow.PropagationMode propagationMode = PropagationBrushWindow.PropagationMode.Random;
    public static PropagationBrushWindow.BrushMode brushMode;

    private void DrawBrushPlane(Camera camera) { ... }
    private void OnPaintBrush(Vector3 point, Vector3 normal, Camera camera) { ... }
    private void OnEraseBrush() { ... }
    */
}

public class BrushDrawerModel
{
    private Vector3 lastHitNormal;
    private Vector3 lastHitPoint;

    public void DrawBrush(Camera camera, PropagationBrushSettingsModel brush)
    {
        if (brush.selectedBrushSO == null || brush.selectedBrushSO.maskTexture == null)
            return;

        UpdateHitPoint();
        DrawBrushPlane(camera, brush);
    }

    private void UpdateHitPoint()
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            lastHitNormal = hit.normal;
            lastHitPoint = hit.point;
        }
    }

    private void DrawBrushPlane(Camera camera, PropagationBrushSettingsModel brush)
    {
        brush.brushMaterial.SetPass(0);
        brush.brushMaterial.SetTexture("_MaskTexture", brush.selectedBrushSO.maskTexture);
        brush.brushMaterial.SetFloat("_BrushSize", brush.brushSize);

        if (brush.brushMode == PropagationBrushWindow.BrushMode.Erase)
            brush.brushMaterial.SetColor("_Tint", Color.red);
        else
            brush.brushMaterial.SetColor("_Tint", Color.white);

        Graphics.DrawMeshNow(
            brush.brushMesh,
            lastHitPoint + lastHitNormal * 0.2f,
            Quaternion.LookRotation(Vector3.Cross(lastHitNormal, camera.transform.right))
        );
    }
}
#endif
