//--------------------------------------------
//This passes the player position to the camera
//--------------------------------------------
// - Oskar Benjamin Trillitzsch


using UnityEngine;

public class MoveCamera : MonoBehaviour
{

    public Transform cameraPosition;

    // Update is called once per frame
    void Update()
    {
        transform.position = cameraPosition.position;
    }
}
