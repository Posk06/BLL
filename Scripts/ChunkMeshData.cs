using System.Numerics;
using Unity.Collections;
using Unity.Mathematics;

public struct ChunkMeshData
{
    NativeArray<float3> vertices;
    NativeArray<int> triangles;
}