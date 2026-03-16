using UnityEngine;

public class ChunkCoordinator
{

    int viewDistanceinChunks;
    LODSystem lodSystem = new LODSystem();



    public void UpdateChunks(Vector2Int currentChunk, ChunkStreamingQueue queue)
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
                    ChunkLOD lod = lodSystem.getLOD(dist);
                    queue.loadChunk(pos, lod);
                }
            }
        }            
    }
}