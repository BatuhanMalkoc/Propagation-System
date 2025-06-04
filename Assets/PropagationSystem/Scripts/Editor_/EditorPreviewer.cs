using PropagationSystem;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Analytics;
using UnityEditor;
using UnityEngine.Rendering;
using System.Collections;
using Unity.VisualScripting;
using PropagationSystem.Editor;

#if UNITY_EDITOR

[ExecuteInEditMode]
[InitializeOnLoad]
public static class EditorPreviewer
{
    static SceneData SceneData;

    static ComputeShader frustumComputeShader;

    static Camera sceneCamera;

    static List<EditorRenderer> renderers = new List<EditorRenderer>();

    static ComputeBuffer planesBuffer;

    static Plane[] frustumPlanes = new Plane[6];

    static Vector3 lastCamPosition;

    static Quaternion lastCamRot;

    static float frustumSmoothDistance = 4;

   static bool isInPlayMode = false;
    
    const string FRUSTUMCOMPUTESHADERGUID = "e82eedd14c716fc4ea275d5898382edc";

    static bool isPreviewing = false;
    static bool isInitialized = false;

    static bool isFrustumCalculationNeeded = false;

    static bool isFrustumWarm = false;

    public static void Setup(SceneData sceneData)
    {
        if (isInPlayMode) return;

        
        SceneData = sceneData;
        FindSceneCamera();
        lastCamPosition = sceneCamera.transform.position;
        Initialize();

        PropagationBrushWindow.OnBrushStroke += OnSceneDataChanged;
    }
    public static void SetPreviewMode(bool previewMode)
    {
        isPreviewing = previewMode;
        if (!isPreviewing)
        {
            Teardown();
           
        }

    
    }

    public static void Teardown()
    {
        PropagationBrushWindow.OnBrushStroke -= OnSceneDataChanged;
        DisposeEverthing();
    }
    public static void OnSceneDataChanged(int index)
    {
        if (!isInitialized) return;

        // 1. Yeni eklenen objeler için TransformTransferData dizisi oluşturuluyor
        TransformTransferData[] trs = new TransformTransferData[SceneData.propagatedObjectDatas[index].trsMatrices.Count];
        for (int j = 0; j < SceneData.propagatedObjectDatas[index].trsMatrices.Count; j++)
        {
            TransformData data = SceneData.propagatedObjectDatas[index];
            Matrix4x4 transferMatrix = Matrix4x4.TRS(
                data.trsMatrices[j].position,
                data.trsMatrices[j].rotation,
                data.trsMatrices[j].scale
            );
            trs[j].trsMatrices = transferMatrix;
        }

        // 2. Eğer index ≥ renderers.Count ise renderers silinip yeniden initialize ediliyor
        if (renderers.Count <= index)
        {
            DisposeRenderers();
            renderers.Clear();
            SetupRenderers(); // ✱
        }

        // 3. Güncellenen TRS verisi ilgili renderer'a set ediliyor

        isFrustumWarm = true;

        renderers[index].Update(trs);


        // 4. SceneView'i yeniden boyamak için
     
    }

    static void DisposeEverthing() {

        isInitialized = false;
        isPreviewing = false;


        DisposeRenderers();


        renderers.Clear();
        SceneData = null;

        if (planesBuffer != null)
        {
            planesBuffer.Dispose();
        }
       
    }

    static void DisposeRenderers()
    {
        foreach (EditorRenderer renderer in renderers)
        {
            renderer.DisposeAll();
        }
    }

    static EditorPreviewer()
    {


        SceneView.duringSceneGui += Update;
        EditorApplication.playModeStateChanged += OnEnterPlayMode;
    }

    private static void OnEnterPlayMode(PlayModeStateChange change)
    {
        switch (change)
        {
            case PlayModeStateChange.EnteredPlayMode:
                DisposeEverthing();
                isInPlayMode = true;
                break;

            case PlayModeStateChange.ExitingPlayMode:
                isInPlayMode = false;
                break;

        }
    }

