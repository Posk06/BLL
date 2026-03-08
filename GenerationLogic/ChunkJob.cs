using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

struct ChunkJob
{
    public JobHandle handle;
    public NativeArray<float3> vertices;
    public Chunk chunk;
}