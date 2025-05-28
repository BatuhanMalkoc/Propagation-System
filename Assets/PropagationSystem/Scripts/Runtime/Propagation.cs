using UnityEngine;
using PropagationSystem;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;

namespace PropagationSystem
{
    public class Propagation : MonoBehaviour
    {
        [SerializeField] private SceneData SceneData;

        [SerializeField] Camera currentCamera;

        [SerializeField] private OnCameraUpdate onCameraUpdate;

        ComputeBuffer planesBuffer;

        [SerializeField] ComputeShader frustumCullingShader;

        [SerializeField] private float frustumSmoothDistance = 0.1f; // Frustum culling için yumuşatma

        Plane[] frustumPlanes = new Plane[6];

        List<Renderer> renderersList = new List<Renderer>();

        bool isFrustumCalculationNeeded = false;

        private void OnEnable()
        {
            onCameraUpdate.OnCameraUpdated += SetFlagToFrustumCulling;
        }

        private void OnDisable()
        {
            onCameraUpdate.OnCameraUpdated -= SetFlagToFrustumCulling;
        }

        private void OnDestroy()
        {
            planesBuffer.Release();
        }

        void SetFlagToFrustumCulling()
        {
            isFrustumCalculationNeeded = true;

        }
        private void Awake()
        {
            Initialize();
        }

        void Initialize()
        {
            planesBuffer = new ComputeBuffer(6, sizeof(float) * 4);
            CalculateFrustum();

            for (int i = 0; i < SceneData.propagatedMeshDefinitions.Count; i++)
            {

                if (SceneData.propagatedObjectDatas[i].trsMatrices.Count > 0)
                {
                    if (SceneData.propagatedMeshDefinitions[i].useFrustumCulling)
                    {
                        renderersList.Add(CreateFrustumRenderer(i));
                    }
                    else
                    {
                        renderersList.Add(CreateNonFrustumRenderer(i));
                    }
                }
            }

            StartCoroutine(WarmUpFrustums());
        }


        Renderer CreateNonFrustumRenderer(int i)
        {

            ComputeShader shader = Instantiate(frustumCullingShader);
            Mesh mesh = SceneData.propagatedMeshDefinitions[i].mesh;
            Material material = Instantiate(SceneData.propagatedMeshDefinitions[i].material);
            TransformTransferData[] trs = new TransformTransferData[SceneData.propagatedObjectDatas[i].trsMatrices.Count];

            for (int j = 0; j < SceneData.propagatedObjectDatas[i].trsMatrices.Count; j++)
            {
                TransformData data = SceneData.propagatedObjectDatas[i];
                Matrix4x4 transferMatrix = Matrix4x4.TRS(data.trsMatrices[j].position, data.trsMatrices[j].rotation, data.trsMatrices[j].scale);

                trs[j].trsMatrices = transferMatrix;
            }


            Renderer renderer = new Renderer(mesh, material, shader, trs);

            return renderer;
        }
        Renderer CreateFrustumRenderer(int i)
        {

            ComputeShader shader = Instantiate(frustumCullingShader);
            Mesh mesh = SceneData.propagatedMeshDefinitions[i].mesh;
            Material material = Instantiate(SceneData.propagatedMeshDefinitions[i].material);
            TransformTransferData[] trs = new TransformTransferData[SceneData.propagatedObjectDatas[i].trsMatrices.Count];

            for (int j = 0; j < SceneData.propagatedObjectDatas[i].trsMatrices.Count; j++)
            {
                TransformData data = SceneData.propagatedObjectDatas[i];
                Matrix4x4 transferMatrix = Matrix4x4.TRS(data.trsMatrices[j].position, data.trsMatrices[j].rotation, data.trsMatrices[j].scale);

                trs[j].trsMatrices = transferMatrix;
            }


            Renderer renderer = new Renderer(mesh, material, shader, trs, planesBuffer);

            return renderer;
        }



        void CalculateFrustum()
        {
            frustumPlanes = GeometryUtility.CalculateFrustumPlanes(currentCamera);
            FrustumPlanes[] fp = new FrustumPlanes[6];
            for (int i = 0; i < frustumPlanes.Length; i++)
            {
                fp[i].normal = frustumPlanes[i].normal;
                fp[i].distance = frustumPlanes[i].distance + frustumSmoothDistance;
            }

            planesBuffer.SetData(fp);
        }


        private void LateUpdate()
        {
            if (isFrustumCalculationNeeded)
            {
                CalculateFrustum();
            }
            foreach (Renderer renderer in renderersList)
            {
                renderer.Render();
                if (isFrustumCalculationNeeded)
                {
                    renderer.UpdateFrustum(planesBuffer);
                   
                }
            }
           
            isFrustumCalculationNeeded= false;

        }
        private IEnumerator WarmUpFrustums()
        {
            foreach (Renderer renderer in renderersList)
            {
                for (int i = 0; i < 5; i++)
                {

                    CalculateFrustum();
                    renderer.UpdateFrustum(planesBuffer);
                    yield return new WaitForSecondsRealtime(0.05f);
                }
               
            }
        }

        private void Update()
        {
           
        }

      


      

    }
}
