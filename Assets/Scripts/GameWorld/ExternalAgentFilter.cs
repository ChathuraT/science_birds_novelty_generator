using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExternalAgentFilter : MonoBehaviour
{
    private GameObject[] externalAgentList;

    // Start is called before the first frame update
    void Start()
    {
        externalAgentList = GameObject.FindGameObjectsWithTag("ExternalAgent");
        foreach (GameObject obj in externalAgentList)
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
