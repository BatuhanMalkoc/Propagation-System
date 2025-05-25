using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;
public class Propagation : MonoBehaviour
{
    [Tooltip("Assign Propagation Data To Propagate props Into your scene")]
    [SerializeField] private PropagationDataSO propagationData;

    [Header("Frustum Culling Settings")]
    [SerializeField] private bool useFrustumCulling = true;
    [SerializeField] private bool useSpecificCamera = false;
    [SerializeField] private Camera specificCamera;
    [Range(0, 5)][SerializeField] private float smoothThreshold = 2f;
    public enum UpdateMode { Update, LateUpdate, FixedUpdate }
    [SerializeField] private UpdateMode updateMode = UpdateMode.LateUpdate;

    [Header("Compute & Draw Settings")]
    [SerializeField] private ComputeShader shader;

    // Compute buffers
    ComputeBuffer meshTypeCounts;    // instanceCount per mesh
    ComputeBuffer allPositions;      // all transforms' positions
    ComputeBuffer visibleIndices;    // AppendStructuredBuffer<uint2>
    ComputeBuffer frustumPlanes;     // 6 planes
    ComputeBuffer visibleOffsets;    // offset+count per mesh

    // Per‐mesh transform buffers & args buffers
    List<ComputeBuffer> transformBuffers = new List<ComputeBuffer>();
    List<GraphicsBuffer> argsBuffers = new List<GraphicsBuffer>();

    int kernelID;
    uint groupSize;

    struct VisibleOffsetData { public int offset, count; }
    struct TransformData { public float4x4 transformMatrix; }

    private Camera renderCamera;

