using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Habrador_Computational_Geometry;

public class Butterfly : ABGameObject
{
    public bool upperHull = false; // whether the object will be moving only around the upper hull
    public int currentStepPointIndex = 0;
    public float stepSize = 0.05f;
    public int movingDirection;
    public List<Vector2> listOfPoints = new List<Vector2>();
    public Vector2 centreOfLevel;
    public bool simulate = true;
    public bool calculated = false;
    public int movingFrame = 1; // magician is moved in every movingFrame number of frames
    public int movingFrameCount = 0;

    public Vector2 hullCenter;

    public List<MyVector2> initHull;

    public int numOfReleventObjects;
    public bool movingDirectionChanged = false;
    public float groundLevel = 0f;

    public List<MyVector2> convhull;
    public List<Vector2> ExpandedStepPoints = new List<Vector2>();

    public bool ifObjectIsMoved = false;
    public bool ifOtherObjectsAreMoved = false;

    public bool ifObjectIsMoving = false;
    public bool ifOtherObjectsAreMoving = false;

    public bool initialLocationCheckCompleted = false;

    // Start is called before the first frame update
    void Start()
    {
        // the Butterfly calculate the convex hull of the vertices of all blocks and platforms on the ground and goes around it
        // when it is going around the convex hull, its gravity scale is set to be 0
        // if the Butterfly if moved, the gravity scale will be set to 1 and not moving anymore - shoot down the kate
        // the Butterfly stops moving around the convex hull when any blocks are being moved
        // NOTE: don't initialise the object among blocks. initalise it as the most left or most right object.

        GameObject ground = GameObject.Find("Ground");
        BoxCollider2D groundCollider = ground.GetComponent<BoxCollider2D>();
        groundLevel = ground.transform.position.y + groundCollider.size.y / 2f + groundCollider.offset.y - 0.00001f;

        // if butterfly is initialized in a wrong location reposition it
        if (!initialLocationCheckCompleted)
        {
            validateInitializedLocation();
            initialLocationCheckCompleted = true;
        }

        movingDirection = FindMovingDirection();

    }


    private void FixedUpdate()
    {

        for (int i = 0; i < ABGameWorld.SimulationSpeed; i++)
        {
            if (simulate)
            {

                IfObjectIsMoved();
                IfObjectIsMoving();
                IfOtherObjectsAreMoved();
                IfOtherObjectsAreMoving();
                if (movingFrameCount >= movingFrame)
                {

                    // move the object only when it is stationary
                    if (IfCalculateStepPoints())
                    {
                        convhull = ComputerCurrentConvexHull();
                        if (convhull != null)
                        {
                            UpdateStepPoints(convhull);
                            ExpandStepPoints(1f);

                            if (!calculated)
                            {
                                initHull = convhull;
                            }

                            ifOtherObjectsAreMoved = false;
                            ifObjectIsMoved = false;
                            calculated = true;
                        }

                    }

                    if (!ifObjectIsMoving && !ifOtherObjectsAreMoving)
                    {
                        moveConvex();

                    }

                    if (ifObjectIsMoved && calculated)
                    {
                        // if the object has been moved, added back gravity scale
                        _rigidBody.gravityScale = 1f;
                        simulate = false;
                    }

                    movingFrameCount = 0;
                }
                else
                {
                    movingFrameCount += 1;
                }
            }
        }
    }


    public void validateInitializedLocation()
    {
           // get all the blocks(including pigs) and platforms in the level
        GameObject[] allBlockObjects = GetObjectsInLayer(10);
        GameObject[] allPlatforms = GetObjectsInLayer(12);

        // find occupide x rage from the game objects
        float minX = Mathf.Infinity;
        float maxX = Mathf.NegativeInfinity;

        foreach (GameObject block in allBlockObjects)
        {
            //Debug.Log(block.name);
            if (block.GetComponent<Renderer>().bounds.min.x < minX)
            {
                minX = block.GetComponent<Renderer>().bounds.min.x;
            }
            if (block.GetComponent<Renderer>().bounds.max.x > maxX)
            {
                maxX = block.GetComponent<Renderer>().bounds.max.x;
            }
        }

        foreach (GameObject platform in allPlatforms)
        {
            //Debug.Log(platform.name);
            if (platform.GetComponent<Renderer>().bounds.min.x < minX)
            {
                minX = platform.GetComponent<Renderer>().bounds.min.x;
            }
            if (platform.GetComponent<Renderer>().bounds.max.x > maxX)
            {
                maxX = platform.GetComponent<Renderer>().bounds.max.x;
            }
        }

        //Debug.Log(minX + " " + maxX);


        //Butterfly should not be placed on the occupied x range, if so adjust its position
        if (gameObject.transform.position.x > minX && gameObject.transform.position.x < maxX) {
             Debug.Log("Butterfly is instantiated in a wrong position, shifting it to the rightmost unoccupied space");
            _rigidBody.transform.position =  new Vector2(maxX + 0.6f, _rigidBody.transform.position.y);

        }

    }

