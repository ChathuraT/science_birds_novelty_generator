using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Magician : ABGameObject
{

    // movement boundries of the magician (level space)
    float maxXCoordinate = 11.263f;
    float minXCoordinate = -7.25f;

    float movingStep = 0.01f;
    float movingDirection = 1;
    float maxMoveRange = 3;
    float movedDistance = 0;

    int movingFrame = 0; // magician is moved in every movingFrame number of frames
    int movingFrameCount = 0;
    float collisionRadius = 0.70f;
    LayerMask collisionLayers;
    int obstructDetectionWaitMovements = 200; // magician will wait this many movements to detect whether the movement is obstructed before making a jump
    int obstructDetectionCount = 0;
    bool initialLocationCheckCompleted = false;

    Vector3 oldPosition;

    // Start is called before the first frame update
    void Start()
    {
        // layers that will be ignored when detecting the collision
        collisionLayers = ~(1 << LayerMask.NameToLayer("Default") | 1 << LayerMask.NameToLayer("TransparentFX") | 1 << LayerMask.NameToLayer("ExternalAgents"));

    }

    private void FixedUpdate()
    {

        // initialLocationCheck in the first frame, this is to check whether the magician is initialized in a colliding position with other objects
        if (!initialLocationCheckCompleted)
        {
            if (isMagicianColliding())
            {
                // Debug.Log("Magician is colliding at the start, repositioning...");
                // reposition the magician in a non coliding location
                repositionInNonCollidingLocation();
            }
            initialLocationCheckCompleted = true;
        }

        for (int i = 0; i < ABGameWorld.SimulationSpeed; i++)
        {

            if (movingFrameCount >= movingFrame)
            {
                move();
                movingFrameCount = 0;
            }
            else
            {
                movingFrameCount += 1;
            }
        }

    }

    // moving to a non colliding posision
    private void repositionInNonCollidingLocation()
    {
        while (isMagicianColliding())
        {
            _rigidBody.transform.position = new Vector3(getXPosition(movingStep * 100), _rigidBody.transform.position.y, _rigidBody.transform.position.z);
            // Debug.Log("shifted");
        }


    }

    // move the magician while beinng sensitive to the collisions
    private void move()
    {


        // Vector3 oldPosition = _rigidBody.transform.position;


        if (isMagicianColliding())
        { // if magician is already collided before moving, make a jump

            if (obstructDetectionCount >= obstructDetectionWaitMovements)
            {
                //Debug.Log("Magician is already collided with something, jumping");

                //movingDirection *= -1;
                _rigidBody.velocity = new Vector2(0, 0);

                // jumping the magician
                _rigidBody.transform.position = new Vector3(getXPosition(movingStep * 100), _rigidBody.transform.position.y, _rigidBody.transform.position.z);
                obstructDetectionCount = 0;
            }
            obstructDetectionCount += 1;
        }
        else
        {
            // moving the magician one step
            _rigidBody.transform.position = new Vector3(getXPosition(movingStep), _rigidBody.transform.position.y, _rigidBody.transform.position.z);

            // if other objects are going to collide, change the direction
            if (isMagicianColliding())
            {
                //Debug.Log("Magician is going to collide with something, changing the direction");
                movingDirection *= -1;

                // moving the magician one step away from the colliding objects
                _rigidBody.transform.position = new Vector3(getXPosition(movingStep), _rigidBody.transform.position.y, _rigidBody.transform.position.z);
            }


        }

        // if the magician is rotated/fallen erect it
        if (_rigidBody.transform.rotation.z != 0)
        {
            _rigidBody.transform.rotation = new Quaternion(0, 0, 0, 0);
        }

    }

    float getXPosition(float movingStep)
    {

        float x_position = _rigidBody.transform.position.x + movingDirection * movingStep;

        if (x_position < minXCoordinate)
        {
            movingDirection = 1;
            return minXCoordinate;
        }
        else if (x_position > maxXCoordinate)
        {
            movingDirection = -1;
            return maxXCoordinate;
        }

        return x_position;
    }


    // checking the objects in the way which are going to be collided
    bool isMagicianColliding()
    {
        // getting the colliders of colliding objects using the overlap circle (exlude the layers in the collisionLayers)
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(new Vector2(_rigidBody.transform.position.x, _rigidBody.transform.position.y), collisionRadius, collisionLayers);

        /*
        foreach (var hitCollider in hitColliders)
        {
            Debug.Log(hitCollider);
        }
        */

        if (hitColliders.Length == 0)
            return false;

        return true;

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

    // collision detection along the x axis (not used)

    void detectCollisionUsingRayCast()
    {

        Vector2 forward = transform.TransformDirection(1, 0, 0);
        // Debug.DrawRay(transform.position, forward, Color.green);
        if (Physics2D.Raycast(transform.position, forward, 0.2f))
        {
            //Debug.Log("There is something in front of the object!");
        }
    }

    // stepwise movement without collision detection (not used)
    private void moveOld()
    {

        // moving the object
        _rigidBody.transform.position = new Vector3(_rigidBody.transform.position.x + movingDirection * movingStep, _rigidBody.transform.position.y, _rigidBody.transform.position.z);
        movedDistance += movingStep;

        // if max range is moved reverse the direction
        if (movedDistance > maxMoveRange)
        {
            movingDirection *= -1;
            movedDistance = 0;
        }

        // if the object is rotated erect it
        if (_rigidBody.transform.rotation.z != 0)
        {
            _rigidBody.transform.rotation = new Quaternion(0, 0, 0, 0);
        }

    }

}
