using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct ChunkJob
    {
        public JobHandle handle;
        public JobHandle normalHandle;
        public NativeArray<float3> vertices;
        public NativeArray<float3> normals;
        public NativeArray<int> triangles;
        public NativeArray<Vector2> uvs;
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

public struct TreeJob
{
    public JobHandle handle;
    public NativeArray<int2> pointsOut;
    public NativeArray<int> colorIndices;
    public NativeArray<float> heights;
    public float2 position;
    public Chunk chunk;
}

public struct NormJob
{
    public JobHandle handle;
    public NativeArray<float3> vertices;
    public NativeArray<float3> normals;
    public NativeArray<int> triangles;
    public NativeArray<Vector2> uvs;
    public Chunk chunk;
}