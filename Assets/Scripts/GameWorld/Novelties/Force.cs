using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Force : MonoBehaviour
{
    [SerializeField] public float forceMagnitude; // magnitude of the applied force
    [SerializeField] string forceDirection; // direction of the force, can be H or V 
    HashSet<Rigidbody2D> bodiesInsideTurbulence;
    // bool m_Started;

    private void Start()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, 5.0f);
        bodiesInsideTurbulence = new HashSet<Rigidbody2D>();

        // get the bodies that are already inside the turbulence at the start of the game level
        getObjectsInsideAirTurbulenceAtTheStart();
        // m_Started = true;
    }

    private void FixedUpdate()
    {
        //for (int i = 0; i < ABGameWorld.SimulationSpeed; i++)
       // {
            // Add air turbulence force to the game objects inside the air turbulence
            AddAirTurbulenceForce();
       // }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // A rigibody entered the air turbulance, capture it
        string tagOfTheCollidedObject = collision.gameObject.tag;
        // exclude the pigs - air turbulence doesnt affect the pigs
        if (!(tagOfTheCollidedObject == "PigSmall" | tagOfTheCollidedObject == "PigMedium" | tagOfTheCollidedObject == "PigBig" | tagOfTheCollidedObject == "Platform"))
        {
            // Debug.Log("rigidbody entered the storm " + collision.GetComponent<Rigidbody2D>());
            bodiesInsideTurbulence.Add(collision.GetComponent<Rigidbody2D>());
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // A rigibody exited the air turbulance, remove it
        // Debug.Log("body exited from storm " + collision.GetComponent<Rigidbody2D>());
        //if (bodiesInsideTurbulence.Contains(collision.GetComponent<Rigidbody2D>()))
        bodiesInsideTurbulence.Remove(collision.GetComponent<Rigidbody2D>());
    }

    private void AddAirTurbulenceForce()
    {
        foreach (Rigidbody2D rigidbody in bodiesInsideTurbulence)
        {
            if (rigidbody && (rigidbody.velocity.magnitude > 0.1f))
            {
                if (forceDirection == "H")
                {
                    // rigidbody.AddForce(transform.right * forceMagnitude);
                    rigidbody.velocity += new Vector2(forceMagnitude, 0.0f); // add a velocity instead of a force to make a similar effect on different objects (o.w. wood and stone behave differently due to the differences in their masses when a force is applied)
                }
                else
                {
                    // rigidbody.AddForce(transform.up * forceMagnitude);
                    rigidbody.velocity += new Vector2(0.0f, forceMagnitude); // add a velocity instead of a force to make a similar effect on different objects (o.w. wood and stone behave differently due to the differences in their masses when a force is applied)

                }
            }
        }
    }

    private void getObjectsInsideAirTurbulenceAtTheStart()
    {
        Collider2D[] hitColliders = Physics2D.OverlapBoxAll(gameObject.transform.position, transform.localScale / 1.55f, 0.0f, 1 << 10); // layermask: 10 is for blocks 
        foreach (Collider2D collider in hitColliders)
        {
            // Debug.Log(collider.gameObject);
            string tagOfTheCollidedObject = collider.gameObject.tag;

            if (!(tagOfTheCollidedObject == "PigSmall" | tagOfTheCollidedObject == "PigMedium" | tagOfTheCollidedObject == "PigBig")) // exclude the pigs - air turbulence doesnt affect the pigs
            {
                bodiesInsideTurbulence.Add(collider.gameObject.GetComponent<Rigidbody2D>());
            }
        }

        //Draw the Box Overlap as a gizmo to show where it currently is testing. Click the Gizmos button to see this
        //void OnDrawGizmos()
        //{
        //    Gizmos.color = Color.red;
        //    //Check that it is being run in Play Mode, so it doesn't try to draw this in Editor mode
        //    if (m_Started)
        //        //Draw a cube where the OverlapBox is (positioned where your GameObject is as well as a size)
        //        Gizmos.DrawWireCube(gameObject.transform.position, transform.localScale / 1.55f);
        //}
    }
}