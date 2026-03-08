using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;


[BurstCompile]
public struct TriangleJob : IJobParallelFor
{
    public int chunkSize;
    
    [NativeDisableParallelForRestriction]
    public NativeArray<int> triangles;

    public void Execute(int index)
    {
        int x = index % (chunkSize - 1);
        int z = index / (chunkSize - 1);

        int vert = z * chunkSize + x;
        int tri = index * 6;

        triangles[tri + 0] = vert;
        triangles[tri + 1] = vert + chunkSize;
        triangles[tri + 2] = vert + 1;

        triangles[tri + 3] = vert + 1;
        triangles[tri + 4] = vert + chunkSize;
        triangles[tri + 5] = vert + chunkSize + 1;
    }
}