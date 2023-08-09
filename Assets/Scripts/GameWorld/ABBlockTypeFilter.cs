using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ABBlockTypeFilter : MonoBehaviour
{
    private ABBlock[] blockList;
    public MATERIALS ignoreMaterial;

    // Start is called before the first frame update
    void Start()
    {
        blockList = FindObjectsOfType<ABBlock>();
        foreach (ABBlock block in blockList)
        {
            if (block._material == ignoreMaterial)
            {
                foreach (Collider2D collider in GetComponents<Collider2D>())
                {
                    foreach (Collider2D collider2 in block.GetComponents<Collider2D>())
                    {
                        Physics2D.IgnoreCollision(collider, collider2);
                    }
                }
            }
        }
    }
}
