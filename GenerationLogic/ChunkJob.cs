using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

struct ChunkJob
{
    public JobHandle handle;
    public NativeArray<float3> vertices;
    public Chunk chunk;
    public float2 position;
}