﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wizard : ABGameObject
{

    // movement boundries of the wizard (level space)
    float maxXCoordinate = 14.54f;
    float minXCoordinate = -6.51f;
    float maxYCoordinate = 8.66f;
    float minYCoordinate = -2.4f;

    float movingStep = 0.01f;
    float movingXDirection = 1;
    float movingYDirection = 1;

    int movingFrame = 0; // wizard is moved in every movingFrame number of frames
    int movingFrameCount = 0;
    float collisionRadius = 0.80f;
    LayerMask collisionLayers;
    int obstructDetectionWaitMovements = 20; // wizard will wait this many movements to detect whether the movement is obstructed before making a jump
    int obstructDetectionCount = 0;

    int yDirectionChangeWaitMovements = 1000;  // wizard will wait this many movements to change its y direction
    int yDirectionChangeCount = 0;
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

        // initialLocationCheck in the first frame, this is to check whether the wizard is initialized in a colliding position with other objects
        if (!initialLocationCheckCompleted)
        {
            if (isWizardColliding())
            {
                Debug.Log("Wizard is colliding at the start, repositioning...");
                // reposition the wizard in a non coliding location
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
        while (isWizardColliding())
        {
            _rigidBody.transform.position = new Vector3(getXPosition(movingStep * 100), _rigidBody.transform.position.y, _rigidBody.transform.position.z);
            // Debug.Log("shifted");
        }


    }

    // move the wizard while beinng sensitive to the collisions
    private void move()
    {

        // moving the wizard one step
        _rigidBody.transform.position = new Vector3(getXPosition(movingStep), getYPosition(movingStep), _rigidBody.transform.position.z);

        // if other objects are going to collide, change the direction
        if (isWizardColliding())
        {
            Debug.Log("wizard is going to collide with something, changing the direction");
            movingXDirection *= -1;
            movingYDirection *= -1;

            // yDirectionChangeCount = 0; // reset the direction change count

            // moving the wizard one step away from the colliding objects
            _rigidBody.transform.position = new Vector3(getXPosition(movingStep), getYPosition(movingStep), _rigidBody.transform.position.z);
        }


        /*
        if (isWizardColliding())
        { // if wizard is already collided before moving, make a jump

            if (obstructDetectionCount >= obstructDetectionWaitMovements)
            {
                Debug.Log("wizard is obstructed");

                // movingXDirection *= -1;
                // jumping the wizard
                _rigidBody.transform.position = new Vector3(getXPosition(movingStep * 10), getYPosition(movingStep), _rigidBody.transform.position.z);
                obstructDetectionCount = 0;
            }
            obstructDetectionCount += 1;
        }
        else
        {
            // moving the wizard one step
            _rigidBody.transform.position = new Vector3(getXPosition(movingStep), getYPosition(movingStep), _rigidBody.transform.position.z);

            // if other objects are going to collide, change the direction
            if (isWizardColliding())
            {
                movingXDirection *= -1;
                Debug.Log("wizard is going to collide with something, changing the direction");

                // moving the wizard one step away from the colliding objects
                _rigidBody.transform.position = new Vector3(getXPosition(movingStep), getYPosition(movingStep), _rigidBody.transform.position.z);
            }


        }
        */
        // if the wizard is rotated/fallen erect it
        if (_rigidBody.transform.rotation.z != 0)
        {
            _rigidBody.transform.rotation = new Quaternion(0, 0, 0, 0);
        }

    }

    float getXPosition(float movingStep)
    {

        float x_position = _rigidBody.transform.position.x + movingXDirection * movingStep;

        if (x_position < minXCoordinate)
        {
            movingXDirection = 1;
            _rigidBody.velocity = new Vector2(0, 0);
            return x_position + 0.05f;
        }
        else if (x_position > maxXCoordinate)
        {
            movingXDirection = -1;
            _rigidBody.velocity = new Vector2(0, 0);
            return x_position - 0.05f;
        }

        return x_position;
    }

    float getYPosition(float movingStep)
    {
        // randomly change the movingYDirection
        // movingYDirection = Random.Range(0, 2) * 2 - 1;
        // Debug.Log("movingYDirection" + movingYDirection);

        yDirectionChangeCount += 1;
        if (yDirectionChangeCount >= yDirectionChangeWaitMovements)
        {
            movingYDirection *= -1;

            yDirectionChangeCount = 0;
            // yDirectionChangeWaitMovements = Random.Range(50, 800);
            // Debug.Log("yDirectionChangeWaitMovements: " + yDirectionChangeWaitMovements);
        }

        float y_position = _rigidBody.transform.position.y + movingYDirection * movingStep;

        if (y_position < minYCoordinate)
        {
            movingYDirection = 1;
            _rigidBody.velocity = new Vector2(0, 0);
            // yDirectionChangeCount = 0; // reset the direction change count
            return y_position + 0.05f;
        }
        else if (y_position > maxYCoordinate)
        {
            movingYDirection = -1;
            _rigidBody.velocity = new Vector2(0, 0);
            // yDirectionChangeCount = 0; // reset the direction change count
            return y_position - 0.05f;
        }

        return y_position;
    }

    // checking the objects in the way which are going to be collided
    bool isWizardColliding()
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

}
