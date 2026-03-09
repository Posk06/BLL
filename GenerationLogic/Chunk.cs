using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Chunk : MonoBehaviour
{
    MeshFilter meshFilter;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    public void ApplyMesh(Vector3[] vertices, int[] triangles, Texture2D texture)
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = (vertices.Length > 65000) ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;

        GetComponent<Renderer>().material.SetTexture("_BaseMap", texture);
        GetComponent<Renderer>().material.SetTexture("_MainTex", texture);

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }
}