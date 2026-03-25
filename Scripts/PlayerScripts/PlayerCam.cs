//--------------------------------------------
//This code manages the rotaton of the camera based on mosue movements
//--------------------------------------------
// - Oskar Benjamin Trillitzsch

using UnityEngine;

public class PlayerCam : MonoBehaviour
{


    public float sensY = 0f;
    public float sensX = 0f;
    float xRotation;
    float yRotation;


    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    void LateUpdate()
    {
    yRotation += Input.GetAxis("Mouse X") * sensX * Time.deltaTime;
    xRotation -= Input.GetAxis("Mouse Y") * sensY * Time.deltaTime;
    xRotation = Mathf.Clamp(xRotation, -90f, 90f);

    Quaternion target = Quaternion.Euler(xRotation, yRotation, 0f);
    transform.rotation = Quaternion.Slerp(transform.rotation, target, 20f * Time.deltaTime);
    }
}
