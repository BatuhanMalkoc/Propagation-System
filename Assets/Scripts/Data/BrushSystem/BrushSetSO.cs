using UnityEngine;
using System.Collections.Generic;
namespace PropagationSystem
{
    [CreateAssetMenu(fileName = "BrushSet", menuName = "PropagationSystem/BrushSet", order = 2)]

    public class BrushSetSO : ScriptableObject
    {
        public List<BrushDataSO> brushes;
    }
}