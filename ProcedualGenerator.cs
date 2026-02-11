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
    int[] biometemps;
    int[] biomemoist;
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

    [Header("Collider")]
    public PhysicsMaterial pmat;


    GraphicsBuffer vertexBuffer;
    GraphicsBuffer normalBuffer;
    GraphicsBuffer uv2Buffer;
    GraphicsBuffer uv3Buffer;
    Mesh mesh;
    Vector3[] verts;
    Vector3[] normals;
    Vector4[] uv2s;
    Vector4[] uv3s;
    Vector2[] uv;
    
    
    int[] indices;

    public Texture2DArray textureArray;

    
    public void Init(int size)
    {
        resolution = size + 1;
    }

    void Start()
    {
        size = resolution - 1;

        if (terrainMaterial == null)
        {
            terrainMaterial = new Material(
                Shader.Find("Custom/TerrainBiomeBlend")
            );
        }

        generateBiomeArrays();
        generateTextureArray();
        generateTerrain();
    }

    /*float time = 0;
    void Update()
    {
        if(time < 2.5)
        {
            time += Time.deltaTime;
            
        } else
        {
            generateTerrain();
            time = 0;
        }
    }*/


    private void generateTerrain()
    {
        CreateShape();
        UpdateMesh();

        GetComponent<MeshFilter>().mesh = mesh;

        if (meshCS == null)
        {
            Debug.LogError("Assign compute shader");
            enabled = false;
            return;
        }

        GetComponent<MeshRenderer>().material = terrainMaterial;

        MeshCollider mc = GetComponent<MeshCollider>();
        if (mc == null)
            mc = gameObject.AddComponent<MeshCollider>();

        mc.convex = false;                                       
        mc.sharedMesh = mesh;
    }


    private void CreateShape()
    {
        int vertexCount = resolution * resolution;
        int indexCount = (resolution - 1) * (resolution - 1) * 6;

        vertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vertexCount, sizeof(float) * 3);
        normalBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vertexCount, sizeof(float) * 3);
        uv2Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vertexCount, sizeof(float) * 4);
        uv3Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vertexCount, sizeof(float) * 4);

        int kernel = meshCS.FindKernel("CSMain");
        meshCS.SetBuffer(kernel, "vertices", vertexBuffer);
        meshCS.SetBuffer(kernel, "normals", normalBuffer);
        meshCS.SetBuffer(kernel, "uv2", uv2Buffer);
        meshCS.SetBuffer(kernel, "uv3", uv3Buffer);

        meshCS.SetInt("resolution", resolution);
        meshCS.SetFloat("xOffset", transform.position.x);
        meshCS.SetFloat("yOffset", transform.position.z);
        meshCS.SetFloat("moistFreq", moistFrequency);
        meshCS.SetFloat("tempFreq", tempFrequency);
        meshCS.SetInt("biomeCount", biomeData.biomes.Count);

        // Create and set ComputeBuffers
        freqBuffer = new ComputeBuffer(biomeData.biomes.Count, sizeof(float));
        freqBuffer.SetData(frequencys);
        meshCS.SetBuffer(kernel, "frequencys", freqBuffer);

        ampBuffer = new ComputeBuffer(biomeData.biomes.Count, sizeof(float));
        ampBuffer.SetData(amplitudes);
        meshCS.SetBuffer(kernel, "amplitudes", ampBuffer);

        lacBuffer = new ComputeBuffer(biomeData.biomes.Count, sizeof(float));
        lacBuffer.SetData(lacunarities);
        meshCS.SetBuffer(kernel, "lacunarities", lacBuffer);

        gainBuffer = new ComputeBuffer(biomeData.biomes.Count, sizeof(float));
        gainBuffer.SetData(gains);
        meshCS.SetBuffer(kernel, "gains", gainBuffer);

        octBuffer = new ComputeBuffer(biomeData.biomes.Count, sizeof(int));
        octBuffer.SetData(octaves);
        meshCS.SetBuffer(kernel, "octaves", octBuffer);

        tempBuffer = new ComputeBuffer(biomeData.biomes.Count, sizeof(int));
        tempBuffer.SetData(biometemps);
        meshCS.SetBuffer(kernel, "biometemps", tempBuffer);

        moistBuffer = new ComputeBuffer(biomeData.biomes.Count, sizeof(int));
        moistBuffer.SetData(biomemoist);
        meshCS.SetBuffer(kernel, "biomemoist", moistBuffer);


        int groups = Mathf.CeilToInt(resolution);
        meshCS.Dispatch(kernel, groups, groups, 1);

        verts = new Vector3[vertexCount];
        normals = new Vector3[vertexCount];
        uv2s = new Vector4[vertexCount];
        uv3s = new Vector4[vertexCount];

        vertexBuffer.GetData(verts);
        normalBuffer.GetData(normals);
        uv2Buffer.GetData(uv2s);
        uv3Buffer.GetData(uv3s);

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
    }

    private void UpdateMesh()
    {

        
        mesh.Clear();

        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.triangles = indices;
        mesh.SetUVs(1, uv2s);
        mesh.SetUVs(2, uv3s);
        mesh.uv = uv;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.Optimize();
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
        amplitudes = new float[biomeData.biomes.Count];
        frequencys = new float[biomeData.biomes.Count];
        lacunarities = new float[biomeData.biomes.Count];
        gains = new float[biomeData.biomes.Count];
        octaves = new int[biomeData.biomes.Count];

        for (int i = 0; i < biomeData.biomes.Count; i++)
        {
            biometemps[i] = (int) biomeData.biomes[i].temperature;
            biomemoist[i] = (int) biomeData.biomes[i].moisture;
            amplitudes[i] = biomeData.biomes[i].amplitude;
            frequencys[i] = biomeData.biomes[i].frequency;
            lacunarities[i] = biomeData.biomes[i].lacunarity;
            gains[i] = biomeData.biomes[i].gain;
            octaves[i] = biomeData.biomes[i].octaves;
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