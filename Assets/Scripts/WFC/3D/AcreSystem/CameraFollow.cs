using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour
{
    public Transform target;    // El personaje que la cámara debe seguir
    public Vector3 offset;      // Offset relativo al personaje (en su espacio local)
    public float followSpeed = 10f;  // Velocidad de seguimiento de la cámara

    void FixedUpdate()
    {
        // Calcular la posición deseada usando el offset basado en la rotación del personaje
        Vector3 desiredPosition = target.TransformPoint(offset);

        // Suavizar la transición hacia la nueva posición
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        // Hacer que la cámara siempre mire al personaje
        transform.LookAt(target);
    }
}
