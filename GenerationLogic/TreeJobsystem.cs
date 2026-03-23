using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class TreeJobSystem : MonoBehaviour
{
    int chunkResolution = 64;
    int chunkSize = 64;
    BiomeData biomeData;

    NativeArray<int2> treepoints;
    List<TreeJob> activeTreeJobs = new List<TreeJob>();

    List<GameObject>[] treePool = new List<GameObject>[0];
    public int treePoolSize = 100;
    public Transform treeParent;

    public void Init(int chunkResolution, int chunkSize, BiomeData biomeData)
    {
        this.chunkResolution = chunkResolution;
        this.chunkSize = chunkSize;
        this.biomeData = biomeData;

        treePool = new List<GameObject>[biomeData.biomes.Count];
        for (int i = 0; i < biomeData.biomes.Count; i++)
        {
            treePool[i] = new List<GameObject>();
            for(int j = 0; j < treePoolSize; j++)
            {
                if(biomeData.biomes[i].tree == null) break;
                var g = Instantiate(biomeData.biomes[i].tree);
                g.SetActive(false);
                treePool[i].Add(g);
                g.transform.SetParent(treeParent);
            }
        }
    }

    void Update()
    {
        updateJob();
    }

    public void GenerateTreePoints(TexJob job)
    {
        int res = chunkResolution;
        
        treepoints = new NativeArray<int2>(res * res, Allocator.TempJob);
        
        PoissonDiscJob treeJob = new PoissonDiscJob
        {
            width = res,
            height = res,
            minRadius = 16f,
            maxRadius = 32f,
            k = 30,
            pointsOut = treepoints,
            chunkSize = chunkSize,
            position = job.position
        };

        JobHandle handle = treeJob.Schedule();

        activeTreeJobs.Add(new TreeJob
        {
            handle = handle,
            pointsOut = treepoints,
            colorIndices = job.colorIndices,
            heights = job.heights,
            position = job.position,
            chunk = job.chunk
        });
    }

    void ApplyTrees(TreeJob job)
    {
        int texRes = Mathf.FloorToInt(Mathf.Sqrt(job.colorIndices.Length));
        float sizing = (float) texRes / (float) chunkResolution;
        var points = job.pointsOut;

        if(points.Length == 0)
        {
            job.colorIndices.Dispose();
            job.heights.Dispose(); 
            return;
        }

        foreach (var point in points)
        {
            if (point.Equals(int2.zero)) continue; // No tree points generated (or unused slot)

            int tx = Mathf.FloorToInt(point.x * sizing);
            int ty = Mathf.FloorToInt(point.y * sizing);
            int index = tx + ty * texRes;

            if (index < 0 || index >= job.colorIndices.Length) continue;
            int colorIdx = job.colorIndices[index];

            if (biomeData.biomes[colorIdx].tree != null)
            {
                float height = job.heights[index];
                Vector3 pos = new Vector3(point.x + job.position.x * chunkSize, height, point.y + job.position.y * chunkSize);

                if(treePool[colorIdx].Count > 0)
                {
                    var tree = treePool[colorIdx][0];
                    treePool[colorIdx].RemoveAt(0);
                    tree.SetActive(true);
                    tree.transform.position = pos;
                    tree.transform.SetParent(job.chunk.transform);
                }
                else
                {
                    Instantiate(biomeData.biomes[colorIdx].tree, pos, Quaternion.identity, job.chunk.transform);
                }
            }
        }
        job.colorIndices.Dispose();
        job.heights.Dispose();
    }

    void updateJob()
    {
        for (int i = activeTreeJobs.Count - 1; i >= 0; i--)
        {
            if (activeTreeJobs[i].handle.IsCompleted)
            {
                activeTreeJobs[i].handle.Complete();
                ApplyTrees(activeTreeJobs[i]);
                activeTreeJobs[i].pointsOut.Dispose();
                activeTreeJobs.RemoveAt(i);
            }
        }
    }
}