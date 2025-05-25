using UnityEngine;
using System.Linq;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Propagation/New Scene Propagation Data", fileName = "PropagationData")]
public class PropagationDataSO : ScriptableObject
{
    /// <summary>Mesh'e kar��l�k gelen object datas� listesi</summary>
    public List<PropagationObjectData> propagatedObjectDatas = new List<PropagationObjectData>();

    /// <summary>Tan�ml� mesh verileri listesi</summary>
    public List<PropagationMeshData> propagatedMeshDefinitions = new List<PropagationMeshData>();

    /// <summary>�nceki frame'deki mesh definition say�s�</summary>
    private int propagatedMeshDefinitionCount = 0;

    /// <summary>Index takibi i�in kullan�lan liste</summary>
    private List<int> indexs = new List<int>();


    private void OnValidate()
    {
        int newCount = propagatedMeshDefinitions.Count;

        #region Ekleme (Yeni mesh definition eklendi�inde)
        if (propagatedMeshDefinitionCount < newCount)
        {
            int addedCount = newCount - propagatedMeshDefinitionCount;

            for (int i = 0; i < addedCount; i++)
            {
                int newIndex = propagatedMeshDefinitionCount + i;

                // propagatedMeshDefinitions struct oldu�u i�in �nce kopya al, sonra de�i�tir, tekrar ata
                var meshData = propagatedMeshDefinitions[newIndex];
                meshData.meshIndex = newIndex;
                propagatedMeshDefinitions[newIndex] = meshData;

                var newObjData = new PropagationObjectData();
                newObjData.meshIndex = newIndex;

                propagatedObjectDatas.Insert(newIndex, newObjData);
                indexs.Insert(newIndex, newIndex);
            }
        }
        #endregion


        #region Silme (Mesh definition silindiyse)
        if (propagatedMeshDefinitionCount > newCount)
        {
            List<int> currentMeshIndices = propagatedMeshDefinitions.Select(x => x.meshIndex).ToList();
            List<int> fark = indexs.Except(currentMeshIndices).ToList();

            foreach (int silinecekMeshIndex in fark)
            {
                int deleteIndex = propagatedObjectDatas.FindIndex(x => x.meshIndex == silinecekMeshIndex);

                if (deleteIndex != -1)
                {
                    propagatedObjectDatas.RemoveAt(deleteIndex);
                    indexs.RemoveAt(deleteIndex);
                    Debug.Log("Silindi: meshIndex = " + silinecekMeshIndex);
                }
            }
        }
        #endregion


        #region MeshIndex G�ncelleme
        for (int i = 0; i < propagatedMeshDefinitions.Count; i++)
        {
            // propagatedMeshDefinitions struct oldu�u i�in kopya �zerinde de�i�iklik yap
            var meshData = propagatedMeshDefinitions[i];
            meshData.meshIndex = i;
            propagatedMeshDefinitions[i] = meshData;

            if (i < propagatedObjectDatas.Count)
            {
                // propagatedObjectDatas struct, ayn� �ekilde kopya al�p de�i�tir
                PropagationObjectData updated = propagatedObjectDatas[i];
                updated.meshIndex = i;
                propagatedObjectDatas[i] = updated;
            }

            if (i < indexs.Count)
                indexs[i] = i;
            else
                indexs.Add(i);
        }
        #endregion


        #region Liste Temizli�i (fazla ��eleri sil)
        while (indexs.Count > propagatedMeshDefinitions.Count)
            indexs.RemoveAt(indexs.Count - 1);

        while (propagatedObjectDatas.Count > propagatedMeshDefinitions.Count)
            propagatedObjectDatas.RemoveAt(propagatedObjectDatas.Count - 1);
        #endregion

        propagatedMeshDefinitionCount = propagatedMeshDefinitions.Count;
    }
}
