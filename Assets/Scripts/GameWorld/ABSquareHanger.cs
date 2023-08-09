using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ABSquareHanger : MonoBehaviour
{
    private GameObject[] gameObjects;
    //private PolygonCollider2D[] polygonColliders;
    //private PolygonCollider2D topCollider;
    //private PolygonCollider2D bottomCollider;

    private BoxCollider2D[] boxColliders;
    private BoxCollider2D topCollider;
    private BoxCollider2D bottomCollider;
    private BoxCollider2D ropeCollider;

    private GameObject topAnchor;
    private GameObject bottomAnchor;

    // Start is called before the first frame update
    void Awake()
    {
        boxColliders = GetComponents<BoxCollider2D>();

        foreach (BoxCollider2D collider in boxColliders)
        {
            if (collider.offset.y < 0.1)
            {
                bottomCollider = collider;
            }
            else if (collider.offset.y > 2.9)
            {
                topCollider = collider;
            }
            else
            {
                ropeCollider = collider;
            }
        }

        topAnchor = transform.parent.transform.Find("Top Anchor").gameObject;
        bottomAnchor = transform.parent.transform.Find("Bottom Anchor").gameObject;

        gameObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject gameObject in gameObjects)
        {
            if (gameObject != topAnchor)
            {
                foreach (Collider2D collider2 in gameObject.GetComponents<Collider2D>())
                {
                    Physics2D.IgnoreCollision(topCollider, collider2);
                }
            }
            if (gameObject != bottomAnchor)
            {
                foreach (Collider2D collider2 in gameObject.GetComponents<Collider2D>())
                {
                    Physics2D.IgnoreCollision(bottomCollider, collider2);
                }
            }
            if (!(gameObject.GetComponent<ABBird>()))
            {
                foreach (Collider2D collider2 in gameObject.GetComponents<Collider2D>())
                {
                    Physics2D.IgnoreCollision(ropeCollider, collider2);
                }
            }
        }
    }
}
