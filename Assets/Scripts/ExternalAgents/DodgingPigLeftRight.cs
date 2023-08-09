using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DodgingPigLeftRight : ABPig
{
    float movingStep = 0.08f;
    float collisionRadius = 2f;
    int comingBirdInd = -1;
    int lastBirdInd = -1;
    bool ifMoved;

    LayerMask collisionMaskingLayers;

    // Start is called before the first frame update
    void Start()
    {

        ifMoved = false;

        collisionMaskingLayers = ~(1 << LayerMask.NameToLayer("Default") | 1 << LayerMask.NameToLayer("TransparentFX") |
            1 << LayerMask.NameToLayer("Platforms") | 1 << LayerMask.NameToLayer("ExternalAgents") |
            1 << LayerMask.NameToLayer("Slingshot") | 1 << LayerMask.NameToLayer("Blocks")
            );

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        move();
    }

    bool IfBirdComing()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(new Vector2(_rigidBody.transform.position.x, _rigidBody.transform.position.y), collisionRadius, collisionMaskingLayers);

        if (hitColliders.Length >= 2)
        {
            foreach(Collider2D collider2D in hitColliders)
            {
                if (collider2D.name.Contains("bird"))
                {
                    string[] splited = collider2D.name.Split(char.Parse("_"));
                    if (int.Parse(splited[1]) > lastBirdInd)
                    {
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
        if (IfBirdComing() && !ifMoved)
        {
            if (Random.value > 0.5)
            {
                _rigidBody.AddForce(Vector3.right * movingStep * 10, ForceMode2D.Impulse);

            }

            else
            {
                _rigidBody.AddForce(Vector3.left * movingStep * 10, ForceMode2D.Impulse);

            }
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
