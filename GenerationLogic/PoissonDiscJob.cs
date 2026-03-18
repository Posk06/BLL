using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]

public struct PoissonDiscJob : IJob
{

    public int width;
    public int height;
    public float radius;
    public int k;
    public NativeArray<Vector2Int> pointsOut;


    public void Execute()
    {
        List<Vector2Int> points = new List<Vector2Int>();
        List<Vector2Int> spawnPoints = new List<Vector2Int>();
        Unity.Mathematics.Random random = new Unity.Mathematics.Random(1234);

        spawnPoints.Add(new Vector2Int(width / 2, height / 2));

        while (spawnPoints.Count > 0)
        {
            int spawnIndex = random.NextInt(0, spawnPoints.Count);
            Vector2Int spawnCenter = spawnPoints[spawnIndex];
            bool candidateAccepted = false;

            for (int i = 0; i < k; i++)
            {
                float angle = random.NextFloat(0, Mathf.PI * 2);
                float distance = random.NextFloat(radius, 2 * radius);
                Vector2Int candidate = spawnCenter + new Vector2Int(Mathf.RoundToInt(Mathf.Cos(angle) * distance), Mathf.RoundToInt(Mathf.Sin(angle) * distance));

                if (candidate.x >= 0 && candidate.x < width && candidate.y >= 0 && candidate.y < height)
                {
                    bool valid = true;
                    foreach (var point in points)
                    {
                        if ((point - candidate).sqrMagnitude < radius * radius)
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (valid)
                    {
                        points.Add(candidate);
                        spawnPoints.Add(candidate);
                        candidateAccepted = true;
                        break;
                    }
                }
            }

            if (!candidateAccepted)
            {
                spawnPoints.RemoveAt(spawnIndex);
            }
        }

        for(int i = 0; i < pointsOut.Length; i++)
        {
            if(i < points.Count)
            {
                pointsOut[i] = points[i];
            }
            else
            {
                pointsOut[i] = new Vector2Int(0, 0); // No more points to generate
            }
        }
    }
}