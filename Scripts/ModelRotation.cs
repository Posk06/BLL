using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelRotation : MonoBehaviour
{

    public Transform Rotation;

    // Update is called once per frame
    void Update()
    {
        transform.rotation = Quaternion.Euler(0, Rotation.rotation.eulerAngles.y, 0);
    }
}
