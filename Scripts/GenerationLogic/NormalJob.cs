using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct NormalJob : IJob
{
    [ReadOnly] public NativeArray<float3> vertices;
    [ReadOnly] public NativeArray<int> triangles;

    public NativeArray<float3> normals;

    public void Execute()
    {
       for (int i = 0; i < triangles.Length; i += 3)
        {
            int i0 = triangles[i];
            int i1 = triangles[i + 1];
            int i2 = triangles[i + 2];

            float3 a = vertices[i0];
            float3 b = vertices[i1];
            float3 c = vertices[i2];

            float3 normal = math.normalize(math.cross(b - a, c - a));

            normals[i0] += normal;
            normals[i1] += normal;
            normals[i2] += normal;
        }

        // Normalize
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = math.normalize(normals[i]);
        } 
    }
}