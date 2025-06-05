// Editor klas�r�ne ekleyin (�rn. Assets/Editor/UndoListener.cs)
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
        // Yapmas�n� istedi�iniz i�lemleri buraya yaz�n.
        // �rne�in:
        SceneData sceneData = PropagationBrushWindow.GetCurrentSceneData();

      

        for (int i = 0; i < sceneData.propagatedMeshDefinitions.Count; i++)
        {


            EditorPreviewer.OnSceneDataChanged(i);
        }

       
    }
}
