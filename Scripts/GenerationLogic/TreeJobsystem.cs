//--------------------------------------------
//This code manages the tree point jobs
//--------------------------------------------
// - Oskar Benjamin Trillitzsch

using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class TreeJobSystem : MonoBehaviour
{
    int chunkResolution = 32;
    int chunkSize = 16;
    BiomeData biomeData;

    NativeArray<int2> treepoints;
    List<TreeJob> activeTreeJobs = new List<TreeJob>();

    List<GameObject>[] treePool = new List<GameObject>[0];
    public int treePoolSize = 100;
    public int maxDistance;
    public int minDistance;
    public int maxTreesPerChunk = 50;
    public Transform treeParent;
    public LayerMask whatIsGround;

    public static TreeJobSystem Instance { get; private set; }

    public void Init(int chunkResolution, int chunkSize, BiomeData biomeData)
    {
        this.chunkResolution = chunkResolution;
        this.chunkSize = chunkSize;
        this.biomeData = biomeData;

        treePool = new List<GameObject>[biomeData.biomes.Count];

        //Create a tree pool for each biome type
        for (int i = 0; i < biomeData.biomes.Count; i++) //AI
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

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        updateJob();
    }

    public void GenerateTreePoints(TexJob job)
    {
        
        treepoints = new NativeArray<int2>(maxTreesPerChunk, Allocator.TempJob);
        
        //Assign values to job
        PoissonDiscJob treeJob = new PoissonDiscJob
        {
            width = chunkSize,
            height = chunkSize,
            minRadius = minDistance,
            maxRadius = maxDistance,
            k = 30,
            pointsOut = treepoints,
            chunkSize = chunkSize,
            position = job.position,
            moistures = job.moistures,
            maxTrees = maxTreesPerChunk
        };

        JobHandle handle = treeJob.Schedule();

        //Store job data for later retrieval
        activeTreeJobs.Add(new TreeJob
        {
            handle = handle,
            pointsOut = treepoints,
            colorIndices = job.colorIndices,
            heights = job.heights,
            position = job.position,
            chunk = job.chunk,
            generationId = job.generationId,
            moistures = job.moistures
        });
    }

    void ApplyTrees(TreeJob job)
    {
        var points = job.pointsOut;

        // If there are no valid points, let the caller dispose the arrays and return
        bool hasValid = false;
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i].x >= 0 && points[i].y >= 0) { hasValid = true; break; }
        }
        if (!hasValid) return;

        // Ensure chunk wasn't reused while tree points were being generated
        if (job.chunk == null || job.chunk.generationId != job.generationId)
        {
            return;
        }

        //This Code was partly genrated by AI, but was also modified and optimized by me
        //Cycle through each generated tree point spawning a tree there, skip invalid markers
        foreach (var point in points)
        {
            if (point.x < 0 || point.y < 0) continue; // skip unused/invalid points

            int textureResolution = Mathf.FloorToInt(Mathf.Sqrt(job.colorIndices.Length));
            float textureScale = (textureResolution - 1) / (float)chunkSize;
            int tx = Mathf.Clamp(Mathf.FloorToInt(point.x * textureScale), 0, textureResolution - 1);
            int ty = Mathf.Clamp(Mathf.FloorToInt(point.y * textureScale), 0, textureResolution - 1);
            int textureIndex = ty * textureResolution + tx;

            int resolution = Mathf.FloorToInt(Mathf.Sqrt(job.heights.Length));

            float height = SampleHeightBilinear(
                point.x,
                point.y,
                job.heights,
                resolution,
                chunkSize
            );

            if(biomeData.biomes[job.colorIndices[textureIndex]].tree != null)
            {
                Vector3 spawnPos = new Vector3(job.position.x * chunkSize + point.x, height, job.position.y * chunkSize + point.y);
                if(treePool[job.colorIndices[textureIndex]].Count == 0)
                {
                    Instantiate(biomeData.biomes[job.colorIndices[textureIndex]].tree, spawnPos, Quaternion.identity, job.chunk.transform);
                } else
                {
                    GameObject tree = treePool[job.colorIndices[textureIndex]][0];
                    tree.transform.position = spawnPos;
                    tree.SetActive(true);
                    tree.transform.SetParent(job.chunk.transform);
                    treePool[job.colorIndices[textureIndex]].RemoveAt(0);
                }
            }
        }
    }

    void updateJob()
    {
        for (int i = activeTreeJobs.Count - 1; i >= 0; i--)
        {   
            //Check if a job has finished
            if (activeTreeJobs[i].handle.IsCompleted)
            {
                activeTreeJobs[i].handle.Complete();

                //Spawn trees
                ApplyTrees(activeTreeJobs[i]);

                activeTreeJobs[i].colorIndices.Dispose();
                activeTreeJobs[i].heights.Dispose();
                activeTreeJobs[i].pointsOut.Dispose();
                activeTreeJobs[i].moistures.Dispose();
                activeTreeJobs.RemoveAt(i);
            }
        }
    }

    public void ReturnTreeToPool(GameObject tree)
    {
        //Return tree to pool based on biome index
        foreach(var biome in biomeData.biomes)
        {
            if(biome.tree != null && tree.name.Contains(biome.tree.name))
            {
                tree.transform.SetParent(treeParent);
                int biomeIndex = biomeData.biomes.IndexOf(biome);
                treePool[biomeIndex].Add(tree);
                break;
            }
        }
    }
    float SampleHeightBilinear(
    float localX, 
    float localZ, 
    NativeArray<float> heights, 
    int resolution, 
    float chunkSize)
{
    // Convert from world (0 → chunkSize) to grid (0 → resolution-1)
    float percentX = localX / chunkSize;
    float percentZ = localZ / chunkSize;

    float gridX = percentX * (resolution - 1);
    float gridZ = percentZ * (resolution - 1);

    int x0 = Mathf.FloorToInt(gridX);
    int x1 = Mathf.Min(x0 + 1, resolution - 1);
    int z0 = Mathf.FloorToInt(gridZ);
    int z1 = Mathf.Min(z0 + 1, resolution - 1);

    float tx = gridX - x0; // interpolation factor X
    float tz = gridZ - z0; // interpolation factor Z

    // Sample 4 surrounding points
    float h00 = heights[z0 * resolution + x0];
    float h10 = heights[z0 * resolution + x1];
    float h01 = heights[z1 * resolution + x0];
    float h11 = heights[z1 * resolution + x1];

    // Interpolate along X
    float hx0 = Mathf.Lerp(h00, h10, tx);
    float hx1 = Mathf.Lerp(h01, h11, tx);

    // Interpolate along Z
    float h = Mathf.Lerp(hx0, hx1, tz);

    return h;
}
}