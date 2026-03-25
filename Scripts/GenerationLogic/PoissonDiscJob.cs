//----------AI GENERATED CODE-----------------
//This code generates an array of points in an semi-avrage pattern to spawn objects naturally
//It uses noise to create natural clusters and moisture in the case of trees
//--------------------------------------------
// - Oskar Benjamin Trillitzsch

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
    public int maxTrees;
    public int k;
    public NativeArray<int2> pointsOut;
    [ReadOnly] public NativeArray<float> moistures;
    public int chunkSize;
    public float2 position;


    public void Execute()
    {
        NativeArray<int2> spawnPoints = new NativeArray<int2>(maxTrees, Allocator.Temp);
        NativeArray<int2> points = new NativeArray<int2>(maxTrees, Allocator.Temp);
        Unity.Mathematics.Random random = new Unity.Mathematics.Random(1234);

        //Set starting point in the middle of the chunk
        spawnPoints[0] = new int2(width / 2, height / 2);
        int spawnCount = 1;
        int pointCount = 0;

        //Cycle trough all spawn points and try to spawn new points around them, if it fails after k tries, remove the spawn point
        //If a new point gets generated try to spawn around it as well
        while (spawnCount > 0)
        {
            int spawnIndex = random.NextInt(0, spawnCount);
            int2 spawnCenter = spawnPoints[spawnIndex];
            bool candidateAccepted = false;
            float noiseValue = (noise.snoise(new float2(spawnCenter.x + position.x * chunkSize, spawnCenter.y + position.y * chunkSize) * 0.1f) * 0.5f + 0.5f + moistures[spawnCenter.y * width + spawnCenter.x]) / 2f;

            for (int i = 0; i < k; i++)
            {
                float angle = random.NextFloat(0, Mathf.PI * 2);
                float noiseRadius = minRadius + noiseValue * (maxRadius - minRadius);
                float distance = random.NextFloat(noiseRadius, 2 * noiseRadius);
                int2 candidate = spawnCenter + new int2(Mathf.RoundToInt(Mathf.Cos(angle) * distance), Mathf.RoundToInt(Mathf.Sin(angle) * distance));

                    if (candidate.x >= 0 && candidate.x < width && candidate.y >= 0 && candidate.y < height)
                    {
                        bool valid = true;
                        for (int p = 0; p < pointCount; p++)
                        {
                            var point = points[p];
                            int dx = point.x - candidate.x;
                            int dy = point.y - candidate.y;
                            if (dx * dx + dy * dy < noiseRadius * noiseRadius)
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
                spawnCount--;
            }

            if(pointCount >= maxTrees)
            {
                break; // Reached maximum tree count
            }
        }

        // Write out generated points; mark unused slots as invalid (-1,-1)
        for (int i = 0; i < pointsOut.Length; i++)
        {
            if (i < pointCount)
            {
                pointsOut[i] = points[i];
            }
            else
            {
                pointsOut[i] = new int2(-1, -1);
            }
        }

        spawnPoints.Dispose();
        points.Dispose();
    }
}