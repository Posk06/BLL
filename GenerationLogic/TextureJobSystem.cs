using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;

public class TextureJobSystem : MonoBehaviour
{

    public GameObject treeJobSystem;
    TreeJobSystem treeJobSystemScript;
    public BiomeData biomeData;

    public int textureResolution = 256;
    float biomeFrequency = 0.001f;
    int chunkSize = 64;
    int chunkResolution = 64;
    int maxAmplitude = 600;
    int seed = 0;
    float seedOffset;

    NativeArray<int> moistures;
    NativeArray<int> elevations;
    Texture2D[] biomeTextures;

    NativeArray<float> moisturesOut;
    NativeArray<float> heightsOut;
    NativeArray<int> colorIndices;

    List<TexJob> activeTextureJobs = new List<TexJob>();


    public void Init(float biomeFrequency, int chunkSize, int chunkResolution, int maxAmplitude, int seed)
    {
        this.biomeFrequency = biomeFrequency;
        this.chunkSize = chunkSize;
        this.chunkResolution = chunkResolution;
        this.maxAmplitude = maxAmplitude;
        this.seed = seed;
    }

    void Start()
    {
        populateArrays();
        treeJobSystemScript = treeJobSystem.GetComponent<TreeJobSystem>();
        treeJobSystemScript.Init(chunkResolution, chunkSize, biomeData);

        Random.InitState(seed);
        seedOffset = Random.Range(0f, 10000f);
    }

    void Update()
    {
        updateJobs();
    }

    public void GenerateTexture(ChunkJob job)
    {
        NativeArray<float> heights = new NativeArray<float>(job.vertices.Length, Allocator.TempJob);
        
        for (int i = 0; i < heights.Length; i++)
            heights[i] = job.vertices[i].y;

        int resolution = Mathf.FloorToInt(Mathf.Sqrt(heights.Length));
        int temptextureResolution = Mathf.FloorToInt(textureResolution * (float) resolution / (float) chunkResolution);
        int pixelCount = temptextureResolution * temptextureResolution;

        moisturesOut = new NativeArray<float>(pixelCount, Allocator.TempJob);
        heightsOut = new NativeArray<float>(pixelCount, Allocator.TempJob);
        colorIndices = new NativeArray<int>(pixelCount, Allocator.TempJob);

        TextureJob texJob = new TextureJob
        {
            chunkPos = job.position,
            terrainResolution = resolution,
            textureResolution = temptextureResolution,
            heightsIn = heights,
            heightsOut = heightsOut,
            moistures = moistures,
            elevations = elevations,
            biomeFrequency = biomeFrequency,
            chunkSize = chunkSize,
            colorIndices = colorIndices,
            maxAmplitude = maxAmplitude,
            seedOffset = seedOffset
        };

        JobHandle handle = texJob.Schedule(pixelCount, 64);

        activeTextureJobs.Add(new TexJob{
            handle = handle,
            chunk = job.chunk,
            position = job.position,
            heights = heightsOut,
            moistures = moisturesOut,
            colorIndices = colorIndices
        });
    }

    void updateJobs()
    {
        for(int i = activeTextureJobs.Count - 1; i >= 0; i--)
        {
            TexJob texJob = activeTextureJobs[i];
            if(texJob.handle.IsCompleted)
            {
                texJob.handle.Complete();

                texJob.chunk.ApplyTexture(MakeTexture(texJob));
                //treeJobSystemScript.GenerateTreePoints(texJob);

                texJob.moistures.Dispose();

                activeTextureJobs.RemoveAt(i);
            }
        }
    }

   private Texture2D MakeTexture(TexJob job)
    {
        int res = Mathf.FloorToInt(Mathf.Sqrt(job.colorIndices.Length));
        Texture2D texture = new Texture2D(res, res, TextureFormat.RGBA32, false);
        int texture_factor = biomeTextures[0].width / res;


        Color32[] col = new Color32[job.colorIndices.Length];

        int x;
        int y;
        for (int i = 0; i < job.colorIndices.Length; i++)
        {
            x = Mathf.FloorToInt(i % res) * texture_factor;
            y = Mathf.FloorToInt(i / res) * texture_factor;
            col[i] = biomeTextures[job.colorIndices[i]].GetPixel(x, y);
        }    

        texture.SetPixels32(col);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply(false);
        return texture;
    }
    private Texture2D MakeTexture2(TexJob job)
    {
        int res = Mathf.FloorToInt(Mathf.Sqrt(job.colorIndices.Length));

        Texture2D tex = new Texture2D(res, res, TextureFormat.R8, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        byte[] data = new byte[job.colorIndices.Length];

        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)job.colorIndices[i];
        }

        tex.LoadRawTextureData(data);
        tex.Apply(false);

        return tex;
    }

    void populateArrays()
    {   
        int length = biomeData.biomes.Count;

        elevations = new NativeArray<int>(length, Allocator.Persistent);
        moistures = new NativeArray<int>(length, Allocator.Persistent);
        biomeTextures = new Texture2D[length];

        for(int i = 0; i < length; i++)
        {
            elevations[i] = (int) biomeData.biomes[i].elevation;
            moistures[i] = (int) biomeData.biomes[i].moisture;
            biomeTextures[i] = biomeData.biomes[i].texture;
        }
    }
    
}
