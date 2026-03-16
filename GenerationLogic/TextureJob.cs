
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct TextureJob : IJobParallelFor
{
    public NativeArray<Color32> colorsOut;
    
    public NativeArray<float> moisturesOut;
    public NativeArray<float> heightsOut;
    
    [ReadOnly]
    public NativeArray<float> heightsIn;

    [ReadOnly]
    public NativeArray<int> moistures;

    [ReadOnly]
    public NativeArray<int> elevations;

    [ReadOnly]
    public NativeArray<Color32> colorsIn;

    public float2 chunkPos;
    public int textureResolution;
    public int terrainResolution;
    public int chunkSize;
    public float maxAmplitude;
    public float biomeFrequency;
 
    

    public void Execute(int index)
    {
        float sizing = (float) (terrainResolution - 1) / Mathf.Max(1, textureResolution - 1);
        float x = (index % textureResolution) * sizing;
        float z = (index / textureResolution) * sizing;   

        float height = heightSampling(x, z);
        float noiseValuemoisture = (noise.snoise(new float2(chunkPos.x * chunkSize + x, chunkPos.y * chunkSize + z) * biomeFrequency) + 1f) * 0.5f;


        colorsOut[index] = colorassign(x, z, height, noiseValuemoisture);
        moisturesOut[index] = noiseValuemoisture;
        heightsOut[index] = height;
    }

    float heightSampling(float x, float y)
    {
        int ix = (int)Mathf.Floor(x);
        int iy = (int)Mathf.Floor(y);
        float tx = x - ix;
        float ty = y - iy;

        int ix0 = Mathf.Clamp(ix, 0, terrainResolution - 1);
        int iy0 = Mathf.Clamp(iy, 0, terrainResolution - 1);
        int ix1 = Mathf.Clamp(ix + 1, 0, terrainResolution - 1);
        int iy1 = Mathf.Clamp(iy + 1, 0, terrainResolution - 1);

        float h00 = heightsIn[ix0 + iy0 * terrainResolution];
        float h10 = heightsIn[ix1 + iy0 * terrainResolution];
        float h01 = heightsIn[ix0 + iy1 * terrainResolution];
        float h11 = heightsIn[ix1 + iy1 * terrainResolution];

        float hx0 = Mathf.Lerp(h00, h10, tx);
        float hx1 = Mathf.Lerp(h01, h11, tx);

        return Mathf.Lerp(hx0, hx1, ty) / maxAmplitude;
    }
    Color32 colorassign(float x, float y, float height, float moisture)
    {
        int biomeCount = colorsIn.Length;

        int tempelev;
        int tempmoist;
        
        if(height == 0) {
            tempelev = 0; // NONE
        } else if(height < 0.2) {
            tempelev = 1; // LOW
        } else if(height < 0.5) {
            tempelev = 2; // MID
        } else {
            tempelev = 3; // HIGH
        }

        if(moisture == 0) {
            tempmoist = 0; // NONE
        } else if(moisture < 0.2) {
            tempmoist = 1; // LOW
        } else if(moisture < 0.5) {
            tempmoist = 2; // MID
        } else {
            tempmoist = 3;  // HIGH
        }

        int biomeIndex = -1;

        for(int i = 0; i < biomeCount; i++) {
            if(tempelev == elevations[i] && tempmoist == moistures[i]) {
                biomeIndex = i;
            }    
        }

        if(biomeIndex == -1) {
            for(int i = 0; i < biomeCount; i++) {
                if(tempelev == elevations[i]) {
                    biomeIndex = i;
                }
            }  
        }

        if(biomeIndex == -1) {
            return new Color32(0,255,0,255);
        } else
        {
            return colorsIn[biomeIndex];
        }

    }


}