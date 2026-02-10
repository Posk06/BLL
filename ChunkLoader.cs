using System;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class ChunkLoader : MonoBehaviour
{
    public Transform player;
    Vector2Int playerPosition;

    public GameObject terrainPrefab;

    [Header("Chunk Settings")]
    // Must match terrain heightmap resolution
    public int viewDistanceInChunks = 2;
    public int chunkSize = 64;
    [Range(0f,0.1f)]
    public float biomeScale;
    public Transform parentFolder;


    private Dictionary<Vector2Int, GameObject> loadedChunks = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> allGeneratedChunks = new Dictionary<Vector2Int, GameObject>();

    private List<Vector2Int> keystoremove = new List<Vector2Int>();




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

                if(Math.Sqrt(x*x + z*z) < viewDistanceInChunks)
                {
                    Vector2Int t = playerPosition + new Vector2Int(x,z);
                
                    if(!loadedChunks.ContainsKey(t))
                    {
                    generateChunk(t.x, t.y);
                    //Debug.Log("generated Chunk at " + t.x + "|" + t.y);
                    }    
                }
            
            }
        }
    }
    private void unloadChunks()
    {
        foreach(var chunk in loadedChunks)
        {
            int tooFarX = Mathf.Abs(chunk.Key.x - playerPosition.x);
            int tooFarZ = Mathf.Abs(chunk.Key.y - playerPosition.y);

            

            if(Math.Sqrt(tooFarX*tooFarX + tooFarZ*tooFarZ) > viewDistanceInChunks)
            {
                chunk.Value.SetActive(false);
                keystoremove.Add(chunk.Key);
                // Debug.Log("Removed Chunk");
            }
        }
        
        foreach(var key in keystoremove)
        {
            loadedChunks.Remove(key);
        }
        keystoremove.Clear();
    }

    private void generateChunk(int x, int z)
    {

        if(!allGeneratedChunks.ContainsKey(new Vector2Int(x,z)))
        {
            GameObject ter = Instantiate(terrainPrefab, new Vector3(x * chunkSize , 0, z * chunkSize), Quaternion.identity);
            ter.GetComponent<ProcedualGenerator>().Init(chunkSize);
            loadedChunks.Add(new Vector2Int(x,z), ter);
            allGeneratedChunks.Add(new Vector2Int(x,z), ter);
            ter.transform.parent = parentFolder;



        } else
        {
            allGeneratedChunks[new Vector2Int(x,z)].SetActive(true);
            loadedChunks.Add(new Vector2Int(x,z), allGeneratedChunks[new Vector2Int(x,z)]);
        }

    } 
}

