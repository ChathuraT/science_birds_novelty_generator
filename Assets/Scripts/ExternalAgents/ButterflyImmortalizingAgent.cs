using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Habrador_Computational_Geometry;

public class ButterflyImmortalizingAgent : Butterfly
{
    private ABPig[] pigsList;

    public override void Die(bool withEffect = true)
    {
        

        foreach (ABPig pig in pigsList)
        {
            pig.setCurrentLife(float.PositiveInfinity);
        }
        base.Die(withEffect);
    }   

    public override List<MyVector2> ComputerCurrentConvexHull()
    
    {
        // get all objects vertices that within certain range of pig.
        double pig_min_x = 999;
        double pig_max_x = -999;
        double pig_min_y = 999;
        double pig_max_y = -999;
        double range = 0.5;
        pigsList = FindObjectsOfType<ABPig>();
        Debug.Log(pigsList);
        foreach (ABPig pig in pigsList){
            if (pig.transform.position.x < pig_min_x){
                    pig_min_x = pig.transform.position.x;
            }
            if (pig.transform.position.x > pig_max_x){
                    pig_max_x = pig.transform.position.x;
            }
            if (pig.transform.position.y < pig_min_y){
                    pig_min_y = pig.transform.position.y;
            }
            if (pig.transform.position.y > pig_max_y){
                    pig_max_y = pig.transform.position.y;
            }
        }

        List<GameObject> allRelevantObjects = new List<GameObject>();

        GameObject[] allBlockObjects = GetObjectsInLayer(10);
        GameObject[] allPlatforms = GetObjectsInLayer(12);

        if (allBlockObjects != null)
        {
            foreach (var item in allBlockObjects)
            {
                if ((item.transform.position.x < pig_max_x + range) && (item.transform.position.x > pig_min_x - range) 
                && (item.transform.position.y < pig_max_y + range) && (item.transform.position.y > pig_min_y - range)  ){
                allRelevantObjects.Add(item);

                }
            }
        }

        if (allPlatforms != null)
        {
            foreach (var item in allPlatforms)
            {
                // include all platform except the ones on the ground.

                // if ((item.transform.position.x < pig_max_x + range) && (item.transform.position.x > pig_min_x - range) 
                // && (item.transform.position.y < pig_max_y + range) && (item.transform.position.y > pig_min_y - range))
                // {
                // allRelevantObjects.Add(item);
                // }
                if (item.transform.position.y > -3.1){
                    allRelevantObjects.Add(item);
                }

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

}
