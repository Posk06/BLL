using System;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class ChunkLoader_old : MonoBehaviour
{
    public Transform player;
    Vector2Int playerPosition;

    public GameObject chunkPrefab;
    [Range(1,20)]
    public int ticksinsecond;

    [Header("Chunk Settings")]
    // Must match terrain heightmap resolution
    [Range(2, 128)]
    public int viewDistanceInChunks = 2;
    public int chunkSize = 64;
    public int chunkResolution = 64;
    public int textureResolution = 64;
    public Transform parentFolder;
    [Range(0f,1f)]
    public float lowresDistanceFactor;
    [Range(0f,1f)]
    public float lowresResolutionFactor;
    [Range(0f,0.5f)]
    public float overlap;
    [Header("Terrain Generation")]
    public float amplitude;
    public float maxAmplitude;
    public float frequency;
    public float lacunarity;
    public float gain;
    public int octave;
    public float redistrobution;

    public int seed = 0;

    NativeArray<float3> vertices;
    NativeArray<int> triangles;
    List<ChunkJob> activeJobs = new List<ChunkJob>();

    private Dictionary<Vector2Int, GameObject> loadedChunks = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> allGeneratedChunks = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> loadedLowResChunks = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> allGeneratedLowResChunks = new Dictionary<Vector2Int, GameObject>();

    private List<Vector2Int> keystoremove = new List<Vector2Int>();
    private List<Vector2Int> lowreskeystoremove = new List<Vector2Int>();

    // Queues to stagger chunk generation across frames
    private Queue<Vector2Int> chunkGenerationQueue = new Queue<Vector2Int>();
    private Queue<Vector2Int> lowResGenerationQueue = new Queue<Vector2Int>();
    private HashSet<Vector2Int> queuedHighRes = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> queuedLowRes = new HashSet<Vector2Int>();

    [Header("Streaming Settings")]
    [Tooltip("Maximum number of high-res chunks to generate per tick. Set to 1 for sequential generation.")]
    public int chunksPerTick = 1;
    [Tooltip("Maximum number of low-res chunks to generate per tick.")]
    public int lowResChunksPerTick = 2;




    void Start()
    {
        playerPosition = new Vector2Int(Mathf.FloorToInt(player.position.x / chunkSize), Mathf.FloorToInt(player.position.z / chunkSize));

        System.Random rand = new System.Random(seed);
        seed = rand.Next();
    }

    float time = 0;
    void Update()
    { 
        playerPosition = new Vector2Int(Mathf.FloorToInt(player.position.x / chunkSize), Mathf.FloorToInt(player.position.z / chunkSize));
        loadChunks();
        ProcessGenerationQueues();
        unloadChunks();

        for (int i = activeJobs.Count - 1; i >= 0; i--)
        {
            var job = activeJobs[i];

            if (job.handle.IsCompleted)
            {
                job.handle.Complete();

                ApplyMesh(job);

                job.vertices.Dispose();

                activeJobs.RemoveAt(i);
            }
        }
    }

    private void loadChunks()
    {
        for(int x = -viewDistanceInChunks; x <= viewDistanceInChunks; x++)
        {
            for(int z = -viewDistanceInChunks; z <= viewDistanceInChunks; z++)
            {
                Vector2Int t = playerPosition + new Vector2Int(x,z);
                double dist = Math.Sqrt(x*x + z*z);

                if(dist < viewDistanceInChunks * lowresDistanceFactor && !loadedChunks.ContainsKey(t))
                {
                    if(dist >= viewDistanceInChunks * (lowresDistanceFactor - overlap) && dist <= viewDistanceInChunks && !loadedLowResChunks.ContainsKey(t))
                    {
                        EnqueueChunk(t); //LowRes
                    }
                    EnqueueChunk(t);
                } else if(dist >= viewDistanceInChunks * lowresDistanceFactor && dist <= viewDistanceInChunks && !loadedLowResChunks.ContainsKey(t))
                {
                    EnqueueChunk(t); //LowRES
                }
            }
        }
    }

    private void EnqueueChunk(Vector2Int pos)
    {
        if (queuedHighRes.Contains(pos)) return;
        queuedHighRes.Add(pos);
        chunkGenerationQueue.Enqueue(pos);
    }

    private void ProcessGenerationQueues()
    {
        // Process a limited number of high-res chunks first
        int processed = 0;
        while (processed < chunksPerTick && chunkGenerationQueue.Count > 0)
        {
            var pos = chunkGenerationQueue.Dequeue();
            queuedHighRes.Remove(pos);
            // double-check we still need it
            if (!loadedChunks.ContainsKey(pos))
            {
                SpawnChunk(pos);
            }
            processed++;
        }

        // Process low-res queue
        int processedLow = 0;
        while (processedLow < lowResChunksPerTick && lowResGenerationQueue.Count > 0)
        {
            var pos = lowResGenerationQueue.Dequeue();
            queuedLowRes.Remove(pos);
            if (!loadedLowResChunks.ContainsKey(pos))
            {
                SpawnChunk(pos); //LowRes
            }
            processedLow++;
        }
    }

    private void unloadChunks()
    {

        foreach(var chunk in loadedLowResChunks)
        {
            int relx = Mathf.Abs(chunk.Key.x - playerPosition.x);
            int relz = Mathf.Abs(chunk.Key.y - playerPosition.y);

            double dist = Math.Sqrt(relx*relx + relz*relz);

            if(dist > viewDistanceInChunks || dist < viewDistanceInChunks * (lowresDistanceFactor - overlap))
            {
                chunk.Value.SetActive(false);
                lowreskeystoremove.Add(chunk.Key);
                // Debug.Log("Unloaded Chunk");
            }
        
        }

        foreach(var chunk in loadedChunks)
        {
            int relx = Mathf.Abs(chunk.Key.x - playerPosition.x);
            int relz = Mathf.Abs(chunk.Key.y - playerPosition.y);

            double dist = Math.Sqrt(relx*relx + relz*relz);

            

            if(dist >= viewDistanceInChunks * lowresDistanceFactor)
            {
                chunk.Value.SetActive(false);
                keystoremove.Add(chunk.Key);
            }
            
        }
        
        foreach(var key in keystoremove)
        {
            loadedChunks.Remove(key);
        }
        keystoremove.Clear();

        foreach(var key in lowreskeystoremove)
        {
            loadedLowResChunks.Remove(key);
        }
        lowreskeystoremove.Clear();
    }

    private void GenerateChunk(Vector2Int pos, Chunk chunk)
    {
        if(!allGeneratedLowResChunks.ContainsKey(pos))
        {
            int vertCount = chunkResolution * chunkResolution;
            int triCount = (chunkResolution - 1) * (chunkResolution - 1) * 6;

            vertices = new NativeArray<float3>(vertCount, Allocator.TempJob);
            triangles = new NativeArray<int>(triCount, Allocator.TempJob);

            TerrainJob job = new TerrainJob
            {
                chunkSize = chunkSize,
                frequency = frequency,
                amplitude = amplitude,
                gain = gain,
                lacunarity = lacunarity,
                octaves = octave,
                chunkPosition = new float2(pos.x,pos.y),
                vertices = vertices
            };

            TriangleJob trijob = new TriangleJob
            {
                chunkSize = chunkSize,
                triangles = triangles
            };

            JobHandle handle = job.Schedule(vertCount, 64);
            JobHandle trihandle = job.Schedule(triCount, 64);

            activeJobs.Add(new ChunkJob
            {
                handle = handle,

                vertices = vertices,

                chunk = chunk
            });

            /*GameObject ter = Instantiate(lowResTerrainPrefab != null ? lowResTerrainPrefab : terrainPrefab, new Vector3(pos.x * chunkSize , 0, pos.y * chunkSize), Quaternion.identity);
            ter.name = "LowResChunk_" + pos.x + "_" + pos.y;
            ter.transform.parent = lowResParentFolder;
            ter.GetComponent<ProcedualGenerator>().Init(chunkSize, Mathf.FloorToInt(chunkResolution * lowresResolutionFactor), seed, Mathf.FloorToInt(textureResolution * lowresResolutionFactor));*/
        } else
        {
            allGeneratedLowResChunks[pos].SetActive(true);
            loadedLowResChunks.Add(pos, allGeneratedLowResChunks[pos]);
        }
    }


    void ApplyMesh(ChunkJob job)
    {
        Vector3[] verts = new Vector3[job.vertices.Length];
  

        for (int i = 0; i < verts.Length; i++)
            verts[i] = job.vertices[i];

   

    }

    private void SpawnChunk(Vector2Int coord)
    {
        Vector3 pos = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);

        GameObject obj = Instantiate(chunkPrefab, pos, Quaternion.identity);
        obj.transform.parent = parentFolder;

        Chunk chunk = obj.GetComponent<Chunk>();

        GenerateChunk(coord, chunk);

        loadedLowResChunks.Add(coord, obj);
        allGeneratedLowResChunks.Add(coord, obj);
    }

    void regenerateTerrain()
    {
        loadedChunks.Clear();
        foreach(var chunk in allGeneratedChunks)
        {
            Destroy(chunk.Value);
        }
        allGeneratedChunks.Clear();
        loadedLowResChunks.Clear();
        foreach(var chunk in allGeneratedLowResChunks)
        {
            Destroy(chunk.Value);
        }
        allGeneratedLowResChunks.Clear();
    }
}

