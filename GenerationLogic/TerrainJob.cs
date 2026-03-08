using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Rendering;

[BurstCompile]
public struct TerrainJob : IJobParallelFor
{
    public int chunkSize;
    public float amplitude;
    public float maxAmplitude;
    public float frequency;
    public float lacunarity;
    public float gain;
    public int octaves;
    public float redistrobution;

    public float2 chunkPosition;

    public NativeArray<float3> vertices;

    public void Execute(int index)
    {
        int x = index % chunkSize;
        int z = index / chunkSize;

        float2 worldPos = new float2(
        x + chunkPosition.x * chunkSize,
        z + chunkPosition.y * chunkSize
        );

        float height = FractalNoise(worldPos);

        vertices[index] = new float3(x, height, z);
    }

    float FractalNoise(float2 pos)
    {
        float v = 0;
        float ampsum = 0;

        for (int i = 0; i < octaves; i++)
        {
            v += noise.snoise(pos * frequency) * amplitude;
            ampsum += amplitude;

            frequency *= lacunarity;  // usually 2.0
            amplitude *= gain;    // usually 0.5
        }

    return (float) Math.Pow(Math.Abs(v / ampsum), redistrobution);
}
}