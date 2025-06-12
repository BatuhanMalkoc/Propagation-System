using System;
using UnityEngine;
using UnityEditor;

namespace PropagationSystem.Editor
{
#if UNITY_EDITOR
    public class PropagationBrushWindowPresenter
    {
        #region Fields

        private readonly PropagationBrushWindow _view;
        private readonly PropagationBrushSettingsModel _brushSettingsModel;
        private readonly PropagationBrushModel _brushModel;
        private readonly PropagationSceneDataModel _sceneDataModel;
        private readonly EditorPreviewer _editorPreviewerModel;

        #endregion

        #region Constructor

        public PropagationBrushWindowPresenter(PropagationBrushWindow view)
        {
            SceneView.duringSceneGui += OnSceneGUI;

            _view = view;
            _brushSettingsModel = new PropagationBrushSettingsModel();
            _sceneDataModel = new PropagationSceneDataModel();
            _brushModel = new PropagationBrushModel(_brushSettingsModel);
            _editorPreviewerModel = new EditorPreviewer();

            RegisterViewEvents();
            _brushModel.OnStroke += OnBrushApplied;

            
           

        }

        #endregion

        #region Scene GUI

        private void OnSceneGUI(SceneView view)
        {
            if (_brushModel.GetBrushActive())
            {
                _brushModel.DrawBrush(view.camera);
                view.Repaint();
            }
        }

        #endregion

        #region View Event Registration

        private void RegisterViewEvents()
        {
            _view.OnBrushSizeChanged += HandleBrushSizeChanged;
            _view.OnBrushDensityChanged += HandleBrushDensityChanged;
            _view.OnBrushToggled += HandleActivateBrush;
            _view.OnBrushSetChanged += HandleBrushSetChanged;
            _view.OnSelectedBrushChanged += HandleSelectedBrushChanged;
            _view.OnSelectedMeshChanged += HandleSelectedMeshChanged;
            _view.OnBrushModeChanged += HandleBrushModeChanged;
            _view.OnSampleModeChanged += HandleSampleModeChanged;
            _view.OnMeshCreated += HandleMeshCreated;
            _view.OnSceneDataChanged += HandleSceneDataChanged;
            _view.OnMeshRemoved += HandleMeshRemoved;
            _view.OnPaintRequested += HandlePaintRequested;
            _view.OnInstanceCountChanged += HandleInstanceCountChanged;
            _view.OnStartPreviewing += HandleStartPreview;
            _view.OnStopPreviewing += HandleStopPreview;
        }

        #endregion

        #region View Event Handlers

        private void HandleBrushSizeChanged(float size)
        {
            _brushSettingsModel.SetBrushSize(size);
        }

        private void HandleBrushDensityChanged(int brushDensity)
        {
            _brushSettingsModel.SetBrushDensity(brushDensity);
        }

        private void HandleInstanceCountChanged(int instanceCount)
        {
            _brushSettingsModel.SetInstanceCount(instanceCount);
        }

        private void HandleActivateBrush(bool isActive)
        {
            _brushModel.ToggleBrush(isActive);
        }

        private void HandleBrushSetChanged(BrushSetSO brushSetSO)
        {
            _brushSettingsModel.SetCurrentBrushSet(brushSetSO);
        }

        private void HandleSelectedBrushChanged(int brushIndex)
        {
            _brushSettingsModel.SetCurrentBrushSO(brushIndex);
            _view.SetCurrentBrushTextureAndName(
                _brushSettingsModel.selectedBrushSO.maskTexture,
                _brushSettingsModel.selectedBrushSO.brushName
            );
        }

        private void HandleSelectedMeshChanged(int meshIndex)
        {
            _brushSettingsModel.SetSelectedMeshIndex(meshIndex);
        }

        private void HandleBrushModeChanged(PropagationBrushWindow.BrushMode brushMode)
        {
            _brushSettingsModel.SetBrushMode(brushMode);
        }

