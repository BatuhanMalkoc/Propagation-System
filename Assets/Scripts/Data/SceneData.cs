using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

namespace PropagationSystem
{


    [CreateAssetMenu(menuName = "Propagation/New Scene Propagation Data", fileName = "PropagationData")]
    public class SceneData : ScriptableObject
    {
        /// <summary>Mesh'e kar��l�k gelen object datas� listesi</summary>
        public List<TransformData> propagatedObjectDatas = new List<TransformData>();

        
        /// <summary>Tan�ml� mesh verileri listesi</summary>
        public List<MeshData> propagatedMeshDefinitions = new List<MeshData>();

        /// <summary>�nceki frame'deki mesh definition say�s�</summary>
        private int propagatedMeshDefinitionCount = 0;

        /// <summary>Index takibi i�in kullan�lan liste</summary>
        private List<int> indexs = new List<int>();


        private void OnValidate()
        {
            int newCount = propagatedMeshDefinitions.Count;

            // E�er mesh definition say�s� de�i�memi�se, hi�bir �ey yapma
            if (propagatedMeshDefinitionCount == newCount)
                return;

            // Silinmi�se, eski meshIndex'e g�re kar��la�t�r
            if (propagatedMeshDefinitionCount > newCount)
            {
                // �u anki meshIndex'lerin listesi
                List<int> currentMeshIndices = propagatedMeshDefinitions.Select(x => x.meshIndex).ToList();

                // Kay�tl� index'lerde olup �u an olmayanlar� bul
                List<int> silinecekler = indexs.Except(currentMeshIndices).ToList();

                foreach (int silinecekMeshIndex in silinecekler)
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
            if(propagatedMeshDefinitionCount < newCount)
            {
                propagatedObjectDatas.Add(new TransformData() { meshIndex = newCount-1});
               
            }

            // Mevcut t�m mesh'lerin meshIndex'lerini do�ru s�raya g�re g�ncelle
            for (int i = 0; i < propagatedMeshDefinitions.Count; i++)
            {
                var meshData = propagatedMeshDefinitions[i];
                meshData.meshIndex = i;
                propagatedMeshDefinitions[i] = meshData;

                if (i < propagatedObjectDatas.Count)
                {
                    var objData = propagatedObjectDatas[i];
                    objData.meshIndex = i;
                    propagatedObjectDatas[i] = objData;
                }

                if (i < indexs.Count)
                    indexs[i] = i;
                else
                    indexs.Add(i);
            }

            // Fazla elemanlar� temizle
            while (indexs.Count > propagatedMeshDefinitions.Count)
                indexs.RemoveAt(indexs.Count - 1);

            while (propagatedObjectDatas.Count > propagatedMeshDefinitions.Count)
                propagatedObjectDatas.RemoveAt(propagatedObjectDatas.Count - 1);

            propagatedMeshDefinitionCount = propagatedMeshDefinitions.Count;
        }

    }

}
