// Assets/PropagationSystem/Editor/PropagationBrushWindow.cs
using UnityEditor;
using UnityEngine;
using PropagationSystem;
using PropagationSystem.Editor;
using Unity.VisualScripting;

namespace PropagationSystem.Editor
{
#if UNITY_EDITOR
    public class PropagationBrushWindow : EditorWindow
    {
        #region Variables

        #region Static Variables 

        private static SceneData sceneData;
        private static int selectedMeshIndex_Static;

        #endregion

        #region Brush Variables

        private Texture2D selectedBrushTexture;
        private BrushDataSO selectedBrushSO;
        private BrushSetSO brushSet;
        private bool brushEnabled;
        private float brushSize = 1f; // Ýleride BrushDataSO'dan alýnabilir
        private int density = 50;
        private bool isBrushEnabled = false; // Brush'un aktif olup olmadýðýný tutar
        private int selectedBrushIndex;
        private Mesh brushMesh;
        private Material brushMaterial;
        public enum BrushMode
        {
            Paint,
            Erase
        }

        public enum PropagationMode
        {
            Random,
            RandomWeighted,
            Density
        }

        private PropagationMode SampleMode = PropagationMode.Random;
        private BrushMode brushMode = BrushMode.Paint;
        #endregion

        #region Mesh Variables

        private int selectedMeshIndex = 0;

        #endregion

        #region EditorPrefs

        const string SceneData_PrefKey = "EditorPref_SceneDataKey";
        const string BrushSet_PrefKey = "EditorPref_BrushSetKey";
        const string BrushSize_PrefKey = "EditorPref_BrushSizeKey";
        const string Density_PrefKey = "EditorPref_DensityKey";
        const string SelectedMeshIndex_PrefKey = "EditorPref_SelectedMeshIndexKey";
        const string SelectedBrushIndex_PrefKey = "EditorPref_SelectedBrushIndexKey";

        #endregion

        #region GUID Keys

        private const string BRUSHICONGUID = "b50705d6170a42d4a9480d8e579bca3b";
        private const string ERASEICONGUID = "b1c9c7ca80357484b993b162ad1bf81b";
        private const string PROPAGATIONICONGUID = "53b798f87c3af534893455e7f6514777";

        #endregion

        #endregion

        #region Window Utilities

        [MenuItem("Tools/Propagation Brush")]
        public static void OpenWindow()
        {
            GetWindow<PropagationBrushWindow>("Propagation Brush");
           
        }
        public static SceneData GetCurrentSceneData()
        {
            return sceneData;
        }
        public static void CloseWindow()
        {
            PropagationBrush.OnBrushApplied -= OnBrushApplied;
            GetWindow<PropagationBrushWindow>().Close();
        }
        void UpdatePropagationBrush()
        {
            PropagationBrush.SetCurrentBrush(selectedBrushSO);
            PropagationBrush.SetCurrentBrushMode(brushMode);
        }
        #endregion

        private static void OnBrushApplied(BrushPaintData[] datas)
        {
            Debug.Log("Event çaðýrýldý data countýda þu :" + datas.Length);

          for(int i = 0; i < datas.Length; i++)
            {
               


                SavedPositions savedPositions = new SavedPositions();
                savedPositions.position = datas[i].position;
                savedPositions.rotation = datas[i].rotation;
                savedPositions.scale = datas[i].scale;

               
                sceneData.propagatedObjectDatas[selectedMeshIndex_Static].trsMatrices.Add(savedPositions);

                Debug.Log("Eklendi" + selectedMeshIndex_Static);
            }

          EditorUtility.SetDirty(sceneData);


        }


        

        #region GUI Functions
        void GUI_Label()
        {
            GUILayout.Label("Propagation", EditorStyles.boldLabel); //Header
        }

        void GUI_UpdateIcon()
        {
            string windowIconPath = AssetDatabase.GUIDToAssetPath(PROPAGATIONICONGUID);
            Texture2D windowIcon = EditorGUIUtility.Load(windowIconPath) as Texture2D;
            titleContent = new GUIContent(" Propagation", windowIcon);
        }

        void GUI_SceneData()
        {
            sceneData = (SceneData)EditorGUILayout.ObjectField("Scene Data", sceneData, typeof(SceneData), false);
        }

