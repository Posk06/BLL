using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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

    [Header("Thresholds")]
    public int threshold_near_mid = 500;
    public float factorA = 0.5f;
    public int threshold_mid_far = 1000;
    public float factorB = 0.25f;
    public int threshold_far_veryfar = 2000;
    public float factorC = 0.125f;

    private int threshold_near_midSq;
    private int threshold_mid_farSq;
    private int threshold_far_veryfarSq;
    private int viewDistanceinChunksSq;

    
    Texture2D texture;
    Vector2Int lastPlayerChunk = new Vector2Int(100, 100);

    NativeArray<float3> vertices;
    NativeArray<int> trianglesA, trianglesB, trianglesC, trianglesD;
    NativeArray<Color32> colors;
    List<ChunkJob> activeJobs = new List<ChunkJob>();
    List<TexJob> activeTexJobs = new List<TexJob>();

    private Dictionary<Vector2Int, ChunkData> loadedChunks = new Dictionary<Vector2Int, ChunkData>();
    private Dictionary<Vector2Int, ChunkData>[] allGeneratedChunks = new Dictionary<Vector2Int, ChunkData>[4];

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
        allGeneratedChunks[(int) LOD.NEAR] = new Dictionary<Vector2Int, ChunkData>();
        allGeneratedChunks[(int) LOD.MIDDLE] = new Dictionary<Vector2Int, ChunkData>();
        allGeneratedChunks[(int) LOD.FAR] = new Dictionary<Vector2Int, ChunkData>();
        allGeneratedChunks[(int) LOD.VERY_FAR] = new Dictionary<Vector2Int, ChunkData>();

        threshold_near_midSq = Mathf.FloorToInt(Mathf.Pow(threshold_near_mid / chunkSize, 2f));
        threshold_mid_farSq = Mathf.FloorToInt(Mathf.Pow(threshold_mid_far / chunkSize, 2f));
        threshold_far_veryfarSq = Mathf.FloorToInt(Mathf.Pow(threshold_far_veryfar / chunkSize, 2f));
        viewDistanceinChunksSq = Mathf.FloorToInt(Mathf.Pow(viewDistanceInChunks, 2f));


        generateTriangleArrays();
        playerPosition = new Vector2Int(Mathf.FloorToInt(player.position.x / chunkSize), Mathf.FloorToInt(player.position.z / chunkSize));

        System.Random rand = new System.Random(seed);
        seed = rand.Next();
    }

    void Update()
    { 
        playerPosition = new Vector2Int(Mathf.FloorToInt(player.position.x / chunkSize), Mathf.FloorToInt(player.position.z / chunkSize));

        Vector2Int currentChunk = new Vector2Int(
        Mathf.FloorToInt(player.position.x / chunkSize),
        Mathf.FloorToInt(player.position.z / chunkSize)
        );

        if(currentChunk != lastPlayerChunk)
        {
            loadChunks();
            unloadChunks();
            lastPlayerChunk = currentChunk;
        }

        ProcessGenerationQueues();

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
        for (int r = 0; r <= viewDistanceInChunks; r++) 
        {
            for (int x = -r; x <= r; x++)
            {
                for (int z = -r; z <= r; z++)
                {
                    if (Mathf.Abs(x) != r && Mathf.Abs(z) != r)
                        continue;

                    Vector2Int t = playerPosition + new Vector2Int(x,z);
                    float dist = x*x + z*z;

                    if(dist < viewDistanceinChunksSq && !loadedChunks.ContainsKey(t))
                    {
                        if(dist > threshold_far_veryfarSq)
                        {
                            EnqueueChunk(t, LOD.VERY_FAR);
                        } else if(dist > threshold_mid_farSq)
                        {
                            EnqueueChunk(t, LOD.FAR);
                        } else if(dist > threshold_near_midSq)
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
        float processed = 0;
        while (processed < chunksPerTick && chunkGenerationQueue.Count > 0)
        {
            var pos = chunkGenerationQueue.Dequeue();
            queued.Remove(pos);
            // double-check we still need it
            if (!loadedChunks.ContainsKey(pos.coord))
            {
                SpawnChunk(pos.coord, pos.lod);
            }

            processed += Mathf.Pow(0.5f, (int) pos.lod);
        }
    }

    private void unloadChunks()
    {

        foreach(var chunk in loadedChunks)
        {
            int relx = Mathf.Abs(chunk.Key.x - playerPosition.x);
            int relz = Mathf.Abs(chunk.Key.y - playerPosition.y);

            double dist = relx*relx + relz*relz;
            int count = chunk.Value.resolution;

            if(dist >= viewDistanceinChunksSq)
            {
                chunk.Value.obj.SetActive(false);
                keystoremove.Add(chunk.Key);
            } else if(dist >= threshold_far_veryfarSq)
            {
                if(count != countCompare(factorC))
                {
                    chunk.Value.obj.SetActive(false);
                    keystoremove.Add(chunk.Key);
                    EnqueueChunk(chunk.Key, LOD.VERY_FAR);
                }

            } else if( dist >= threshold_mid_farSq)
            {
                if(count != countCompare(factorB)) {
                    chunk.Value.obj.SetActive(false);
                    keystoremove.Add(chunk.Key);
                    EnqueueChunk(chunk.Key, LOD.FAR);
                }

            } else if(dist >= threshold_near_midSq) 
            {
                if(count != countCompare(factorA))
                {
                    chunk.Value.obj.SetActive(false);
                    keystoremove.Add(chunk.Key);
                    EnqueueChunk(chunk.Key, LOD.MIDDLE);
                }

            } else if(count == countCompare(factorA))
            {
                chunk.Value.obj.SetActive(false);
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
            return chunkResolution * factor;
        }
    }

    private void GenerateChunk(Vector2Int pos, Chunk chunk, LOD distance, GameObject obj)
    {

        int resolution = Mathf.FloorToInt(chunkResolution * Mathf.Pow(0.5f, (int) distance));


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

        ChunkData chunkData = new ChunkData(obj, resolution);
        loadedChunks.Add(pos, chunkData);
        allGeneratedChunks[(int) distance].Add(pos, chunkData);
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

            // Find or create a child folder for this LOD (avoid Instantiate(new GameObject(...)) which spawns clones)
            Transform lodFolder = parentFolder.Find(dist.ToString());
            if (lodFolder == null)
            {
                var folderObj = new GameObject(dist.ToString());
                folderObj.transform.SetParent(parentFolder, false);
                lodFolder = folderObj.transform;
            }

            obj.transform.SetParent(lodFolder, false);
            obj.name = dist + "_Chunk_" + coord.x + "_" + coord.y;

            Chunk chunk = obj.GetComponent<Chunk>();

            GenerateChunk(coord, chunk, dist, obj);
        } else
        {
            loadedChunks.Add(coord, allGeneratedChunks[(int) dist][coord]);
            allGeneratedChunks[(int) dist][coord].obj.SetActive(true);
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
public class CoordDistRelation : IEquatable<CoordDistRelation>
{
    public Vector2Int coord;
    public LOD lod;

    public CoordDistRelation(Vector2Int coord, LOD lod)
    {
        this.coord = coord;
        this.lod = lod;
    }

    public bool Equals(CoordDistRelation other)
    {
        if (ReferenceEquals(other, null)) return false;
        if (ReferenceEquals(this, other)) return true;
        return coord.Equals(other.coord) && lod == other.lod;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as CoordDistRelation);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + coord.x;
            hash = hash * 31 + coord.y;
            hash = hash * 31 + (int)lod;
            return hash;
        }
    }

    public static bool operator ==(CoordDistRelation left, CoordDistRelation right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (ReferenceEquals(left, null) || ReferenceEquals(right, null)) return false;
        return left.Equals(right);
    }

    public static bool operator !=(CoordDistRelation left, CoordDistRelation right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"Coord({coord.x},{coord.y}) LOD:{lod}";
    }
}

public class ChunkData
{
    public GameObject obj;
    public int resolution;

    public ChunkData(GameObject obj, int resolution)
    {
        this.obj = obj;
        this.resolution = resolution;
    }
}

public enum LOD
{
    NEAR,
    MIDDLE,
    FAR,
    VERY_FAR
}