    static void FindSceneCamera()
    {
        sceneCamera = UnityEditor.SceneView.lastActiveSceneView.camera;

    }


    public static void Initialize()
    {
        if (SceneData == null&&isInitialized) { return; }

      
            planesBuffer = new ComputeBuffer(6, sizeof(float) * 4);
       
        CalculateFrustum();

        if (frustumComputeShader == null)
        {
            string shaderPath = AssetDatabase.GUIDToAssetPath(FRUSTUMCOMPUTESHADERGUID);
            frustumComputeShader = EditorGUIUtility.Load(shaderPath) as ComputeShader;
           

        }

      

        DisposeRenderers(); // ✱
        renderers.Clear();  // ✱
        SetupRenderers();   // ✱

        isInitialized = true;
    }

    static void SetupRenderers()
    {
        for (int i = 0; i < SceneData.propagatedMeshDefinitions.Count; i++)
        {
            if (SceneData.propagatedObjectDatas[i].trsMatrices.Count > 0)
            {
                renderers.Add(CreateEditorRenderer(i));
            }
        }

    }
   static EditorRenderer CreateEditorRenderer(int i)
    {
        ComputeShader shader = Object.Instantiate(frustumComputeShader);
        Mesh mesh = SceneData.propagatedMeshDefinitions[i].mesh;
        Material material = Object.Instantiate(SceneData.propagatedMeshDefinitions[i].material);
        TransformTransferData[] trs = new TransformTransferData[SceneData.propagatedObjectDatas[i].trsMatrices.Count];

        for (int j = 0; j < SceneData.propagatedObjectDatas[i].trsMatrices.Count; j++)
        {
            TransformData data = SceneData.propagatedObjectDatas[i];
            Matrix4x4 transferMatrix = Matrix4x4.TRS(data.trsMatrices[j].position, data.trsMatrices[j].rotation, data.trsMatrices[j].scale);

            trs[j].trsMatrices = transferMatrix;
        }


        EditorRenderer renderer = new EditorRenderer(mesh, material, shader, trs, planesBuffer);

        return renderer;
    }

   public static void CalculateFrustum()
    {
        if (!isInitialized) return;
        if (sceneCamera == null)
        {
            FindSceneCamera();
        }

        frustumPlanes = GeometryUtility.CalculateFrustumPlanes(sceneCamera);
        PropagationSystem.FrustumPlanes[] fp = new PropagationSystem.FrustumPlanes[6];
        for (int i = 0; i < frustumPlanes.Length; i++)
        {
            fp[i].normal = frustumPlanes[i].normal;
            fp[i].distance = frustumPlanes[i].distance + frustumSmoothDistance;
        }

        
            planesBuffer.SetData(fp);
        
        
    }

    static void Update(SceneView sceneView)
    {
        if (!isInitialized) return;

        if (Event.current.type != EventType.Repaint)
            return;

     

        if (Vector3.Magnitude(sceneCamera.transform.position - lastCamPosition) > 5 || Quaternion.Angle(sceneCamera.transform.rotation,lastCamRot) > 15 )
        {
            
            isFrustumCalculationNeeded = true;
            lastCamPosition = sceneCamera.transform.position;
            

        }



        if (isPreviewing)
        {
            if (isFrustumCalculationNeeded)
            {
                CalculateFrustum();
           
                

            }
            if (isFrustumWarm)
            {
                for(int i =0; i < 10; i++)
                {
                    CalculateFrustum();
                }
                isFrustumWarm = false;

            }
         

            foreach (EditorRenderer renderer in renderers)
            {
                if (isFrustumCalculationNeeded)
                {
                    renderer.UpdateFrustum(planesBuffer);

                }

                renderer.Render();
            
               

            }
            isFrustumCalculationNeeded = false;
        }

     

    }

   


}

#endif




