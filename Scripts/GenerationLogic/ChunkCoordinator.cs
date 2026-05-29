//--------------------------------------------
//This code manages the chunk loading and unloading based on the players position
//--------------------------------------------
// - Oskar Benjamin Trillitzsch

using System.Collections.Generic;
using UnityEngine;

public class ChunkCoordinator : MonoBehaviour
{

    public int viewDistanceinChunks = 8;
    public int objectsViewDistanceInChunks = 2;
    LODSystem lodSystem = new LODSystem();
    public GameObject chunkStreamingQueue;
    ChunkStreamingQueue chunkStreamingQueueScript;
    public GameObject chunkSpawner;
    ChunkSpawner chunkSpawnerScript;

    Dictionary<Vector2Int, CoordinationData> activeChunks = new Dictionary<Vector2Int, CoordinationData>();
    List<Vector2Int> keystoRemove = new List<Vector2Int>();

    void Awake()
    {
        chunkStreamingQueueScript = chunkStreamingQueue.GetComponent<ChunkStreamingQueue>();
        chunkSpawnerScript = chunkSpawner.GetComponent<ChunkSpawner>();
        chunkStreamingQueueScript.Init(viewDistanceinChunks);
    }

    public void UpdateChunks(Vector2Int currentChunk)
    {

        //The for-loop was created by AI, as well as the lod-update logic,
        //but the enqueuing was made by without AI
        //Loop trough all the chunks in the view Distance in a spiral pattern
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

                    //Check if chunk is in view distance
                    if(dist > viewDistanceinChunks * viewDistanceinChunks)
                        continue;

                    ChunkLOD lod = lodSystem.getLOD(dist);
                    if(activeChunks.TryGetValue(pos, out var data))
                    {
                        // Check if we need to update LOD
                        if (lod != data.lod)
                        {
                            // If we're within the objects view distance, ensure objects are (re)spawned
                            if(dist <= objectsViewDistanceInChunks * objectsViewDistanceInChunks)
                            {
                                chunkStreamingQueueScript.EnqueueChunk(pos, lod, true, true);
                                activeChunks[pos] = new CoordinationData { lod = lod, hasObjects = true };
                            }
                            else
                            {
                                chunkStreamingQueueScript.EnqueueChunk(pos, lod, true, false);
                                chunkSpawnerScript.UnloadChildren(pos); 
                                activeChunks[pos] = new CoordinationData { lod = lod, hasObjects = false };
                                
                            }
                        }

                        if(!data.hasObjects && dist <= objectsViewDistanceInChunks * objectsViewDistanceInChunks) {
                            chunkStreamingQueueScript.EnqueueChunk(pos, lod, true, true);
                            activeChunks[pos] = new CoordinationData { lod = lod, hasObjects = true };
                        }

                        continue;

                    } else
                    {
                        // Check if objects should be spawned
                        if(dist <= objectsViewDistanceInChunks * objectsViewDistanceInChunks)
                        {
                            chunkStreamingQueueScript.EnqueueChunk(pos, lod, false, true);
                            activeChunks[pos] = new CoordinationData { lod = lod, hasObjects = true };

                        } else
                        {
                            chunkStreamingQueueScript.EnqueueChunk(pos, lod, false, false);
                            activeChunks[pos] = new CoordinationData { lod = lod, hasObjects = false };
                        }
                    }
                }
            }
        }
        
        //Check if any chunks need to be removed or have their objects toggled
        var keys = new List<Vector2Int>(activeChunks.Keys);

        foreach(var key in keys)
        {
            var chunk = activeChunks[key];
            Vector2Int diffrence = key - currentChunk;
            float distSq = diffrence.x * diffrence.x + diffrence.y * diffrence.y;

            if (distSq > viewDistanceinChunks * viewDistanceinChunks)
            {
                chunkSpawnerScript.DespawnChunk(key);
                keystoRemove.Add(key);
            }
            else if (distSq > objectsViewDistanceInChunks * objectsViewDistanceInChunks && chunk.hasObjects)
            {
                chunkSpawnerScript.UnloadChildren(key);
                activeChunks[key] = new CoordinationData { lod = chunk.lod, hasObjects = false };
            }
            else if (distSq <= objectsViewDistanceInChunks * objectsViewDistanceInChunks && !chunk.hasObjects)
            {
                chunkStreamingQueueScript.EnqueueChunk(key, chunk.lod, true, true);
            }
        }

        foreach(var key in keystoRemove)
        {
            activeChunks.Remove(key);
        }
        keystoRemove.Clear();            
    }

    struct CoordinationData
    {
        public ChunkLOD lod;
        public bool hasObjects;
    }
}