#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace PropagationSystem.Editor
{
    public class CreateNewMeshType : EditorWindow
    {
        public static SceneData sceneData;

        public static CreateNewMeshType Instance { get; private set; }

        private Mesh mesh;
        private Material material;
        private string meshName;
        private bool useFrustumCulling = true; // Obsolete

        private GUIStyle headerStyle;
        private GUIStyle errorStyle;

        public Action<MeshData> OnMeshCreated;
        public Action OnWindowClosed;

        public static void OpenWindow()
        {
            var window = GetWindow<CreateNewMeshType>("Add Mesh Type", true);
            window.minSize = new Vector2(320, 400);

            if (Instance == null)
            {
                Instance = window;
            }
            else
            {
                Instance.Focus(); // Eğer zaten açıksa öne getir
            }
                
            window.SetSceneData();
            
        }

        public  void SetSceneData()
        {
            sceneData = PropagationBrushWindow.GetCurrentSceneData();
        }

        private void OnEnable()
        {
            InitStyles();
            Instance = this;
        }
        private void OnDisable()
        {
            OnWindowClosed?.Invoke();
            if (Instance  == this)
            {
                Instance = null;
            }
        }
        private void InitStyles()
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16
            };

            errorStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.red },
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("📦 Add New Mesh Type", headerStyle);
            DrawSeparator();

            GUILayout.Space(8);
            DrawMeshField();
            DrawMeshPreview();

            DrawMaterialField();
            DrawNameField();

            GUILayout.Space(12);
            DrawSeparator();
            GUILayout.Space(8);

            DrawActionButtons();
        }

        private void DrawSeparator()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 1f));
        }
        private void DrawMeshPreview()
        {
            if (mesh == null) return;

            GUILayout.Space(4);
           
           
            Texture2D previewTexture = AssetPreview.GetAssetPreview(mesh);

            if (previewTexture == null)
            {
                GUILayout.Label("Loading preview...");
                // AssetPreview henüz hazır değil, tekrar repainte zorla
                if (AssetPreview.IsLoadingAssetPreview(mesh.GetInstanceID()))
                    Repaint();
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.Label(previewTexture, GUILayout.Width(150), GUILayout.Height(150));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        private void DrawMeshField()
        {
            GUILayout.Label("Mesh", EditorStyles.boldLabel);
            mesh = (Mesh)EditorGUILayout.ObjectField(mesh, typeof(Mesh), false);

            if (mesh == null)
                GUILayout.Label("Please assign a mesh.", errorStyle);

            GUILayout.Space(6);
        }

        private void DrawMaterialField()
        {
            GUILayout.Label("Material", EditorStyles.boldLabel);
            material = (Material)EditorGUILayout.ObjectField(material, typeof(Material), false);

            if (material == null)
                GUILayout.Label("Please assign a material.", errorStyle);

            GUILayout.Space(6);
        }

        private void DrawNameField()
        {
            GUILayout.Label("Mesh Name", EditorStyles.boldLabel);
            meshName = EditorGUILayout.TextField(meshName);

            if (string.IsNullOrWhiteSpace(meshName))
                GUILayout.Label("Please enter a name.", errorStyle);

            GUILayout.Space(6);
        }

        private void DrawActionButtons()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.Height(32), GUILayout.Width(120)))
            {
                OnWindowClosed?.Invoke();
                Close();
            }

            GUILayout.Space(16);

            if (GUILayout.Button("Add Mesh Type", GUILayout.Height(32), GUILayout.Width(160)))
            {
                TryCreateMesh();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }



        private void TryCreateMesh()
        {
            if (mesh == null || material == null || string.IsNullOrWhiteSpace(meshName))
            {
                EditorUtility.DisplayDialog("Error", "All fields must be filled.", "OK");
                return;
            }

            var createdMeshData = new MeshData()
            {
                mesh = mesh,
                material = material,
                name = meshName,
                useFrustumCulling = useFrustumCulling
            };

            OnMeshCreated?.Invoke(createdMeshData);

           
            

            OnWindowClosed?.Invoke();

            Close();
        }

    }
}
#endif
