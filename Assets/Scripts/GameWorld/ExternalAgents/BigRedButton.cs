using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BigRedButton : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject obj = collision.gameObject;
        Debug.Log(obj);

        if (obj.GetComponent<ABBird>())
        {
            Debug.Log("I will be avenged!");
            ABBird[] birdList = FindObjectsOfType<ABBird>();
            foreach (ABBird bird in birdList)
            {
                bird.Die();
            }
        }
    }
}
