
#ifndef VOXEL_MESH_INFO
#define VOXEL_MESH_INFO


struct TransformData {
    float4x4 transform;
};

StructuredBuffer<TransformData> transformBuffer;
StructuredBuffer<uint> visibleIndices;
void GetTransformMatrix_float(float i, out float4x4 transform)
{


    transform = transformBuffer[visibleIndices[i]].transform;

}
#endif