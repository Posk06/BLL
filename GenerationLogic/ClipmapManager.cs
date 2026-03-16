using System.Collections.Generic;
using TreeEditor;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class ClipmapManager
{

    List<ChunkJob> activeJobs = new List<ChunkJob>();

    float amplitude;
    float frequency;
    int octaves;
    float gain;
    float lacunarity;
    float redistorbution;
    int chunkResolution;

    NativeArray<float3> vertices;

    public ClipmapManager(float amplitude,float frequency, int octaves, float gain, float lacunarity, float redistorbution, int chunkResolution)
    {
        this.amplitude = amplitude;
        this.frequency = frequency;
        this.octaves = octaves;
        this.gain = gain;
        this.lacunarity = lacunarity;
        this.redistorbution = redistorbution;
        this.chunkResolution = chunkResolution;
    }


    public void updateClipmaps(List<GameObject> rings)
    {
        foreach(GameObject ring in rings) {
            
        }
    }

    void startJob(GameObject ring, int index)
    {
    }

}