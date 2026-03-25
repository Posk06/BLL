//--------------------------------------------
//This code calculates the triangles indicies for the meshes
//--------------------------------------------
// - Oskar Benjamin Trillitzsch

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;


[BurstCompile]
public struct TriangleJob : IJobParallelFor
{
    public int chunkResolution;
    
    [NativeDisableParallelForRestriction]
    public NativeArray<int> triangles;

    public void Execute(int index)
    {

        // Calculate the triangles corner points

        int x = index % (chunkResolution - 1);
        int z = index / (chunkResolution - 1);

        int vert = z * chunkResolution + x;
        int tri = index * 6;

        triangles[tri + 0] = vert;
        triangles[tri + 1] = vert + chunkResolution;
        triangles[tri + 2] = vert + 1;

        triangles[tri + 3] = vert + 1;
        triangles[tri + 4] = vert + chunkResolution;
        triangles[tri + 5] = vert + chunkResolution + 1;
    }
}