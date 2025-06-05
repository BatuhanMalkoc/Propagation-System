// Editor klasörüne ekleyin (örn. Assets/Editor/UndoListener.cs)
using PropagationSystem.Editor;
using UnityEditor;
using UnityEngine;
using PropagationSystem;
using System;
[InitializeOnLoad]
public static class UndoListener
{
    static UndoListener()
    {
        Undo.undoRedoPerformed += OnUndo;
 
    }




    private static void OnUndo()
    {
      
        MyCustomCommand();
    }

    private static void MyCustomCommand()
    {
        // Yapmasýný istediðiniz iþlemleri buraya yazýn.
        // Örneðin:
        SceneData sceneData = PropagationBrushWindow.GetCurrentSceneData();

      

        for (int i = 0; i < sceneData.propagatedMeshDefinitions.Count; i++)
        {


            EditorPreviewer.OnSceneDataChanged(i);
        }

       
    }
}
