using System.Collections.Generic;
using Unity.VisualScripting;
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

    void Start()
    {
        terrainJobSystemScript = terrainJobSystem.GetComponent<TerrainJobSystem>();
        terrainJobSystemScript.Init(chunkSize, chunkResolution);
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(chunkPrefab, chunkPoolParent);
            obj.SetActive(false);
            chunkPool.Add(obj);
        }
    }

    public void SpawnChunk(Vector2Int position, ChunkLOD lod, bool replace)
    {
        if (!replace)
        {
            if(chunkPool.Count > 0)
            {
                GameObject chunk = chunkPool[chunkPool.Count - 1];
                chunk.transform.position = new Vector3(position.x * chunkSize, 0, position.y * chunkSize);
                chunk.SetActive(true);
                chunk.name = "Chunk_" + position.x + "_" + position.y;

                Transform lodFolder = chunkPoolParent.Find(lod.ToString());

                if (lodFolder == null)
                {
                    var folderObj = new GameObject(lod.ToString());
                    folderObj.transform.SetParent(chunkPoolParent, false);
                    lodFolder = folderObj.transform;
                }

                chunk.transform.SetParent(lodFolder, false);

                chunkPool.RemoveAt(chunkPool.Count - 1);
                spawnedChunks[position] = chunk;
                terrainJobSystemScript.GenerateChunk(position, lod, chunk.GetComponent<Chunk>());
            }

        } else
        {

            Transform lodFolder = chunkPoolParent.Find(lod.ToString());

            if (!spawnedChunks.TryGetValue(position, out GameObject chunk))
                return;

            if (lodFolder == null)
            {
                var folderObj = new GameObject(lod.ToString());
                folderObj.transform.SetParent(chunkPoolParent, false);
                lodFolder = folderObj.transform;
            }

            chunk.transform.SetParent(lodFolder, false);
            terrainJobSystemScript.GenerateChunk(position, lod, chunk.GetComponent<Chunk>()); 
        }
    }

    public void DespawnChunk(Vector2Int position)
    {
        if (spawnedChunks.TryGetValue(position, out GameObject chunk))
        {
            chunk.SetActive(false);
            chunkPool.Add(chunk);
            spawnedChunks.Remove(position);
        }
    }
}