    private void moveConvex()
    {

        if (currentStepPointIndex < ExpandedStepPoints.Count)
        {
            _rigidBody.gravityScale = 0f;

            Vector2 nextPoint = new Vector2(ExpandedStepPoints[currentStepPointIndex].x, ExpandedStepPoints[currentStepPointIndex].y);
            transform.position = new Vector3(nextPoint.x, nextPoint.y, 0);
            currentStepPointIndex++;
            //_rigidBody.gravityScale = 1f;

        }

        else
        {   // the object has reached the end
            // the list the run again
            // ExpandedStepPoints.Reverse();
            currentStepPointIndex = 0;
            ExpandedStepPoints.Reverse();
            movingDirection *= -1;
            movingDirectionChanged = true;
            //Debug.Log("moving direction changed");
        }

    }

    private void UpdateStepPoints(List<MyVector2> convextHull)
    {

        listOfPoints.Clear();
        currentStepPointIndex = 0;

        // add the first point to the list
        listOfPoints.Add(new Vector2(convextHull[0].x, convextHull[0].y));
        // if clockwise
        for (int i = 0; i < convextHull.Count - 1; i++)
        {
            MyVector2 p1 = convextHull[i];
            MyVector2 p2 = convextHull[i + 1];
            Vector2 directionalVector = new Vector2(p1.x - p2.x, p1.y - p2.y);
            directionalVector.Normalize();
            while (true)
            {
                Vector2 nextPoint = listOfPoints[listOfPoints.Count - 1] - directionalVector * stepSize;


                // if nextPoint is within the stepSize distance to the next critical point
                // append the next critical point and break the while loop for next pair of points
                if (Vector2.Distance(nextPoint, new Vector2(p2.x, p2.y)) < stepSize)
                {
                    listOfPoints.Add(new Vector2(p2.x, p2.y));
                    break;
                }
                else
                {
                    listOfPoints.Add(nextPoint);
                }
            }
        }
    }

    private void ExpandStepPoints(float magnitute)
    {
        ExpandedStepPoints.Clear();

        // for each point in list of points, calculate the angel between the ground level and the average x of the hull
        // and prolong the points along the same direction

        float SlingshotX = GameObject.Find("slingshot_back").transform.position.x;

        foreach (Vector2 p in listOfPoints)
        {
            Vector2 scaledVector = p - hullCenter;
            float vectorMag = scaledVector.magnitude;
            scaledVector.Normalize();
            scaledVector = scaledVector * (vectorMag + magnitute);
            // scale it back
            scaledVector = scaledVector + hullCenter;
            if ((scaledVector.y >= groundLevel + 0.5) & (scaledVector.x >= SlingshotX + 0.5))
            {
                ExpandedStepPoints.Add(scaledVector);
            }
        }
        // foreach (Vector2 p in listOfPoints)
        // {
        //     ExpandedStepPoints.Add(p);


        // }

        //now we need to create the path from the current object position to the first point of the ExpandedStepPoints
        Vector2 currentLocation = new Vector2(transform.position.x, transform.position.y);
        Vector2 targetLocation = ExpandedStepPoints[0];
        // Debug.Log("currentLocation" + currentLocation);
        // Debug.Log("targetLocation" + ExpandedStepPoints[0]);

        Vector2 directionalVector = ExpandedStepPoints[0] - currentLocation;
        // Debug.Log("directionalVector" + directionalVector);

        directionalVector.Normalize();
        int insertInd = 0;
        while (true)
        {
            Vector2 nextPoint = currentLocation + directionalVector * stepSize;
            // Debug.Log("nextPoint"+ nextPoint);
            // if nextPoint is within the stepSize distance to the next critical point
            // append the next critical point and break the while loop for next pair of points
            if (Vector2.Distance(nextPoint, targetLocation) < stepSize)
            {
                break;
            }
            else
            {
                ExpandedStepPoints.Insert(insertInd, nextPoint);
                // Debug.Log("ExpandedStepPoints indx "+insertInd + " " + ExpandedStepPoints[insertInd]);
                insertInd++;
            }
            currentLocation = nextPoint;
        }

    }

