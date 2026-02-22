using System;
using System.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.WSA;

public class ProcedualGenerator : MonoBehaviour
{
    public Material terrainMaterial;

    public ComputeShader meshCS;

    public int resolution = 0;
    int size = 0;

    [Header("Terrain Generation")]
    public float moistFrequency = 0.0005f;
    public float tempFrequency = 0.0003f;
    public BiomeData biomeData;
    public Texture2DArray textureArray;

    [Header("Test Mode")]
    public bool testMode = false;
    [Range(0.0001f, 0.01f)]
    public float frequency = 0.0005f;
    [Range(1f, 50f)]
    public float amplitude = 1f;
    [Range(0.5f, 5f)]
    public float lacunarity = 2f;
    [Range(0.1f, 2f)]
    public float gain = 0.5f;
    [Range(1, 8)]
    public int octave = 4;
    public Material testMaterial;
    public Transform treePrefab;
    public Transform treeFolder;
    int[] biometemps;
    int[] biomemoist;
    int[] biomecont;
    float[] amplitudes;
    float[] frequencys;
    float[] lacunarities;
    float[] gains;
    int[] octaves;

    ComputeBuffer freqBuffer;
    ComputeBuffer ampBuffer;
    ComputeBuffer lacBuffer;
    ComputeBuffer gainBuffer;
    ComputeBuffer octBuffer;
    ComputeBuffer tempBuffer;
    ComputeBuffer moistBuffer;
    ComputeBuffer contBuffer;
    ComputeBuffer randomBuffer;
    
    GraphicsBuffer vertexBuffer;
    GraphicsBuffer normalBuffer;
    GraphicsBuffer uv2Buffer;
    GraphicsBuffer uv3Buffer;
    GraphicsBuffer treeBuffer;
    int currentVertexCount = 0;
    Mesh mesh;
    Vector3[] verts;
    Vector3[] normals;
    Vector4[] uv2s;
    Vector4[] uv3s;
    Vector2[] uv;
    int[] hastree;
    int[] indices;

    
    public void Init(int size)
    {
        resolution = size + 1;
    }

    void Start()
    {
        size = resolution - 1;

        if (terrainMaterial == null)
        {
            if(!testMode)
            {
                terrainMaterial = new Material(Shader.Find("Custom/TerrainBiomeBlend"));
                generateTextureArray();
            }
            else
            {
                terrainMaterial = testMaterial;
            }
        }

        generateBiomeArrays();
        generateTerrain();
    }

    float time = 0;
    void Update()
    {
        if(testMode)
        {
            if(time < resolution * resolution * 0.00001f) // Arbitrary time threshold to prevent constant regeneration - adjust as needed
        {
            time += Time.deltaTime;
            
        } else
        {
            generateBiomeArrays();
            generateTerrain();
            foreach(Transform child in treeFolder)
            {
                Destroy(child.gameObject);
            }
            time = 0;
        }
        }
    }


    private void generateTerrain()
    {
        CreateShape();

        if (meshCS == null)
        {
            Debug.LogError("Assign compute shader");
            enabled = false;
            return;
        }
        if(!testMode) {
            GetComponent<MeshRenderer>().material = terrainMaterial;
        }
        else
        {
            GetComponent<MeshRenderer>().material = testMaterial;
        }
    }


