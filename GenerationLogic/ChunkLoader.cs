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
using UnityEngine.XR;

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
    
    Texture2D texture;

    NativeArray<float3> vertices;
    NativeArray<int> trianglesA, trianglesB, trianglesC, trianglesD;
    NativeArray<Color32> colors;
    List<ChunkJob> activeJobs = new List<ChunkJob>();
    List<TexJob> activeTexJobs = new List<TexJob>();

    private Dictionary<Vector2Int, GameObject> loadedChunks = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject>[] allGeneratedChunks = new Dictionary<Vector2Int, GameObject>[4];

    private List<Vector2Int> keystoremove = new List<Vector2Int>();


    // Queues to stagger chunk generation across frames
    private Queue<CoordDistRelation> chunkGenerationQueue = new Queue<CoordDistRelation>();
    private HashSet<CoordDistRelation> queued = new HashSet<CoordDistRelation>();

    [Header("Streaming Settings")]
    [Tooltip("Maximum number of high-res chunks to generate per tick. Set to 1 for sequential generation.")]
    public int chunksPerTick = 1;
    [Tooltip("Maximum number of low-res chunks to generate per tick.")]
    public int lowResChunksPerTick = 2;




    void Start()
    {
        allGeneratedChunks[(int) LOD.NEAR] = new Dictionary<Vector2Int, GameObject>();
        allGeneratedChunks[(int) LOD.MIDDLE] = new Dictionary<Vector2Int, GameObject>();
        allGeneratedChunks[(int) LOD.FAR] = new Dictionary<Vector2Int, GameObject>();
        allGeneratedChunks[(int) LOD.VERY_FAR] = new Dictionary<Vector2Int, GameObject>();

        generateTriangleArrays();
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

        for(int i = activeTexJobs.Count - 1; i >= 0; i--)
        {
            var job = activeTexJobs[i];

            if (job.handle.IsCompleted)
            {
                job.handle.Complete();
                
                MakeTexture(job);
                ApplyTexture(job);

                job.colors.Dispose();
                job.heights.Dispose();

                activeTexJobs.RemoveAt(i);
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
                float dist = Mathf.Sqrt(x*x + z*z);

                if(dist < viewDistanceInChunks && !loadedChunks.ContainsKey(t))
                {
                    if(viewDistanceInChunks > 2000 / chunkSize)
                    {
                        EnqueueChunk(t, LOD.VERY_FAR);
                    } else if(viewDistanceInChunks > 1000 / chunkSize)
                    {
                        EnqueueChunk(t, LOD.FAR);
                    } else if(viewDistanceInChunks > 500 / chunkSize)
                    {
                        EnqueueChunk(t, LOD.MIDDLE);
                    } else
                    {
                        EnqueueChunk(t, LOD.NEAR);
                    }
                }
            }
        }
    }

    private void EnqueueChunk(Vector2Int pos, LOD dist)
    {
        if (queued.Contains(new CoordDistRelation(pos, dist))) return;
        queued.Add(new CoordDistRelation(pos, dist));
        chunkGenerationQueue.Enqueue(new CoordDistRelation(pos, dist));
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
            if (!loadedChunks.ContainsKey(pos.coord))
            {
                SpawnChunk(pos.coord, pos.lod);
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
            int count = chunk.Value.gameObject.GetComponent<MeshFilter>().mesh.vertexCount;

            if(dist >= viewDistanceInChunks)
            {
                chunk.Value.SetActive(false);
                keystoremove.Add(chunk.Key);
            } else if(dist >= 2000f / (float) chunkSize && count == countCompare(0.25f))
            {
                chunk.Value.SetActive(false);
                keystoremove.Add(chunk.Key);
                EnqueueChunk(chunk.Key, LOD.VERY_FAR);
            } else if( dist >= 1000f / (float) chunkSize && (count == countCompare(0.125f) || count == countCompare(0.5f)))
            {
                chunk.Value.SetActive(false);
                keystoremove.Add(chunk.Key);
                EnqueueChunk(chunk.Key, LOD.FAR);
            } else if(dist >= 500f / (float) chunkSize && (count == countCompare(0.25f) || count == countCompare(1f)))
            {
                chunk.Value.SetActive(false);
                keystoremove.Add(chunk.Key);
                EnqueueChunk(chunk.Key, LOD.MIDDLE);
            } else if(count == countCompare(0.5f))
            {
                chunk.Value.SetActive(false);
                keystoremove.Add(chunk.Key);
                EnqueueChunk(chunk.Key, LOD.NEAR);
            } else {}
        }
        
        foreach(var key in keystoremove)
        {
            loadedChunks.Remove(key);
        }
        keystoremove.Clear();

        float countCompare(float factor)
        {
            return chunkResolution * chunkResolution * factor * factor;
        }
    }

    private void GenerateChunk(Vector2Int pos, Chunk chunk, LOD distance)
    {

        int resolution = chunkResolution;

        if((int) distance == 0)
        {} 
        else if((int) distance == 1)
        {
            resolution = Mathf.FloorToInt(chunkResolution * 0.5f);
        } else if((int) distance == 2)
        {
            resolution = Mathf.FloorToInt(chunkResolution * 0.25f);
        } else
        {
            resolution = Mathf.FloorToInt(chunkResolution * 0.125f);
        }

        int vertCount = resolution * resolution;

        vertices = new NativeArray<float3>(vertCount, Allocator.TempJob);

        TerrainJob job = new TerrainJob
        {
            chunkSize = chunkSize,
            resolution = resolution,
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
        NativeArray<float> heights = new NativeArray<float>(job.vertices.Length, Allocator.TempJob);
        
        for (int i = 0; i < heights.Length; i++)
            heights[i] = job.vertices[i].y;


        int pixelCount = textureResolution * textureResolution;
        colors = new NativeArray<Color32>(pixelCount, Allocator.TempJob);

        TextureJob texJob = new TextureJob
        {
            chunkPos = job.position,
            terrainResolution = Mathf.FloorToInt(Mathf.Sqrt(heights.Length)),
            textureResolution = textureResolution,
            maxAmplitude = maxAmplitude,
            colors = colors,
            heights = heights
        };

        JobHandle handle = texJob.Schedule(pixelCount, 64);

        activeTexJobs.Add(new TexJob{
            handle = handle,
            colors = colors,
            chunk = job.chunk,
            position = job.position,
            heights = heights
        });
    }

    private void MakeTexture(TexJob job)
    {
        texture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);

        Color32[] col = new Color32[job.colors.Length];

        for(int i = 0; i < job.colors.Length; i++)
            col[i] = job.colors[i];

        texture.SetPixels32(col);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply(false);
    }

    void ApplyMesh(ChunkJob job)
    {
        if(Mathf.FloorToInt(Mathf.Sqrt(job.vertices.Length)) > chunkResolution * 0.5f)
        {
            job.chunk.ApplyMesh(job.vertices, trianglesA);
        } else if(Mathf.FloorToInt(Mathf.Sqrt(job.vertices.Length)) > chunkResolution * 0.25f)
        {
            job.chunk.ApplyMesh(job.vertices, trianglesB);
        } else if(Mathf.FloorToInt(Mathf.Sqrt(job.vertices.Length)) > chunkResolution * 0.125f)
        {
            job.chunk.ApplyMesh(job.vertices, trianglesC);
        } else
        {
            job.chunk.ApplyMesh(job.vertices, trianglesD);
        }
    }

    void ApplyTexture(TexJob job)
    {
        job.chunk.ApplyTexture(texture);
    }

    private void SpawnChunk(Vector2Int coord, LOD dist)
    {
        if(!allGeneratedChunks[(int) dist].ContainsKey(coord))
        {
            Vector3 pos = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);

            GameObject obj = Instantiate(chunkPrefab, pos, Quaternion.identity);
            obj.transform.parent = parentFolder;

            Chunk chunk = obj.GetComponent<Chunk>();

            GenerateChunk(coord, chunk, dist);

            loadedChunks.Add(coord, obj);
            allGeneratedChunks[(int) dist].Add(coord, obj);
        } else
        {
            loadedChunks.Add(coord, allGeneratedChunks[(int) dist][coord]);
            allGeneratedChunks[(int) dist][coord].SetActive(true);

        }
    }

    private void generateTriangleArrays()
    {
        ArrayA();
        ArrayB();
        ArrayC();
        ArrayD();

        void ArrayA()
        {
            int resolution = chunkResolution;
            int triCount = (resolution - 1) * (resolution - 1) * 6;
            trianglesA = new NativeArray<int>(triCount, Allocator.Persistent);

            TriangleJob trijob = new TriangleJob
            {
                chunkResolution = resolution,
                triangles = trianglesA
            };

            JobHandle trihandle = trijob.Schedule((resolution - 1) * (resolution - 1), 64);
            trihandle.Complete();
        }

        void ArrayB()
        {
            int resolution = Mathf.FloorToInt(chunkResolution * 0.5f);
            int triCount = (resolution - 1) * (resolution - 1) * 6;
            trianglesB = new NativeArray<int>(triCount, Allocator.Persistent);

            TriangleJob trijob = new TriangleJob
            {
                chunkResolution = resolution,
                triangles = trianglesB
            };

            JobHandle trihandle = trijob.Schedule((resolution - 1) * (resolution - 1), 64);
            trihandle.Complete();
        }

        void ArrayC()
        {
            int resolution = Mathf.FloorToInt(chunkResolution * 0.25f);
            int triCount = (resolution - 1) * (resolution - 1) * 6;
            trianglesC = new NativeArray<int>(triCount, Allocator.Persistent);

            TriangleJob trijob = new TriangleJob
            {
                chunkResolution = resolution,
                triangles = trianglesC
            };

            JobHandle trihandle = trijob.Schedule((resolution - 1) * (resolution - 1), 64);
            trihandle.Complete();
        }

        void ArrayD()
        {
            int resolution = Mathf.FloorToInt(chunkResolution * 0.125f);
            int triCount = (resolution - 1) * (resolution - 1) * 6;
            trianglesD = new NativeArray<int>(triCount, Allocator.Persistent);

            TriangleJob trijob = new TriangleJob
            {
                chunkResolution = resolution,
                triangles = trianglesD
            };

            JobHandle trihandle = trijob.Schedule((resolution - 1) * (resolution - 1), 64);
            trihandle.Complete();
        }
    }
}
public class CoordDistRelation
{
    public Vector2Int coord;
    public LOD lod;

     public CoordDistRelation(Vector2Int coord, LOD lod)
    {
        this.coord = coord;
        this.lod = lod;
    }
}

public enum LOD
{
    NEAR,
    MIDDLE,
    FAR,
    VERY_FAR
}