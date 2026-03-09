using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

struct TexJob
{
    public JobHandle handle;
    public NativeArray<Color32> colors;
    public NativeArray<float> heights;
    public Chunk chunk;
    public float2 position;
}