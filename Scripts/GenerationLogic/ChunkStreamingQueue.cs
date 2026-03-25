//--------------------------------------------
//This code handles the spawning of chunks by portioning it over multiple ticks or frames
//--------------------------------------------
// - Oskar Benjamin Trillitzsch

using System.Collections.Generic;
using UnityEngine;

public class ChunkStreamingQueue : MonoBehaviour
{
    private Queue<QueueChunk> chunkGenerationQueue = new Queue<QueueChunk>(); 
    private HashSet<QueueChunk> queued = new HashSet<QueueChunk>();
    
    public int chunksPerTick = 2;
    public int ticksPerSecond = 10;
    public GameObject chunkSpawner;
    ChunkSpawner chunkSpawnerScript;
    void Start()
    {
        chunkSpawnerScript = chunkSpawner.GetComponent<ChunkSpawner>();
    }

    float time = 0;

    void Update()
    {
        //Updates only when a tick has passed
        time += Time.deltaTime;
        if (time >= 1f / ticksPerSecond)
        {
            time = 0;
            ProcessGenerationQueue();
        }
    }

    //AI generated, but modifed by QueueChunk struct
    //This method adds a chunk to the generation Queue while maintaining relevant Data
    public void EnqueueChunk(Vector2Int pos, ChunkLOD dist, bool replace, bool spawnObjects)
    {
        if (queued.Contains(new QueueChunk(pos, dist, replace, spawnObjects))) return;
        queued.Add(new QueueChunk(pos, dist, replace, spawnObjects));
        chunkGenerationQueue.Enqueue(new QueueChunk(pos, dist, replace, spawnObjects));
    }

    //Ai generated, but modifed by QueueChunk struct and diffrent progress-values for diffrent LODs
    //This method processes a limited number of chunks per tick to distrubute the workload
    private void ProcessGenerationQueue()
    {
        // Process a limited number of high-res chunks first
        float processed = 0;
        while (processed < chunksPerTick && chunkGenerationQueue.Count > 0)
        {
            var pos = chunkGenerationQueue.Dequeue();
            queued.Remove(pos);
            // double-check we still need it
            chunkSpawnerScript.SpawnChunk(pos.coord, pos.lod, pos.replace, pos.spawnObjects);
            processed += Mathf.Pow(0.5f, (int) pos.lod);
        }
    }

    private struct QueueChunk
    {
        public Vector2Int coord;
        public ChunkLOD lod;
        public bool replace;
        public bool spawnObjects;


        public QueueChunk(Vector2Int coord, ChunkLOD lod, bool replace, bool spawnObjects)
        {
            this.coord = coord;
            this.lod = lod;
            this.replace = replace;
            this.spawnObjects = spawnObjects;
        }
    }
}