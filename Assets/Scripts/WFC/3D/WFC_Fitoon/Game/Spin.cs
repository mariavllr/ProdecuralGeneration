using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spin : MonoBehaviour
{
    public float rotationSpeed = 50f; // Velocidad de rotación

    // Update se llama una vez por frame
    void Update()
    {
        // Rotar el objeto alrededor del eje Y a la velocidad constante
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}
