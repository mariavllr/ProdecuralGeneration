using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour
{
    public Transform target;    // El personaje que la c�mara debe seguir
    public Vector3 offset;      // Offset relativo al personaje (en su espacio local)
    public float followSpeed = 10f;  // Velocidad de seguimiento de la c�mara

    void FixedUpdate()
    {
        // Calcular la posici�n deseada usando el offset basado en la rotaci�n del personaje
        Vector3 desiredPosition = target.TransformPoint(offset);

        // Suavizar la transici�n hacia la nueva posici�n
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        // Hacer que la c�mara siempre mire al personaje
        transform.LookAt(target);
    }
}
