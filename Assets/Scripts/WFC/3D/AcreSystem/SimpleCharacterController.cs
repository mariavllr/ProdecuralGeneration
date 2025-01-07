using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCharacterController : MonoBehaviour
{
    public float speed = 5f;             // Velocidad de movimiento
    public float turnSpeed = 360f;       // Velocidad de rotaci�n
    public Animator animator;            // Referencia al Animator del personaje
    public Rigidbody rb;                 // Referencia al Rigidbody del personaje

    private Vector3 movementDirection;

    void Update()
    {
        // Obtener la entrada del teclado (WASD o flechas)
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Definir la direcci�n de movimiento en funci�n del vector forward del personaje
        movementDirection = new Vector3(0, 0, vertical).normalized;

        // Girar el personaje seg�n la entrada horizontal (eje X)
        if (horizontal != 0)
        {
            float turn = horizontal * turnSpeed * Time.deltaTime;
            transform.Rotate(0, turn, 0);
        }

        // Cambiar la animaci�n si el personaje se est� moviendo
        if (movementDirection.magnitude >= 0.1f)
        {
            animator.SetBool("isWalking", true);
        }
        else
        {
            animator.SetBool("isWalking", false);
        }
    }

    void FixedUpdate()
    {
        // Mover al personaje en la direcci�n hacia donde mira (forward)
        Vector3 move = transform.forward * movementDirection.z * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);
    }
}
