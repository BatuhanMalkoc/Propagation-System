using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

namespace PropagationSystem
{


    [CreateAssetMenu(menuName = "Propagation/New Scene Propagation Data", fileName = "PropagationData")]
    public class SceneData : ScriptableObject
    {
        /// <summary>Mesh'e karþýlýk gelen object datasý listesi</summary>
        public List<TransformData> propagatedObjectDatas = new List<TransformData>();

        
        /// <summary>Tanýmlý mesh verileri listesi</summary>
        public List<MeshData> propagatedMeshDefinitions = new List<MeshData>();

        /// <summary>Önceki frame'deki mesh definition sayýsý</summary>
        private int propagatedMeshDefinitionCount = 0;

        /// <summary>Index takibi için kullanýlan liste</summary>
        private List<int> indexs = new List<int>();


        private void OnValidate()
        {
            int newCount = propagatedMeshDefinitions.Count;

            // Eðer mesh definition sayýsý deðiþmemiþse, hiçbir þey yapma
            if (propagatedMeshDefinitionCount == newCount)
                return;

            // Silinmiþse, eski meshIndex'e göre karþýlaþtýr
            if (propagatedMeshDefinitionCount > newCount)
            {
                // Þu anki meshIndex'lerin listesi
                List<int> currentMeshIndices = propagatedMeshDefinitions.Select(x => x.meshIndex).ToList();

                // Kayýtlý index'lerde olup þu an olmayanlarý bul
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

            // Mevcut tüm mesh'lerin meshIndex'lerini doðru sýraya göre güncelle
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

            // Fazla elemanlarý temizle
            while (indexs.Count > propagatedMeshDefinitions.Count)
                indexs.RemoveAt(indexs.Count - 1);

            while (propagatedObjectDatas.Count > propagatedMeshDefinitions.Count)
                propagatedObjectDatas.RemoveAt(propagatedObjectDatas.Count - 1);

            propagatedMeshDefinitionCount = propagatedMeshDefinitions.Count;
        }

    }

}
