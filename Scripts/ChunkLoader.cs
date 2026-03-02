using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor.Rendering;
using UnityEngine;

public class ChunkLoader : MonoBehaviour
{
    public Transform player;
    Vector2Int playerPosition;

    public GameObject terrainPrefab;
    public GameObject lowResTerrainPrefab;

    [Header("Chunk Settings")]
    // Must match terrain heightmap resolution
    [Range(2, 128)]
    public int viewDistanceInChunks = 2;
    public int chunkSize = 64;
    public int chunkResolution = 64;
    [Range(0f,0.1f)]
    public float biomeScale;
    public Transform parentFolder;
    public Transform lowResParentFolder;


    private Dictionary<Vector2Int, GameObject> loadedChunks = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> allGeneratedChunks = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> loadedLowResChunks = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> allGeneratedLowResChunks = new Dictionary<Vector2Int, GameObject>();

    private List<Vector2Int> keystoremove = new List<Vector2Int>();
    private List<Vector2Int> lowreskeystoremove = new List<Vector2Int>();




    void Start()
    {
        playerPosition = new Vector2Int(Mathf.FloorToInt(player.position.x / chunkSize), Mathf.FloorToInt(player.position.z / chunkSize));
    }
    void Update()
    {
        playerPosition = new Vector2Int(Mathf.FloorToInt(player.position.x / chunkSize), Mathf.FloorToInt(player.position.z / chunkSize));
        loadChunks();
        unloadChunks();
    }

    private void loadChunks()
    {
        for(int x = -viewDistanceInChunks; x <= viewDistanceInChunks; x++)
        {
            for(int z = -viewDistanceInChunks; z <= viewDistanceInChunks; z++)
            {
                Vector2Int t = playerPosition + new Vector2Int(x,z);
                double dist = Math.Sqrt(x*x + z*z);

                if(dist < viewDistanceInChunks * 0.5f && !loadedChunks.ContainsKey(t))
                {
                    generateChunk(t);
                } else if(dist >= viewDistanceInChunks * 0.5f && dist <= viewDistanceInChunks && !loadedLowResChunks.ContainsKey(t))
                {
                    generateLowResChunk(t);
                }
            }
        }
    }

    private void unloadChunks()
    {
        foreach(var chunk in loadedChunks)
        {
            int relx = Mathf.Abs(chunk.Key.x - playerPosition.x);
            int relz = Mathf.Abs(chunk.Key.y - playerPosition.y);

            double dist = Math.Sqrt(relx*relx + relz*relz);

            

            if(dist >= viewDistanceInChunks * 0.5f)
            {
                chunk.Value.SetActive(false);
                keystoremove.Add(chunk.Key);
            }
            
        }

        foreach(var chunk in loadedLowResChunks)
        {
            int relx = Mathf.Abs(chunk.Key.x - playerPosition.x);
            int relz = Mathf.Abs(chunk.Key.y - playerPosition.y);

            double dist = Math.Sqrt(relx*relx + relz*relz);

            if(dist > viewDistanceInChunks || dist < viewDistanceInChunks * 0.5f)
            {
                chunk.Value.SetActive(false);
                lowreskeystoremove.Add(chunk.Key);
                // Debug.Log("Unloaded Chunk");
            }
        
        }
        
        foreach(var key in keystoremove)
        {
            loadedChunks.Remove(key);
        }
        keystoremove.Clear();

        foreach(var key in lowreskeystoremove)
        {
            loadedLowResChunks.Remove(key);
        }
        lowreskeystoremove.Clear();
    }

    private void generateChunk(Vector2Int pos)
    {

        if(!allGeneratedChunks.ContainsKey(pos))
        {
            GameObject ter = Instantiate(terrainPrefab, new Vector3(pos.x * chunkSize , 0, pos.y * chunkSize), Quaternion.identity);
            ter.name = "Chunk_" + pos.x + "_" + pos.y;
            ter.GetComponent<ProcedualGenerator>().Init(chunkSize, Mathf.FloorToInt(chunkResolution));
            loadedChunks.Add(pos, ter);
            allGeneratedChunks.Add(pos, ter);
            ter.transform.parent = parentFolder;
        } else
        {
            allGeneratedChunks[pos].SetActive(true);
            loadedChunks.Add(pos, allGeneratedChunks[pos]);
        }

    }

    private void generateLowResChunk(Vector2Int pos)
    {
        if(!allGeneratedLowResChunks.ContainsKey(pos))
        {
            GameObject ter = Instantiate(terrainPrefab, new Vector3(pos.x * chunkSize , 0, pos.y * chunkSize), Quaternion.identity);
            ter.name = "LowResChunk_" + pos.x + "_" + pos.y;
            ter.GetComponent<ProcedualGenerator>().Init(chunkSize, Mathf.FloorToInt(chunkResolution * 0.5f));
            loadedLowResChunks.Add(pos, ter);
            allGeneratedLowResChunks.Add(pos, ter);
            ter.transform.parent = lowResParentFolder;
        } else
        {
            allGeneratedLowResChunks[pos].SetActive(true);
            loadedLowResChunks.Add(pos, allGeneratedLowResChunks[pos]);
        }
    }

    void regenerateTerrain()
    {
        loadedChunks.Clear();
        foreach(var chunk in allGeneratedChunks)
        {
            Destroy(chunk.Value);
        }
        allGeneratedChunks.Clear();
        loadedLowResChunks.Clear();
        foreach(var chunk in allGeneratedLowResChunks)
        {
            Destroy(chunk.Value);
        }
        allGeneratedLowResChunks.Clear();
    }
}

