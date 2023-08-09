using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathObstructedSolverAndAddDistractions : MonoBehaviour
{
    public Scenario scenario;
    public Utilities utilities;

    public bool pathObstructedSolverCompleted = false;

    // parameters specific to the scenario (not a good way to do it, but... TODO: capture these fromn the level layout!)
    bool needTopWall;
    bool neeedLeftWall;
    float topWallStartAdjustment; // the wall starting point will be shifted by this amount
    float leftWallStartAdjustment; // the wall starting point will be shifted by this amount
    float[] finalTopWallPosition; // [minX, maxX, topOfTopWallY] the final positions of the top wall to be used when placing the distraction objects
    public void SolvePathObstructedTermsAndAddDistractions()
    {
        /*
             This function adds path blocking platforms to satisfy the pathObstructedTerms.
             Currently path obstructed terms are in the format pathObstructed<normalBird><normalPig><left&right&above&below>, so a wall is build protecting the pig along the MBR while exposing the first target object to the bird
         */

        utilities = new Utilities(); // for some reason utilities object is not assigned from the layoutgenerator, check later, therefore create a new object

        // get all the pathObstrcuted terms
        List<PathObstructed> pathObstructedTerms = GetPathObstructedTerms();

        // check if all of them are associated with the bird and pig (as currently only this scenario is handled) - if not throw an error
        // also save the path obstructed direction between bird and the pig
        Direction pathObstructedDirection = null;
        foreach (PathObstructed pathObstructedterm in pathObstructedTerms)
        {
            if (pathObstructedterm.a.gameObject.GetType().BaseType != typeof(Bird) & pathObstructedterm.b.gameObject.GetType().BaseType != typeof(Pig))
            {
                throw new System.Exception("pathObstructed terms are associated with unhandled objects " + pathObstructedterm.a.gameObject.GetType().BaseType + " and " + pathObstructedterm.b.gameObject.GetType().BaseType);
            }
            pathObstructedDirection = pathObstructedterm.d;
        }
        // if no pathObstructedterms are available return
        if (pathObstructedDirection is null)
        {
            // add the current objects in the layout (before adding distraction objects) to the scenarioInfo data
            utilities.AddLayoutObjectsToScenarioInfo(scenario);

            // add distraction objects
            AddDistractionObjects();

            pathObstructedSolverCompleted = true;
            return;
        }
        Debug.Log("all path obstructed terms are associated with " + typeof(Bird) + " and " + typeof(Pig) + " and the obstructed direction is: " + pathObstructedDirection);

        // get the minimum bounding box of the layout
        float[] MBROfTheLayout = GetTheMBROfAllTheLayout();
        Debug.Log("minX: " + MBROfTheLayout[0] + ", maxX: " + MBROfTheLayout[1] + ", minY: " + MBROfTheLayout[2] + ", maxY: " + MBROfTheLayout[3]);

        // get the target object of the bird
        List<Object> birdsTargetObjects = GetBirdsTarget();
        Debug.Log("Bird's target objects are: " + birdsTargetObjects.Cast<object>().Aggregate("", (current, o) => current + (o + ", ")));

        // add platform walls while keeping space to reach the birdsTargetObjects
        AddPlatformWalls(MBROfTheLayout, birdsTargetObjects, pathObstructedDirection);

        // add the current objects in the layout (before adding distraction objects) to the scenarioInfo data
        utilities.AddLayoutObjectsToScenarioInfo(scenario);

        // add distraction objects
        AddDistractionObjects();

        pathObstructedSolverCompleted = true;
    }

    private void AddDistractionObjects()
    {
        /*
         * Given the minimum bounding box of the object layout and the boundries of the added walls, this function add distraction objects.
         * Distraction objects are added on top of the top wall and on the ground avoiding the (MBR + left wall) occupied space
         * 
         * Input: MBROfTheLayout = [minX, maxX, minY, maxY]
         *        topWallBoundaries = [minX, maxX, topOfWallY]
         *        leftWallBoundaries = [minY, maxY, leftOfWallX]
         */

        List<ObjectGrammar> distractionObjects = new List<ObjectGrammar>();

        // if top wall exists, place objects ontop of it
        if (needTopWall)
        {
            // get the x range of the top wall
            distractionObjects.AddRange(GetRandomObjectsToSpanARange(finalTopWallPosition));
        }

        // add the distractions on ground
        // get  the spaces on the ground that are not occupied
        float[] MBROfTheLayout = GetTheMBROfAllTheLayout();  // get the minimum bounding box of the layout

        if ((MBROfTheLayout[0] - 1) > Utilities.X_MIN_REACHABLE)
        { // if there is a space between the sling and the layout start
            distractionObjects.AddRange(GetRandomObjectsToSpanARange(new float[] { Utilities.X_MIN_REACHABLE, MBROfTheLayout[0] - 1, Utilities.GROUND_LEVEL_Y }));
        }
        // space after the layout end and the maxX position
        if ((MBROfTheLayout[1] + 1) < (Utilities.X_MAX_REACHABLE - 2)) // +1 is to leave some gap between last object of the layout and the distraction obejct and -2 is just to reduce the range a little bit to avoid placing objects too far away
        { // if there is a space between the sling and the layout start
            distractionObjects.AddRange(GetRandomObjectsToSpanARange(new float[] { MBROfTheLayout[1] + 1, Utilities.X_MAX_REACHABLE - 2, Utilities.GROUND_LEVEL_Y }));
        }
        // further check if there is space between the ground and the loutout's minY to place objects
        if (MBROfTheLayout[2] > (Utilities.GROUND_LEVEL_Y + 0.84f)) // 0.84 is the height of the tallest unrotated block (square hole)
        {
            distractionObjects.AddRange(GetRandomObjectsToSpanARange(new float[] { MBROfTheLayout[0], MBROfTheLayout[1], Utilities.GROUND_LEVEL_Y }));
        }

        // add the distraction objects to the scenario's object list
        scenario.objects.AddRange(distractionObjects);

        // add the distraction objects in the scenario to the scenarioInfo data
        utilities.AddDistractionObjectsToScenarioInfo(scenario, distractionObjects);
    }

    private List<ObjectGrammar> GetRandomObjectsToSpanARange(float[] xBoundaries)
    {
        /*
         Get random objects that span a given range
         Input: MBROfTheLayout = [minX, maxX, minY, maxY]
                xBoundaries =  [minX, maxX, basePositionY], basePositionY is the top of the wall or top of the ground
         */

        Block[] blockPool = { new Circle(), new CircleSmall(), new RectBig(), new RectFat(), new RectMedium(), new RectSmall(), new SquareHole(), new Triangle(), new TriangleHole() }; // tiny objects are not considered
        List<ObjectGrammar> selectedObjects = new List<ObjectGrammar>();

        float currentXPosition = xBoundaries[0]; // keep track of the current y position - start from the minY
        float gapBetweenObjects;  // get a random gap between objects
        int infeasibleObjectCounter = 0; // it will tr this many times to find a feasible position

        while (currentXPosition < xBoundaries[1])
        { // do until the whole span is covered

            // get a random object
            Block randomObject = blockPool[UnityEngine.Random.Range(0, blockPool.Length)];

            // check if it can be placed in the remaining x range
            if (randomObject.size[0] < (xBoundaries[1] - currentXPosition))
            {
                gapBetweenObjects = UnityEngine.Random.Range(1f, 2.5f);
                Block distractionGameObject = (Block)(Activator.CreateInstance(randomObject.GetType())); // create the disctaction game object
                distractionGameObject.positionX = currentXPosition + distractionGameObject.size[0] / 2; // assign the xLocation
                distractionGameObject.positionY = xBoundaries[2] + distractionGameObject.size[1] / 2; // assign the yLocation
                selectedObjects.Add(new DistractionObject(distractionGameObject)); // add the distraction object to the selectedObjects list

                // update the currentXPosition
                currentXPosition += (distractionGameObject.size[0] + gapBetweenObjects);
            }
            else
            {
                infeasibleObjectCounter += 1;
                if (infeasibleObjectCounter > 10) // after 10 attempts if a feasible object cannot be found, break the loop
                {
                    break;
                }
            }
        }
        return selectedObjects;
    }


    private void AddPlatformWalls(float[] MBROfTheLayout, List<Object> birdsTargetObjects, Direction pathObstructedDirection)
    {
        /*
         This function adds new platforms to the right and top of the MBROfTheLayout while keeping space to reach the firstTargetObejct 
         */

        // start by setting scenario specific parameters
        SetScenarioSpecificParamaters(pathObstructedDirection);
        Debug.Log("wall parameters: scenario: " + scenario.scenarioName + ", needTopWall: " + needTopWall + ", neeedLeftWall: " + neeedLeftWall + ", topWallStartAdjustment: " + topWallStartAdjustment + ", leftWallStartAdjustment: " + leftWallStartAdjustment);
        List<(float, float)> targetsWidthAndHeight = Utilities.GetMBRWidthAndHeight(birdsTargetObjects);

        // get the topmost target object and in the leftmost target object 
        int topMostObjectIndex = 0;
        int bottomMostObjectIndex = 0;
        for (int i = 1; i < birdsTargetObjects.Count; i++)
        {
            if (birdsTargetObjects[i].positionY > birdsTargetObjects[topMostObjectIndex].positionY)
            {
                topMostObjectIndex = i;
            }
            if (birdsTargetObjects[i].positionY < birdsTargetObjects[bottomMostObjectIndex].positionY)
            {
                bottomMostObjectIndex = i;
            }
        }



        // top wall
        if (needTopWall)
        {
            float topWalloffset; // the top wall will be raised by this amount
            float topWallLeftmostPosition;
            if (scenario.scenarioName == "BouncingBirdFallingObject" | scenario.scenarioName == "BouncingBird")
            {
                topWallLeftmostPosition = MBROfTheLayout[0] + topWallStartAdjustment; // leftmost position of the wall is the minX for BouncingBirdFallingObject scenario
                topWalloffset = 3f;
            }
            else
            {
                topWallLeftmostPosition = birdsTargetObjects[topMostObjectIndex].positionX + targetsWidthAndHeight[topMostObjectIndex].Item1 / 2 + topWallStartAdjustment; // determine the leftmost position according to the position of the topMostObject
                topWalloffset = 0.5f;
            }

            float topWallRightmostPosition = MBROfTheLayout[1]; // rightmost position of the wall is the maxX

            scenario.objects.AddRange(AddADiscreteWall(topWallLeftmostPosition, topWallRightmostPosition, MBROfTheLayout[3] + topWalloffset, "Y")); // adding a discrete wall

            // save the top wall's final position to be used to place the distraction objects
            finalTopWallPosition = new float[] { topWallLeftmostPosition, topWallRightmostPosition, MBROfTheLayout[3] + topWalloffset };
        }

        // left wall
        if (neeedLeftWall)
        {
            float leftWalloffset = 0.5f; // the left wall will be shifter to the left by this amount

            float leftWallTopmostPosition = birdsTargetObjects[topMostObjectIndex].positionY - targetsWidthAndHeight[topMostObjectIndex].Item2 / 2 - leftWallStartAdjustment; // determine the topmost position according to the position of the firstTargetObejct, - 0.5 is to avoid the wall getting too close to the target

            float leftWallBottommostPosition = birdsTargetObjects[bottomMostObjectIndex].positionY + targetsWidthAndHeight[bottomMostObjectIndex].Item2 / 2 + leftWallStartAdjustment;
            // float leftWallBottommostPosition = MBROfTheLayout[2]; // bottommost position of the wall is the minY

            scenario.objects.AddRange(AddADiscreteWall(leftWallTopmostPosition, leftWallBottommostPosition, MBROfTheLayout[0] - leftWalloffset, "X")); // adding a discrete wall
        }

        // return the positions of the walls (to be used when adding distraction obejcts)

    }

    private void SetScenarioSpecificParamaters(Direction pathObstructedDirection)
    {
        /*
            This function defines some scenario specific constraints when adding the platform walls - this is not the ideal way to do it, but just for now ... TODO: determine these parameters from the layout
        */

        switch (scenario.scenarioName)
        {
            case "SingleForce":
                needTopWall = false; // this overwrites later
                neeedLeftWall = false; // this overwrites later
                topWallStartAdjustment = -0.5f; // these are set to a negative value as for SingleForceTopBlocked and SingleForceLeftBlocked scenarios we actually need to block the target object from a side, rather than keeping them open
                leftWallStartAdjustment = -0.5f;
                break;
            case "SingleForceTopBlocked":
                needTopWall = false; // this overwrites later
                neeedLeftWall = false; // this overwrites later
                topWallStartAdjustment = -0.5f; // these are set to a negative value as for SingleForceTopBlocked and SingleForceLeftBlocked scenarios we actually need to block the target object from a side, rather than keeping them open
                leftWallStartAdjustment = -0.5f;
                break;
            case "SingleForceLeftBlocked":
                needTopWall = false; // this overwrites later
                neeedLeftWall = false; // this overwrites later
                topWallStartAdjustment = -0.5f; // these are set to a negative value as for SingleForceTopBlocked and SingleForceLeftBlocked scenarios we actually need to block the target object from a side, rather than keeping them open
                leftWallStartAdjustment = -0.5f;
                break;
            case "MultipleForces":
                needTopWall = false; // this overwrites later
                neeedLeftWall = false; // this overwrites later
                topWallStartAdjustment = -2f; // these are set to a negative value as for MultipleForcesTopBlocked and MultipleForcesLeftBlocked scenarios we actually need to block the target object from a side, rather than keeping them open
                leftWallStartAdjustment = -1f;
                break;
            case "RollingObject":
                needTopWall = true;
                neeedLeftWall = false;
                topWallStartAdjustment = 0.5f;
                leftWallStartAdjustment = 0.5f;
                break;
            case "FallingObject":
                needTopWall = true;
                neeedLeftWall = true;
                topWallStartAdjustment = 2f;
                leftWallStartAdjustment = 1f;
                break;
            case "SlidingObject":
                needTopWall = true;
                neeedLeftWall = false;
                topWallStartAdjustment = -1f;
                leftWallStartAdjustment = 0.5f;
                break;
            case "BouncingBird":
                needTopWall = true;
                neeedLeftWall = true;
                topWallStartAdjustment = 2f; //2f for higher trajectories, 0 for lower trajectories
                leftWallStartAdjustment = 1.5f;
                break;

            case "RollingFallingObject":
                needTopWall = true;
                neeedLeftWall = true;
                topWallStartAdjustment = 0.5f;
                leftWallStartAdjustment = 2f;
                break;
            case "RollingSlidingObject":
                needTopWall = true;
                neeedLeftWall = false;
                topWallStartAdjustment = 0.5f;
                leftWallStartAdjustment = 0.5f;
                break;
            case "FallingRollingObject":
                needTopWall = true;
                neeedLeftWall = true;
                topWallStartAdjustment = 2f;
                leftWallStartAdjustment = 0f;
                break;
            case "SlidingRollingObject":
                needTopWall = true;
                neeedLeftWall = false;
                topWallStartAdjustment = 0.5f;
                leftWallStartAdjustment = 0.5f;
                break;
            case "BouncingBirdFallingObject":
                needTopWall = true;
                neeedLeftWall = true;
                topWallStartAdjustment = 1f;
                leftWallStartAdjustment = 2f;
                break;

            case "SlidingRollingFallingObject":
                needTopWall = true;
                neeedLeftWall = true;
                topWallStartAdjustment = 0.5f;
                leftWallStartAdjustment = 2f;
                break;
            case "SlidingFallingRollingObject":
                needTopWall = true;
                neeedLeftWall = true;
                topWallStartAdjustment = 2f;
                leftWallStartAdjustment = 0.5f;
                break;
            case "RollingRollingRollingObject":
                needTopWall = true;
                neeedLeftWall = false;
                topWallStartAdjustment = 0.5f;
                leftWallStartAdjustment = 0.5f;
                break;
            case "RollingRollingFallingObject":
                needTopWall = true;
                neeedLeftWall = true;
                topWallStartAdjustment = 0.5f;
                leftWallStartAdjustment = 2f;
                break;

            // novelty scenarios
            case "RollingObjectNovel":
                needTopWall = true;
                neeedLeftWall = true;
                topWallStartAdjustment = -2f;
                leftWallStartAdjustment = 1f;
                break;
            case "FallingObjectNovel": // for this novel scenario, destraction objects should be added below the bottom target object
                needTopWall = true;
                neeedLeftWall = true;
                topWallStartAdjustment = -2f;
                leftWallStartAdjustment = 2f;
                break;
            case "SlidingObjectNovel":
                needTopWall = true;
                neeedLeftWall = true;
                topWallStartAdjustment = -2f;
                leftWallStartAdjustment = 1f;
                break;
            case "RollingFallingObjectNovel":
                needTopWall = true;
                neeedLeftWall = true;
                topWallStartAdjustment = -2f;
                leftWallStartAdjustment = 1f;
                break;

            default:
                needTopWall = false;
                neeedLeftWall = false;
                topWallStartAdjustment = 0.5f;
                leftWallStartAdjustment = 0.5f;
                break;
        }

        // overwrite which walls to add depending on the path blocking direction (applied for cannotReachDirectly<a><b><d> terms - which is only in SingleForceTopBlocked and SingleForceLeftBlocked scenarios)
        if (pathObstructedDirection.GetType() == typeof(Left))
        {
            needTopWall = false;
            neeedLeftWall = true;
        }
        else if (pathObstructedDirection.GetType() == typeof(Above))
        {
            needTopWall = true;
            neeedLeftWall = false;
        }
    }


    private List<ObjectGrammar> AddADiscreteWall(float startPosition, float endPosition, float fixedCoordinate, string fixedAxis)
    {
        /*
            This function returns a set of small platforms that forms a discrete wall given the boundries and the axis
            for horizontal wall (top wall) starting position is the leftmost position
            for vertical wall (left wall) starting position is the topmost position
         */
        Debug.Log("AddADiscreteWall - startPosition: " + startPosition + " endPosition: " + endPosition + " fixedCoordinate: " + fixedCoordinate + " fixedAxis: " + fixedAxis);
        // fix for FallingObjectNovel novelties, left wall start position should be below the below target and end position should be ground
        if ((scenario.scenarioName == "FallingObjectNovel") & (fixedAxis == "X"))
        {
            startPosition = endPosition - leftWallStartAdjustment - 2f;
            endPosition = -3.5f;
        }

        List<ObjectGrammar> platformWalls = new List<ObjectGrammar>();
        float gap = 0.7f; // the gap between two platform blocks
        float currentWallSpannedPosition = startPosition;

        if (fixedAxis == "Y") // horizontal wall (top wall)
        {
            while (currentWallSpannedPosition <= endPosition)
            {
                FlatSurface topWall = new FlatSurface();
                float centreX = currentWallSpannedPosition + topWall.gameObject.size[0] / 2; // calculate the centreX considering the width of the object
                topWall.gameObject.positionX = centreX;
                topWall.gameObject.positionY = fixedCoordinate; // y is fixed

                topWall.gameObject.scaleX = 1; // width is kept as the original width
                topWall.gameObject.scaleY = 0.3f; // height is made thinner to look better

                topWall.gameObject.groupID = Utilities.pathObstructingWallsGroupID;
                platformWalls.Add(topWall);

                currentWallSpannedPosition = centreX + gap; // update the span of the wall
            }
        }
        else if (fixedAxis == "X") // vertical wall (left wall)
        {
            while (currentWallSpannedPosition >= endPosition)
            {
                FlatSurface leftWall = new FlatSurface();
                float centreY = currentWallSpannedPosition - leftWall.gameObject.size[1] / 2; // calculate the centreY considering the height of the object
                leftWall.gameObject.positionY = centreY;
                leftWall.gameObject.positionX = fixedCoordinate; // x is fixed

                leftWall.gameObject.scaleY = 1; // height is kept as the original height
                leftWall.gameObject.scaleX = 0.3f; // width is made thinner to look better

                leftWall.gameObject.groupID = Utilities.pathObstructingWallsGroupID;
                platformWalls.Add(leftWall);

                currentWallSpannedPosition = centreY - gap; // update the span of the wall
            }

        }

        // code to add a single solid platform as the wall (saved from AddPlatformWalls function)
        //List<ObjectGrammar> platformWalls = new List<ObjectGrammar>();

        // top wall
        //FlatSurface topWall = new FlatSurface();
        //topWall.gameObject.positionX = topWallLeftmostPosition + (topWallRightmostPosition - topWallLeftmostPosition) / 2; // calculate the centre x position according to the calcualted rightmostPosition and leftmostPosition
        //topWall.gameObject.positionY = MBROfTheLayout[3] + 0.5f; // +0.5 is to shift the wall up a little bit

        //topWall.gameObject.scaleX = (topWallRightmostPosition - topWallLeftmostPosition) / topWall.gameObject.size[0]; // scale the wall according to the calcualted rightmostPosition and leftmostPosition
        //topWall.gameObject.scaleY = 0.3f; // make it thinner

        //topWall.gameObject.groupID = Utilities.pathObstructingWallsGroupID;
        //platformWalls.Add(topWall);

        //left wall
        //FlatSurface leftWall = new FlatSurface();
        //leftWall.gameObject.positionX = MBROfTheLayout[0] - 0.5f; // +0.5 is to shift the wall to the left a bit
        //leftWall.gameObject.positionY = leftWallBottommostPosition + (leftWallTopmostPosition - leftWallBottommostPosition) / 2; // calculate the centre x position according to the calcualted topMostPosition and bottomMostPosition

        //leftWall.gameObject.scaleX = 0.3f; // make it thinner
        //leftWall.gameObject.scaleY = (leftWallTopmostPosition - leftWallBottommostPosition) / leftWall.gameObject.size[1]; // scale the wall according to the calcualted topMostPosition and bottomMostPosition

        //leftWall.gameObject.groupID = Utilities.pathObstructingWallsGroupID;
        //platformWalls.Add(leftWall);

        return platformWalls;
    }



    private List<Object> GetBirdsTarget()
    {
        /*
            Function borrowed from the liesOnPathSolver
            Given a scenario this function returns the first object that the bird hits.
        */

        List<Object> birdsTargets = new List<Object>(); // bird has multiple(two) target ojects in the novely version

        foreach (VerbGrammar verb in scenario.verbs)
        {
            if ((verb.GetType() == typeof(Hit)) | (verb.GetType() == typeof(HitDestroy))) // get hit/hit destroy verbs
            {
                switch (verb)  // check if it is with a bird
                {
                    case Hit hit:
                        if (hit.a.GetType() == typeof(NormalBird))
                        {
                            birdsTargets.Add(hit.b.gameObject);
                        }
                        break;
                    case HitDestroy hitDestroy:
                        if (hitDestroy.a.GetType() == typeof(NormalBird))
                        {
                            birdsTargets.Add(hitDestroy.b.gameObject);
                        }
                        break;
                }
            }
        }
        return birdsTargets;
    }

    private float[] GetTheMBROfAllTheLayout()
    {
        /*
            This function returns the MBR of all the objects in the layout
            rerurns [ minX, maxX, minY, maxY ]
         */

        List<PathObstructed> pathObstructedTerms = new List<PathObstructed>();
        float minX = 10000, maxX = -10000, minY = 10000, maxY = -10000;
        float[] objectWidthAndHeight;
        float objectXPosition;
        float objectYPosition;

        foreach (ObjectGrammar objectGrammar in scenario.objects)
        {
            // skip birds as birds position is not considered when determining the MBR of the layout
            if (objectGrammar.GetType() == typeof(NormalBird))
            {
                continue;
            }

            objectWidthAndHeight = Utilities.GetMBRWidthAndHeight(objectGrammar.gameObject);
            objectXPosition = objectGrammar.gameObject.positionX;
            objectYPosition = objectGrammar.gameObject.positionY;
            Debug.Log(objectGrammar.gameObject.GetType() + ", objectXPosition: " + objectXPosition + " objectYPosition: " + objectYPosition);

            if ((objectXPosition - objectWidthAndHeight[0] / 2) < minX)
            {
                minX = (objectXPosition - objectWidthAndHeight[0] / 2);
            }
            if ((objectXPosition + objectWidthAndHeight[0] / 2) > maxX)
            {
                maxX = (objectXPosition + objectWidthAndHeight[0] / 2);
            }
            if ((objectYPosition - objectWidthAndHeight[1] / 2) < minY)
            {
                minY = (objectYPosition - objectWidthAndHeight[1] / 2);
            }
            if ((objectYPosition + objectWidthAndHeight[1] / 2) > maxY)
            {
                maxY = (objectYPosition + objectWidthAndHeight[1] / 2);
            }
        }

        return new float[] { minX, maxX, minY, maxY };
    }


    private List<PathObstructed> GetPathObstructedTerms()
    {
        /*
            This function returns all the pathObstructed terms
         */

        List<PathObstructed> pathObstructedTerms = new List<PathObstructed>();

        foreach (LayoutGrammar layout in scenario.layouts)
        {
            if ((layout.GetType() == typeof(PathObstructed))) // get the PathObstructed layout terms
            {
                Debug.Log("PathObstructed Term considered: a: " + layout.a.gameObject + " b: " + layout.b.gameObject);
                // the liesonpath 
                pathObstructedTerms.Add((PathObstructed)layout);
            }
        }
        return pathObstructedTerms;
    }

}
