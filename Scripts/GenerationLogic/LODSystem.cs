//--------------------------------------------
//This code holds the diffrent LOD thresholds and
//provides useful methods to handle the LOD-Logic of chunks
//--------------------------------------------
// - Oskar Benjamin Trillitzsch

using UnityEngine;

public class LODSystem
{
    float thresholds_near = 4;
    float thresholds_mid = 6;
    float thresholds_far = 8;


    public LODSystem()
    {
        thresholds_far = Mathf.Pow(thresholds_far,2f);
        thresholds_mid = Mathf.Pow(thresholds_mid,2f);
        thresholds_near = Mathf.Pow(thresholds_near,2f);
    }
    
    public ChunkLOD getLOD(float distanceSq)
    {
        if(distanceSq <= thresholds_near) return ChunkLOD.NEAR;
        else if (distanceSq <= thresholds_mid) return ChunkLOD.MIDDEL;
        else if (distanceSq <= thresholds_far) return ChunkLOD.FAR;
        else return ChunkLOD.VERY_FAR;
    }
    public float getFactorForLOD(ChunkLOD lod)
    {
        switch (lod)
        {
            case ChunkLOD.NEAR:
                return 1f;
            case ChunkLOD.MIDDEL:
                return 0.5f;
            case ChunkLOD.FAR:
                return 0.25f;
            case ChunkLOD.VERY_FAR:
                return 0.125f;
            default:
                return 1f;
        }
    }
}

public enum ChunkLOD
{
    NEAR,
    MIDDEL,
    FAR,
    VERY_FAR
}