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
            moistures = job.moistures
        });
    }

    void ApplyTrees(TreeJob job)
    {
        var points = job.pointsOut;

        if(points.Length == 0)
        {
            job.colorIndices.Dispose();
            job.heights.Dispose(); 
            return;
        }

        //Cycling trough each generated tree point spawning a tree there, again based on the biome index
        foreach (var point in points)
        {
            int heightResolution = Mathf.FloorToInt(Mathf.Sqrt(job.heights.Length));
            float heightsizing = (float) heightResolution / chunkSize;
            int textureResolution = Mathf.FloorToInt(Mathf.Sqrt(job.colorIndices.Length));
            float texturesizing = (float) textureResolution / chunkSize;

            int textureIndex = Mathf.FloorToInt(point.y * texturesizing * textureResolution + point.x * texturesizing);
            int heightIndex = Mathf.FloorToInt(point.y * heightsizing * (heightResolution - 1) + point.x * heightsizing);

            if(biomeData.biomes[job.colorIndices[textureIndex]].tree != null)
            {
                Vector3 spawnPos = new Vector3(job.position.x * chunkSize + point.x, job.heights[heightIndex], job.position.y * chunkSize + point.y);
                if(treePool[job.colorIndices[textureIndex]].Count == 0)
                {
                    Instantiate(biomeData.biomes[job.colorIndices[textureIndex]].tree, spawnPos, Quaternion.identity, treeParent);
                } else
                {
                    GameObject tree = treePool[job.colorIndices[textureIndex]][0];
                    tree.transform.position = spawnPos;
                    tree.SetActive(true);
                    tree.transform.SetParent(treeParent);
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
}