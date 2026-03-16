using Unity.VisualScripting;
using UnityEngine;

public class PlayerTracker : MonoBehaviour
{
    Vector2Int currentChunk;
    Vector2Int lastchunk;
    ChunkCoordinator coordinator;
    int chunkSize = 16;
    ChunkStreamingQueue queue;

    void Start()
    {
        coordinator = new ChunkCoordinator();
        queue.AddComponent<ChunkStreamingQueue>();
    }
    void Update()
    {
        currentChunk = new Vector2Int(Mathf.FloorToInt(transform.position.x / chunkSize), Mathf.FloorToInt(transform.position.z / chunkSize));

        if(currentChunk != lastchunk)
        {
            coordinator.UpdateChunks(currentChunk, queue);
            lastchunk = currentChunk;
        }
    }
}