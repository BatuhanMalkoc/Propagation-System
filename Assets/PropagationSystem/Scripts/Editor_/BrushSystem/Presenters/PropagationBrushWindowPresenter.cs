
using System;
using UnityEngine;
using PropagationSystem.Editor;



namespace PropagationSystem.Editor
{

#if UNITY_EDITOR
    public class PropagationBrushWindowPresenter
    {
        private readonly PropagationBrushWindow _view;
        private readonly PropagationBrushSettingsModel _brushSettingsModel;
        private readonly PropagationBrushModel _brushModel;

        public PropagationBrushWindowPresenter(PropagationBrushWindow view)
        {
            _view = view;

            _brushSettingsModel = new PropagationBrushSettingsModel();

            _brushModel = new PropagationBrushModel(_brushSettingsModel);

            _view.OnBrushSizeChanged += HandleBrushSizeChanged;

            _view.OnBrushDensityChanged += HandleBrushDensityChanged;

            _view.OnBrushToggled += HandleActivateBrush;

            _view.OnBrushSetChanged += HandleBrushSetChanged;

            _view.OnSelectedBrushChanged += HandleSelectedBrushChanged;

            _view.OnSelectedMeshChanged += HandleSelectedMeshChanged;

            _view.OnBrushModeChanged += HandleBrushModeChanged;

            _view.OnSampleModeChanged += HandleSampleModeChanged;

            _brushModel.OnStroke += OnBrushApplied;
        }

        #region View Event Handlers


        private void HandleBrushSizeChanged(float size) { _brushSettingsModel.SetBrushSize(size); }

        private void HandleBrushDensityChanged(int brushDensity) { _brushSettingsModel.SetBrushDensity(brushDensity); }

        private void HandleActivateBrush(bool isActive) { _brushModel.ToggleBrush(isActive); }

        private void HandleBrushSetChanged(BrushSetSO brushSetSO) { _brushSettingsModel.SetCurrentBrushSet(brushSetSO); }

        private void HandleSelectedBrushChanged(int brushIndex) {

            _brushSettingsModel.SetCurrentBrushSO(brushIndex);
            _view.SetCurrentBrushTextureAndName(_brushSettingsModel.selectedBrushSO.maskTexture, _brushSettingsModel.selectedBrushSO.brushName);

        }

        private void HandleSelectedMeshChanged(int meshIndex) { _brushSettingsModel.SetSelectedMeshIndex(meshIndex); }

        private void HandleBrushModeChanged(PropagationBrushWindow.BrushMode brushMode) { _brushSettingsModel.SetBrushMode(brushMode); }

        private void HandleSampleModeChanged(PropagationBrushWindow.PropagationMode sampleMode) { _brushSettingsModel.SetSampleMode(sampleMode); }
       


        #endregion


        #region Getters






        #endregion

        private void OnBrushApplied(StrokeData stroke)
        {

        }



        // Editor penceresi kapandýðýnda çaðýrýlmalý
        public void Dispose()
        {
            _view.OnBrushSizeChanged -= HandleBrushSizeChanged;
            _view.OnBrushDensityChanged -= HandleBrushDensityChanged;
            _view.OnBrushToggled -= HandleActivateBrush;
            _view.OnBrushSetChanged -= HandleBrushSetChanged;
            _view.OnSelectedBrushChanged -= HandleSelectedBrushChanged;
            _brushModel.OnStroke -= OnBrushApplied;
        }
    }
#endif
}


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