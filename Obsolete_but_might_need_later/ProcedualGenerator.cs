using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;


public class ProcedualGenerator : MonoBehaviour
{
    public Material terrainMaterial;
    public BiomeData biomeData;

    public ComputeShader meshCS;
    public ComputeShader biomeCS;
    public GameObject treePrefab;

    int pendingReadbacks = 2;

    public int resolution = 0;
    public int textureres = 0;
    public int size = 16;
    public bool bakeCollider = false;

    [Header("Terrain Generation")]
    public float amplitude;
    public float maxAmplitude;
    public float frequency;
    public float lacunarity;
    public float gain;
    public int octave;
    public float redistrobution;
    public float biomeFrequency = 0.01f;
    int seed = 0;
    
    GraphicsBuffer vertexBuffer;
    GraphicsBuffer normalBuffer;
    GraphicsBuffer colorBuffer;
    GraphicsBuffer treeBuffer;

    ComputeBuffer heightBuffer;
    int[] elev;
    int[] moist;
    int[] cont;
    Vector3Int[] col;
    Mesh mesh;
    Vector3[] verts;
    Vector3[] normals;
    Vector2[] uv;
    Vector3Int[] color;
    Vector3[] trees;
    int[] indices;

    private Texture2D biomeMap;



    
    public void Init(int size, int resolution,int seed, int textureres)
    {
        this.size = size;
        this.resolution = resolution;
        this.seed = seed;
        this.textureres = textureres;
        // Instantiate compute shader instances per generator to avoid global parameter races
        if (meshCS != null)
        {
            meshCS = Instantiate(meshCS);
        }
        if (biomeCS != null)
        {
            biomeCS = Instantiate(biomeCS);
        }
    }
    private void populateTrees()
    {
        // Spawn trees over multiple frames to avoid frame hitching
        if (gameObject != null && gameObject.activeInHierarchy)
        {
            StartCoroutine(SpawnTrees());
        }
    }

    private IEnumerator SpawnTrees()
    {
        if (trees == null || treePrefab == null) yield break;
        int batchSize = 20; // number of trees to instantiate per frame (reduced)
        int count = trees.Length;
        for (int i = 0; i < count; i++)
        {
            Vector3 tree = trees[i];
            if (tree.y > 0)
            {
                Instantiate(treePrefab, new Vector3(tree.x, tree.y, tree.z), Quaternion.identity);
            }
            if (i % batchSize == 0) // yield occasionally to spread workload
            {
                yield return null;
            }
        }
    }

