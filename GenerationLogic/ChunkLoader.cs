using System;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class ChunkLoader : MonoBehaviour
{
    public Transform player;
    Vector2Int playerPosition;

    public GameObject chunkPrefab;

    [Header("Chunk Settings")]
    // Must match terrain heightmap resolution
    [Range(2, 128)]
    public int viewDistanceInChunks = 2;
    public int chunkSize = 64;
    public int chunkResolution = 64;
    public int textureResolution = 64;
    public Transform parentFolder;
    [Range(0f,0.5f)]
    public float overlap;
    [Header("Terrain Generation")]
    public float amplitude = 1f;
    public float maxAmplitude = 10f;
    public float frequency = 1f;
    public float lacunarity = 2f;
    public float gain = 0.5f;
    public int octave = 4;
    public float redistrobution = 1f;

    public int seed = 0;
    
    int[] tris; 
    Texture2D texture;

    NativeArray<float3> vertices;
    NativeArray<int> triangles;
    NativeArray<Color32> colors;
    NativeArray<float> heights;
    List<ChunkJob> activeJobs = new List<ChunkJob>();

    private Dictionary<Vector2Int, GameObject> loadedChunks = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> allGeneratedChunks = new Dictionary<Vector2Int, GameObject>();

    private List<Vector2Int> keystoremove = new List<Vector2Int>();

    // Queues to stagger chunk generation across frames
    private Queue<Vector2Int> chunkGenerationQueue = new Queue<Vector2Int>();
    private HashSet<Vector2Int> queued = new HashSet<Vector2Int>();

    [Header("Streaming Settings")]
    [Tooltip("Maximum number of high-res chunks to generate per tick. Set to 1 for sequential generation.")]
    public int chunksPerTick = 1;
    [Tooltip("Maximum number of low-res chunks to generate per tick.")]
    public int lowResChunksPerTick = 2;




    void Start()
    {
        generateTriangleArray();
        playerPosition = new Vector2Int(Mathf.FloorToInt(player.position.x / chunkSize), Mathf.FloorToInt(player.position.z / chunkSize));

        System.Random rand = new System.Random(seed);
        seed = rand.Next();
    }

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
                
                GenerateTexture(job);
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

                if(dist < viewDistanceInChunks && !loadedChunks.ContainsKey(t))
                {
                    EnqueueChunk(t);
                }
            }
        }
    }

    private void EnqueueChunk(Vector2Int pos)
    {
        if (queued.Contains(pos)) return;
        queued.Add(pos);
        chunkGenerationQueue.Enqueue(pos);
    }

    private void ProcessGenerationQueues()
    {
        // Process a limited number of high-res chunks first
        int processed = 0;
        while (processed < chunksPerTick && chunkGenerationQueue.Count > 0)
        {
            var pos = chunkGenerationQueue.Dequeue();
            queued.Remove(pos);
            // double-check we still need it
            if (!loadedChunks.ContainsKey(pos))
            {
                SpawnChunk(pos);
            }
            processed++;
        }
    }

    private void unloadChunks()
    {

        foreach(var chunk in loadedChunks)
        {
            int relx = Mathf.Abs(chunk.Key.x - playerPosition.x);
            int relz = Mathf.Abs(chunk.Key.y - playerPosition.y);

            double dist = Math.Sqrt(relx*relx + relz*relz);

            if(dist >= viewDistanceInChunks)
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
    }

    private void GenerateChunk(Vector2Int pos, Chunk chunk)
    {
        int vertCount = chunkResolution * chunkResolution;

        vertices = new NativeArray<float3>(vertCount, Allocator.TempJob);

        TerrainJob job = new TerrainJob
        {
            chunkSize = chunkSize,
            resolution = chunkResolution,
            frequency = frequency,
            amplitude = amplitude,
            maxAmplitude = maxAmplitude,
            gain = gain,
            lacunarity = lacunarity,
            octaves = octave,
            redistrobution = redistrobution,
            chunkPosition = new float2(pos.x,pos.y),
            vertices = vertices
        };

        JobHandle handle = job.Schedule(vertCount, 64);

        activeJobs.Add(new ChunkJob
        {
            handle = handle,
            vertices = vertices,
            chunk = chunk,
            position = new float2(pos.x,pos.y)
        });
    }

    private void GenerateTexture(ChunkJob job)
    {
        heights = new NativeArray<float>(job.vertices.Length, Allocator.TempJob);
        
        for(int i = 0; i < heights.Length; i++)
            heights[i] = job.vertices[i].y;

        int pixelCount = textureResolution * textureResolution;
        colors = new NativeArray<Color32>(pixelCount, Allocator.TempJob);

        TextureJob texJob = new TextureJob
        {
            chunkPos = job.position,
            terrainResolution = chunkResolution,
            textureResolution = textureResolution,
            maxAmplitude = maxAmplitude,
            colors = colors,
            heights = heights
        };

        JobHandle handle = texJob.Schedule(pixelCount, 64);
        handle.Complete();
        
        texture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);

        Color32[] col = new Color32[colors.Length];

        for(int i = 0; i < colors.Length; i++)
            col[i] = colors[i];

        texture.SetPixels32(col);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply(false);

        colors.Dispose();
        heights.Dispose();
    }


    void ApplyMesh(ChunkJob job)
    {
        Vector3[] verts = new Vector3[job.vertices.Length];

        for (int i = 0; i < verts.Length; i++)
            verts[i] = job.vertices[i];

        job.chunk.ApplyMesh(verts, tris, texture);
    }

    private void SpawnChunk(Vector2Int coord)
    {
        if(!allGeneratedChunks.ContainsKey(coord))
        {
            Vector3 pos = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);

            GameObject obj = Instantiate(chunkPrefab, pos, Quaternion.identity);
            obj.transform.parent = parentFolder;

            Chunk chunk = obj.GetComponent<Chunk>();

            GenerateChunk(coord, chunk);

            loadedChunks.Add(coord, obj);
            allGeneratedChunks.Add(coord, obj);
        } else
        {
            loadedChunks.Add(coord, allGeneratedChunks[coord]);
            allGeneratedChunks[coord].SetActive(true);

        }
    }

    private void generateTriangleArray()
    {
        int triCount = (chunkResolution - 1) * (chunkResolution - 1) * 6;
        triangles = new NativeArray<int>(triCount, Allocator.TempJob);

        TriangleJob trijob = new TriangleJob
        {
            chunkResolution = chunkResolution,
            triangles = triangles
        };

        JobHandle trihandle = trijob.Schedule(triCount / 6, 64);

        trihandle.Complete();

        tris = new int[triangles.Length];
        for(int i = 0; i < tris.Length; i++) tris[i] = triangles[i];

        triangles.Dispose();
    }

    void regenerateTerrain()
    {
        loadedChunks.Clear();
        foreach(var chunk in allGeneratedChunks)
        {
            Destroy(chunk.Value);
        }
        allGeneratedChunks.Clear();
    }
}