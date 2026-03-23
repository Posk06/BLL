using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class ChunkCoordinator : MonoBehaviour
{

    public int viewDistanceinChunks = 8;
    LODSystem lodSystem = new LODSystem();
    public GameObject chunkStreamingQueue;
    ChunkStreamingQueue chunkStreamingQueueScript;
    public GameObject chunkSpawner;
    ChunkSpawner chunkSpawnerScript;

    Dictionary<Vector2Int, ChunkLOD> activeChunks = new Dictionary<Vector2Int, ChunkLOD>();
    List<Vector2Int> keystoRemove = new List<Vector2Int>();

    void Start()
    {
        chunkStreamingQueueScript = chunkStreamingQueue.GetComponent<ChunkStreamingQueue>();
        chunkSpawnerScript = chunkSpawner.GetComponent<ChunkSpawner>();
    }

    public void UpdateChunks(Vector2Int currentChunk)
    {
        for (int r = 0; r <= viewDistanceinChunks; r++) 
        {
            for (int x = -r; x <= r; x++)
            {
                for (int z = -r; z <= r; z++)
                {
                    if (Mathf.Abs(x) != r && Mathf.Abs(z) != r)
                        continue;

                    Vector2Int pos = currentChunk + new Vector2Int(x,z);
                    float dist = x*x + z*z;

                    if(dist > viewDistanceinChunks * viewDistanceinChunks)
                        continue;

                    ChunkLOD lod = lodSystem.getLOD(dist);
                    if(activeChunks.TryGetValue(pos, out var chunkLOD))
                    {
                        // Check if we need to update LOD
                        if (lod != chunkLOD)
                        {
                            activeChunks[pos] = lod;
                            // Update chunk LOD
                            chunkStreamingQueueScript.EnqueueChunk(pos, lod, true);
                        }
                        continue;
                    } else
                    {
                        chunkStreamingQueueScript.EnqueueChunk(pos, lod, false);
                        activeChunks.Add(pos, lod);
                    }
                }
            }
        }

        foreach(var chunk in activeChunks)
        {
            Vector2Int delta = chunk.Key - currentChunk;
            float dist = delta.x * delta.x + delta.y * delta.y;
            if (dist > viewDistanceinChunks * viewDistanceinChunks)
            {
                chunkSpawnerScript.DespawnChunk(chunk.Key);
                keystoRemove.Add(chunk.Key);
                Debug.Log("Unloading chunk at " + chunk.Key);
            }
        }

        foreach(var key in keystoRemove)
        {
            activeChunks.Remove(key);
        }
        keystoRemove.Clear();            
    }
}