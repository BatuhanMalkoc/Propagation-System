﻿using System;
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
    private BrushDrawerSubModel brushDrawerModel;
    private bool isBrushActive = false;
    private Camera sceneCamera;
    #endregion

    #region Scene State
     bool isPressing;
    #endregion

    #region Constructor
    public PropagationBrushModel(PropagationBrushSettingsModel brush)
    {
        Brush = brush;
        brushDrawerModel = new BrushDrawerSubModel();
    }
    #endregion

    public void ToggleBrush(bool isActive)
    {
        isBrushActive = isActive;
    }

    public bool GetBrushActive()
    {
        return isBrushActive;
    }

    public void DrawBrush(Camera camera)
    {
       
        sceneCamera = camera;
        brushDrawerModel.DrawBrush(camera, Brush);
        
    }


    public void HandleBrushCommand()
    {
        switch (Brush.brushMode)
        {
            case PropagationBrushWindow.BrushMode.Paint: Paint(); break;

            case PropagationBrushWindow.BrushMode.Erase: break;
        }
    }

    private void Paint()
    {
        StrokeData stroke = new StrokeData();

        switch (Brush.sampleMode)
        {
            case PropagationBrushWindow.PropagationMode.Random:
                stroke = PaintRandom();
                OnStroke?.Invoke(stroke);
                break;

            case PropagationBrushWindow.PropagationMode.RandomWeighted:
                stroke = PaintRandomWeighted();
                OnStroke?.Invoke(stroke);
                break;

            case PropagationBrushWindow.PropagationMode.Grid:
                stroke = PaintGrid();
                OnStroke?.Invoke(stroke);
                break;

            default:
                Debug.LogWarning("Unsupported sample mode: " + Brush.sampleMode);
                break;
        }
    }


    private StrokeData PaintRandom() {

        BrushPoint brushPoint = brushDrawerModel.GetBrushPoint();
        BrushDataSO brushDataSO = Brush.selectedBrushSO;
        float brushSize = Brush.brushSize;
        float brushDensity = Brush.brushDensity;
        int count = Brush.instanceCount;
        int meshIndex = Brush.selectedMeshIndex;

        int finalCount = Mathf.Max(1,Mathf.RoundToInt((brushDensity / 100f) * count));

        AdditionalBrushData additionalData = new AdditionalBrushData
        {
            staticPositionOffset = Brush.staticPositionOffset,
            staticScale = Brush.staticScale,
            staticRotationEuler = Brush.staticRotationEuler,
            randomPositionOffsetMin = Brush.randomPositionOffsetMin,
            randomPositionOffsetMax = Brush.randomPositionOffsetMax,
            randomPositionOffsetPerComponent = Brush.randomPositionOffsetPerComponent,
            randomScaleMin = Brush.randomScaleMin,
            randomScaleMax = Brush.randomScaleMax,
            randomScalePerComponent = Brush.randomScalePerComponent,
            randomRotationMinEuler = Brush.randomRotationMinEuler,
            randomRotationMaxEuler = Brush.randomRotationMaxEuler,
            randomRotationPerComponent = Brush.randomRotationPerComponent,
            staticSize = Brush.staticSize,
            randomSizeMin = Brush.randomSizeMin,
            randomSizeMax = Brush.randomSizeMax,
        };

        BrushRandom2D brushRandom2D = new BrushRandom2D();

        StrokeData data = brushRandom2D.ApplyBrush(brushDataSO, brushPoint.brushPoint, brushPoint.brushNormal, brushSize,brushDensity,finalCount, meshIndex,additionalData, sceneCamera);

        return data;
    }
    private StrokeData PaintRandomWeighted()
    {
        BrushPoint brushPoint = brushDrawerModel.GetBrushPoint();
        BrushDataSO brushDataSO = Brush.selectedBrushSO;
        float brushSize = Brush.brushSize;
        float brushDensity = Brush.brushDensity;
        int count = Brush.instanceCount;
        int meshIndex = Brush.selectedMeshIndex;

        int finalCount = Mathf.Max(1,Mathf.RoundToInt((brushDensity / 100f) * count));

        BrushRandomWeighted2D weightedBrush = new BrushRandomWeighted2D();

        StrokeData data = weightedBrush.ApplyBrush(
            brushDataSO,
            brushPoint.brushPoint,
            brushPoint.brushNormal,
            brushSize,
            brushDensity,
            finalCount,
            meshIndex,
            sceneCamera
        );

        return data;
    }

    private StrokeData PaintGrid()
    {
        BrushPoint brushPoint = brushDrawerModel.GetBrushPoint();
        BrushDataSO brushDataSO = Brush.selectedBrushSO;
        float brushSize = Brush.brushSize;
        float brushDensity = Brush.brushDensity;
        int count = Brush.instanceCount;
        int meshIndex = Brush.selectedMeshIndex;

        // Density ile instanceCount oranı
        int finalCount = Mathf.Max(1,Mathf.RoundToInt((brushDensity / 100f) * count));

       

        BrushGrid2D gridBrush = new BrushGrid2D();
        return gridBrush.ApplyBrush(
            brushDataSO,
            brushPoint.brushPoint,
            brushPoint.brushNormal,
            brushSize,
            finalCount,
            meshIndex,
            sceneCamera
        );
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

public class BrushDrawerSubModel
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

    public BrushPoint GetBrushPoint()
    {
        BrushPoint brushPoint = new BrushPoint
        {
            brushPoint = lastHitPoint,
            brushNormal = lastHitNormal
        };

        return brushPoint;
    }
}


public struct BrushPoint
{
    public Vector3 brushPoint;
    public Vector3 brushNormal;
}
#endif
