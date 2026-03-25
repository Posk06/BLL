//--------------------------------------------
//This code generates terrain height values for each chunk
//Not AI generated but first iterations came from AI, but was modified and completely rewritten
//--------------------------------------------
// - Oskar Benjamin Trillitzsch

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct TerrainJob : IJobParallelFor
{
    public int chunkSize;
    // number of vertices along each edge (resolution of the heightmap)
    public int resolution;

    public float amplitude;
    public int maxAmplitude;
    public float frequency;
    public float lacunarity;
    public float gain;
    public int octaves;
    public float redistrobution;
    public float seedOffset;

    public float2 chunkPosition;

    public NativeArray<float3> vertices;

    public void Execute(int index)
    {
        //Generate Sizing to allow for more/less than chunkSize x chunkSize verticies
        float resolutionSizing = (float) chunkSize / (float) ( resolution - 1);
        float x = index % resolution * resolutionSizing;
        float z = index / resolution * resolutionSizing;

        float2 worldPos = new float2(
            x + chunkPosition.x * chunkSize + seedOffset,
            z + chunkPosition.y * chunkSize + seedOffset
        );

        float height = FractalNoise(worldPos) * maxAmplitude;

        vertices[index] = new float3(x, height, z);
    }

    float FractalNoise(float2 pos)
    {
        if (octaves <= 0)
        {
            // fall back to single‐layer noise
            return (noise.snoise(pos * frequency) + 1) * 0.5f * amplitude;
        }

        float v = 0;
        float ampsum = 0;

        // work on copies so values stay constant per vertex
        float f = frequency;
        float a = amplitude;

        //Generate multiple Layers of noise with increasing detail and decreasing impact
        for (int i = 0; i < octaves; i++)
        {
            v += noise.snoise(pos * f) * a;
            ampsum += a;

            f *= lacunarity;  // usually 2.0
            a *= gain;    // usually 0.5
        }

        if (ampsum == 0)
            return 0;
        //Redistubute using a power function for higher/lower contrast
        return (float) Math.Pow(Math.Abs(v / ampsum), redistrobution);
    }
}