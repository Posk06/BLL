using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class TreeJobSystem : MonoBehaviour
{
    int chunkResolution = 64;
    int chunkSize = 64;
    BiomeData biomeData;

    NativeArray<Vector2Int> treepoints;
    List<TreeJob> activeTreeJobs = new List<TreeJob>();


    public void Init(int chunkResolution, int chunkSize, BiomeData biomeData)
    {
        this.chunkResolution = chunkResolution;
        this.chunkSize = chunkSize;
        this.biomeData = biomeData;
    }

    void Start()
    {
    }

    void Update()
    {
        updateJob();
    }

    public void GenerateTreePoints(TexJob job)
    {
        int res = chunkResolution;
        
        treepoints = new NativeArray<Vector2Int>(res * res, Allocator.TempJob);
        
        PoissonDiscJob treeJob = new PoissonDiscJob
        {
            width = res,
            height = res,
            minRadius = 32f,
            maxRadius = 64f,
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
        });
    }

    void ApplyTrees(TreeJob job)
    {
        int texRes = Mathf.FloorToInt(Mathf.Sqrt(job.colorIndices.Length));
        float sizing = (float) texRes / (float) chunkResolution;
        var points = job.pointsOut;
        foreach (var point in points)
        {
            if (point.Equals(Vector2Int.zero)) continue; // No tree points generated (or unused slot)
            int tx = Mathf.FloorToInt(point.x * sizing);
            int ty = Mathf.FloorToInt(point.y * sizing);
            int index = tx + ty * texRes;
            if (index < 0 || index >= job.colorIndices.Length) continue;
            int colorIdx = job.colorIndices[index];
            if (biomeData.biomes[colorIdx].tree != null)
            {
                float height = job.heights[index];
                Vector3 pos = new Vector3(point.x + job.position.x * chunkSize, height, point.y + job.position.y * chunkSize);
                Instantiate(biomeData.biomes[colorIdx].tree, pos, Quaternion.identity);
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