using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

struct TexJob
{
    public JobHandle handle;
    public NativeArray<Color32> colors;
    public NativeArray<float> heights;
    public NativeArray<float> moistures;
    public Chunk chunk;
    public float2 position;
}