    void Start()
    {   
        biomeMap = new Texture2D(textureres, textureres, TextureFormat.RGBA32, false);
        GetComponent<Renderer>().material.SetTexture("_BaseMap", biomeMap);
        GetComponent<Renderer>().material.SetTexture("_MainTex", biomeMap);

        populateArrays();

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
           

            vertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vertexCount, sizeof(float) * 3);
            normalBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vertexCount, sizeof(float) * 3);
           
        }

        int kernel = meshCS.FindKernel("TerrainCSMain");
        meshCS.SetBuffer(kernel, "vertices", vertexBuffer);
        meshCS.SetBuffer(kernel, "normals", normalBuffer);

        meshCS.SetInt("seed", seed);

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
        // Chain readbacks to avoid launching both at once (reduces stalls)
        pendingReadbacks = 1; // we'll call ReadbackDone() once after normals are received
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
            Debug.Log("Vertex readback completed");
            // Now request normals — do this on the callback thread to stagger GPU/CPU work
            AsyncGPUReadback.Request(normalBuffer, (AsyncGPUReadbackRequest req2) =>
            {
                if (req2.hasError)
                {
                    Debug.LogError("AsyncGPUReadback error for normals");
                }
                else
                {
                    var data2 = req2.GetData<Vector3>();
                    data2.CopyTo(normals);
                }
                Debug.Log("Normal readback completed");
                ReadbackDone();
            });
        });
    }

    private void ReadbackDone()
    {
        pendingReadbacks--;
        if (pendingReadbacks == 0)
        {
            // All data received, finalize mesh on main thread spread over multiple frames
            StartCoroutine(FinalizeMeshCoroutine());
            // Now that verts are populated, generate the biome texture
            StartCoroutine(GenerateTextureCoroutine());
        }
        Debug.Log("Readback completed, pending readbacks: " + pendingReadbacks);
    }

    private void texture()
    { 
        if (biomeMap == null) return;
        // Use a single SetPixels32 call with a preallocated array to reduce overhead
        Color32[] pixels = new Color32[textureres * textureres];
        for (int y = 0; y < textureres; y++)
        {
            for (int x = 0; x < textureres; x++)
            {
                var c = color[x + y * textureres];
                pixels[x + y * textureres] = new Color32((byte)c.x, (byte)c.y, (byte)c.z, 255);
            }
        }
        biomeMap.SetPixels32(pixels);
        biomeMap.filterMode = FilterMode.Bilinear;
        biomeMap.wrapMode = TextureWrapMode.Clamp;
        biomeMap.Apply(false);
    }

    private IEnumerator GenerateTextureCoroutine()
    {
        // Fill texture data on a background of frames (SetPixels32 + Apply already scheduled)
        generateTexture();
        texture();
        // yield one frame to allow Apply to complete without blocking other tasks
        yield return null;
    }
    
    private void generateTexture()
    {
        int size = textureres * textureres;

        color = new Vector3Int[size];
        trees = new Vector3[size];

        int kernel = biomeCS.FindKernel("BiomeCSMain");

        colorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size, sizeof(int) * 3);
        treeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size, sizeof(float) * 3);

        biomeCS.SetBuffer(kernel, "colors", colorBuffer);
        biomeCS.SetBuffer(kernel, "trees", treeBuffer);

        biomeCS.SetInt("terrainresolution", resolution);
        biomeCS.SetInt("textureresolution", textureres);

        biomeCS.SetFloat("xOffset", transform.position.x);
        biomeCS.SetFloat("yOffset", transform.position.z);
        biomeCS.SetFloat("maxAmp", maxAmplitude);

        biomeCS.SetFloat("biomeFrequency", biomeFrequency);
        biomeCS.SetInt("biomeCount", biomeData.biomes.Count);

        biomeCS.SetInt("seed", seed);

        heightBuffer = new ComputeBuffer(resolution * resolution, sizeof(float));
        float[] heights = new float[resolution * resolution];
        for(int i = 0; i < resolution * resolution; i++)
        {
            heights[i] = verts[i].y;
        }
        heightBuffer.SetData(heights);
        biomeCS.SetBuffer(kernel, "heights", heightBuffer);

        computeBuffer(sizeof(int), elev, biomeData.biomes.Count, biomeCS, kernel, "elevations");
        computeBuffer(sizeof(int), moist, biomeData.biomes.Count, biomeCS, kernel, "moistures");
        computeBuffer(sizeof(int), cont, biomeData.biomes.Count, biomeCS, kernel, "continentalnesses");
        computeBuffer(sizeof(int) * 3, col, biomeData.biomes.Count, biomeCS, kernel, "biomecolors");

        biomeCS.GetKernelThreadGroupSizes(kernel, out uint tgx, out uint tgy, out uint tgz);
        int groupsX = Mathf.CeilToInt((float)textureres / tgx);
        int groupsY = Mathf.CeilToInt((float)textureres / tgy);
        biomeCS.Dispatch(kernel, groupsX, groupsY, 1);

        // Request color first, then request trees after colors are applied — chains readbacks
        AsyncGPUReadback.Request(colorBuffer, (AsyncGPUReadbackRequest req) =>
        {
            if (req.hasError)
            {
                Debug.LogError("AsyncGPUReadback error for biome colors");
            }
            else
            {
                var data = req.GetData<Vector3Int>();
                data.CopyTo(color);
                // Apply the texture now that colors are available
                texture();
            }
            Debug.Log("Biome color readback completed");

            // Chain tree readback after color is applied to avoid simultaneous readbacks
            AsyncGPUReadback.Request(treeBuffer, (AsyncGPUReadbackRequest req2) =>
            {
                if (req2.hasError)
                {
                    Debug.LogError("AsyncGPUReadback error for biome trees");
                }   
                else
                {
                    var data2 = req2.GetData<Vector3>();
                    data2.CopyTo(trees);
                    // Release height buffer now that both computations are done
                    if (heightBuffer != null) { heightBuffer.Release(); heightBuffer = null; }
                    // Spawn trees over multiple frames to avoid hitching
                }
                Debug.Log("Biome tree readback completed");
            });
        });
    }        
    
    private IEnumerator FinalizeMeshCoroutine()
    {
        Debug.Log("Finalizing mesh over multiple frames");
        if (mesh == null) mesh = new Mesh();
        mesh.indexFormat = (verts.Length > 65000) ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.Clear();

        // Assign vertices first
        mesh.vertices = verts;
        yield return null;

        // Then triangles
        mesh.triangles = indices;
        yield return null;

        // Then normals and UVs
        mesh.normals = normals;
        mesh.uv = uv;
        yield return null;

        // Recalculate bounds (may be relatively expensive)
        // mesh.RecalculateBounds();
        // yield return null;

        if (gameObject != null)
        {
            var mf = GetComponent<MeshFilter>();
            if (mf != null) mf.mesh = mesh;
        }

        if(bakeCollider)
        {
            // Delay collider creation one more frame to avoid concurrent spikes
            yield return null;


            MeshCollider mc = GetComponent<MeshCollider>();
            if (mc == null)
            {
                mc = gameObject.AddComponent<MeshCollider>();
            }
            mc.convex = false;
            mc.sharedMesh = mesh;
        }
    }

    private void computeBuffer(int typesize, Array a, int size, ComputeShader CS, int kernel, string name)
    {
        ComputeBuffer buffer = new ComputeBuffer(size, typesize);
        buffer.SetData(a);
        CS.SetBuffer(kernel, name, buffer);
    }

    private void populateArrays()
    {   
        int length = biomeData.biomes.Count;

        elev = new int[length];
        moist = new int[length];
        cont = new int[length];
        col = new Vector3Int[length];

        for(int i = 0; i < length; i++)
        {
            elev[i] = (int) biomeData.biomes[i].elevation;
            moist[i] = (int) biomeData.biomes[i].moisture;
            cont[i] = (int) biomeData.biomes[i].continentaless;
            col[i] = new Vector3Int(Mathf.FloorToInt(biomeData.biomes[i].color.r * 255), Mathf.FloorToInt(biomeData.biomes[i].color.g * 255),Mathf.FloorToInt(biomeData.biomes[i].color.b * 255));
        }
    }
}    