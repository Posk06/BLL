using UnityEngine;

public class LODSystem
{
    float thresholds_near = 500f;
    float thresholds_mid = 1000f;
    float thresholds_far = 2000f;


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
}

public enum ChunkLOD
{
    NEAR,
    MIDDEL,
    FAR,
    VERY_FAR
}