#if UNITY_EDITOR

using PropagationSystem;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class EditorPreviewer
{
    SceneData sceneData;
    ComputeShader frustumComputeShader;
    Camera sceneCamera;
    List<EditorRenderer> renderers = new List<EditorRenderer>();
    ComputeBuffer planesBuffer;

    Plane[] frustumPlanes = new Plane[6];
    Vector3 lastCamPosition;
    Quaternion lastCamRot;
    float frustumSmoothDistance = 4;

    bool isInPlayMode = false;
    bool isPreviewing = false;
    bool isInitialized = false;
    bool isFrustumCalculationNeeded = false;
    bool isFrustumWarm = false;

    const string FRUSTUMCOMPUTESHADERGUID = "e82eedd14c716fc4ea275d5898382edc";

    public EditorPreviewer()
    {
        SceneView.duringSceneGui += Update;
        EditorApplication.playModeStateChanged += OnEnterPlayMode;
    }

    public void Setup(SceneData sceneData)
    {
        if (isInPlayMode) return;

        this.sceneData = sceneData;
        FindSceneCamera();
        lastCamPosition = sceneCamera.transform.position;
        Initialize();
    }

    public void SetPreviewMode(bool previewMode)
    {
        isPreviewing = previewMode;
        if (!isPreviewing)
            Teardown();
    }

    public bool GetIsPreviewing()
    {
        return isPreviewing;
    }
    public void Teardown()
    {
        DisposeEverything();
    }

    void DisposeEverything()
    {
        isInitialized = false;
        isPreviewing = false;

        DisposeRenderers();
        renderers.Clear();
        sceneData = null;

        planesBuffer?.Dispose();
        planesBuffer = null;
    }

    void DisposeRenderers()
    {
        foreach (EditorRenderer renderer in renderers)
            renderer.DisposeAll();
    }

    void OnEnterPlayMode(PlayModeStateChange change)
    {
        switch (change)
        {
            case PlayModeStateChange.EnteredPlayMode:
                DisposeEverything();
                isInPlayMode = true;
                break;

            case PlayModeStateChange.ExitingPlayMode:
                isInPlayMode = false;
                break;
        }
    }

    void FindSceneCamera()
    {
        sceneCamera = SceneView.lastActiveSceneView.camera;
    }

    public void Initialize()
    {
        if (sceneData == null && isInitialized) return;

        planesBuffer = new ComputeBuffer(6, sizeof(float) * 4);
        CalculateFrustum();

        if (frustumComputeShader == null)
        {
            string shaderPath = AssetDatabase.GUIDToAssetPath(FRUSTUMCOMPUTESHADERGUID);
            frustumComputeShader = EditorGUIUtility.Load(shaderPath) as ComputeShader;
        }

        DisposeRenderers();
        renderers.Clear();
        SetupRenderers();

        isInitialized = true;
    }

    void SetupRenderers()
    {
        for (int i = 0; i < sceneData.propagatedMeshDefinitions.Count; i++)
        {
            if (sceneData.propagatedObjectDatas[i].instanceDatas.Count > 0)
                renderers.Add(CreateEditorRenderer(i));
        }
    }

    EditorRenderer CreateEditorRenderer(int i)
    {
        ComputeShader shader = Object.Instantiate(frustumComputeShader);
        Mesh mesh = sceneData.propagatedMeshDefinitions[i].mesh;
        Material material = Object.Instantiate(sceneData.propagatedMeshDefinitions[i].material);
        TransformTransferData[] trs = new TransformTransferData[sceneData.propagatedObjectDatas[i].instanceDatas.Count];

        for (int j = 0; j < sceneData.propagatedObjectDatas[i].instanceDatas.Count; j++)
        {
            TransformData data = sceneData.propagatedObjectDatas[i];
            Matrix4x4 transferMatrix = Matrix4x4.TRS(
                data.instanceDatas[j].position,
                data.instanceDatas[j].rotation,
                data.instanceDatas[j].scale
            );
            trs[j].trsMatrices = transferMatrix;
        }

        return new EditorRenderer(mesh, material, shader, trs, planesBuffer);
    }

    public void CalculateFrustum()
    {
        if (!isInitialized) return;
        if (sceneCamera == null)
            FindSceneCamera();

        frustumPlanes = GeometryUtility.CalculateFrustumPlanes(sceneCamera);
        PropagationSystem.FrustumPlanes[] fp = new PropagationSystem.FrustumPlanes[6];

        for (int i = 0; i < frustumPlanes.Length; i++)
        {
            fp[i].normal = frustumPlanes[i].normal;
            fp[i].distance = frustumPlanes[i].distance + frustumSmoothDistance;
        }

        planesBuffer.SetData(fp);
    }

    void Update(SceneView sceneView)
    {
        if (!isInitialized) return;
        if (Event.current.type != EventType.Repaint)
            return;

        if ((sceneCamera.transform.position - lastCamPosition).sqrMagnitude > 25 ||
            Quaternion.Angle(sceneCamera.transform.rotation, lastCamRot) > 15)
        {
            isFrustumCalculationNeeded = true;
            lastCamPosition = sceneCamera.transform.position;
        }

        if (isPreviewing)
        {
            if (isFrustumCalculationNeeded)
                CalculateFrustum();

            if (isFrustumWarm)
            {
                for (int i = 0; i < 10; i++)
                    CalculateFrustum();
                isFrustumWarm = false;
            }

            foreach (var renderer in renderers)
            {
                if (isFrustumCalculationNeeded)
                    renderer.UpdateFrustum(planesBuffer);
                renderer.Render();
            }

            isFrustumCalculationNeeded = false;
        }
    }

    public void OnSceneDataChanged(int index)
    {
        if (!isInitialized) return;

        TransformTransferData[] trs = new TransformTransferData[sceneData.propagatedObjectDatas[index].instanceDatas.Count];

        for (int j = 0; j < sceneData.propagatedObjectDatas[index].instanceDatas.Count; j++)
        {
            TransformData data = sceneData.propagatedObjectDatas[index];
            Matrix4x4 transferMatrix = Matrix4x4.TRS(
                data.instanceDatas[j].position,
                data.instanceDatas[j].rotation,
                data.instanceDatas[j].scale
            );
            trs[j].trsMatrices = transferMatrix;
        }

        if (renderers.Count <= index)
        {
            DisposeRenderers();
            renderers.Clear();
            if (trs.Length <= 0) return;
            SetupRenderers();
        }

        if (trs.Length <= 0) return;
        isFrustumWarm = true;
        renderers[index].Update(trs);
    }
}

#endif
