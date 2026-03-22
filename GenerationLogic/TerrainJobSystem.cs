using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class TerrainJobSystem : MonoBehaviour
{

    public GameObject TextureJobSystem;
    TextureJobSystem textureJobSystemScript;
    [Header("Terrain Generation")]
    public float amplitude = 1f;
    public int maxAmplitude = 600;
    public float frequency = 1f;
    public float lacunarity = 2f;
    public float gain = 0.5f;
    public int octave = 4;
    public float redistrobution = 1f;
    public float biomeFrequency = 0.0001f;
    public int seed = 0;
    int chunkSize = 64;
    int chunkResolution = 64;

    NativeArray<float3> vertices;
    NativeArray<int>[] triangles;

    LODSystem lodSystem;

    List<ChunkJob> activeTerrainJobs = new List<ChunkJob>();



    public void Init(int chunkSize, int resolution)
    {
        this.chunkSize = chunkSize;
        this.chunkResolution = resolution;
    }

    void Start()
    {
        textureJobSystemScript = TextureJobSystem.GetComponent<TextureJobSystem>();
        lodSystem = new LODSystem();
        triangles = new NativeArray<int>[4];
        textureJobSystemScript.Init(biomeFrequency, chunkSize, chunkResolution, maxAmplitude);
        generateTriangleArrays();
    }

    void Update()
    {
        updateJobs();
    }

    public void GenerateChunk(Vector2Int position, ChunkLOD lod, Chunk chunk)
    {
        int resolution = Mathf.FloorToInt(chunkResolution * lodSystem.getFactorForLOD(lod));
        int vertexCount = resolution * resolution;
        vertices = new NativeArray<float3>(vertexCount, Allocator.TempJob);

        TerrainJob terrainJob = new TerrainJob
        {
            chunkSize = chunkSize,
            resolution = resolution,
            amplitude = amplitude,
            maxAmplitude = maxAmplitude,
            frequency = frequency,
            lacunarity = lacunarity,
            gain = gain,
            octaves = octave,
            redistrobution = redistrobution,
            chunkPosition = new float2(position.x, position.y),
            vertices = vertices
        };

        JobHandle handle = terrainJob.Schedule(vertexCount, 64);

        ChunkJob chunkJob = new ChunkJob
        {
            handle = handle,
            vertices = vertices,
            triangles = triangles[(int)lod],
            position = new float2(position.x, position.y),
            chunk = chunk
        };

        activeTerrainJobs.Add(chunkJob);
    }


    private void generateTriangleArrays()
    {
        for(int i = 0; i < triangles.Length; i++)
        {
            int res = Mathf.FloorToInt(chunkResolution * lodSystem.getFactorForLOD((ChunkLOD) i));
            int triCount = (res - 1) * (res - 1) * 6;
            triangles[i] = new NativeArray<int>(triCount, Allocator.Persistent);

            TriangleJob trijob = new TriangleJob
            {
                chunkResolution = res,
                triangles = triangles[i]
            };

            JobHandle trihandle = trijob.Schedule((res - 1) * (res - 1), 64);
            trihandle.Complete();
        }

        
    }

    void updateJobs()
    {
        for (int i = activeTerrainJobs.Count - 1; i >= 0; i--)
        {
            ChunkJob chunkJob = activeTerrainJobs[i];
            if (chunkJob.handle.IsCompleted)
            {
                chunkJob.handle.Complete();
                
                chunkJob.chunk.ApplyMesh(chunkJob.vertices, chunkJob.triangles);
                textureJobSystemScript.GenerateTexture(chunkJob);


                chunkJob.vertices.Dispose();
                activeTerrainJobs.RemoveAt(i);
            }
        }
    }
}