#ifndef VOXEL_MESH_INFO
#define VOXEL_MESH_INFO

struct TransformData
{
    float4x4 transformMatrix;
};

// Görünür instance indeksleri (meshIndex, localIndex)
StructuredBuffer<uint2> visibleIndices;

// Her mesh için ayrı pre‐computed offset+count verisi
struct VisibleOffsetData {
    int offset;
    int count;
};
StructuredBuffer<VisibleOffsetData> visibleOffsets;

// Bu shader’a her materyalin meshIndex’i atanacak
int _MeshIndex;

// Her mesh’in kendi transform buffer’ı olarak atanıyor
StructuredBuffer<TransformData> transformBuffer;

// i: bu mesh’e özel sıralı index (0..visibleOffsets[_MeshIndex].count-1)
void GetPositionMatrix_float (float i, out float3 position)
{
    // 1) İlgili mesh’in global offset’ini al
    int baseOffset = visibleOffsets[_MeshIndex].offset;

    // 2) global visibleIndices index’ini hesapla
    uint globalIdx = baseOffset + i;

    // 3) globalIdx’den (meshIndex, localIndex) çiftini oku
    uint2 indexPair = visibleIndices[globalIdx];

    // 4) indexPair.x zaten == _MeshIndex olacak; localIndex ise y
    uint localIndex = indexPair.y;

    // 5) TransformBuffer’dan localIndex’e karşılık gelen matrisi OKU
    TransformData tData = transformBuffer[localIndex];

    // 6) Matristen translation (4. satır) kısmını al
    position = tData.transformMatrix[3].xyz;
}

#endif
