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
            DisposeEverthing();
        }

    
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
            Initialize();
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

        SetupRenderers();
       

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

       

        if (Vector3.Magnitude(sceneCamera.transform.position - lastCamPosition) > 5)
        {
            
            isFrustumCalculationNeeded = true;
            lastCamPosition = sceneCamera.transform.position;
            

        }



        if (isPreviewing)
        {
            if (isFrustumCalculationNeeded)
            {
                CalculateFrustum();
                Debug.Log("Tekrar Hesaplandı");
                

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

[ExecuteInEditMode]
public class EditorRenderer
{
    TransformTransferData[] trs;

    Material material;
    ComputeShader shader;
    Mesh mesh;


    ComputeBuffer transformBuffer;
    GraphicsBuffer argsBuffer;

    ComputeBuffer visibleIndicesBuffer;
    ComputeBuffer visibleCountBuffer;

    ComputeBuffer planesBuffer;

    RenderParams rp;

    bool useFrustumCulling = true;
    float frustumSmoothDistance;

    int kernelID;
    int groupSizeX;
    int visibleCount;
    int instanceCount;

    const string kernelName = "CSFrustumCulling";


   
    #region Constructor With Frustum Culling
    public EditorRenderer(Mesh mesh, Material material, ComputeShader shader, TransformTransferData[] trs, ComputeBuffer planesBuffer)
    {
        this.mesh = mesh;
        this.material = material;
        this.shader = shader;
        this.trs = trs;
        this.planesBuffer = planesBuffer;

        useFrustumCulling = true;
        Initialize();
    }
    #endregion

    void Initialize()
    {
      
        kernelID = shader.FindKernel(kernelName);
        instanceCount = trs.Length;

        shader.GetKernelThreadGroupSizes(kernelID, out uint tx, out _, out _);
        groupSizeX = Mathf.CeilToInt(instanceCount / (float)tx);

        transformBuffer = new ComputeBuffer(trs.Length, sizeof(float) * 16);
        transformBuffer.SetData(trs);

        Vector3[] positions = new Vector3[trs.Length];

        for (int i = 0; i < trs.Length; i++)
        {
            positions[i] = trs[i].trsMatrices.GetColumn(3);
        }

        ComputeBuffer positionBuffer = new ComputeBuffer(positions.Length, sizeof(float) * 3);
        positionBuffer.SetData(positions);
        //Planes Buffer Zaten Var

        visibleIndicesBuffer = new ComputeBuffer(instanceCount, sizeof(uint), ComputeBufferType.Append);
        visibleIndicesBuffer.SetCounterValue(0); // Temizle

        visibleCountBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw);

        shader.SetBuffer(kernelID, "visibleIndices", visibleIndicesBuffer);
        shader.SetBuffer(kernelID, "positionBuffer", positionBuffer);
        shader.SetBuffer(kernelID, "frustumPlanesBuffer", planesBuffer);

        shader.SetInt("_InstanceCount", instanceCount);

        material.SetBuffer("transformBuffer", transformBuffer);


        argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        GraphicsBuffer.IndirectDrawIndexedArgs[] args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        args[0].indexCountPerInstance = mesh.GetIndexCount(0);
        args[0].instanceCount = (uint)instanceCount;
        argsBuffer.SetData(args);

        rp = new RenderParams(material)
        {
            worldBounds = new Bounds(Vector3.zero, Vector3.one * 10000f)
        };



    }

    public void Update(TransformTransferData[] updatedTRS)
    {
        trs = updatedTRS;
        Initialize();
    }





    public void Render()
    {
        GraphicsBuffer.IndirectDrawIndexedArgs[] args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        args[0].indexCountPerInstance = mesh.GetIndexCount(0);
        args[0].instanceCount = (uint)visibleCount;
        argsBuffer.SetData(args);
        material.SetBuffer("visibleIndices", visibleIndicesBuffer);

        visibleIndicesBuffer.SetCounterValue(0); // Her karede sıfırla

        shader.Dispatch(kernelID, groupSizeX, 1, 1);

        if (visibleCount == 0) return;
       
        Graphics.RenderMeshIndirect(rp, mesh, argsBuffer);

    }

    public void UpdateFrustum(ComputeBuffer newFrustumPlanesBuffer)
    {
        planesBuffer = newFrustumPlanesBuffer;

        shader.SetBuffer(kernelID, "frustrumPlanesBuffer", planesBuffer);

        ComputeBuffer.CopyCount(visibleIndicesBuffer, visibleCountBuffer, 0);

        uint[] countArray = { 0 };

        visibleCountBuffer.GetData(countArray);

        uint visibleCount = countArray[0];

        this.visibleCount = (int)visibleCount;
    }

   public void DisposeAll()
    {
        transformBuffer?.Dispose();
        planesBuffer?.Dispose();
        argsBuffer?.Dispose();
        visibleCountBuffer?.Dispose();
        visibleIndicesBuffer?.Dispose();
    }
    void OnDestroy()
    {
        transformBuffer?.Dispose();
        planesBuffer?.Dispose();
        argsBuffer?.Dispose();
        visibleCountBuffer?.Dispose();
        visibleIndicesBuffer?.Dispose();

    }
}