    private void CreateShape()
    {
        int vertexCount = resolution * resolution;
        int indexCount = (resolution - 1) * (resolution - 1) * 6;

        // Reuse graphics buffers when possible to avoid repeated allocations
        if (vertexBuffer == null || vertexBuffer.count != vertexCount)
        {
            if (vertexBuffer != null) vertexBuffer.Release();
            if (normalBuffer != null) normalBuffer.Release();
            if (uv2Buffer != null) uv2Buffer.Release();
            if (uv3Buffer != null) uv3Buffer.Release();

            vertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vertexCount, sizeof(float) * 3);
            normalBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vertexCount, sizeof(float) * 3);
            uv2Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vertexCount, sizeof(float) * 4);
            uv3Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vertexCount, sizeof(float) * 4);
            treeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vertexCount, sizeof(int));
        }

        int kernel = meshCS.FindKernel("CSMain");
        meshCS.SetBuffer(kernel, "vertices", vertexBuffer);
        meshCS.SetBuffer(kernel, "normals", normalBuffer);
        meshCS.SetBuffer(kernel, "uv2", uv2Buffer);
        meshCS.SetBuffer(kernel, "uv3", uv3Buffer);
        meshCS.SetBuffer(kernel, "hastree", treeBuffer);

        meshCS.SetInt("resolution", resolution);
        meshCS.SetFloat("xOffset", transform.position.x);
        meshCS.SetFloat("yOffset", transform.position.z);
        meshCS.SetFloat("moistFreq", moistFrequency);
        meshCS.SetFloat("tempFreq", tempFrequency);
        meshCS.SetInt("biomeCount", biomeData.biomes.Count);

        // Create or update and set ComputeBuffers (reuse when possible to avoid allocations)
        int biomeCount = biomeData.biomes.Count;

        if (freqBuffer == null || freqBuffer.count != biomeCount)
        {
            if (freqBuffer != null) freqBuffer.Release();
            freqBuffer = new ComputeBuffer(biomeCount, sizeof(float));
        }
        freqBuffer.SetData(frequencys);
        meshCS.SetBuffer(kernel, "frequencys", freqBuffer);

        if (ampBuffer == null || ampBuffer.count != biomeCount)
        {
            if (ampBuffer != null) ampBuffer.Release();
            ampBuffer = new ComputeBuffer(biomeCount, sizeof(float));
        }
        ampBuffer.SetData(amplitudes);
        meshCS.SetBuffer(kernel, "amplitudes", ampBuffer);

        if (lacBuffer == null || lacBuffer.count != biomeCount)
        {
            if (lacBuffer != null) lacBuffer.Release();
            lacBuffer = new ComputeBuffer(biomeCount, sizeof(float));
        }
        lacBuffer.SetData(lacunarities);
        meshCS.SetBuffer(kernel, "lacunarities", lacBuffer);

        if (gainBuffer == null || gainBuffer.count != biomeCount)
        {
            if (gainBuffer != null) gainBuffer.Release();
            gainBuffer = new ComputeBuffer(biomeCount, sizeof(float));
        }
        gainBuffer.SetData(gains);
        meshCS.SetBuffer(kernel, "gains", gainBuffer);

        if (octBuffer == null || octBuffer.count != biomeCount)
        {
            if (octBuffer != null) octBuffer.Release();
            octBuffer = new ComputeBuffer(biomeCount, sizeof(int));
        }
        octBuffer.SetData(octaves);
        meshCS.SetBuffer(kernel, "octaves", octBuffer);

        if (tempBuffer == null || tempBuffer.count != biomeCount)
        {
            if (tempBuffer != null) tempBuffer.Release();
            tempBuffer = new ComputeBuffer(biomeCount, sizeof(int));
        }
        tempBuffer.SetData(biometemps);
        meshCS.SetBuffer(kernel, "biometemps", tempBuffer);

        if (moistBuffer == null || moistBuffer.count != biomeCount)
        {
            if (moistBuffer != null) moistBuffer.Release();
            moistBuffer = new ComputeBuffer(biomeCount, sizeof(int));
        }
        moistBuffer.SetData(biomemoist);
        meshCS.SetBuffer(kernel, "biomemoist", moistBuffer);
        
        if (contBuffer == null || contBuffer.count != biomeCount)
        {
            if (contBuffer != null) contBuffer.Release();
            contBuffer = new ComputeBuffer(biomeCount, sizeof(int));
        }
        contBuffer.SetData(biomecont);
        meshCS.SetBuffer(kernel, "biomecont", contBuffer);

        if(randomBuffer == null || randomBuffer.count != vertexCount)
        {
            if (randomBuffer != null) randomBuffer.Release();
            randomBuffer = new ComputeBuffer(vertexCount, sizeof(float));
        }
        float[] randomValues = new float[vertexCount];
        System.Random rand = new System.Random();
        for (int i = 0; i < vertexCount; i++)
        {
            randomValues[i] =  (float) rand.NextDouble();
        }
        randomBuffer.SetData(randomValues);
        meshCS.SetBuffer(kernel, "randomValues", randomBuffer);


        // Calculate dispatch size using kernel thread group size to avoid over/under dispatching
        meshCS.GetKernelThreadGroupSizes(kernel, out uint tgx, out uint tgy, out uint tgz);
        int groupsX = Mathf.CeilToInt((float)resolution / tgx);
        int groupsY = Mathf.CeilToInt((float)resolution / tgy);
        meshCS.Dispatch(kernel, groupsX, groupsY, 1);

        verts = new Vector3[vertexCount];
        normals = new Vector3[vertexCount];
        uv2s = new Vector4[vertexCount];
        uv3s = new Vector4[vertexCount];
        hastree = new int[vertexCount];

        // Read back GPU buffers asynchronously to avoid GPU stalls
        int pendingReadbacks = 5;

        void ReadbackDone()
        {
            pendingReadbacks--;
            if (pendingReadbacks == 0)
            {
                // All data received, finalize mesh on main thread
                FinalizeMesh();
            }
        }

        AsyncGPUReadback.Request(vertexBuffer, (AsyncGPUReadbackRequest req) =>
        {
            if (req.hasError)
            {
                Debug.LogError("AsyncGPUReadback error for vertices");
            }
            else
            {
                var data = req.GetData<Vector3>();
                data.CopyTo(verts);
            }
            ReadbackDone();
        });

        AsyncGPUReadback.Request(normalBuffer, (AsyncGPUReadbackRequest req) =>
        {
            if (req.hasError)
            {
                Debug.LogError("AsyncGPUReadback error for normals");
            }
            else
            {
                var data = req.GetData<Vector3>();
                data.CopyTo(normals);
            }
            ReadbackDone();
        });

        AsyncGPUReadback.Request(uv2Buffer, (AsyncGPUReadbackRequest req) =>
        {
            if (req.hasError)
            {
                Debug.LogError("AsyncGPUReadback error for uv2");
            }
            else
            {
                var data = req.GetData<Vector4>();
                data.CopyTo(uv2s);
            }
            ReadbackDone();
        });

        AsyncGPUReadback.Request(uv3Buffer, (AsyncGPUReadbackRequest req) =>
        {
            if (req.hasError)
            {
                Debug.LogError("AsyncGPUReadback error for uv3");
            }
            else
            {
                var data = req.GetData<Vector4>();
                data.CopyTo(uv3s);
            }
            ReadbackDone();
        });

        AsyncGPUReadback.Request(treeBuffer, (AsyncGPUReadbackRequest req) =>
        {
            if (req.hasError)
            {
                Debug.LogError("AsyncGPUReadback error for tree data");
            }
            else
            {
                var data = req.GetData<int>();
                data.CopyTo(hastree);
            }
            ReadbackDone();
        });

        indices = new int[indexCount];
        int ti = 0;

        for (int y = 0; y < resolution - 1; y++)
        {
            for (int x = 0; x < resolution - 1; x++)
            {
                int i = x + y * resolution;
                indices[ti++] = i;
                indices[ti++] = i + resolution;
                indices[ti++] = i + 1;

                indices[ti++] = i + 1;
                indices[ti++] = i + resolution;
                indices[ti++] = i + resolution + 1;
            }
        }
        
        uv = new Vector2[vertexCount];
        for (int y = 0; y < resolution; y++) {
            for (int x = 0; x < resolution; x++)
            {
                uv[x + y * resolution] = new Vector2((float)x / (resolution - 1), (float)y / (resolution - 1));
            }
        }

        if (mesh == null) mesh = new Mesh();
        mesh.indexFormat = (vertexCount > 65000) ? IndexFormat.UInt32 : IndexFormat.UInt16;
        currentVertexCount = vertexCount;
    }
    
    private void FinalizeMesh()
    {
        if (mesh == null) mesh = new Mesh();
        mesh.Clear();

        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.triangles = indices;

        // Set UV2/UV3 from arrays
        mesh.SetUVs(1, new System.Collections.Generic.List<Vector4>(uv2s));
        mesh.SetUVs(2, new System.Collections.Generic.List<Vector4>(uv3s));
        mesh.uv = uv;

        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;

        MeshCollider mc = GetComponent<MeshCollider>();
        if (mc == null)
            mc = gameObject.AddComponent<MeshCollider>();

        mc.convex = false;
        mc.sharedMesh = mesh;

        // Instantiate trees after GPU readbacks have populated `verts` and `hastree`
        if (treePrefab != null && hastree != null && verts != null)
        {
            for (int i = 0; i < hastree.Length; i++)
            {
                if (hastree[i] == 1)
                {
                    Instantiate(treePrefab, verts[i], Quaternion.identity, treeFolder);
                }
            }
        }
    }

    void OnDestroy()
    {
        if (vertexBuffer != null) vertexBuffer.Release();
        if (normalBuffer != null) normalBuffer.Release();
        if (uv2Buffer != null) uv2Buffer.Release();
        if (freqBuffer != null) freqBuffer.Release();
        if (ampBuffer != null) ampBuffer.Release();
        if (lacBuffer != null) lacBuffer.Release();
        if (gainBuffer != null) gainBuffer.Release();
        if (octBuffer != null) octBuffer.Release();
        if (tempBuffer != null) tempBuffer.Release();
        if (moistBuffer != null) moistBuffer.Release();
        if (uv3Buffer != null) uv3Buffer.Release();
    }

    void generateBiomeArrays()
    {
        biometemps = new int[biomeData.biomes.Count];
        biomemoist = new int[biomeData.biomes.Count];
        biomecont = new int[biomeData.biomes.Count];
        amplitudes = new float[biomeData.biomes.Count];
        frequencys = new float[biomeData.biomes.Count];
        lacunarities = new float[biomeData.biomes.Count];
        gains = new float[biomeData.biomes.Count];
        octaves = new int[biomeData.biomes.Count];

        if (!testMode)
        {
            for (int i = 0; i < biomeData.biomes.Count; i++)
            {
                amplitudes[i] = biomeData.biomes[i].amplitude;
                frequencys[i] = biomeData.biomes[i].frequency;
                lacunarities[i] = biomeData.biomes[i].lacunarity;
                gains[i] = biomeData.biomes[i].gain;
                octaves[i] = biomeData.biomes[i].octaves;
            }
        }
        else
        {
            Debug.Log("Test mode enabled - using selected biome parameters for all biomes");
            for (int i = 0; i < biomeData.biomes.Count; i++)
            {
                amplitudes[i] = amplitude;
                frequencys[i] = frequency;
                lacunarities[i] = lacunarity;
                gains[i] = gain;
                octaves[i] = octave;
            }
        }

        for (int i = 0; i < biomeData.biomes.Count; i++)
        {
            biometemps[i] = (int) biomeData.biomes[i].temperature;
                biomemoist[i] = (int) biomeData.biomes[i].moisture;
                biomecont[i] = (int) biomeData.biomes[i].continentaless;
        }
    }

    void generateTextureArray()
    {
        textureArray = new Texture2DArray(512, 512, biomeData.biomes.Count, biomeData.biomes[0].groundTexture.format, false);

        for (int i = 0; i < biomeData.biomes.Count; i++)
        {
            Texture2D tex = biomeData.biomes[i].groundTexture;
            if (tex != null)
            {
                Graphics.CopyTexture(tex, 0, 0, textureArray, i, 0);
            }
        }

        textureArray.Apply();
        terrainMaterial.SetTexture("_TexArray", textureArray);
        
    }
}