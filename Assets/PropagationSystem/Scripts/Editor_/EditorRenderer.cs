using PropagationSystem;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Analytics;
using UnityEditor;
using UnityEngine.Rendering;
using System.Collections;
using Unity.VisualScripting;
using PropagationSystem.Editor;

[ExecuteInEditMode]
public class EditorRenderer
{
    TransformTransferData[] trs;

    Material material;
    ComputeShader shader;
    Mesh mesh;


    ComputeBuffer transformBuffer;
    ComputeBuffer positionBuffer;
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
        DisposeAll();

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

        positionBuffer = new ComputeBuffer(positions.Length, sizeof(float) * 3);
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
        material.SetBuffer("visibleIndices", visibleIndicesBuffer);


        argsBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.IndirectArguments,
                1,
                GraphicsBuffer.IndirectDrawIndexedArgs.size);
        var args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        args[0].indexCountPerInstance = mesh.GetIndexCount(0);
        args[0].instanceCount = 0;   // ★ reset before dispatch
        args[0].startIndex = 0;
        args[0].baseVertexIndex = 0;
        args[0].startInstance= 0;
        argsBuffer.SetData(args);

        shader.SetBuffer(kernelID, "_argsBuffer", argsBuffer); // ★ bind argsBuffer


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
        var resetArgs = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        resetArgs[0].indexCountPerInstance = mesh.GetIndexCount(0);
        resetArgs[0].instanceCount = 0;
        resetArgs[0].startIndex = 0;
        resetArgs[0].baseVertexIndex = 0;
        resetArgs[0].startInstance = 0;
        argsBuffer.SetData(resetArgs);

        visibleIndicesBuffer.SetCounterValue(0); // Her karede sıfırla

        shader.Dispatch(kernelID, groupSizeX, 1, 1);

        // ★ Debug: argsBuffer’dan CPU’ya geri oku ve instanceCount’ı logla
   
     

        Graphics.RenderMeshIndirect(rp, mesh, argsBuffer);
    }

    public void UpdateFrustum(ComputeBuffer newFrustumPlanesBuffer)
    {
        planesBuffer = newFrustumPlanesBuffer;

        shader.SetBuffer(kernelID, "frustrumPlanesBuffer", planesBuffer);

     
   
    }

    public void DisposeAll()
    {
        transformBuffer?.Dispose(); // ✱
        transformBuffer = null;     // ✱

        argsBuffer?.Dispose();      // ✱
        argsBuffer = null;          // ✱

        visibleCountBuffer?.Dispose(); // ✱
        visibleCountBuffer = null;     // ✱

        visibleIndicesBuffer?.Dispose(); // ✱
        visibleIndicesBuffer = null;      // ✱

        positionBuffer?.Dispose(); // ✱
        positionBuffer = null;     // ✱
    }

    void OnDestroy()
    {
        DisposeAll(); // ✱
    }
}