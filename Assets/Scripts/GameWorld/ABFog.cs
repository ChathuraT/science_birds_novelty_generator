using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ABFog : MonoBehaviour
{
    public float extraDrag;
    private Rigidbody2D collisionRigidbody;

    private void Start()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, 5.0f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<ABBird>())
        {
            collisionRigidbody = collision.GetComponent<Rigidbody2D>();
            collisionRigidbody.drag += extraDrag;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<ABBird>())
        {
            collisionRigidbody = collision.GetComponent<Rigidbody2D>();
            collisionRigidbody.drag -= extraDrag;
        }
    }
}