    public virtual List<MyVector2> ComputerCurrentConvexHull()
    {
        // get all objects vertices
        List<GameObject> allRelevantObjects = new List<GameObject>();

        GameObject[] allBlockObjects = GetObjectsInLayer(10);
        GameObject[] allPlatforms = GetObjectsInLayer(12);

        if (allBlockObjects != null)
        {
            foreach (var item in allBlockObjects)
            {
                allRelevantObjects.Add(item);
            }
        }

        if (allPlatforms != null)
        {
            foreach (var item in allPlatforms)
            {
                allRelevantObjects.Add(item);
            }
        }

        numOfReleventObjects = allRelevantObjects.Count;

        if (numOfReleventObjects == 0)
        {
            simulate = false;
            return null;
        }

        List<MyVector2> vertices = new List<MyVector2>();
        foreach (GameObject o in allRelevantObjects)
        {
            //filter out all object under ground
            if (o.transform.position.y <= groundLevel)
            {
                continue;
            }

            //get vertices
            if (o.GetComponent<Collider2D>().GetType().ToString() == "UnityEngine.BoxCollider2D")
            {
                if (o.GetComponent<RectTransform>() == null)
                {
                    o.AddComponent<RectTransform>();
                }

                Vector3[] corners = new Vector3[4];

                o.GetComponent<RectTransform>().GetWorldCorners(corners);

                //dilate corners

                foreach (Vector3 p in corners)
                {
                    //Debug.Log(o.transform.position);

                    //Debug.Log(p.x);
                    //Debug.Log(p.y);

                    Vector2 op = new Vector2(p.x - o.transform.position.x, p.y - o.transform.position.y);
                    float vectorMag = op.magnitude;
                    op.Normalize();
                    op = op * (vectorMag + 1f);
                    //Debug.Log(op.x);
                    //Debug.Log(op.y);
                    vertices.Add(new MyVector2(op.x + o.transform.position.x, op.y + o.transform.position.y));
                }

                // for (int i = 0; i < 4; i++)
                // {
                //     vertices.Add(new MyVector2(corners[i].x, corners[i].y));

                // }
            }

            else if (o.GetComponent<Collider2D>().GetType().ToString() == "UnityEngine.PolygonCollider2D")
            {

                Vector2[] corners = o.GetComponent<PolygonCollider2D>().points;
                float xCenter = 0;
                float yCenter = 0;
                for (int i = 0; i < corners.Length; i++)
                {
                    xCenter = xCenter + corners[i].x;
                    yCenter = yCenter + corners[i].y;
                }
                Vector2 ObjectCenter = new Vector2(xCenter / corners.Length, yCenter / corners.Length);

                for (int i = 0; i < corners.Length; i++)
                {
                    Vector3 worldPoint = o.transform.TransformPoint(new Vector3(corners[i].x, corners[i].y, 0));
                    Vector3 worldCenterPoint = o.transform.TransformPoint(new Vector3(ObjectCenter.x, ObjectCenter.y, 0));

                    Vector2 op = new Vector2(worldPoint.x - o.transform.position.x, worldPoint.y - o.transform.position.y);
                    float vectorMag = op.magnitude;
                    op.Normalize();
                    op = op * (vectorMag + 1f);
                    vertices.Add(new MyVector2(op.x + o.transform.position.x, op.y + o.transform.position.y));
                }

            }
        }

        //// add the right most and left most point
        //// vertices.Add(new MyVector2(minXCoordinate, groundLevel));
        //vertices.Add(new MyVector2(maxXCoordinate + 1, transform.position.y));

        // add the current point of the Butterfly
        // vertices.Add(new MyVector2(transform.position.x, transform.position.y));


        List<MyVector2> convexHull = Habrador_Computational_Geometry.JarvisMarchAlgorithm2D.GenerateConvexHull(vertices);


        //arange convex hull points by starting the bottom left point to bottom right
        List<MyVector2> arrangedConvexHull = new List<MyVector2>();
        float maxX = 0f;
        float minY = 999f;
        int bottomLeftInd = 0;

        if (upperHull)
        {
            float minXY = 999f;
            for (int i = 0; i < convexHull.Count; i++)
            {
                MyVector2 p = convexHull[i];
                if (p.x + p.y < minXY)
                {
                    minXY = p.x + p.y;
                    bottomLeftInd = i;
                }

                if (p.x > maxX)
                {
                    maxX = p.x;
                }
            }
        }
        else
        {
            // the most left point that are in the lowest position
            // p* = argmin_p (p.x + abs(p.y - groundLevel))
            float targetP = 999;
            for (int i = 0; i < convexHull.Count; i++)
            {

                MyVector2 p = convexHull[i];
                if (p.x > maxX)
                {
                    maxX = p.x;
                }

                if (p.y < minY)
                {
                    minY = p.y;
                }
            }
            for (int i = 0; i < convexHull.Count; i++)
            {
                MyVector2 p = convexHull[i];
                if (Mathf.Abs(p.y - minY) < 0.1)
                {
                    if (p.x + Mathf.Abs(p.y - minY) < targetP)
                    {
                        targetP = p.x + Mathf.Abs(p.y - minY);
                        bottomLeftInd = i;
                    }
                }

            }

        }

        //find bottom right point
        int bottomRightInd = 0;
        float maxMaxXxy = 0f;
        float minMaxXxy = 999f;

        for (int i = 0; i < convexHull.Count; i++)
        {
            MyVector2 p = convexHull[i];

            if (maxX - p.x + p.y < minMaxXxy)
            {
                minMaxXxy = maxX - p.x + p.y;
                bottomRightInd = i;
            }

        }

        // check if the points in the list is in clockwise order
        float hullMinX = 999f;
        float hullMaxX = 0f;
        float hullMinY = 999f;
        float hullMaxY = 0f;
        foreach (MyVector2 p in convexHull)
        {
            if (p.x > hullMaxX)
            {
                hullMaxX = p.x;
            }
            if (p.x < hullMinX)
            {
                hullMinX = p.x;
            }
            if (p.y > hullMaxY)
            {
                hullMaxY = p.y;
            }
            if (p.y < hullMinY)
            {
                hullMinY = p.y;
            }
        }
        hullCenter = new Vector2((hullMinX + hullMaxX) / 2f, (hullMinY + hullMaxY) / 2f);

        List<Vector2> shfitedPoint = new List<Vector2>();
        foreach (MyVector2 p in convexHull)
        {
            shfitedPoint.Add(new Vector2(p.x - hullCenter.x, p.y - hullCenter.y));
        }
        bool clockwise = false;
        // if it is clockwise, q.x * mx < q.y when x >= 0 and q.x * mx > q.y when x < 0
        Vector2 p1 = shfitedPoint[bottomLeftInd];
        Vector2 p2 = shfitedPoint[bottomLeftInd + 1];
        float mx = p1.y / p1.x;

        if ((p2.y >= mx * p2.x && p1.x >= 0) || (p2.y < mx * p2.x && p1.x < 0))
        {
            clockwise = true;
        }

        if (!clockwise)
        {
            //Debug.Log("clockwise");
            if (bottomLeftInd < bottomRightInd)
            {
                for (int i = bottomLeftInd; i <= bottomRightInd; i++)
                {
                    arrangedConvexHull.Add(convexHull[i]);
                }
            }
            else if (bottomLeftInd > bottomRightInd)
            {
                for (int i = bottomLeftInd; i <= bottomRightInd + convexHull.Count; i++)
                {
                    int indx;
                    if (i < convexHull.Count)
                    {
                        indx = i;
                    }
                    else
                    {
                        indx = i - convexHull.Count;
                    }
                    if (convexHull[indx].y > groundLevel)
                    {
                        arrangedConvexHull.Add(convexHull[indx]);
                    }
                }
            }
            else
            {
                //Debug.Log("We have problems");
            }
        }


        else
        {
            //Debug.Log("counter-clockwise");
            MyVector2 firstPoint = convexHull[bottomLeftInd];
            MyVector2 lastPoint = convexHull[bottomRightInd];
            MyVector2 currentPoint = firstPoint;
            int pointInd = bottomLeftInd;
            while (currentPoint.x != lastPoint.x || currentPoint.y != lastPoint.y)
            {
                arrangedConvexHull.Add(currentPoint);
                if (pointInd - 1 >= 0)
                {
                    pointInd -= 1;
                }
                else
                {
                    pointInd = convexHull.Count - 1;
                }
                currentPoint = convexHull[pointInd];
            }
        }

        //Debug.Log("arrangedConvexHull count: " + arrangedConvexHull.Count);

        //Debug.Log("bottom left point: "+convexHull[bottomLeftInd].x+convexHull[bottomLeftInd].y );
        //Debug.Log("bottom right point: "+convexHull[bottomRightInd].x+convexHull[bottomRightInd].y );

        //Debug.Log("bottomLeftInd: "+bottomLeftInd);
        //Debug.Log("bottomRightInd: "+bottomRightInd);

        List<MyVector2> result = new List<MyVector2>();

        if (movingDirection == 1)
        {
            arrangedConvexHull.Reverse();
        }

        // consider the current location of the bird, we just need to start with the cloest point to the current location
        int closetPointInd = 0;
        float minDistance = 999;
        MyVector2 objectPosition = new MyVector2(transform.position.x, transform.position.y);
        for (int i = 0; i < arrangedConvexHull.Count; i++)
        {
            float distance = MyVector2.Distance(objectPosition, arrangedConvexHull[i]);
            if (distance < minDistance)
            {
                if (arrangedConvexHull[i].y > groundLevel)
                {
                    closetPointInd = i;
                    minDistance = distance;
                }

            }
        }

        // result.Add(new MyVector2(transform.position.x, transform.position.y));
        for (int i = closetPointInd; i < arrangedConvexHull.Count; i++)
        {
            result.Add(arrangedConvexHull[i]);
        }

        //Debug.Log("result.Count: " + result.Count);

        return result;
    }


#if UNITY_EDITOR
    // visualize the convex hull in the editor
    void OnDrawGizmos()
    {

        Gizmos.color = Color.blue;
        for (int i = 0; i < ExpandedStepPoints.Count - 1; i++)
        {
            Vector3 p1 = new Vector3(ExpandedStepPoints[i].x, ExpandedStepPoints[i].y, 0);
            Vector3 p2 = new Vector3(ExpandedStepPoints[i + 1].x, ExpandedStepPoints[i + 1].y, 0);

            Gizmos.DrawLine(p1, p2);

        }
    }
#endif

