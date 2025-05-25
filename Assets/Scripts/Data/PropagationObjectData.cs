using UnityEngine;
using System.Collections.Generic;
[System.Serializable]
public struct PropagationObjectData 
{
   public List<Matrix4x4> trsMatrices;  //Transform , Rotation , Scale
    public int meshIndex; // Added
}
