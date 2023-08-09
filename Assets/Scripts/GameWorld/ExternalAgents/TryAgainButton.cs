using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TryAgainButton : MonoBehaviour
{
    public int children;
    List<GameObject> hitList = new List<GameObject>();

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject obj = collision.gameObject;

        if ((obj.GetComponent<ABBird>()) && (!hitList.Contains(obj)))
        {
            hitList.Add(obj);
            ABGameWorld gameWorld = FindObjectOfType<ABGameWorld>();
            for (int i = 0; i < children; ++i)
            {
                gameWorld.AddBird(ABWorldAssets.BIRDS["BirdRed"], ABWorldAssets.BIRDS["BirdRed"].transform.rotation);
            }
        }
    }

}
