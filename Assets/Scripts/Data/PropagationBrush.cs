using System;
using UnityEditor;

using UnityEngine;

using Random = UnityEngine.Random;
namespace PropagationSystem.Editor
{
#if UNITY_EDITOR
    [InitializeOnLoad]
    public static class PropagationBrush
    {
        private static BrushDataSO currentBrush;
        private static Vector3 lastHitPoint;
        private static Vector3 lastHitNormal;
        private const float circleRadius = 2f; // İleride BrushDataSO'dan alınabilir
        public static Action<BrushPaintData[]> OnBrushApplied;
        static PropagationBrush()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (currentBrush == null || currentBrush.maskTexture == null)
                return;

            // Sürekli raycast at ve çemberi çiz
            UpdateHitPoint();
            DrawBrushHandle();

            // Tıklama ile uygulama
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && e.control)
            {
                ApplyBrushAtPoint(lastHitPoint, lastHitNormal);
                e.Use();
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
               DrawBrushHandle();
                lastHitNormal = hit.normal;
                lastHitPoint = hit.point;
            }
        }

        /// <summary>
        /// Son hizalanan noktada Handles ile çember çizer.
        /// </summary>
        private static void DrawBrushHandle()
        {
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(lastHitPoint, lastHitNormal, circleRadius);
            SceneView.RepaintAll();
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
                    normal =  hit.normal

                };
            }

            OnBrushApplied?.Invoke(brushPaintData);

        }

        public static void CreateRandomPoints(int rayCount,Vector3 point , Vector3 normal, out Ray[] ray)
        {
           ray = new Ray[rayCount];

            for (int i = 0; i < 10; i++)
            {
                ray[i] = new Ray(point + (normal*0.3f) + new Vector3(Random.Range(-circleRadius,circleRadius),0,Random.Range(-circleRadius,circleRadius)), -normal);
            }

        }


        /// <summary>
        /// Aktif brushData'yı ayarlar.
        /// </summary>
        public static void SetCurrentBrush(BrushDataSO brush)
        {
            currentBrush = brush;
        }

        /// <summary>
        /// Şu anki brushData'yı döner.
        /// </summary>
        public static BrushDataSO GetCurrentBrush()
        {
            return currentBrush;
        }
    }
#endif
}