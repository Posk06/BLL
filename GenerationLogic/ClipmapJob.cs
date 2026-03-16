using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
[BurstCompile]
public struct ClipMapJob : IJobParallelFor
{
    public int resolution;
    public int width;
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
        float x = index % resolution;
        float z = index / resolution;
        if(x > width && x > resolution - width && z > width && z < resolution - width) return;

        float2 worldPos = new float2(x,z);

        float height = FractalNoise(worldPos) * maxAmplitude;

        vertices[index] = new float3(x, height, z);
    }

    float FractalNoise(float2 pos)
    {
        if (octaves <= 0)
        {
            // fall back to single‐layer noise
            return noise.snoise(pos * frequency) * amplitude;
        }

        float v = 0;
        float ampsum = 0;

        // work on copies so fields stay constant per vertex
        float f = frequency;
        float a = amplitude;

        for (int i = 0; i < octaves; i++)
        {
            v += noise.snoise(pos * f) * a;
            ampsum += a;

            f *= lacunarity;  // usually 2.0
            a *= gain;    // usually 0.5
        }

        if (ampsum == 0)
            return 0;

        return (float) Math.Pow(Math.Abs(v / ampsum), redistrobution);
    }
}