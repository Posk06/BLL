
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct TextureJob : IJobParallelFor
{
    public NativeArray<Color32> colors;
    
    [NativeDisableParallelForRestriction]
    public NativeArray<float> heights;
    public float2 chunkPos;
    public int textureResolution;
    public int terrainResolution;
    public float maxAmplitude;


    public void Execute(int index)
    {
        float sizing = (float) (terrainResolution - 1) / (float) Mathf.Max(1, textureResolution - 1);
        float x = index % textureResolution;
        float z = index / textureResolution;   

        colors[index] = colorassign(x,z);
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

        float h00 = heights[ix0 + iy0 * terrainResolution];
        float h10 = heights[ix1 + iy0 * terrainResolution];
        float h01 = heights[ix0 + iy1 * terrainResolution];
        float h11 = heights[ix1 + iy1 * terrainResolution];

        float hx0 = Mathf.Lerp(h00, h10, tx);
        float hx1 = Mathf.Lerp(h01, h11, tx);

        return Mathf.Lerp(hx0, hx1, ty) / maxAmplitude;
    }
    Color32 colorassign(float x, float y)
    {
        float sizing = (float) (terrainResolution - 1) / (float) Mathf.Max(1, textureResolution - 1);
        float height = heightSampling(x * sizing, y * sizing);

        if(height < 0.3f)
        {
            return new Color32(0, 0, 255, 255);
        } else if(height < 0.6f)
        {
            return new Color32(0, 255, 0, 255);
        } else if(height < 0.9f)
        {
            return new Color32(190, 190, 190, 255);
        } else
        {
            return new Color32(255, 255, 255, 255);
        }
    }
}