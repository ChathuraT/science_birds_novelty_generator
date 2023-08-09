using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ABPlatformFilter : MonoBehaviour
{
    private GameObject[] platformList;

    // Start is called before the first frame update
    void Start()
    {
        platformList = GameObject.FindGameObjectsWithTag("Platform");
        foreach (GameObject obj in platformList)
        {
            foreach (Collider2D collider in GetComponents<Collider2D>())
            {
                foreach (Collider2D collider2 in obj.GetComponents<Collider2D>())
                {
                    Physics2D.IgnoreCollision(collider, collider2);
                }
            }
        }
    }
}
