using System;
using Unity.VisualScripting;
using UnityEditor;

using UnityEngine;

using Random = UnityEngine.Random;
namespace PropagationSystem.Editor
{
#if UNITY_EDITOR
    [InitializeOnLoad]
    public static class PropagationBrush
    {
        #region Brush
        private static BrushDataSO activeBrush;
        private static float brushSize;
        private static float density;
        private static int count;
        private static Mesh brushMesh;
        private static Material brushMaterial;
        public static Action<BrushPaintData[]> OnBrushApplied;

        public static PropagationBrushWindow.BrushMode brushMode;
        #endregion

        #region Scene Variables

        private static Vector3 lastHitPoint;
        private static Vector3 lastHitNormal;

        public static bool isActive = false;
        static bool isPressing;
        #endregion

        #region Constructor

        static PropagationBrush()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (isActive)
            {
              
            }
        }

        #endregion

        #region Public Setters
        public static void OnOff(bool status)
        {
            isActive = status;
        }
        public static void SetBrushSize(float BrushSize)
        {
          brushSize = BrushSize;
        }

        public static void SetMesh(Mesh mesh)
        {
            brushMesh = mesh;
        }
        public static void SetMaterial(Material material)
        {
            brushMaterial = material;
        }
        public static void SetCurrentBrush(BrushDataSO brush)
        {
            activeBrush = brush;
        }
        public static void SetCurrentBrushMode(PropagationBrushWindow.BrushMode mode)
        {
            brushMode = mode;
        }
        #endregion
        private static void OnSceneGUI(SceneView sceneView)
        {
            if (activeBrush == null || activeBrush.maskTexture == null)
                return;

            if (isActive)
            {
                UpdateHitPoint();
                DrawBrushPlane();
                sceneView.Repaint();
                // Sürekli raycast at ve çemberi çiz


                // Tıklama ile uygulama
                var e = Event.current;
                if (e.type == EventType.MouseDown && e.button == 0 && e.control)
                {
                    isPressing = true;
                   
                    e.Use();
                }
                if (e.type == EventType.MouseUp && e.button == 0 && e.control)
                {
                    isPressing = false;
                    
                    e.Use();
                }

                if (isPressing)
                {
                    ApplyBrushAtPoint(lastHitPoint, lastHitNormal);
                }
            }
        }

        /// <summary>
        /// Maus pozisyonundan sahnedeki mesh'e raycast atar ve sonucu günceller.
        /// </summary>
        private static void UpdateHitPoint()
        {
             Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
             Transform spawnPosition = Physics.Raycast(ray, out RaycastHit hit) ? hit.transform : null;
          
              if (spawnPosition != null)
             {
                DrawBrushPlane();
                lastHitNormal = hit.normal;
                lastHitPoint = hit.point;
             }
        }

        /// <summary>
        /// Son hizalanan noktada Handles ile çember çizer.
        /// </summary>
        private static void DrawBrushPlane()
        {
           
            brushMaterial.SetPass(0);
            brushMaterial.SetTexture("_MaskTexture", activeBrush.maskTexture);
            brushMaterial.SetFloat("_BrushSize", brushSize);

            if (brushMode == PropagationBrushWindow.BrushMode.Erase)
            {
                brushMaterial.SetColor("_Tint", Color.red);
            }
            else
            {
                brushMaterial.SetColor("_Tint", Color.white);

            }

                Graphics.DrawMeshNow(brushMesh, lastHitPoint + lastHitNormal * 0.2f, Quaternion.identity);
            
           
        }

        /// <summary>
        /// Samplelanan değerler doğrultusunda sahne üzerinde efekt uygular.
        /// </summary>
        public static void ApplyBrushAtPoint(Vector3 point, Vector3 normal)
        {
            // TODO: Örn. gizmo çizimi, vertex color boyama, prefab spawn
             Ray[] ray;
             CreateRandomPoints(10,point,normal,out ray);

            BrushPaintData[] brushPaintData = new BrushPaintData[10];

            for (int i = 0; i < ray.Length; i++) {
                Physics.Raycast(ray[i], out RaycastHit hit);
                brushPaintData[i] = new BrushPaintData
                {
                    position = hit.point,
                    rotation = IsValidQuaternion(Quaternion.LookRotation(hit.normal)) ? Quaternion.LookRotation(hit.normal) : Quaternion.identity,
                    scale = Vector3.one

                };
            }

            Debug.Log("Brush Applyied" + brushSize);


            





            OnBrushApplied?.Invoke(brushPaintData);

        }
       static bool IsValidQuaternion(Quaternion q)
        {
            float magnitude = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            return magnitude > 0.0001f && !float.IsNaN(magnitude) && !float.IsInfinity(magnitude);
        }

        public static void CreateRandomPoints(int rayCount,Vector3 point , Vector3 normal, out Ray[] ray)
        {
           ray = new Ray[rayCount];

            for (int i = 0; i < 10; i++)
            {
                ray[i] = new Ray(point + (normal*0.3f) + new Vector3(Random.Range(-brushSize,brushSize),0,Random.Range(-brushSize,brushSize)), -normal);
            }

        }


        /// <summary>
        /// Aktif brushData'yı ayarlar.
        /// </summary>
        

        /// <summary>
        /// Şu anki brushData'yı döner.
        /// </summary>
        public static BrushDataSO GetCurrentBrush()
        {
            return activeBrush;
        }
    }
#endif
}