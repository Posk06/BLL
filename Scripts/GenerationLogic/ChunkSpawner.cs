//--------------------------------------------
//This code manages the chunk spawning and despawning based on the players position,
//as well as the chunk pool
//--------------------------------------------
// - Oskar Benjamin Trillitzsch


using System.Collections.Generic;
using UnityEngine;

public class ChunkSpawner : MonoBehaviour
{
    public GameObject chunkPrefab;
    public int poolSize = 100;
    public int chunkSize = 64;
    public int chunkResolution = 64;
    List<GameObject> chunkPool = new List<GameObject>();
    Dictionary<Vector2Int, GameObject> spawnedChunks = new Dictionary<Vector2Int, GameObject>();
    public Transform chunkPoolParent;
    public GameObject terrainJobSystem;
    TerrainJobSystem terrainJobSystemScript;

    void Awake()
    {
        terrainJobSystemScript = terrainJobSystem.GetComponent<TerrainJobSystem>();
        terrainJobSystemScript.Init(chunkSize, chunkResolution);

        //Create the Chunk Pool
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(chunkPrefab, chunkPoolParent);
            obj.SetActive(false);
            chunkPool.Add(obj);
        }
    }

    public void SpawnChunk(Vector2Int position, ChunkLOD lod, bool replace, bool spawnObjects)
    {
        if (!replace)
        {
            if(chunkPool.Count > 0)
            {
                //Move from Pool to Position
                GameObject chunk = chunkPool[chunkPool.Count - 1];
                chunk.transform.position = new Vector3(position.x * chunkSize, 0, position.y * chunkSize);
                chunk.SetActive(true);
                chunk.name = "Chunk_" + position.x + "_" + position.y;

                //Move to correct LOD folder
                Transform lodFolder = chunkPoolParent.Find(lod.ToString()); //AI

                if (lodFolder == null) //AI
                {
                    var folderObj = new GameObject(lod.ToString());
                    folderObj.transform.SetParent(chunkPoolParent, false);
                    lodFolder = folderObj.transform;
                }

                chunk.transform.SetParent(lodFolder, false);

                chunkPool.RemoveAt(chunkPool.Count - 1);
                spawnedChunks[position] = chunk;
                Chunk chunkScript = chunk.GetComponent<Chunk>();
                chunkScript.spawnObjects = spawnObjects;

                //Call generation function
                terrainJobSystemScript.GenerateChunk(position, lod, chunkScript);
            }

        } else
        {
            Transform lodFolder = chunkPoolParent.Find(lod.ToString());
            
            //Check if chunk already exsists, if not return
            if (!spawnedChunks.TryGetValue(position, out GameObject chunk)) //AI
                return;         
            
            //Move to correct LOD folder
            if (lodFolder == null) //AI
            {
                var folderObj = new GameObject(lod.ToString());
                folderObj.transform.SetParent(chunkPoolParent, false);
                lodFolder = folderObj.transform;
            }

            chunk.transform.SetParent(lodFolder, false);

            //Call generation function
            terrainJobSystemScript.GenerateChunk(position, lod, chunk.GetComponent<Chunk>()); 
        }
    }

    public void DespawnChunk(Vector2Int position)
    {
        if (spawnedChunks.TryGetValue(position, out GameObject chunk)) //AI
        {
            //Move from Position to Pool
            chunk.SetActive(false);
            chunkPool.Add(chunk);
            spawnedChunks.Remove(position);
        }
    }
}