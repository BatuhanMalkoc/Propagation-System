﻿using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

namespace PropagationSystem
{ 
public class Renderer : IDisposable
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

        private bool isDisposed = false;

        bool buffersInitialized;

        #region Constructor Without Frustum Culling

        public Renderer(Mesh mesh,Material material,ComputeShader shader, TransformTransferData[] trs)
        {
            this.mesh = mesh;
            this.material = material;
            this.shader = shader;
            this.trs = trs;
            useFrustumCulling = false;
            Initialize();
        }
        #endregion
        #region Constructor With Frustum Culling
        public Renderer(Mesh mesh, Material material, ComputeShader shader, TransformTransferData[] trs,ComputeBuffer planesBuffer)
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
            //shader = Instantiate(shader);
            //material = Instantiate(material);
            if (buffersInitialized)
                Dispose();

            kernelID = shader.FindKernel(kernelName);
            instanceCount = trs.Length;

            shader.GetKernelThreadGroupSizes(kernelID, out uint tx, out _, out _);
            groupSizeX = Mathf.CeilToInt(instanceCount / (float)tx);

            transformBuffer = new ComputeBuffer(trs.Length,sizeof(float)*16);
            transformBuffer.SetData(trs);

            Vector3[] positions = new Vector3[trs.Length];

            for(int i = 0; i < trs.Length; i++)
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


            argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments,1,GraphicsBuffer.IndirectDrawIndexedArgs.size);
            GraphicsBuffer.IndirectDrawIndexedArgs[] args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
            args[0].indexCountPerInstance = mesh.GetIndexCount(0);
            args[0].instanceCount = (uint)instanceCount;
            argsBuffer.SetData(args);

            rp = new RenderParams(material)
            {
                worldBounds = new Bounds(Vector3.zero, Vector3.one * 10000f)
            };


            
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

       void OnDestroy()
        {
            positionBuffer?.Dispose();
            transformBuffer?.Dispose();
            planesBuffer?.Dispose();
            argsBuffer?.Dispose();
            visibleCountBuffer?.Dispose();
            visibleIndicesBuffer?.Dispose();
            
        }

        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;

            // Tüm ComputeBuffer ve GraphicsBuffer’ları kapat
            transformBuffer?.Dispose();
            transformBuffer = null;

            positionBuffer?.Dispose();
            positionBuffer = null;

            visibleIndicesBuffer?.Dispose();
            visibleIndicesBuffer = null;

            visibleCountBuffer?.Dispose();
            visibleCountBuffer = null;

            // planesBuffer’ı burada dispose etmek, eğer başka yerde paylaşıyorsan sorun olabilir.
            // Ancak eğer her Renderer kendi planesBuffer’ını ayırıyorsa dispose etmelisin:
            planesBuffer?.Dispose();
            planesBuffer = null;

            argsBuffer?.Dispose();
            argsBuffer = null;

            buffersInitialized = false;
        }
    }
    }
