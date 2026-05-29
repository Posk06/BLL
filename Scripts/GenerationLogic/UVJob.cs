//--------------------------------------------
//This code calculates the UV coordinates for the meshes
//--------------------------------------------
// - Oskar Benjamin Trillitzsch

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
public struct UVJob : IJobParallelFor
{
    public NativeArray<Vector2> uvs;
    [ReadOnly]
    public int resolution;

    public void Execute(int index)
    {
        int x = index % resolution;
        int y = index / resolution;

        uvs[index] = new Vector2(
            (float)x / (resolution - 1),
            (float)y / (resolution - 1)
        );
    }
}