    public static GameObject[] GetObjectsInLayer(int layer)
    {
        var goList = new List<GameObject>();
        GameObject[] goArray = GameObject.FindObjectsOfType<GameObject>();

        for (var i = 0; i < goArray.Length; i++)
        {
            if (goArray[i].layer == layer)
            {
                goList.Add(goArray[i]);
            }
        }
        if (goList.Count == 0)
        {
            return null;
        }
        return goList.ToArray();
    }


    public bool IfCalculateStepPoints()
    {
        if ((currentStepPointIndex == 0 || ifOtherObjectsAreMoved || movingDirectionChanged) && (!ifObjectIsMoving && !ifOtherObjectsAreMoving))
        {
            movingDirectionChanged = false; //reset movingDirectionChanged after each calculation of the hull
            return true;
        }

        return false;
    }

    public void IfObjectIsMoved()
    {
        if (ifObjectIsMoving && !ifObjectIsMoved)
        {
            ifObjectIsMoved = true;
        }

        else if (!ifObjectIsMoving && ifObjectIsMoved)
        {
            ifObjectIsMoved = false;
        }

    }

    public void IfObjectIsMoving()
    {

        if (_rigidBody.velocity.magnitude >= 0.1f)
        {
            ifObjectIsMoving = true;
        }
        else
        {
            ifObjectIsMoving = false;
        }
    }

    public void IfOtherObjectsAreMoved()
    {
        if (ifOtherObjectsAreMoving && !ifOtherObjectsAreMoved)
        {
            ifOtherObjectsAreMoved = true;
        }

    }

    public void IfOtherObjectsAreMoving()
    {
        ifOtherObjectsAreMoving = false;
        GameObject[] allBlockObjects = GetObjectsInLayer(10);
        foreach (GameObject o in allBlockObjects)
        {
            if (o.GetComponent<Rigidbody2D>().velocity.magnitude >= 0.5f)
            {
                ifOtherObjectsAreMoving = true;
            }
        }
    }

    public int FindMovingDirection()
    {
        float currentX = transform.position.x;
        float minX = 999;
        float maxX = 0;
        float averageX = 0;
        GameObject[] allBlockObjects = GetObjectsInLayer(10);
        foreach (GameObject o in allBlockObjects)
        {
            if (o.transform.position.x < minX)
            {
                minX = o.transform.position.x;
            }
            if (o.transform.position.x > maxX)
            {
                maxX = o.transform.position.x;
            }
        }

        float midX = (minX + maxX) / 2;

        if (currentX > midX)
        {
            return 1;
        }
        else
        {
            return -1;
        }

    }

}
