using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdShooter: ABGameObject
{
    float movingStep = 0.08f;
    float collisionRadius = 2f;
    int comingBirdInd = -1;
    int lastBirdInd = -1;
    bool ifMoved;
    LineRenderer line;
    int lineActiveCount;
    int lineExistenceTime = 10;

    LayerMask collisionMaskingLayers;

    // Start is called before the first frame update
    void Start()
    {

        ifMoved = false;
        lineActiveCount = 0;
        collisionMaskingLayers = ~(1 << LayerMask.NameToLayer("Default") | 1 << LayerMask.NameToLayer("TransparentFX") |
            1 << LayerMask.NameToLayer("Platforms") | 1 << LayerMask.NameToLayer("ExternalAgents") |
            1 << LayerMask.NameToLayer("Slingshot") | 1 << LayerMask.NameToLayer("Blocks") 
            );

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        for (int i = 0; i < ABGameWorld.SimulationSpeed; i++)
        {
            lineActiveCount++;
            move();
            if(lineActiveCount > lineExistenceTime && line!=null) {
                line.enabled = false;
            }
        }
    }

    bool IfBirdComing()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(new Vector2(_rigidBody.transform.position.x, _rigidBody.transform.position.y), collisionRadius, collisionMaskingLayers);

        if (hitColliders.Length >= 2)
        {
            foreach (Collider2D colliderObj in hitColliders)
            {
                if (colliderObj.name.Contains("bird"))
                {
                    string[] splited = colliderObj.name.Split(char.Parse("_"));
                    if (int.Parse(splited[1]) > lastBirdInd)
                    {
                        colliderObj.gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(0,0);
                        line = GetComponent<LineRenderer>();
                        line.positionCount = 2;
                        line.startWidth = 0.25f;
                        line.endWidth = 0.25f;
                        line.SetPosition(0, _rigidBody.transform.position);
                        line.SetPosition(1, colliderObj.transform.position);
                        line.enabled = true;
                        lineActiveCount = 0;
                        comingBirdInd = int.Parse(splited[1]);
                        return true;
                    }

                }

            }

        }
        return false;
    }


    void move()
    {
        if (IfBirdComing())
        {
            ifMoved = true;
            lastBirdInd = comingBirdInd;
        }

        if (lastBirdInd < comingBirdInd)
        {
            ifMoved = false;
        }

    }

#if UNITY_EDITOR
    // visualize the collision circle in the editor
    void OnDrawGizmos()
    {

        int vertexCount = 50;
        float deltaTheta = (2f * Mathf.PI) / vertexCount;
        float theta = 0f;

        Vector3 oldPos = _rigidBody.transform.position;
        for (int i = 0; i < vertexCount + 1; i++)
        {
            Vector3 pos = new Vector3(collisionRadius * Mathf.Cos(theta), collisionRadius * Mathf.Sin(theta), 0f);
            Gizmos.DrawLine(oldPos, transform.position + pos);
            oldPos = transform.position + pos;

            theta += deltaTheta;
        }
    }
#endif
}
