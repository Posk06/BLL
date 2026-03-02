using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class ProcedualGenerator : MonoBehaviour
{
    public Material terrainMaterial;

    public ComputeShader meshCS;

    int pendingReadbacks = 3;

    public int resolution = 0;
    public int size = 16;

    [Header("Terrain Generation")]
    public float amplitude;
    public float maxAmplitude;
    public float frequency;
    public float lacunarity;
    public float gain;
    public int octave;
    public float redistrobution;
    public float biomeFrequency = 0.01f;
    
    GraphicsBuffer vertexBuffer;
    GraphicsBuffer normalBuffer;
    GraphicsBuffer colorBuffer;
    Mesh mesh;
    Vector3[] verts;
    Vector3[] normals;
    Vector2[] uv;
    Vector3Int[] color;
    int[] indices;

    private Texture2D biomeMap;
    

    
    public void Init(int size, int resolution)
    {
        this.size = size;
        this.resolution = resolution;
    }

    void Start()
    {
        
        biomeMap = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        GetComponent<Renderer>().material.SetTexture("_BaseMap", biomeMap);
        GetComponent<Renderer>().material.SetTexture("_MainMap", biomeMap);

        generateTerrain();

    }

    void Update(){}

    private void generateTerrain()
    {
        if (meshCS == null)
        {
            Debug.LogError("Assign compute shader");
            enabled = false;
            return;
        }

        CreateShape();
    }


    private void CreateShape()
    {
        Debug.Log("Creating shape with resolution: " + resolution);
        int vertexCount = resolution * resolution;
        int indexCount = (resolution - 1) * (resolution - 1) * 6;

        Debug.Log("Sending Data to GPU");
        sendDatatoGPU();
        
        verts = new Vector3[vertexCount];
        normals = new Vector3[vertexCount];
        color = new Vector3Int[vertexCount];

        Debug.Log("Waiting for GPU readback...");
        doReadbacks();

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
    }
    
    private void FinalizeMesh()
    {
        Debug.Log("Finalizing mesh on main thread");
        if (mesh == null) mesh = new Mesh();
        mesh.indexFormat = (verts.Length > 65000) ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.Clear();

        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.triangles = indices;

        uv = new Vector2[verts.Length];
        for (int y = 0; y < resolution; y++) {
            for (int x = 0; x < resolution; x++)
            {
                uv[x + y * resolution] = new Vector2((float)x / (resolution - 1), (float)y / (resolution - 1));
            }
        }

        mesh.uv = uv;

        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;

        MeshCollider mc = GetComponent<MeshCollider>();
        if (mc == null)
            mc = gameObject.AddComponent<MeshCollider>();

        mc.convex = false;
        mc.sharedMesh = mesh;

        texture();
    }

    void OnDestroy()
    {
        if (vertexBuffer != null) vertexBuffer.Release();
        if (normalBuffer != null) normalBuffer.Release();
        if (colorBuffer != null) colorBuffer.Release();
    }

    private void sendDatatoGPU()
    {

        int vertexCount = resolution * resolution;

        // Reuse graphics buffers when possible to avoid repeated allocations
        if (vertexBuffer == null || vertexBuffer.count != vertexCount)
        {
            if (vertexBuffer != null) vertexBuffer.Release();
            if (normalBuffer != null) normalBuffer.Release();
            if (colorBuffer != null) colorBuffer.Release();
           

            vertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vertexCount, sizeof(float) * 3);
            normalBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vertexCount, sizeof(float) * 3);
            colorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vertexCount, sizeof(int) * 3);
           
        }

        int kernel = meshCS.FindKernel("TerrainCSMain");
        meshCS.SetBuffer(kernel, "vertices", vertexBuffer);
        meshCS.SetBuffer(kernel, "normals", normalBuffer);
        meshCS.SetBuffer(kernel, "colors", colorBuffer);


        meshCS.SetInt("resolution", resolution);
        meshCS.SetInt("size", size);

        meshCS.SetFloat("xOffset", transform.position.x);
        meshCS.SetFloat("yOffset", transform.position.z);

        meshCS.SetFloat("amplitude", amplitude);
        meshCS.SetFloat("frequency", frequency);
        meshCS.SetFloat("lacunarity", lacunarity);
        meshCS.SetFloat("gain", gain);
        meshCS.SetInt("octaves", octave);
        meshCS.SetFloat("maxHeight", maxAmplitude);
        meshCS.SetFloat("redistrobution", redistrobution);
        
        meshCS.SetFloat("biomeFrequency", biomeFrequency);


        meshCS.GetKernelThreadGroupSizes(kernel, out uint tgx, out uint tgy, out uint tgz);
        int groupsX = Mathf.CeilToInt((float)resolution / tgx);
        int groupsY = Mathf.CeilToInt((float)resolution / tgy);
        meshCS.Dispatch(kernel, groupsX, groupsY, 1);
    }

    private void doReadbacks()
    {
        // ensure pending readbacks is reset each generation
        pendingReadbacks = 3;
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
            Debug.Log("Vertex readback completed");
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
            Debug.Log("Normal readback completed");
        });

        AsyncGPUReadback.Request(colorBuffer, (AsyncGPUReadbackRequest req) =>
        {
            if (req.hasError)
            {
                Debug.LogError("AsyncGPUReadback error for colors");
            }
            else
            {
                var data = req.GetData<Vector3Int>();
                data.CopyTo(color);
            }
            ReadbackDone();
            Debug.Log("Color readback completed");
        });
    }

    private void ReadbackDone()
    {
        pendingReadbacks--;
        if (pendingReadbacks == 0)
        {
            // All data received, finalize mesh on main thread
            FinalizeMesh();
        }
        Debug.Log("Readback completed, pending readbacks: " + pendingReadbacks);
    }

    private void texture()
    {
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                biomeMap.SetPixel(x, y, new Color32((byte)color[x + y * resolution].x, (byte)color[x + y * resolution].y, (byte)color[x + y * resolution].z, 255));
            }
        }
        biomeMap.filterMode = FilterMode.Bilinear;
        biomeMap.wrapMode = TextureWrapMode.Clamp;
        biomeMap.Apply();
    }        
}