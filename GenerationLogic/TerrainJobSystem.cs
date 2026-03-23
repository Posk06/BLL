using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;

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
    float seedOffset;
    int chunkSize = 64;
    int chunkResolution = 64;

    NativeArray<float3> vertices;
    NativeArray<float3> normals;  
    NativeArray<int>[] triangles;
    NativeArray<Vector2>[] uvs;

    LODSystem lodSystem;

    List<ChunkJob> activeTerrainJobs = new List<ChunkJob>();
    List<NormJob> activeNormalJobs = new List<NormJob>();




    public void Init(int chunkSize, int resolution)
    {
        this.chunkSize = chunkSize;
        this.chunkResolution = resolution;
    }

    void Start()
    {
        seed = Random.Range(0, int.MaxValue);

        textureJobSystemScript = TextureJobSystem.GetComponent<TextureJobSystem>();
        lodSystem = new LODSystem();
        triangles = new NativeArray<int>[4];
        uvs = new NativeArray<Vector2>[4];
        textureJobSystemScript.Init(biomeFrequency, chunkSize, chunkResolution, maxAmplitude, seed);
        generatePersistentArrays();

        Random.InitState(seed);
        seedOffset = Random.Range(0f, 10000f);
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
            vertices = vertices,
            seedOffset = seedOffset
        };


        JobHandle handle = terrainJob.Schedule(vertexCount, 64);

        ChunkJob chunkJob = new ChunkJob
        {
            handle = handle,
            vertices = vertices,
            triangles = triangles[(int)lod],
            uvs = uvs[(int)lod],
            normals = normals,
            position = new float2(position.x, position.y),
            chunk = chunk
        };

        activeTerrainJobs.Add(chunkJob);
    }

    public void GenerateNormals(ChunkJob chunkJob)
    {
        int vertexCount = vertices.Length;
        normals = new NativeArray<float3>(vertexCount, Allocator.TempJob);

        NormalJob normalJob = new NormalJob
        {
            vertices = chunkJob.vertices,
            triangles = chunkJob.triangles,
            normals = normals
        };

        JobHandle normalHandle = normalJob.Schedule();

        NormJob normJob = new NormJob
        {
            handle = normalHandle,
            vertices = chunkJob.vertices,
            triangles = chunkJob.triangles,
            uvs = chunkJob.uvs,
            chunk = chunkJob.chunk,
            normals = normals
        };
        activeNormalJobs.Add(normJob);
    }


    private void generatePersistentArrays()
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

        for (int i = 0; i < uvs.Length; i++)
        {
            int res = Mathf.FloorToInt(chunkResolution * lodSystem.getFactorForLOD((ChunkLOD)i));
            uvs[i] = new NativeArray<Vector2>(res * res, Allocator.Persistent);

            UVJob uvJob = new UVJob
            {
                resolution = res,
                uvs = uvs[i]
            };

            JobHandle uvHandle = uvJob.Schedule(res * res, 64);
            uvHandle.Complete();
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

                textureJobSystemScript.GenerateTexture(chunkJob);
                GenerateNormals(chunkJob);

                activeTerrainJobs.RemoveAt(i);
            }
        }

        for(int i = activeNormalJobs.Count - 1; i >= 0; i--)
        {
            NormJob chunkJob = activeNormalJobs[i];
            if (chunkJob.handle.IsCompleted)
            {
                chunkJob.handle.Complete();

                chunkJob.chunk.ApplyMesh(chunkJob.vertices, chunkJob.triangles, chunkJob.uvs, chunkJob.normals);

                chunkJob.normals.Dispose();
                chunkJob.vertices.Dispose();
                activeNormalJobs.RemoveAt(i);
            }
        }
    }
}