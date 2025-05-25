using UnityEngine;
using PropagationSystem;
using System.Collections.Generic;
using Unity.VisualScripting;

namespace PropagationSystem
{
    public class Propagation : MonoBehaviour
    {
        [SerializeField] private SceneData SceneData;

        [SerializeField] Camera currentCamera;

        ComputeBuffer planesBuffer;

        [SerializeField] ComputeShader frustumCullingShader;

        [SerializeField] private float frustumSmoothDistance = 0.1f; // Frustum culling için yumuşatma

        Plane[] frustumPlanes = new Plane[6];

        List<Renderer> renderersList = new List<Renderer>();


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


        Renderer CreateNonFrustumRenderer(int i)
        {

            ComputeShader shader = Instantiate(frustumCullingShader);
            Mesh mesh = SceneData.propagatedMeshDefinitions[i].mesh;
            Material material = Instantiate(SceneData.propagatedMeshDefinitions[i].material);
            TransformTransferData[] trs = new TransformTransferData[SceneData.propagatedObjectDatas[i].trsMatrices.Count];

            for (int j = 0; j < SceneData.propagatedObjectDatas[i].trsMatrices.Count; j++)
            {
                trs[j].trsMatrices = SceneData.propagatedObjectDatas[i].trsMatrices[j];
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
                trs[j].trsMatrices = SceneData.propagatedObjectDatas[i].trsMatrices[j];
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
            CalculateFrustum();
            foreach (Renderer renderer in renderersList)
            {
                renderer.Render();
                renderer.UpdateFrustum(planesBuffer);
            }
           

        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                CreateRandomMatrices();
            }
        }

        void CreateRandomMatrices()
        {
            for (int i = 0; i < SceneData.propagatedMeshDefinitions.Count; i++)
            {
                // 100 adet rastgele matris oluştur
                for (int j = 0; j < 100; j++)
                {
                    // Rastgele pozisyon, rotasyon ve ölçek oluştur
                    Vector3 position = new Vector3(
                        Random.Range(-100f, 100f),
                        Random.Range(-100f, 100f),
                        Random.Range(-100f, 100f)
                    );
                    Quaternion rotation = Random.rotation;
                    Vector3 scale = Vector3.one;
                       
               
                    // TRS matrisi oluştur
                    Matrix4x4 trs = Matrix4x4.TRS(position, rotation, scale);

                    // Listeye ekle
                    SceneData.propagatedObjectDatas[i].trsMatrices.Add(trs);
                }
            }
        }

    }
}
