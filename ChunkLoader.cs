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
    [Range(0f,0.1f)]
    public float biomeScale;
    public Transform parentFolder;


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

                if(Math.Sqrt(x*x + z*z) < viewDistanceInChunks)
                {
                    Vector2Int t = playerPosition + new Vector2Int(x,z);
                
                    if(!loadedChunks.ContainsKey(t))
                    {
                        if(!loadedLowResChunks.ContainsKey(t)) {
                            generateLowResChunk(t);
                        } else
                        {
                            generateChunk(t);
                            loadedLowResChunks[t].SetActive(false);                        
                        }
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

            

            if(Math.Sqrt(tooFarX*tooFarX + tooFarZ*tooFarZ) > viewDistanceInChunks * 0.5f)
            {
                chunk.Value.SetActive(false);
                keystoremove.Add(chunk.Key);
            }
            
        }

        foreach(var chunk in loadedLowResChunks)
        {
            int tooFarX = Mathf.Abs(chunk.Key.x - playerPosition.x);
            int tooFarZ = Mathf.Abs(chunk.Key.y - playerPosition.y);

            if(Math.Sqrt(tooFarX*tooFarX + tooFarZ*tooFarZ) > viewDistanceInChunks)
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
            //ter.GetComponent<ProcedualGenerator>().Init(chunkSize);
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
            //ter.GetComponent<ProcedualGenerator>().Init(chunkSize);
            loadedLowResChunks.Add(pos, ter);
            allGeneratedLowResChunks.Add(pos, ter);
            ter.transform.parent = parentFolder;
        } else
        {
            allGeneratedLowResChunks[pos].SetActive(true);
            loadedLowResChunks.Add(pos, allGeneratedLowResChunks[pos]);
        }
    }
}

