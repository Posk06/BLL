using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCam : MonoBehaviour
{


    public float sensY = 0f;
    public float sensX = 0f;

    public Transform orientation;
    float xRotation;
    float yRotation;


    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    void Update()
    {
        yRotation += Input.GetAxis("Mouse X") * Time.deltaTime * sensX;

        xRotation -= Input.GetAxis("Mouse Y") * Time.deltaTime * sensY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //Debug.Log(rotationX);
        //Debug.Log(rotationY);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }
}
