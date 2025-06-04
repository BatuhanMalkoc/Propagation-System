using PropagationSystem;
using UnityEngine;

public interface IBrush 
{
   
    public BrushPaintData[] ApplyBrush(BrushDataSO brush,Vector3 hitPoint, Vector3 hitNormal, float brushSize, float density, int count,Camera camera);

  

}
