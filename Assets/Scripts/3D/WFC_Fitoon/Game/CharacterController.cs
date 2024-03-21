using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : MonoBehaviour
{
    public float speed = 5f; // Velocidad de movimiento del jugador
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // FixedUpdate se utiliza para el movimiento con Rigidbody
    void FixedUpdate()
    {
        // Movimiento automático hacia adelante
        rb.MovePosition(rb.position + transform.forward * speed * Time.fixedDeltaTime);

        // Movimiento horizontal con las teclas A y D o flechas izquierda y derecha
        float horizontalInput = Input.GetAxis("Horizontal");
        Vector3 movement = new Vector3(horizontalInput, 0f, 0f) * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Verifica si la colisión es con el suelo u otro objeto
        if (collision.gameObject.CompareTag("Obstaculo"))
        {
            print("colision");
            // Agrega una fuerza hacia arriba para simular el rebote
            GetComponent<Rigidbody>().AddForce(Vector3.up * 5f, ForceMode.Impulse);
        }
    }
}
