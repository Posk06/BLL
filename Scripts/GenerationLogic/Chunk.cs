using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;


public class Chunk : MonoBehaviour
{
    MeshFilter meshFilter;
    Renderer render;
    MeshCollider meshCollider;
    public NativeArray<float> biomeMap;
    public NativeArray<float> heightmap;
    public int maxAmplitude = 600;
    Mesh mesh;
    public bool spawnObjects;

    void Awake()
    {
        
        meshFilter = GetComponent<MeshFilter>();
        render = GetComponent<Renderer>();
        meshCollider = GetComponent<MeshCollider>();
        mesh = new Mesh();
        meshFilter.mesh = mesh;
        }

    public void ApplyMesh(NativeArray<float3> vertices, NativeArray<int> triangles, NativeArray<Vector2> uvs, NativeArray<float3> normals)
    {
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        meshData.SetVertexBufferParams(
            vertices.Length,
            new VertexAttributeDescriptor(
                VertexAttribute.Position,
                VertexAttributeFormat.Float32,
                3
            ) 
        );
        NativeArray<float3> vertexBuffer = meshData.GetVertexData<float3>();
        vertexBuffer.CopyFrom(vertices);

        meshData.SetIndexBufferParams(
            triangles.Length,
            IndexFormat.UInt32
        );
        NativeArray<int> indexBuffer = meshData.GetIndexData<int>();
        indexBuffer.CopyFrom(triangles);  

        meshData.subMeshCount = 1;

        meshData.SetSubMesh(
            0,
            new SubMeshDescriptor(0, triangles.Length)
        );


        mesh.Clear();

        mesh.indexFormat = (vertices.Length > 65000) ? IndexFormat.UInt32 : IndexFormat.UInt16;

        Mesh.ApplyAndDisposeWritableMeshData(
            meshDataArray,
            mesh
        );   
        

        mesh.uv = uvs.ToArray();
        mesh.SetNormals(normals);
        mesh.RecalculateBounds();
        

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    public void ApplyTexture(Texture2D texture) {
        render.material.mainTexture = texture;
        render.material.SetFloat("_Smoothness", 0f);
    
    }
}