        private void HandleSampleModeChanged(PropagationBrushWindow.PropagationMode sampleMode)
        {
            _brushSettingsModel.SetSampleMode(sampleMode);
        }

        private void HandlePaintRequested()
        {
            _brushModel.HandleBrushCommand();
        }

        private void HandleMeshCreated(MeshData meshData)
        {
            _editorPreviewerModel.SetPreviewMode(false);
            _sceneDataModel.AddNewMeshDefinition(meshData);
        }

        private void HandleSceneDataChanged(SceneData sceneData)
        {
            _sceneDataModel.SetSceneData(sceneData);
        }

        private void HandleMeshRemoved(int meshIndex)
        {
            _sceneDataModel.RemoveMeshDefinition(meshIndex);
        }

        private void HandleStartPreview()
        {
            _editorPreviewerModel.SetPreviewMode(true);
            _editorPreviewerModel.Setup(GetSceneData());
            _view.SetPreviewMode(_editorPreviewerModel.GetIsPreviewing());
        }

        private void HandleStopPreview()
        {
            _editorPreviewerModel.SetPreviewMode(false);
            _view.SetPreviewMode(_editorPreviewerModel.GetIsPreviewing());
        }

        #endregion

        #region Brush Callback

        private void OnBrushApplied(StrokeData stroke)
        {
            Debug.Log("OnBrushApplied called with " + stroke.savedPositions.Length + " paint data items.");
            _sceneDataModel.AddInstances(stroke.meshIndex, stroke.savedPositions);
        }

        #endregion

        #region Getters

        public SceneData GetSceneData()
        {
            return _sceneDataModel.GetSceneData();
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            _view.OnBrushSizeChanged -= HandleBrushSizeChanged;
            _view.OnBrushDensityChanged -= HandleBrushDensityChanged;
            _view.OnBrushToggled -= HandleActivateBrush;
            _view.OnBrushSetChanged -= HandleBrushSetChanged;
            _view.OnSelectedBrushChanged -= HandleSelectedBrushChanged;
            _view.OnSelectedMeshChanged -= HandleSelectedMeshChanged;
            _view.OnBrushModeChanged -= HandleBrushModeChanged;
            _view.OnSampleModeChanged -= HandleSampleModeChanged;
            _view.OnMeshCreated -= HandleMeshCreated;
            _view.OnSceneDataChanged -= HandleSceneDataChanged;
            _view.OnMeshRemoved -= HandleMeshRemoved;
            _view.OnPaintRequested -= HandlePaintRequested;
            _view.OnInstanceCountChanged -= HandleInstanceCountChanged;
            _view.OnStartPreviewing -= HandleStartPreview;
            _view.OnStopPreviewing -= HandleStopPreview;

            _brushModel.OnStroke -= OnBrushApplied;

            SceneView.duringSceneGui -= OnSceneGUI;
        }

        #endregion
    }
#endif
}

#region GUIDS

public struct GUIDS
{
    public const string BRUSHICONGUID = "b50705d6170a42d4a9480d8e579bca3b";
    public const string ERASEICONGUID = "b1c9c7ca80357484b993b162ad1bf81b";
    public const string PROPAGATIONICONGUID = "53b798f87c3af534893455e7f6514777";
    public const string REMOVEBRUSHICONGUID = "d3ef903f0f464a84396bb1ca53951c6b";
    public const string ADDBRUSHICONGUID = "391b56a0b0f4c714aab3c36bd98ed49f";
    public const string ADDMESHTYPEICONGUID = "16c2e3a7c6ce0284f99dee00097bf695";
    public const string REMOVEMESHTYPEICONGUID = "76dcae87ac35952478b0c03f6443e691";
    public const string DEFAULTBRUSHGUID = "8c4177208ea9dda4b86c883af3f144f9";
    public const string BRUSHMESHGUID = "c165c719ed1e59d46a5ae3c460f62d64";
    public const string BRUSHMATERIALGUID = "e253b5f545f25494b97e5e5eacebf0b9";
}

#endregion
