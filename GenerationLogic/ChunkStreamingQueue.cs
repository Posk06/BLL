using System.Collections.Generic;
using UnityEngine;

public class ChunkStreamingQueue : MonoBehaviour
{

    List<ChunkJob> activeJobs = new List<ChunkJob>();
    private Queue<CoordDistRelation> chunkGenerationQueue = new Queue<CoordDistRelation>();
    private HashSet<CoordDistRelation> queued = new HashSet<CoordDistRelation>();
    public int chunksPerTick;

    void Update()
    {
        
    }
    public void loadChunk(Vector2Int pos, ChunkLOD lod)
    {
        
    }

    public void unloadChunk(GameObject obj, ChunkLOD lod)
    {
        
    }

    private void EnqueueChunk(Vector2Int pos, LOD dist)
    {
        if (queued.Contains(new CoordDistRelation(pos, dist))) return;
        queued.Add(new CoordDistRelation(pos, dist));
        chunkGenerationQueue.Enqueue(new CoordDistRelation(pos, dist));
    }
    private void ProcessGenerationQueues()
    {
        // Process a limited number of high-res chunks first
        float processed = 0;
        while (processed < chunksPerTick && chunkGenerationQueue.Count > 0)
        {
            var pos = chunkGenerationQueue.Dequeue();
            queued.Remove(pos);
            // double-check we still need it
            //SpawnChunk();

            processed += Mathf.Pow(0.5f, (int) pos.lod);
        }
    }
}