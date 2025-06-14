// Assets/PropagationSystem/Editor/PropagationBrushWindow.cs
using PropagationSystem;
using PropagationSystem.Editor;
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace PropagationSystem.Editor
{
#if UNITY_EDITOR
  
    public class PropagationBrushWindow : EditorWindow //View class
    {

        #region Variables

        #region Static & Core References

        private PropagationBrushWindowPresenter presenter;
        private PropagationWindowViewTextures icons = new PropagationWindowViewTextures();

      
     

        #endregion

        #region GUI State & Scroll

        private Vector2 scrollPos;

        #endregion

        #region Brush - Runtime State

        private bool brushEnabled;
        private SceneData view_SceneData;
        private float view_BrushSize = 1f;
        private int view_BrushDensity = 50;
        private int view_SelectedBrushIndex;
        private BrushSetSO view_SelectedBrushSet;
        private bool view_IsBrushEnabled = false;
        private int view_SelectedMeshIndex;
        private int view_InstanceCount;
        private bool view_IsPressing;
        private bool view_isPreviewing = false; // Preview mode state

        private Vector3 view_staticPositionOffset;
        private Vector3 view_staticScale = Vector3.one;
        private Vector3 view_staticRotationEuler;

        private Vector3 view_randomPositionOffsetMin;
        private Vector3 view_randomPositionOffsetMax;
        private bool view_randomPositionOffsetPerComponent;

        private Vector3 view_randomScaleMin = Vector3.one;
        private Vector3 view_randomScaleMax = Vector3.one;
        private bool view_randomScalePerComponent;

        private Vector3 view_randomRotationMinEuler;
        private Vector3 view_randomRotationMaxEuler;
        private bool view_randomRotationPerComponent;

        private bool view_useMultipleMeshes;


        private bool foldoutBrushSelection = true;
        private bool foldoutStaticTransform = true;
        private bool foldoutRandomTransform = false;
        private bool foldoutCoreParameters = true;
        private bool foldoutSamplingMode = true;
        private bool foldoutBrushControls = true;

        #endregion

        #region Brush - Enums

        public enum BrushMode
        {
            Paint,
            Erase
        }

        public enum PropagationMode
        {
            Random,
            RandomWeighted,
            Grid
        }

        private PropagationMode SampleMode = PropagationMode.Random;
        private BrushMode brushMode = BrushMode.Paint;

        #endregion

        #region View - Visual Cache

        private Texture2D view_CurrentBrushTexture;
        private string view_SelectedBrushName;

        #endregion

        #region Editor Preferences Keys

        const string SceneData_PrefKey = "EditorPref_SceneDataKey";
        const string BrushSet_PrefKey = "EditorPref_BrushSetKey";
        const string BrushSize_PrefKey = "EditorPref_BrushSizeKey";
        const string InstanceCount_PrefKey = "EditorPref_InstanceCountKey";
        const string BrushMode_PrefKey = "EditorPref_BrushModeKey";
        const string SampleMode_PrefKey = "EditorPref_SampleModeKey";
        const string Density_PrefKey = "EditorPref_DensityKey";
        const string SelectedMeshIndex_PrefKey = "EditorPref_SelectedMeshIndexKey";
        const string SelectedBrushIndex_PrefKey = "EditorPref_SelectedBrushIndexKey";

        #endregion

       

        #region UI - View Event Actions

     public event Action<float> OnBrushSizeChanged;
public event Action<int> OnBrushDensityChanged;
public event Action<bool> OnBrushToggled;
public event Action<int> OnSelectedBrushChanged;
public event Action<BrushSetSO> OnBrushSetChanged;
public event Action<int> OnSelectedMeshChanged;
public event Action<BrushMode> OnBrushModeChanged;
public event Action<PropagationMode> OnSampleModeChanged;
public event Action<int> OnInstanceCountChanged;
public event Action<MeshData> OnMeshCreated;
public event Action<SceneData> OnSceneDataChanged;
public event Action<int> OnMeshRemoved;
public event Action OnPaintRequested;
public event Action OnStartPreviewing;
public event Action OnStopPreviewing;

// Yeni eklenen eventler
public event Action<Vector3> OnStaticPositionOffsetChanged;
public event Action<Vector3> OnStaticScaleChanged;
public event Action<Vector3> OnStaticRotationChanged;
public event Action<Vector3,Vector3> OnRandomPositionOffsetChanged;
public event Action<Vector3,Vector3> OnRandomScaleChanged;
public event Action<Vector3,Vector3> OnRandomRotationChanged;
public event Action<bool> OnRandomPositionPerComponentChanged;
public event Action<bool> OnRandomScalePerComponentChanged;
public event Action<bool> OnRandomRotationPerComponentChanged;


        #endregion

        #endregion


        #region Presenter Set Functions

        public void SetCurrentBrushTextureAndName(Texture2D texture, string brushName)
        {
            Debug.Log("Texture Atandý");
            view_CurrentBrushTexture = texture;
            view_SelectedBrushName = brushName;
        }

      

        public void SetPreviewMode(bool mode)
        {
            view_isPreviewing = mode;
        }

        #endregion

        #region External View Events 

        private void OnNewMeshAdded(MeshData data)
        {
            OnMeshCreated?.Invoke(data);
        }

        private void OnNewMeshAddWindowClosed()
        {
            CreateNewMeshType.Instance.OnMeshCreated -= OnNewMeshAdded;
            CreateNewMeshType.Instance.OnWindowClosed -= OnNewMeshAddWindowClosed;
        }

        #endregion

        #region Window Utilities

        [MenuItem("Tools/Propagation Brush")]
        public static void OpenWindow()
        {
            PropagationBrushWindow window = GetWindow<PropagationBrushWindow>("Propagation Brush");

            window.minSize = new Vector2(409, 1004);

           
                window.presenter = new PropagationBrushWindowPresenter(window);
               
            

        }

       //

        public  void Refresh()
        {
            Initialize();
        }

        private  void Initialize()
        {

            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;

            

        }

     
     
        public static void CloseWindow()
        {
           
            GetWindow<PropagationBrushWindow>().Close();
        }

        #endregion







        #region GUI Functions
        void GUI_Label()
        {
            GUILayout.Label("Propagation", EditorStyles.boldLabel); // Header
        }  //*

        void GUI_UpdateIcon()
        {
            titleContent = new GUIContent(" Propagation", icons.windowIcon);
        } //*

        void GUI_SceneData() 
        {
            view_SceneData = (SceneData)EditorGUILayout.ObjectField("Scene Data", view_SceneData, typeof(SceneData), false);
            OnSceneDataChanged?.Invoke(view_SceneData);
        }//*

        void GUI_MeshList()
        {
            #region Header Label
            GUILayout.Label("Mesh Picker", EditorStyles.boldLabel);
            #endregion

           
            #region Empty Mesh List Warning
            if (view_SceneData.propagatedMeshDefinitions.Count == 0)
            {
                EditorGUILayout.HelpBox("Please Add Mesh To Propagate.", MessageType.Warning);
                return;
            }
            #endregion

            #region Selected Mesh Preview
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();

            Texture2D selectedPreview = AssetPreview.GetAssetPreview(view_SceneData.propagatedMeshDefinitions[view_SelectedMeshIndex].mesh);

            GUILayout.Label(selectedPreview, GUILayout.Width(150), GUILayout.Height(150));
            GUILayout.Label("Selected Mesh: " + view_SceneData.propagatedMeshDefinitions[view_SelectedMeshIndex].name, EditorStyles.centeredGreyMiniLabel);

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            #endregion

            #region Mesh Selection Buttons
            int buttonSize = 80;
            int padding = 10;
            int viewWidth = (int)EditorGUIUtility.currentViewWidth;
            int buttonsPerRow = Mathf.Max(1, (viewWidth - padding) / (buttonSize + padding));

            GUILayout.BeginVertical();

            for (int i = 0; i < view_SceneData.propagatedMeshDefinitions.Count; i++)
            {
                if (i % buttonsPerRow == 0)
                {
                    if (i != 0) GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }

                Texture2D preview = AssetPreview.GetAssetPreview(view_SceneData.propagatedMeshDefinitions[i].mesh);
                GUIContent buttonContent = new GUIContent(preview, view_SceneData.propagatedMeshDefinitions[i].name);

                if (GUILayout.Button(buttonContent, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                {
                    view_SelectedMeshIndex = i;
                    // selectedMeshIndex_Static = i;
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            #endregion

            #region Bottom Bar: Create + Remove + Selection Changed Callback
            EditorGUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            string removeMeshIconPath = AssetDatabase.GUIDToAssetPath(GUIDS.REMOVEMESHTYPEICONGUID);
            Texture2D removeMeshIcon = EditorGUIUtility.Load(removeMeshIconPath) as Texture2D;

            GUI_CreateNewMesh();

            if (GUILayout.Button(removeMeshIcon, GUILayout.Width(48), GUILayout.Height(48)))
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Are you sure?",
                    "This will delete selected mesh type and all data belongs to it?",
                    "Yes",
                    "No"
                );

                if (confirmed)
                {
                    OnMeshRemoved?.Invoke(view_SelectedMeshIndex);
                    view_SelectedMeshIndex = 0;
                }
            }

            OnSelectedMeshChanged?.Invoke(view_SelectedMeshIndex);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            #endregion
        } //*


        void GUI_BrushSet()
        {    
            view_SelectedBrushSet = (BrushSetSO)EditorGUILayout.ObjectField("Brush Set", view_SelectedBrushSet, typeof(BrushSetSO), false);
            OnBrushSetChanged?.Invoke(view_SelectedBrushSet);
        } //*

        private void GUI_BrushSettings()
        {
            Color oldColor = GUI.backgroundColor;
            GUILayout.Label("Brush Settings", EditorStyles.boldLabel);



            #region Brush Selection
            foldoutBrushSelection = EditorGUILayout.Foldout(foldoutBrushSelection, "Brush Selection", true);
            if (foldoutBrushSelection)
            {
                EditorGUILayout.BeginVertical("box");

                // <<< Seçili Brush Önizleme >>>
                if (view_CurrentBrushTexture != null)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.BeginVertical();
                    GUILayout.Label(view_CurrentBrushTexture, GUILayout.Width(128), GUILayout.Height(128));
                    GUILayout.Label($"Selected Brush: {view_SelectedBrushName}", EditorStyles.centeredGreyMiniLabel);
                    GUILayout.EndVertical();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    EditorGUILayout.Space(8);
                }

                GUILayout.Label("Select Brush", EditorStyles.miniBoldLabel);
                int buttonSize = 64, padding = 5;
                int viewWidth = (int)EditorGUIUtility.currentViewWidth;
                int buttonsPerRow = Mathf.Max(1, (viewWidth - padding) / (buttonSize + padding));

                for (int i = 0; i < view_SelectedBrushSet.brushes.Count; i++)
                {
                    if (i % buttonsPerRow == 0)
                    {
                        if (i != 0) GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                    }

                    Texture2D preview = view_SelectedBrushSet.brushes[i].maskTexture;
                    if (GUILayout.Button(preview, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                    {
                        view_SelectedBrushIndex = i;
                        view_CurrentBrushTexture = preview;
                        view_SelectedBrushName = view_SelectedBrushSet.brushes[i].brushName;
                        OnSelectedBrushChanged?.Invoke(view_SelectedBrushIndex);
                    }
                }
                GUILayout.EndHorizontal();

                // Add / Remove
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                Texture2D addIcon = EditorGUIUtility.Load(AssetDatabase.GUIDToAssetPath(GUIDS.ADDBRUSHICONGUID)) as Texture2D;
                Texture2D removeIcon = EditorGUIUtility.Load(AssetDatabase.GUIDToAssetPath(GUIDS.REMOVEBRUSHICONGUID)) as Texture2D;
                if (GUILayout.Button(addIcon, GUILayout.Width(48), GUILayout.Height(48)))
                {
                    CreateNewBrushType.OpenWindow();
                    CreateNewBrushType.SetBrushSet(view_SelectedBrushSet);
                }
                if (GUILayout.Button(removeIcon, GUILayout.Width(48), GUILayout.Height(48)))
                {
                    bool ok = EditorUtility.DisplayDialog("Confirm", "Delete selected brush?", "Yes", "No");
                    if (ok)
                    {
                        view_SelectedBrushSet.brushes.RemoveAt(view_SelectedBrushIndex);
                        if (view_SelectedBrushSet.brushes.Count == 0)
                            view_SelectedBrushSet.brushes.Add(GetDefaultBrush());
                        EditorUtility.SetDirty(view_SelectedBrushSet);
                        view_SelectedBrushIndex = 0;
                        OnSelectedBrushChanged?.Invoke(view_SelectedBrushIndex);
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            #endregion

           

            #region Core Parameters
            foldoutCoreParameters = EditorGUILayout.Foldout(foldoutCoreParameters, "Core Parameters", true);
            if (foldoutCoreParameters)
            {
                EditorGUILayout.BeginVertical("box");
                view_BrushSize = EditorGUILayout.Slider("Brush Size", view_BrushSize, 0.1f, 100f);
                view_BrushDensity = EditorGUILayout.IntSlider("Density", view_BrushDensity, 1, 100);
                view_InstanceCount = EditorGUILayout.IntSlider("Instance Count", view_InstanceCount, 1, 10000);
                view_useMultipleMeshes = EditorGUILayout.Toggle("Use Multiple Meshes", view_useMultipleMeshes);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginVertical("box");

                // Paint / Erase icons (centered)
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                // Paint icon
                if (brushMode == BrushMode.Paint) GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button(
                        EditorGUIUtility.Load(AssetDatabase.GUIDToAssetPath(GUIDS.BRUSHICONGUID)) as Texture2D,
                        GUILayout.Width(48), GUILayout.Height(48)))
                {
                    brushMode = BrushMode.Paint;
                    OnBrushModeChanged?.Invoke(brushMode);
                }
                GUI.backgroundColor = oldColor;

                GUILayout.Space(10);

                // Erase icon
                if (brushMode == BrushMode.Erase) GUI.backgroundColor = new Color(1f, 0.5f, 0.5f); // light red
                if (GUILayout.Button(
                        EditorGUIUtility.Load(AssetDatabase.GUIDToAssetPath(GUIDS.ERASEICONGUID)) as Texture2D,
                        GUILayout.Width(48), GUILayout.Height(48)))
                {
                    brushMode = BrushMode.Erase;
                    OnBrushModeChanged?.Invoke(brushMode);
                }
                GUI.backgroundColor = oldColor;

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                EditorGUILayout.Space(5);

                // Toggle brush on/off
                if (GUILayout.Button(view_IsBrushEnabled ? "Disable Brush" : "Enable Brush"))
                {
                    view_IsBrushEnabled = !view_IsBrushEnabled;
                    OnBrushToggled?.Invoke(view_IsBrushEnabled);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
         
           
            #endregion


            #region Sampling Mode
            foldoutSamplingMode = EditorGUILayout.Foldout(foldoutSamplingMode, "Sampling Mode", true);
            if (foldoutSamplingMode)
            {
                EditorGUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();
                if (GUILayout.Toggle(SampleMode == PropagationMode.Grid, "Grid", "Button")) SampleMode = PropagationMode.Grid;
                if (GUILayout.Toggle(SampleMode == PropagationMode.Random, "Random", "Button")) SampleMode = PropagationMode.Random;
                if (GUILayout.Toggle(SampleMode == PropagationMode.RandomWeighted, "Weighted", "Button")) SampleMode = PropagationMode.RandomWeighted;
                GUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            #endregion

            #region Static Transform
            foldoutStaticTransform = EditorGUILayout.Foldout(foldoutStaticTransform, "Static Transform", true);
            if (foldoutStaticTransform)
            {
                EditorGUILayout.BeginVertical("box");
                view_staticPositionOffset = EditorGUILayout.Vector3Field("Position Offset", view_staticPositionOffset);
                view_staticScale = EditorGUILayout.Vector3Field("Scale", view_staticScale);
                view_staticRotationEuler = EditorGUILayout.Vector3Field("Rotation (Euler)", view_staticRotationEuler);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            #endregion

            #region Random Transform
            foldoutRandomTransform = EditorGUILayout.Foldout(foldoutRandomTransform, "Random Transform", true);
            if (foldoutRandomTransform)
            {
                EditorGUILayout.BeginVertical("box");
                GUILayout.Label("Position Offset Range", EditorStyles.miniBoldLabel);
                view_randomPositionOffsetMin = EditorGUILayout.Vector3Field("  Min", view_randomPositionOffsetMin);
                view_randomPositionOffsetMax = EditorGUILayout.Vector3Field("  Max", view_randomPositionOffsetMax);
                view_randomPositionOffsetPerComponent = EditorGUILayout.Toggle("Per Component", view_randomPositionOffsetPerComponent);

                GUILayout.Label("Scale Range", EditorStyles.miniBoldLabel);
                view_randomScaleMin = EditorGUILayout.Vector3Field("  Min", view_randomScaleMin);
                view_randomScaleMax = EditorGUILayout.Vector3Field("  Max", view_randomScaleMax);
                view_randomScalePerComponent = EditorGUILayout.Toggle("Per Component", view_randomScalePerComponent);

                GUILayout.Label("Rotation Range (Euler)", EditorStyles.miniBoldLabel);
                view_randomRotationMinEuler = EditorGUILayout.Vector3Field("  Min", view_randomRotationMinEuler);
                view_randomRotationMaxEuler = EditorGUILayout.Vector3Field("  Max", view_randomRotationMaxEuler);
                view_randomRotationPerComponent = EditorGUILayout.Toggle("Per Component", view_randomRotationPerComponent);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            #endregion

          

         

            #region Presenter Calls
            OnBrushSizeChanged?.Invoke(view_BrushSize);
            OnBrushDensityChanged?.Invoke(view_BrushDensity);
            OnInstanceCountChanged?.Invoke(view_InstanceCount);
            OnBrushModeChanged?.Invoke(brushMode);
            OnSampleModeChanged?.Invoke(SampleMode);
            OnStaticPositionOffsetChanged?.Invoke(view_staticPositionOffset);
            OnStaticScaleChanged?.Invoke(view_staticScale);
            OnStaticRotationChanged?.Invoke(view_staticRotationEuler);
            OnRandomPositionOffsetChanged?.Invoke(view_randomPositionOffsetMin,view_randomPositionOffsetMax);
            OnRandomScaleChanged?.Invoke(view_randomScaleMin,view_randomScaleMax);
            OnRandomRotationChanged?.Invoke(view_randomRotationMinEuler,view_randomRotationMaxEuler);
            OnRandomPositionPerComponentChanged?.Invoke(view_randomPositionOffsetPerComponent);
            OnRandomScalePerComponentChanged?.Invoke(view_randomScalePerComponent);
            OnRandomRotationPerComponentChanged?.Invoke(view_randomRotationPerComponent);
         
            #endregion

            GUI.backgroundColor = oldColor;
        }


        void GUI_CreateNewMesh()
        {
            string addMeshIconPath = AssetDatabase.GUIDToAssetPath(GUIDS.ADDMESHTYPEICONGUID);
            Texture2D addMeshIcon = EditorGUIUtility.Load(addMeshIconPath) as Texture2D;

            if (GUILayout.Button(addMeshIcon,GUILayout.Width(48),GUILayout.Height(48)))
            {
               
                CreateNewMeshType.OpenWindow();
                CreateNewMeshType.Instance.OnMeshCreated += OnNewMeshAdded;
                CreateNewMeshType.Instance.OnWindowClosed += OnNewMeshAddWindowClosed;
            }
        } //*

        private BrushDataSO GetDefaultBrush()
        {
            string defaultFolder = "Assets/EditorGenerated/Brushes";
            string defaultSearchName = "DefaultBrush";

            // 1. Mevcut asset'leri kontrol et
            string[] guids = AssetDatabase.FindAssets($"{defaultSearchName} t:BrushDataSO", new[] { defaultFolder });

            if (guids != null && guids.Length > 0)
            {
                string existingPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                BrushDataSO existing = AssetDatabase.LoadAssetAtPath<BrushDataSO>(existingPath);
                if (existing != null)
                {
                    Debug.Log("[BrushSystem] Var olan DefaultBrush kullanýldý.");
                    return existing;
                }
            }

            // 2. Orijinal default brush'ý al
            string sourcePath = AssetDatabase.GUIDToAssetPath(GUIDS.DEFAULTBRUSHGUID);
            BrushDataSO original = AssetDatabase.LoadAssetAtPath<BrushDataSO>(sourcePath);
           
            if (original == null)
            {
                Debug.LogError("Default brush bulunamadý!");
                return null;
            }

            // 3. Kopyasýný oluþtur
            BrushDataSO copy = ScriptableObject.Instantiate(original);

            // 4. Klasörleri oluþtur (yoksa)
            if (!AssetDatabase.IsValidFolder(defaultFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/EditorGenerated"))
                    AssetDatabase.CreateFolder("Assets", "EditorGenerated");

                AssetDatabase.CreateFolder("Assets/EditorGenerated", "Brushes");
            }

            // 5. Uygun isim üret
            string fullFolderPath = Path.Combine(Application.dataPath, "EditorGenerated/Brushes");
            string uniqueFileName = CreateNewBrushType.GetUniqueAssetPath(fullFolderPath, defaultSearchName); // örn: DefaultBrush_1.asset
            string fullAssetPath = $"{defaultFolder}/{uniqueFileName}";

            // 6. Asset olarak kaydet
            AssetDatabase.CreateAsset(copy, fullAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 7. Yükleyip geri dön
            BrushDataSO loaded = AssetDatabase.LoadAssetAtPath<BrushDataSO>(fullAssetPath);
            Debug.Log("[BrushSystem] Yeni DefaultBrush instance'ý oluþturuldu.");
            return loaded;
        }




        private void OnGUI()
        {
            #region Begin ScrollView
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height));
            #endregion

            #region Header UI
            GUI_Label();
            GUI_UpdateIcon();
            #endregion

            #region SceneData UI Container
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI_SceneData();
            EditorGUILayout.Space(2);
            #endregion

            #region Null Check - SceneData
            if (view_SceneData == null)
            {
                EditorGUILayout.HelpBox("Assign A Scene Data First", MessageType.Warning);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();
                return;
            }
            else if (view_SceneData.propagatedMeshDefinitions.Count == 0)
            {
                EditorGUILayout.HelpBox("Add Atleast One Mesh To Propagate", MessageType.Warning);
                EditorGUILayout.EndVertical();
                GUI_CreateNewMesh();
                EditorGUILayout.EndScrollView();
                return;
            }
            #endregion

            #region Brush Set UI
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI_BrushSet();
            EditorGUILayout.Space(2);
            EditorGUILayout.EndVertical();
            #endregion

            #region Null Check - BrushSet
            if (view_SelectedBrushSet == null)
            {
                EditorGUILayout.HelpBox("Assign A Brush Set", MessageType.Warning);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();
                return;
            }

            if (view_SelectedBrushSet.brushes.Count == 0)
            {
                EditorGUILayout.HelpBox("Assign A Brush", MessageType.Warning);

                string addBrushIconPath = AssetDatabase.GUIDToAssetPath(GUIDS.ADDBRUSHICONGUID);
                Texture2D addBrushIcon = EditorGUIUtility.Load(addBrushIconPath) as Texture2D;

                if (GUILayout.Button(addBrushIcon, GUILayout.Width(48), GUILayout.Height(48)))
                {
                    CreateNewBrushType.OpenWindow();
                    CreateNewBrushType.SetBrushSet(view_SelectedBrushSet);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();
                return;
            }
            #endregion

            #region Brush Settings UI
            GUI_BrushSettings();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
            #endregion

            #region Mesh List UI
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI_MeshList();
            EditorGUILayout.EndVertical();
            #endregion

            #region Preview Buttons

            string previewButtonText = view_isPreviewing ? "Stop Previewing" : "Start Previewing";

            if (GUILayout.Button(previewButtonText))
            {
                view_isPreviewing = !view_isPreviewing;

                if (view_isPreviewing)
                {
                    OnStartPreviewing?.Invoke();
                
                }
                else
                {
                    OnStopPreviewing?.Invoke();

                }
            }
            #endregion

            #region End ScrollView
            EditorGUILayout.EndScrollView();
            #endregion
        } //*





        private void OnSceneGUI(SceneView sceneView)
        {
          
            Handles.BeginGUI();
            GUI.Label(new Rect(10, 10, 200, 20), "Scene GUI aktif");
            Handles.EndGUI();

            Event e = Event.current;
            if (e == null)
            {
                Debug.Log("Event Null");
                return;
            }

            BrushMode currentMode = e.shift ? BrushMode.Erase : brushMode;
            OnBrushModeChanged?.Invoke(currentMode);

            if (view_IsBrushEnabled)
            {
              
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

              
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    view_IsPressing = true;
                    e.Use(); 
                }

                if (view_IsPressing)
                {
                    Debug.Log("Basýyor");
                    OnPaintRequested?.Invoke();
                    view_IsPressing = false;
                }
            }
            else
            {
               
            }
        } //*




        #endregion

        #region On Enable Disable
        private void OnEnable()
        {
            if (presenter == null)
            {
                presenter = new PropagationBrushWindowPresenter(this);
                Debug.Log("Presnter Oluþturuldu");
            }
            Initialize();

            string path = EditorPrefs.GetString(SceneData_PrefKey, null);



            if (!string.IsNullOrEmpty(path))
            {
                view_SceneData = AssetDatabase.LoadAssetAtPath<SceneData>(path);
            }

            string brushSetPath = EditorPrefs.GetString(BrushSet_PrefKey, null);

            if (!string.IsNullOrEmpty(brushSetPath))
            {
                view_SelectedBrushSet = AssetDatabase.LoadAssetAtPath<BrushSetSO>(brushSetPath);
            }

            view_BrushSize = EditorPrefs.GetFloat(BrushSize_PrefKey, 1f);

            view_SelectedMeshIndex = EditorPrefs.GetInt(SelectedMeshIndex_PrefKey, 0);

            if (view_SceneData != null)
            {
                if (view_SelectedMeshIndex >= view_SceneData.propagatedMeshDefinitions.Count)
                {
                    view_SelectedMeshIndex = 0;
                }
            }


            view_SelectedBrushIndex = EditorPrefs.GetInt(SelectedBrushIndex_PrefKey, 0);

            if (view_SelectedBrushIndex >= view_SelectedBrushSet.brushes.Count)
            {
                view_SelectedBrushIndex = 0;
            }




            view_BrushDensity = EditorPrefs.GetInt(Density_PrefKey, 50);

            view_InstanceCount = EditorPrefs.GetInt(InstanceCount_PrefKey, 0);
            brushMode = (BrushMode)EditorPrefs.GetInt(BrushMode_PrefKey, (int)BrushMode.Paint);
            SampleMode = (PropagationMode)EditorPrefs.GetInt(SampleMode_PrefKey, (int)PropagationMode.Random);


           



        }

     


        void OnDisable()
        {
           
            // Pencere kapanýrken seçilen referansýn yolunu kaydet
            if (view_SceneData != null)
            {
                string path = AssetDatabase.GetAssetPath(view_SceneData);
                EditorPrefs.SetString(SceneData_PrefKey, path);
                
            }
            else
            {
                EditorPrefs.DeleteKey(SceneData_PrefKey);

            }

            if (view_SelectedBrushSet != null)
            {
                string brushSetPath = AssetDatabase.GetAssetPath(view_SelectedBrushSet);
                EditorPrefs.SetString(BrushSet_PrefKey, brushSetPath);
            }
            else
            {
                EditorPrefs.DeleteKey(BrushSet_PrefKey);
            }


            SceneView.duringSceneGui -= OnSceneGUI;

            EditorPrefs.SetFloat(BrushSize_PrefKey, view_BrushSize);
            EditorPrefs.SetInt(SelectedMeshIndex_PrefKey, view_SelectedMeshIndex);
            EditorPrefs.SetInt(Density_PrefKey, view_BrushDensity);
            EditorPrefs.SetInt(SelectedBrushIndex_PrefKey, view_SelectedBrushIndex);

            EditorPrefs.SetInt(InstanceCount_PrefKey, view_InstanceCount);
            EditorPrefs.SetInt(BrushMode_PrefKey, (int)brushMode);
            EditorPrefs.SetInt(SampleMode_PrefKey, (int)SampleMode);
        }
        #endregion
    }

  

public  class PropagationWindowViewTextures
    {
        private Texture2D _windowIcon;
        private Texture2D _brushMesh;
        private Texture2D _brushMaterial;

        public Texture2D windowIcon
        {
            get
            {
                if (_windowIcon == null)
                {
                    _windowIcon = EditorGUIUtility.Load(AssetDatabase.GUIDToAssetPath(GUIDS.PROPAGATIONICONGUID)) as Texture2D;
                }
                return _windowIcon;
            }
            set
            {
                _windowIcon = value;
            }
        }

        public Texture2D brushMesh
        {
            get
            {
                if (_brushMesh == null)
                {
                    _brushMesh = EditorGUIUtility.Load(AssetDatabase.GUIDToAssetPath(GUIDS.BRUSHMESHGUID)) as Texture2D;
                }
                return _brushMesh;
            }
            set
            {
                _brushMesh = value;
            }
        }

        public Texture2D brushMaterial
        {
            get
            {
                if (_brushMaterial == null)
                {
                    _brushMaterial = EditorGUIUtility.Load(AssetDatabase.GUIDToAssetPath(GUIDS.BRUSHMATERIALGUID)) as Texture2D;
                }
                return _brushMaterial;
            }
            set
            {
                _brushMaterial = value;
            }
        }

    }



#endif
}




