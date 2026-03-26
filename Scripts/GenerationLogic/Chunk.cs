//--------------------------------------------
//This code manages the mesh, collider and texture of a single Chunk
//--------------------------------------------
// - Oskar Benjamin Trillitzsch

using System.IO;
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
    public float[] heightmap;
    public Vector2Int chunkPos;
    public int generationId;
    public int maxAmplitude = 600;
    Mesh mesh;
    public bool spawnObjects;

    string saveFile;


    public void Init(int maxAmplitude, bool spawnObjects, int generationId)
    {
        this.maxAmplitude = maxAmplitude;
        this.spawnObjects = spawnObjects;
        this.generationId = generationId;
    }

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

        chunkPos = new Vector2Int((int)(transform.position.x / mesh.bounds.size.x), (int)(transform.position.z / mesh.bounds.size.z));
        saveFile = Application.persistentDataPath + "/chunk_" + chunkPos.x + "_" + chunkPos.y + ".chunk";

        heightmap = new float[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            heightmap[i] = vertices[i].y;

        Save();
    }



    public void ApplyTexture(Texture2D texture) {
        render.material.mainTexture = texture;
        render.material.SetFloat("_Smoothness", 0f);
    
    }

    private void Save()
    {
        string saveFile = Application.persistentDataPath + "/chunk_" + chunkPos.x + "_" + chunkPos.y + ".chunk";

        ChunkSaveData data = new ChunkSaveData
        {
            position = chunkPos,
            heightmap = heightmap
        };

        File.WriteAllBytes(saveFile, System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(data, true)));
    }

    private void Load(int chunkSize, int resolution)
    {
       
        ChunkSaveData data = JsonUtility.FromJson<ChunkSaveData>(File.ReadAllText(saveFile));

        mesh.Clear();

        Vector3[] vertices = new Vector3[data.heightmap.Length];
        float resolutionSizing = (float) chunkSize / (float) ( resolution - 1);

        for (int i = 0; i < vertices.Length; i++)
        {
            float x = i % resolution * resolutionSizing;
            float z = i / resolution * resolutionSizing;
            vertices[i] = new Vector3(x, data.heightmap[i], z);
        }

        


    }
    
}

[System.Serializable]

public struct ChunkSaveData
{
    public Vector2Int position;
    public float[] heightmap;
}