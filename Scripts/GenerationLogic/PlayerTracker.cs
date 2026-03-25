//--------------------------------------------
//This code tracks the players position and updates the active chunks accordingly
//--------------------------------------------
// - Oskar Benjamin Trillitzsch

using UnityEngine;

public class PlayerTracker : MonoBehaviour
{
    Vector2Int currentChunk;
    Vector2Int lastchunk = new Vector2Int(100,100);
    int chunkSize = 32;
    public GameObject chunkCoordinator;
    ChunkCoordinator coordinator;

    void Start()
    {
        coordinator = chunkCoordinator.GetComponent<ChunkCoordinator>();
    }
    void Update()
    {
        currentChunk = new Vector2Int(Mathf.FloorToInt(transform.position.x / chunkSize), Mathf.FloorToInt(transform.position.z / chunkSize));

        //Check if current chunk is the same as before, if not update world
        if(currentChunk != lastchunk)
        {
            coordinator.UpdateChunks(currentChunk);
            lastchunk = currentChunk;
        }
    }
}