    void Start()
    {
        Initialize();
    }

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        renderCamera = cam;
    }

    void Initialize()
    {
        // --- find kernel & thread group size
        kernelID = shader.FindKernel("CSFrustumCulling");
        shader.GetKernelThreadGroupSizes(kernelID, out groupSize, out _, out _);

        

        int meshCount = propagationData.propagatedMeshDefinitions.Count;

        // --- instance counts per mesh
        var counts = new int[meshCount];
        for (int i = 0; i < meshCount; i++)
            counts[i] = propagationData.propagatedObjectDatas[i].trsMatrices.Count;

        meshTypeCounts = new ComputeBuffer(meshCount, sizeof(int));
        meshTypeCounts.SetData(counts);
        shader.SetInt("meshCount", meshCount);
        shader.SetBuffer(kernelID, "instanceCount", meshTypeCounts);

        // --- allPositions
        var positions = GetAllPositions();
        allPositions = new ComputeBuffer(positions.Length, sizeof(float) * 3);
        allPositions.SetData(positions);
        shader.SetBuffer(kernelID, "allPositions", allPositions);

        // --- visibleIndices (AppendStructuredBuffer<uint2>)
        visibleIndices = new ComputeBuffer(positions.Length, sizeof(uint) * 2, ComputeBufferType.Append);
        visibleIndices.SetCounterValue(0);
        shader.SetBuffer(kernelID, "visibleIndices", visibleIndices);

        // --- frustumPlanes
        frustumPlanes = new ComputeBuffer(6, sizeof(float) * 4);
        frustumPlanes.SetData(CalculateFrustum());
        shader.SetBuffer(kernelID, "frustumPlanesBuffer", frustumPlanes);

        // --- per‐mesh transform buffers & indirect args buffers
        Bounds worldBounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
        for (int i = 0; i < meshCount; i++)
        {
            // TransformBuffer
            var mats = propagationData.propagatedObjectDatas[i].trsMatrices;
            var td = new TransformData[mats.Count];
            for (int j = 0; j < mats.Count; j++)
                td[j].transformMatrix = mats[j];
            var trsBuf = new ComputeBuffer(mats.Count, sizeof(float) * 16);
            trsBuf.SetData(td);
            transformBuffers.Add(trsBuf);

            // Assign to material
            var def = propagationData.propagatedMeshDefinitions[i];
            def.material.SetBuffer("transformBuffer", trsBuf);
            def.material.SetInt("_MeshIndex", i);

            // Indirect args buffer
            var gb = new GraphicsBuffer(
                GraphicsBuffer.Target.IndirectArguments,
                1,
                GraphicsBuffer.IndirectDrawIndexedArgs.size
            );
            var args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
            args[0].indexCountPerInstance = def.mesh.GetIndexCount(0);
            args[0].instanceCount = 0;     // updated each frame
            args[0].startIndex = 0;     // will use visibleOffsets
            args[0].baseVertexIndex = def.mesh.GetBaseVertex(0);
            args[0].startInstance = 0;
            gb.SetData(args);
            argsBuffers.Add(gb);
        }
    }

    Vector3[] GetAllPositions()
    {
        var list = new List<Vector3>();
        foreach (var obj in propagationData.propagatedObjectDatas)
            foreach (var m in obj.trsMatrices)
                list.Add(m.GetColumn(3));
        return list.ToArray();
    }

    void LateUpdate()
    {
        if (updateMode != UpdateMode.LateUpdate) return;
        if (useFrustumCulling) DoCullingAndDraw();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            // For testing: create random objects
            CreateRandom();
        }

        if (updateMode != UpdateMode.Update) return;
        if (useFrustumCulling) DoCullingAndDraw();

       
    }

    void FixedUpdate()
    {
        if (updateMode != UpdateMode.FixedUpdate) return;
        if (useFrustumCulling) DoCullingAndDraw();
    }
    void CreateRandom()
    {
        for(int i=0;i < 100; i++)
        {
            propagationData.propagatedObjectDatas[0].trsMatrices.Add(
                Matrix4x4.TRS(
                    new Vector3(Random.Range(-50f, 50f), Random.Range(-50f, 50f), Random.Range(-50f, 50f)),
                    Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f)),
                    Vector3.one * Random.Range(0.5f, 2f)
                )
            );


        }
    }
    void DoCullingAndDraw()
    {
        // --- 1) Dispatch frustum culling
        visibleIndices.SetCounterValue(0);
        shader.SetBuffer(kernelID, "visibleIndices", visibleIndices);
        shader.Dispatch(
            kernelID,
            Mathf.CeilToInt(propagationData.propagatedObjectDatas.Count / (float)groupSize),
            1, 1
        );

        // --- 2) Read back visibleIndices count
        uint visibleCount = 0;
        using (var countBuf = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw))
        {
            ComputeBuffer.CopyCount(visibleIndices, countBuf, 0);
            var tmp = new uint[1];
            countBuf.GetData(tmp);
            visibleCount = tmp[0];
        }
        if (visibleCount == 0) return;

        // --- 3) Read back all (meshIndex, localIndex)
        var visibleData = new uint2[visibleCount];
        visibleIndices.GetData(visibleData, 0, 0, (int)visibleCount);

        // --- 4) Sort by meshIndex
        System.Array.Sort(visibleData, (a, b) => a.x.CompareTo(b.x));

        // --- 5) Compute offsets & counts
        int meshCount = propagationData.propagatedMeshDefinitions.Count;
        var offsets = new VisibleOffsetData[meshCount];
        int lastMesh = (int)visibleData[0].x;
        offsets[lastMesh].offset = 0;
        int start = 0;
        for (int i = 1; i < visibleData.Length; i++)
        {
            int m = (int)visibleData[i].x;
            if (m != lastMesh)
            {
                offsets[lastMesh].count = i - start;
                offsets[m].offset = i;
                start = i;
                lastMesh = m;
            }
        }
        offsets[lastMesh].count = visibleData.Length - start;
        // remaining meshes have count=0 by default

        // --- 6) Upload visibleOffsets
        if (visibleOffsets == null || visibleOffsets.count != meshCount)
        {
            if (visibleOffsets != null) visibleOffsets.Release();
            visibleOffsets = new ComputeBuffer(meshCount, sizeof(int) * 2);
        }
        visibleOffsets.SetData(offsets);
        shader.SetBuffer(kernelID, "visibleOffsets", visibleOffsets);
        shader.SetBuffer(kernelID, "visibleIndices", visibleIndices);

        // --- 7) Draw each mesh via Indirect
        for (int i = 0; i < meshCount; i++)
        {
            var def = propagationData.propagatedMeshDefinitions[i];
            var mat = def.material;
            // pass buffers to material
            mat.SetBuffer("visibleIndices", visibleIndices);
            mat.SetBuffer("visibleOffsets", visibleOffsets);

            // update argsBuffer
            var gb = argsBuffers[i];
            var args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
            gb.GetData(args);
            args[0].instanceCount = (uint)offsets[i].count;
            args[0].startIndex = (uint)offsets[i].offset;
            gb.SetData(args);

            // finally draw
            Graphics.DrawMeshInstancedIndirect(
                def.mesh,
                0,
                mat,
                def.mesh.bounds,
                gb
            );
        }
    }

    FrustumPlanes[] CalculateFrustum()
    {
        var unityPlanes = GeometryUtility.CalculateFrustumPlanes(useSpecificCamera ? specificCamera : Camera.main);
        var arr = new FrustumPlanes[6];
        for (int i = 0; i < 6; i++)
            arr[i] = new FrustumPlanes
            {
                normal = unityPlanes[i].normal,
                distance = unityPlanes[i].distance + smoothThreshold
            };
        return arr;
    }

    void OnDestroy()
    {
        meshTypeCounts?.Release();
        allPositions?.Release();
        visibleIndices?.Release();
        frustumPlanes?.Release();
        visibleOffsets?.Release();
        foreach (var b in transformBuffers) b.Release();
        foreach (var b in argsBuffers) b.Release();
    }

    // must match your HLSL struct
    struct FrustumPlanes
    {
        public Vector3 normal;
        public float distance;
    }
}