        void GUI_MeshList()
        {
            GUILayout.Label("Mesh Picker", EditorStyles.boldLabel);

            SerializedObject so = new SerializedObject(sceneData);
            SerializedProperty meshList = so.FindProperty("propagatedMeshDefinitions");
            so.ApplyModifiedProperties();

            if (sceneData.propagatedMeshDefinitions.Count == 0)
            {
                EditorGUILayout.HelpBox("Please Add Mesh To Propagate.", MessageType.Warning);
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();

            Texture2D selectedPreview = AssetPreview.GetAssetPreview(sceneData.propagatedMeshDefinitions[selectedMeshIndex].mesh);

            GUILayout.Label(selectedPreview, GUILayout.Width(150), GUILayout.Height(150));

            GUILayout.Label("Selected Mesh: " + sceneData.propagatedMeshDefinitions[selectedMeshIndex].name, EditorStyles.centeredGreyMiniLabel);


            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

      
            int buttonSize = 80;
            int padding = 10;
            int viewWidth = (int)EditorGUIUtility.currentViewWidth;
            int buttonsPerRow = Mathf.Max(1, (viewWidth - padding) / (buttonSize + padding));

            GUILayout.BeginVertical();

            for (int i = 0; i < sceneData.propagatedMeshDefinitions.Count; i++)
            {
                if (i % buttonsPerRow == 0)
                {
                    if (i != 0) GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }

                Texture2D preview = AssetPreview.GetAssetPreview(sceneData.propagatedMeshDefinitions[i].mesh);
                GUIContent buttonContent = new GUIContent(preview, sceneData.propagatedMeshDefinitions[i].name);

                if (GUILayout.Button(buttonContent, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                {
                    selectedMeshIndex = i;
                    selectedMeshIndex_Static = i; // Update the selected mesh index
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            EditorGUILayout.Space(10);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Delete Mesh Type"))
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Are you sure?",
                    "This will delete selected mesh type and all data belongs to it?",
                    "Yes",    // Accept button
                    "No"      // Cancel button
                );

                if (confirmed)
                {
                    // Silme iþlemi
                  sceneData.propagatedMeshDefinitions.RemoveAt(selectedMeshIndex);

                    selectedMeshIndex = 0;

                    sceneData.OnValidateExternalCall();

                    EditorUtility.SetDirty(sceneData);

                }
            }

            GUI_CreateNewMesh();

            GUILayout.EndHorizontal();
        }

        void GUI_BrushSet()
        {    
            brushSet = (BrushSetSO)EditorGUILayout.ObjectField("Brush Set", brushSet, typeof(BrushSetSO), false);
        }

        void GUI_BrushSettings()
        {

            Color oldColor = GUI.backgroundColor;

            GUILayout.Label("Brush Settings", EditorStyles.boldLabel);
        
            if (selectedBrushTexture != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.BeginVertical();

                GUILayout.Label(selectedBrushTexture, GUILayout.Height(128), GUILayout.Width(128));

                if (selectedBrushSO != null)
                {
                    GUILayout.Label("Selected Brush: " + selectedBrushSO.brushName, EditorStyles.centeredGreyMiniLabel);

                }

                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            int selectedBrushIndex = -1;
            int buttonSize = 64;
            int padding = 5;

            int viewWidth = (int)EditorGUIUtility.currentViewWidth;
            int buttonsPerRow = Mathf.Max(1, (viewWidth - padding) / (buttonSize + padding));

            GUILayout.BeginVertical();

            for (int i = 0; i < brushSet.brushes.Count; i++)
            {
                if (i % buttonsPerRow == 0)
                {
                    if (i != 0) GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }

                GUIContent buttonContent = new GUIContent("", brushSet.brushes[i].maskTexture);

                if (GUILayout.Button(buttonContent, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                {
                    selectedBrushIndex = i;

                    this.selectedBrushIndex = i; // Update the selected brush index

                    selectedBrushSO = brushSet.brushes[i];

                    selectedBrushTexture = brushSet.brushes[i].maskTexture;

                    PropagationBrush.SetCurrentBrush(brushSet.brushes[i]);
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            brushSize = EditorGUILayout.Slider("Brush Size", brushSize, 0.1f, 100f);
            density = EditorGUILayout.IntSlider("Density", density, 1, 100);

            PropagationBrush.SetBrushSize(brushSize);

            EditorGUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            string brushIconPath = AssetDatabase.GUIDToAssetPath(BRUSHICONGUID);
            string eraserIconPath = AssetDatabase.GUIDToAssetPath(ERASEICONGUID);
            Texture2D brushIcon = EditorGUIUtility.Load(brushIconPath) as Texture2D;
            Texture2D eraserIcon = EditorGUIUtility.Load(eraserIconPath) as Texture2D;

            


            if(brushMode == BrushMode.Paint)
            {
                GUI.backgroundColor = Color.cyan;
            }
        
            if (GUILayout.Button(brushIcon,GUILayout.Width(64),GUILayout.Height(64)))
            {
                if(!isBrushEnabled)
                {
                    isBrushEnabled = true;
                    PropagationBrush.OnOff(isBrushEnabled);
                    PropagationBrush.SetMesh(brushMesh);
                    PropagationBrush.SetMaterial(brushMaterial);
                }
                brushMode = BrushMode.Paint;
            }
            GUI.backgroundColor = oldColor;
            string brushButtonText = isBrushEnabled ? "Disable Brush" : "Enable Brush";
            if (GUILayout.Button(brushButtonText,GUILayout.Width(128),GUILayout.Height(64)))
            {
                isBrushEnabled = !isBrushEnabled;
                brushMode = BrushMode.Paint; // Reset brush mode to Paint when turning off
                PropagationBrush.OnOff(isBrushEnabled);
                PropagationBrush.SetMesh(brushMesh);
                PropagationBrush.SetMaterial(brushMaterial);
            }
            if (brushMode == BrushMode.Erase)
            {
                GUI.backgroundColor = Color.red;
            }
           
            if (GUILayout.Button(eraserIcon,GUILayout.Width(64),GUILayout.Height(64)))
            {
                if (!isBrushEnabled)
                {
                    isBrushEnabled = true;
                    PropagationBrush.OnOff(isBrushEnabled);
                    PropagationBrush.SetMesh(brushMesh);
                    PropagationBrush.SetMaterial(brushMaterial);
                }
                brushMode = BrushMode.Erase;
            }
            GUI.backgroundColor = oldColor;


            GUI.backgroundColor = oldColor;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

           

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

           



            if (SampleMode == PropagationMode.Random)
            {
                GUI.backgroundColor = Color.green;
            }
            if (GUILayout.Button("Random", GUILayout.Width(128), GUILayout.Height(64)))
            {
                SampleMode = PropagationMode.Random;
            }
            GUI.backgroundColor = oldColor;
            if (SampleMode == PropagationMode.RandomWeighted)
            {
                GUI.backgroundColor = Color.green;
            }
            if (GUILayout.Button("Random Weighted", GUILayout.Width(128), GUILayout.Height(64)))
            {
                SampleMode = PropagationMode.RandomWeighted;
            }
            GUI.backgroundColor = oldColor;
            if (SampleMode == PropagationMode.Density)
            {
                GUI.backgroundColor = Color.green;
            }
            if (GUILayout.Button("By Density", GUILayout.Width(128), GUILayout.Height(64)))
            {
               SampleMode = PropagationMode.Density;
            }

            GUI.backgroundColor = oldColor;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        void GUI_CreateNewMesh()
        {
            if (GUILayout.Button("Add New Mesh Type"))
            {
                CreateNewMeshDefinitionWindow.OpenWindow();
            }
        }
      

        private void OnGUI()
        {
            GUI_Label();

            GUI_UpdateIcon();

            GUI_SceneData();

            EditorGUILayout.Space(2);


            if (sceneData == null)
            {
                EditorGUILayout.HelpBox("Assign A Scene Data First", MessageType.Warning);
                return;
            }
            else if (sceneData.propagatedMeshDefinitions.Count == 0)
            {
                EditorGUILayout.HelpBox("Add Atleast One Mesh To Propagate", MessageType.Warning);
                GUI_CreateNewMesh();
                return;
            }

            GUI_BrushSet();

            if(brushSet == null)
            {
                EditorGUILayout.HelpBox("Assign A Brush Set", MessageType.Warning);
                return;
            }
            GUI_BrushSettings();
            EditorGUILayout.Space(8);
            GUI_MeshList();

            
            brushMesh = (Mesh)EditorGUILayout.ObjectField(brushMesh, typeof(Mesh), true);
            brushMaterial = (Material)EditorGUILayout.ObjectField(brushMaterial, typeof(Material), true);

            UpdatePropagationBrush();

                  
        }

        #endregion

        #region On Enable Disable
        private void OnEnable()
        {
            string path = EditorPrefs.GetString(SceneData_PrefKey, null);

            PropagationBrush.OnBrushApplied += OnBrushApplied;

            if (!string.IsNullOrEmpty(path))
            {
                sceneData = AssetDatabase.LoadAssetAtPath<SceneData>(path);
            }

            string brushSetPath = EditorPrefs.GetString(BrushSet_PrefKey, null);

            if (!string.IsNullOrEmpty(brushSetPath))
            {
                brushSet = AssetDatabase.LoadAssetAtPath<BrushSetSO>(brushSetPath);
            }

            brushSize = EditorPrefs.GetFloat(BrushSize_PrefKey, 1f);

            selectedMeshIndex = EditorPrefs.GetInt(SelectedMeshIndex_PrefKey, 0);

            if (selectedMeshIndex >= sceneData.propagatedMeshDefinitions.Count)
            {
                selectedMeshIndex = 0;
            }

            

            selectedBrushIndex = EditorPrefs.GetInt(SelectedBrushIndex_PrefKey, 0);    

            if (selectedBrushIndex >= brushSet.brushes.Count)
            {
                selectedBrushIndex = 0;
            }

            selectedBrushSO = brushSet?.brushes[selectedBrushIndex];
            selectedBrushTexture = selectedBrushSO?.maskTexture;

            density = EditorPrefs.GetInt(Density_PrefKey, 50);
          
        }

        
        void OnDisable()
        {
            PropagationBrush.OnBrushApplied -= OnBrushApplied;
            // Pencere kapanýrken seçilen referansýn yolunu kaydet
            if (sceneData != null)
            {
                string path = AssetDatabase.GetAssetPath(sceneData);
                EditorPrefs.SetString(SceneData_PrefKey, path);
                
            }
            else
            {
                EditorPrefs.DeleteKey(SceneData_PrefKey);

            }

            if (brushSet != null)
            {
                string brushSetPath = AssetDatabase.GetAssetPath(brushSet);
                EditorPrefs.SetString(BrushSet_PrefKey, brushSetPath);
            }
            else
            {
                EditorPrefs.DeleteKey(BrushSet_PrefKey);
            }

            EditorPrefs.SetFloat(BrushSize_PrefKey, brushSize);
            EditorPrefs.SetInt(SelectedMeshIndex_PrefKey, selectedMeshIndex);
            EditorPrefs.SetInt(Density_PrefKey, density);
            EditorPrefs.SetInt(SelectedBrushIndex_PrefKey, selectedBrushIndex);
        }
        #endregion
    }

#endif
}



#if UNITY_EDITOR
public class CreateNewMeshDefinitionWindow : EditorWindow
{

    public static SceneData sceneData;
    public Mesh mesh;
    public Material material;
    public string meshName;
    public bool useFrustumCulling; // Added to indicate if frustum culling is used for this mesh

    public static void OpenWindow()
    {
       GetWindow<CreateNewMeshDefinitionWindow>("Create New Mesh Type");
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
                if (mesh == null&&!isShown)
                {
                    EditorUtility.DisplayDialog("Error", "Please Assign A Mesh", "OK");
                    isShown = true;
                }
                if (material == null&&!isShown)
                {
                    EditorUtility.DisplayDialog("Error", "Please Assign A Material", "OK");
                    isShown = true;
                }
                if (string.IsNullOrEmpty(meshName)&&!isShown)
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

#endif


public struct BrushPaintData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;


    public BrushPaintData(Vector3 position,Quaternion rotation,Vector3 scale)
    {
        this.position = position;
        this.rotation = rotation;
        this.scale = scale;

    }
}
