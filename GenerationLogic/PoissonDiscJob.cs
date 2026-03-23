using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]

public struct PoissonDiscJob : IJob
{

    public int width;
    public int height;
    public float minRadius;
    public float maxRadius;
    public int k;
    public NativeArray<int2> pointsOut;
    public int chunkSize;
    public float2 position;


    public void Execute()
    {
        NativeArray<int2> spawnPoints = new NativeArray<int2>(width * height, Allocator.Temp);
        NativeArray<int2> points = new NativeArray<int2>(width * height, Allocator.Temp);
        Unity.Mathematics.Random random = new Unity.Mathematics.Random(1234);

        spawnPoints[0] = new int2(width / 2, height / 2);
        int spawnCount = 1;
        int pointCount = 0;

        while (spawnCount > 0)
        {
            int spawnIndex = random.NextInt(0, spawnCount);
            int2 spawnCenter = spawnPoints[spawnIndex];
            bool candidateAccepted = false;
            float noiseValue = noise.snoise(new float2(spawnCenter.x + position.x * chunkSize, spawnCenter.y + position.y * chunkSize) * 0.1f);

            for (int i = 0; i < k; i++)
            {
                float angle = random.NextFloat(0, Mathf.PI * 2);
                float noiseRadius = minRadius + noiseValue * (maxRadius - minRadius);
                float distance = random.NextFloat(noiseRadius, 2 * noiseRadius);
                int2 candidate = spawnCenter + new int2(Mathf.RoundToInt(Mathf.Cos(angle) * distance), Mathf.RoundToInt(Mathf.Sin(angle) * distance));

                if (candidate.x >= 0 && candidate.x < width && candidate.y >= 0 && candidate.y < height)
                {
                    bool valid = true;
                    foreach (var point in points)
                    {
                        if ((point - candidate).x * (point - candidate).x + (point - candidate).y * (point - candidate).y < noiseRadius * noiseRadius)
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (valid)
                    {
                        points[pointCount] = candidate;
                        spawnPoints[spawnCount] = candidate;
                        pointCount++;
                        spawnCount++;
                        candidateAccepted = true;
                        break;
                    }
                }
            }

            if (!candidateAccepted)
            {
                spawnPoints[spawnIndex] = new int2(0, 0); // Mark as invalid
                spawnCount--;
            }
        }

        for(int i = 0; i < pointCount; i++)
        {
            if(i < points.Length)
            {
                pointsOut[i] = points[i];
            }
            else
            {
                pointsOut[i] = new int2(0, 0); // No more points to generate
            }
        }

        spawnPoints.Dispose();
        points.Dispose();
    }
}