using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct ChunkJob
    {
        public JobHandle handle;
        public NativeArray<float3> vertices;
        public NativeArray<int> triangles;
        public Chunk chunk;
        public float2 position;
    }
public struct TexJob
{
    public JobHandle handle;
    public NativeArray<int> colorIndices;
    public NativeArray<float> heights;
    public NativeArray<float> moistures;
    public Chunk chunk;
    public float2 position;
}

public class TreeJob
{
    public JobHandle handle;
    public NativeArray<Vector2Int> pointsOut;
    public NativeArray<int> colorIndices;
    public NativeArray<float> heights;
    public float2 position;
}