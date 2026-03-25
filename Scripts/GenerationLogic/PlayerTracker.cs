using Unity.VisualScripting;
using UnityEngine;

public class PlayerTracker : MonoBehaviour
{
    Vector2Int currentChunk;
    Vector2Int lastchunk = new Vector2Int(100,100);
    int chunkSize = 16;
    public GameObject chunkCoordinator;
    ChunkCoordinator coordinator;

    void Start()
    {
        coordinator = chunkCoordinator.GetComponent<ChunkCoordinator>();
    }
    void Update()
    {
        currentChunk = new Vector2Int(Mathf.FloorToInt(transform.position.x / chunkSize), Mathf.FloorToInt(transform.position.z / chunkSize));

        if(currentChunk != lastchunk)
        {
            coordinator.UpdateChunks(currentChunk);
            lastchunk = currentChunk;
        }
    }
}