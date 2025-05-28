
#if UNITY_EDITOR
using PropagationSystem.Editor;
using PropagationSystem;
using UnityEditor;
using UnityEngine;

namespace PropagationSystem.Editor
{
    public class CreateNewMeshType : EditorWindow
    {

        public static SceneData sceneData;
        public Mesh mesh;
        public Material material;
        public string meshName;
        public bool useFrustumCulling; // Added to indicate if frustum culling is used for this mesh

        public static void OpenWindow()
        {
            GetWindow<CreateNewMeshType>("Create New Mesh Type");
            SetSceneData();
        }

        public static void SetSceneData()
        {
            sceneData = PropagationBrushWindow.GetCurrentSceneData();

        }
        private void OnGUI()
        {


            mesh = (Mesh)EditorGUILayout.ObjectField("Mesh", mesh, typeof(Mesh), false);
            material = (Material)EditorGUILayout.ObjectField("Material", material, typeof(Material), false);
            meshName = EditorGUILayout.TextField("Mesh Name", meshName);
            useFrustumCulling = EditorGUILayout.Toggle("Use Frustum Culling", useFrustumCulling);

            GUILayout.BeginHorizontal();


            if (GUILayout.Button("Confirm", GUILayout.Height(32), GUILayout.Width(128)))
            {

                if (mesh != null && material != null && !string.IsNullOrEmpty(meshName))
                {

                    MeshData createdMeshData = new MeshData() { mesh = mesh, material = material, name = meshName, useFrustumCulling = useFrustumCulling };
                    sceneData.propagatedMeshDefinitions.Add(createdMeshData);
                    sceneData.OnValidateExternalCall();
                    Close();
                }
                else
                {
                    bool isShown = false;
                    if (mesh == null && !isShown)
                    {
                        EditorUtility.DisplayDialog("Error", "Please Assign A Mesh", "OK");
                        isShown = true;
                    }
                    if (material == null && !isShown)
                    {
                        EditorUtility.DisplayDialog("Error", "Please Assign A Material", "OK");
                        isShown = true;
                    }
                    if (string.IsNullOrEmpty(meshName) && !isShown)
                    {
                        EditorUtility.DisplayDialog("Error", "Please Enter A Mesh Name", "OK");
                        isShown = true;
                    }
                }

            }
            if (GUILayout.Button("Cancel", GUILayout.Height(32), GUILayout.Width(128)))
            {

                Close();
            }

            GUILayout.EndHorizontal();
        }
    }
}
#endif