using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ChunkStreamingQueue : MonoBehaviour
{
    private Queue<QueueChunk> chunkGenerationQueue = new Queue<QueueChunk>();
    private HashSet<QueueChunk> queued = new HashSet<QueueChunk>();
    
    public int chunksPerTick = 2;
    public GameObject chunkSpawner;
    ChunkSpawner chunkSpawnerScript;
    bool processcheck = true;

    void Start()
    {
        chunkSpawnerScript = chunkSpawner.GetComponent<ChunkSpawner>();
    }

    void Update()
    {
        ProcessGenerationQueue();
    }

    public void EnqueueChunk(Vector2Int pos, ChunkLOD dist, bool replace)
    {
        if (queued.Contains(new QueueChunk(pos, dist, replace))) return;
        queued.Add(new QueueChunk(pos, dist, replace));
        chunkGenerationQueue.Enqueue(new QueueChunk(pos, dist, replace));
    }
    private void ProcessGenerationQueue()
    {
        // Process a limited number of high-res chunks first
        float processed = 0;
        while (processed < chunksPerTick && chunkGenerationQueue.Count > 0)
        {
            var pos = chunkGenerationQueue.Dequeue();
            queued.Remove(pos);
            // double-check we still need it
            chunkSpawnerScript.SpawnChunk(pos.coord, pos.lod, pos.replace);
            processed += Mathf.Pow(0.5f, (int) pos.lod);
            processcheck = true;
        }

        if(chunkGenerationQueue.Count == 0 && processcheck)
        {
            processcheck = false;
            Debug.Log("Finished processing chunk queue after " + Time.timeSinceLevelLoad + " seconds");
        }
    }



    private struct QueueChunk
    {
        public Vector2Int coord;
        public ChunkLOD lod;
        public bool replace;


        public QueueChunk(Vector2Int coord, ChunkLOD lod, bool replace)
        {
            this.coord = coord;
            this.lod = lod;
            this.replace = replace;
        }
    }
}