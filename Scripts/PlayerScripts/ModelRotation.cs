//--------------------------------------------
//This passes the camera rotation to the player model
//--------------------------------------------
// - Oskar Benjamin Trillitzsch


using UnityEngine;

public class ModelRotation : MonoBehaviour
{

    public Transform Rotation;

    void Update()
    {
        transform.rotation = Quaternion.Euler(0, Rotation.rotation.eulerAngles.y, 0);
    }
}
