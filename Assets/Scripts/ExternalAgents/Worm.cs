using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Worm : ABGameObject
{
    float[] movingXRange;
    Vector3 intialPosition;

    float movingStep = 0.01f;
    float movingDirection = -1;

    // Start is called before the first frame update
    void Start()
    {
        // layers that will be ignored when detecting the collision
        intialPosition = _rigidBody.transform.position;
        movingXRange = CalculateMovingXRange();
        // Debug.Log("movingXRange" + movingXRange[0] + " to " + movingXRange[1]);
    }

    private void FixedUpdate()
    {

        move();

    }

    float[] CalculateMovingXRange()
    {
        // the worm can only be moved inside the platform it is instantiated
        // calculating the boundaries of connected platforms

        // get all the platforms in the level
        GameObject[] allPlatforms = GameObject.FindGameObjectsWithTag("Platform");

        float minDistance = Mathf.Infinity;
        GameObject closestPlatform = null;

        // find the platform closest to the worm
        foreach (GameObject platform in allPlatforms)
        {

            float distance = Vector3.Distance(platform.GetComponent<Rigidbody2D>().transform.position, _rigidBody.transform.position);

            if (distance < minDistance)
            {
                // Debug.Log(platform.GetComponent<Rigidbody2D>().transform.position);
                minDistance = distance;
                closestPlatform = platform;
            }

        }

        // Debug.Log("closestPlatform" + closestPlatform.GetComponent<Rigidbody2D>().transform.position);

        // get the connected platforms to the closest platform
        List<GameObject> conenctedPlatforms = new List<GameObject>();
        conenctedPlatforms.Add(closestPlatform);
        List<GameObject> updatedConenctedPlatforms = null;
        bool newPlatformsFound = true;

        while (newPlatformsFound)
        {
            newPlatformsFound = false;
            updatedConenctedPlatforms = new List<GameObject>();

            foreach (GameObject platform in allPlatforms)
            {
    
                if (conenctedPlatforms.Contains(platform) || updatedConenctedPlatforms.Contains(platform))
                {
                    // Debug.Log("already seleceted platform" + platform.GetComponent<Rigidbody2D>().transform.position);
                    updatedConenctedPlatforms.Add(platform);
                    continue;
                }

                foreach (GameObject platformSelected in conenctedPlatforms)
                {

                    if (platformSelected.GetComponent<Renderer>().bounds.Intersects(platform.GetComponent<Renderer>().bounds))
                    {
                        //if platform's y location is far away from the closestPlatform, then disregard it - 0.25 is the half height of the worm
                        if ((platform.GetComponent<Renderer>().bounds.min.y <= (intialPosition.y - 0.25)) && (platform.GetComponent<Renderer>().bounds.max.y >= (intialPosition.y + 0.25)))
                        {

                            // Debug.Log("new platform found!" + platform.GetComponent<Rigidbody2D>().transform.position);
                            newPlatformsFound = true;
                            updatedConenctedPlatforms.Add(platform);

                        }
                    }

                }

            }
            conenctedPlatforms = new List<GameObject>(updatedConenctedPlatforms);
        }

        // Debug.Log("size of conenctedPlatforms" + conenctedPlatforms.Count);

        // find the minimum x and y of the conencted platforms
        float minX = Mathf.Infinity;
        float maxX = Mathf.NegativeInfinity;
        foreach (GameObject platform in conenctedPlatforms)
        {
            //Debug.Log(" bounds: "+ platform.GetComponent<BoxCollider2D>().bounds);
            //Debug.Log("min bounds: "+ platform.GetComponent<BoxCollider2D>().bounds.min);
            if (platform.GetComponent<BoxCollider2D>().bounds.min.x < minX)
            {
                minX = platform.GetComponent<BoxCollider2D>().bounds.min.x;
            }
            if (platform.GetComponent<BoxCollider2D>().bounds.max.x > maxX)
            {
                maxX = platform.GetComponent<BoxCollider2D>().bounds.max.x;
            }
        }

        // Debug.Log("min X: " + minX + "max X: " + maxX);

        // reduce the width of the worm from the movable x range
        return new float[] { minX + 0.33f, maxX - 0.33f };
    }


    private void move()
    {
        // set velocity to zero to avoid the worm throwing away due to forces
        _rigidBody.velocity = new Vector2(0, 0);

        // moving the object
        _rigidBody.transform.position = new Vector3(_rigidBody.transform.position.x + movingDirection * movingStep, intialPosition.y, _rigidBody.transform.position.z);

        if (_rigidBody.transform.position.x < movingXRange[0])  // if min bound is reached reverse

        {
            movingDirection = 1;
        }
        else if (_rigidBody.transform.position.x > movingXRange[1])  // if max bound is reached reverse
        {
            movingDirection = -1;
        }

    }


}
