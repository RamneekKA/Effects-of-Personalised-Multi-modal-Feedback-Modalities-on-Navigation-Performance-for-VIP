using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainRotorRotation : MonoBehaviour
{
    public float rotationSpeed = 1000f; // Velocidade da rotação

    void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.Self);
    }
}
