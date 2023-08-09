using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine.SceneManagement;

public class LiesOnPathSolver : MonoBehaviour
{
    public Scenario scenario;
    public Utilities utilities;
    public ConstraintSolver constraintSolver;
    private static LiesOnPathSolver _instance;

    public bool liesOnPathSolverCompleted = false;
    public int solutionShotIndex = -1; // this variable is used to save the index of the solution shot to verify whether the solution works later
    public List<(Vector2, string)> birdsTargetPoints; // this is used to save the target points tested using different trajectories in TrajectorySimulator
    public Object[] birdsTargetObjects;
    private bool reloading = false;
    //private void Awake()
    //{
    //    // avoid destroying this object even after moving to another scene
    //    DontDestroyOnLoad(this.gameObject);

    //    // set this object to the _instance in order to access this monobehaviour from other objects (e.g. LayoutGenerator)
    //    if (_instance != null && _instance != this)
    //    {
    //        Destroy(this.gameObject);
    //    }
    //    else
    //    {
    //        _instance = this;
    //    }
    //}


    public void SolveLiesOnPathTerms()
    {
        /*
             This function extract out the birds' target object/s and the objects(pathObservingObjects) associated with liesOnPath terms.
             Then simulate different inputs to the target object/s (bird shot in different angles) and record the output behaviour of the pathObservingObjects.
         */

        utilities = new Utilities(); // for some reason utilities object is not assigned from the layoutgenerator, check later, therefore create a new object

        // assign the instance IDs for the objects
        utilities.GetInstanceIDsOfObjects(scenario.objects);

        // get the first target object (which the bird hits) from the scenario description
        //Object birdsTargetObject = GetBirdsTarget();
        //Debug.Log("Target object found: " + birdsTargetObject);

        // there are two target objects, one for N and the other for NN

        Object[] targetObejcts = { scenario.nonNovelTarget, scenario.novelTarget };

        // get the object whose path needs to be observed
        HashSet<Object> pathObservingObjects = GetPathObeservingObjects();
        foreach (Object objct in pathObservingObjects)
            Debug.Log("Path observing object: " + objct + " with ID: " + objct.instanceID);

        // immortalize the blocks in the main scene
        // ImmortalizeBlocksInMainScene();

        // simulate the scene and record the trajectories
        SimulateAndRecordTrajectories(targetObejcts, pathObservingObjects);

        // using the simulation results adjust the positions of the gameobjects
        // wait until the trajectory simulation is ended

        //if (!TrajectorySimulator.Instance.trajectorySimulationEnded)
        //{
        StartCoroutine(ProcessSimulationResults());
        //}

    }

    IEnumerator ProcessSimulationResults()
    {
        /*
            This function uses the simulation results (recorded trajectories of the pathObservingObjects) and satisfy the simulation related layout terms (liesOnPath)
        */

        while (!TrajectorySimulator.Instance.trajectorySimulationEnded)
        {
            // Debug.Log("trajectory simulation has not yet ended: waiting...");
            yield return null;
        }

        // the trajectory simulation is ended, use the results
        if (TrajectorySimulator.Instance.trajectorySimulationEnded)
        {
            Debug.Log("trajectory simulation has ended: adjusting the object positions");

            // int shotsPrinted = 0;
            List<IDictionary<String, List<Vector2>>> nonNovelSimulationResults = TrajectorySimulator.Instance.nonNovelSimulationResults;
            List<IDictionary<String, List<Vector2>>> novelSimulationResults = TrajectorySimulator.Instance.novelSimulationResults;

            // split the simulation results into 2 for convenience (for NN solution and N solution separately) =========== start from here

            Debug.Log("NN simulation results");
            foreach (IDictionary<string, List<Vector2>> shotData in nonNovelSimulationResults)
            {
                // Debug.Log("Sim solver: Printing the shot: " + (shotsPrinted + 1));
                foreach (KeyValuePair<string, List<Vector2>> kvp in shotData)
                {
                    Debug.Log("Key = " + kvp.Key + " Value = " + string.Join(", ", kvp.Value));
                }
                // shotsPrinted++;
            }

            Debug.Log("N simulation results");
            foreach (IDictionary<string, List<Vector2>> shotData in novelSimulationResults)
            {
                // Debug.Log("Sim solver: Printing the shot: " + (shotsPrinted + 1));
                foreach (KeyValuePair<string, List<Vector2>> kvp in shotData)
                {
                    Debug.Log("Key = " + kvp.Key + " Value = " + string.Join(", ", kvp.Value));
                }
                //  shotsPrinted++;
            }

            // analyze the NN simulation results and adjust the object to make it solvable for the NN shot
            AdjustObjectPositionsFromSimulationResults(nonNovelSimulationResults);

            // then with the current placement, check if the Novel path does not solve the task
            Debug.Log("calling CheckObjectLiesOnPathSatisfaction for novel path");
            Dictionary<LayoutGrammar, int[]> novelPathSatisfaction = CheckObjectLiesOnPathSatisfaction(novelSimulationResults, "Novel");
            // check if the last liesonpath is satisfied for any shot, if so abort
            if (Array.Exists<int>(novelPathSatisfaction.Values.Last(), element => element == 1))
            {
                throw new Exception("The novel path is solvable in the non novel template"); // uncomment later
            }

            // throw new Exception("liesonpthsolver: Ended VerifyNovelPathUnsolvability");

            // save the birds targetObject and birdsTargetPoints for future simulations
            birdsTargetObjects = TrajectorySimulator.Instance.birdsTargetObjects;
            birdsTargetPoints = TrajectorySimulator.Instance.birdsTargetPoints;

            liesOnPathSolverCompleted = true;
        }

    }

    private void AdjustObjectPositionsFromSimulationResults(List<IDictionary<String, List<Vector2>>> simulationResults)
    {
        /* 
            Using the simulation results, if a liesOnPath term is not satisfied for enough shots,
            this function tries to place the object A (in liesOnPath<A><B>) in the position that most paths of B lies and 
            verify the position still satisfy the constraints in the dimension grapgh.

            TODO: ideally variable values should be propageted again considering the new position of A
        */

        // analyze the simulation results to get the satisfied/unsatisfied shots for the liesOnPath term
        Dictionary<LayoutGrammar, int[]> liesOnPathSatisfactionResults = CheckObjectLiesOnPathSatisfaction(simulationResults, "NonNovel");

        foreach (int[] liesOnPathSatisfactionResult in liesOnPathSatisfactionResults.Values)
        {
            Debug.Log("successful shots: " + string.Join(" ", liesOnPathSatisfactionResult));
        }

        // throw new Exception("Test:AdjustObjectPositionsFromSimulationResults");

        // check if atleast one shot cuts the path for the last liesOnPath term (used later to decide whether to keep the original layout if the modified layout by palcing the object in best path cutting position does not satisfy CSP)
        bool originalLayoutSatisfyLastLiesOnPath = false;
        int originalLayoutSolutionShotIndex = -1;
        if (Array.Exists<int>(liesOnPathSatisfactionResults.Values.Last(), element => element == 1))
        {
            originalLayoutSatisfyLastLiesOnPath = true;
            originalLayoutSolutionShotIndex = Array.IndexOf(liesOnPathSatisfactionResults.Values.Last(), 1);
            Debug.Log("there is atleast one shot for the last liesOnPath term that cuts the object in the original placed location.");
        }

        // if the scenario is single force / multiple forces, no need to do the below things as the pig is already placed in a reachable position, so just return the function
        //if (scenario.scenarioName == "SingleForce" | scenario.scenarioName == "SingleForceTopBlocked" | scenario.scenarioName == "SingleForceLeftBlocked" | scenario.scenarioName == "MultipleForces")
        //{
        //    if (originalLayoutSatisfyLastLiesOnPath)
        //    { // there are shots that solves the level
        //      // set the solution shot index (this is used to verify the solution works later)
        //        solutionShotIndex = Array.IndexOf(liesOnPathSatisfactionResults.Values.Last(), 1);
        //        //Debug.Log("solutionShotIndex: " + solutionShotIndex);
        //        return;
        //    }
        //    else
        //    {
        //        throw new System.Exception("Single/multiple force scenario bird cannot reach the pig");
        //    }
        //}


        // checking the most relevant place for A (place that cuts the most number of paths of B)
        List<Tuple<float[], int[]>> observingObjectsXLocationsAndPathsCutted = new List<Tuple<float[], int[]>>();
        foreach (LayoutGrammar liesOnPathTerm in liesOnPathSatisfactionResults.Keys)
        {
            // Debug.Log(liesOnPathTerm.b.gameObject.GetType().BaseType.Name);
            if (scenario.scenarioName != "BouncingBird" & liesOnPathTerm.b.gameObject.GetType().BaseType.Name != "Bird") // skip the bird liesOnPath terms as there are other liesOnPath terms after that
            {
                observingObjectsXLocationsAndPathsCutted.Add(GetTheNumberOfPathsCuttedInLocations(liesOnPathTerm.a.gameObject, utilities.GetPathsFromSimulation(simulationResults, liesOnPathTerm.b.gameObject.instanceID)));
            }
            else
            { // for birdBouncing bird has to be considered as bird is associated with all the liesOnPath terms
                observingObjectsXLocationsAndPathsCutted.Add(GetTheNumberOfPathsCuttedInLocations(liesOnPathTerm.a.gameObject, utilities.GetPathsFromSimulation(simulationResults, liesOnPathTerm.b.gameObject.instanceID)));
            }
        }

        // place the objects (adjust the x coordinate) in the most path crossing position
        // TODO: currently only the last liesOnPath satisfaction is checked (assuminmg that more than one liesonpath terms in sequence are rare, and to satisfy the last, prior ones should have been already satisfied).
        // get the last liesOnPath terms XLocationsAndPathsCutted data and adjust the object B accordingly
        Debug.Log("xCoorinatesOfBAtAsYLevel: " + string.Join(" ", observingObjectsXLocationsAndPathsCutted.Last().Item1));
        Debug.Log("numberOfPathsCutted: " + string.Join(" ", observingObjectsXLocationsAndPathsCutted.Last().Item2));

        MoveObjectToMostPathCutPositionAndVerifyCSP(liesOnPathSatisfactionResults.Keys.Last().a.gameObject, observingObjectsXLocationsAndPathsCutted.Last().Item1, observingObjectsXLocationsAndPathsCutted.Last().Item2, originalLayoutSatisfyLastLiesOnPath, originalLayoutSolutionShotIndex);
    }

    private void MoveObjectToMostPathCutPositionAndVerifyCSP(Object gameObjectToMove, float[] xPositions, int[] pathsCutAtEachXPosition, bool doesOriginalLayoutSatisfyLastLiesOnPath, int originalLayoutSolutionShotIndex)
    {
        /*
         This function adjusts the x position of the object to the most path cutting position, then verifies whether the new placement still satisfies the constraints
         TODO: maybe it is not always needed to adjust the position (as the original position is already good enough - when that position already solves for enough shots) - this is partially done, if the new layout doenst satisfy CSP, and the
         original layout has solutions, it is reverted to the original layout
        */

        // special case for RollingObjectNovel2 and SlidingObjectNovel2 scenario as the sliding surface is flat, consider only the case where OriginalLayoutSatisfyLastLiesOnPath
        if (((scenario.scenarioName == "RollingObjectNovel") | (scenario.scenarioName == "SlidingObjectNovel")) & (scenario.scenarioIndex == 2))
        {
            if (doesOriginalLayoutSatisfyLastLiesOnPath) // start from here: as seems to be always this is false
            {
                Debug.Log("the original layout cut paths, keep the initial posision of the obejcts!");
                // set the solution shot index (this is used to verify the solution works later)
                solutionShotIndex = originalLayoutSolutionShotIndex;
                return;
            }
            else
            {
                throw new System.Exception("Special scenario where surface is flat: the original placement does not satisfy the liesonpth, aborting!");
            }
        }

        // get the index of the position of most cutted number of paths
        int indexOfMaxNumberOfPathsCutted = pathsCutAtEachXPosition.ToList().IndexOf(pathsCutAtEachXPosition.Max());
        Debug.Log("gameObject: " + gameObject.GetType() + " indexOfMaxNumberOfPathsCutted: " + indexOfMaxNumberOfPathsCutted + " maximumPathsCuttedPosition: " + xPositions[indexOfMaxNumberOfPathsCutted]);

        // validation check (if the maximumPathsCuttedPosition is -100 then the gameObjectToMove never reaches the position of A for the simulated six shots, throw exception)
        if (xPositions[indexOfMaxNumberOfPathsCutted] == -100)
        {
            // return;
            throw new System.Exception("No feasible position for the object to place!");
            // Debug.Log("Exception: No feasible position for the object to place!");
            // ABSceneManager.Instance.LoadScene("LevelGenerator");
            // ReloadTheGame();
        }
        //throw new System.Exception("waiteeeeeeeeeeeeeeeeeeeeeeeeeeeee");
        // find the objects with the same groupID of that of gameObjectToMove
        List<Object> objectsInTheSameGroupToMove = GetObjectsWithSameID(gameObjectToMove, scenario.objects);
        // Debug.Log(string.Join(" ", objectsInTheSameGroupToMove));

        // move the gameObjectToMove first (only x coordinate is needed to be moved, as currently previous implementations are only for x axis). 
        // the selected x coordinate is the leftmost point of the object, so the centre position of the obejct needs to be calculated using the width of the object.
        Debug.Log("gameObjectToMove old position: " + gameObjectToMove + " " + gameObjectToMove.positionX);
        //float xCoordinateShift = xPositions[indexOfMaxNumberOfPathsCutted] + gameObjectToMove.size[0] / 2 - gameObjectToMove.positionX;
        //gameObjectToMove.positionX = xPositions[indexOfMaxNumberOfPathsCutted] + gameObjectToMove.size[0] / 2;
        float xCoordinateShift = xPositions[indexOfMaxNumberOfPathsCutted] - gameObjectToMove.positionX;
        gameObjectToMove.positionX = xPositions[indexOfMaxNumberOfPathsCutted];
        Debug.Log("gameObjectToMove new position: " + gameObjectToMove + " " + gameObjectToMove.positionX);

        // shift the other objects in the same group
        foreach (Object objct in objectsInTheSameGroupToMove)
        {
            //Debug.Log("same group object old position: " + objct+" " + objct.positionX);
            objct.positionX += xCoordinateShift;
            Debug.Log("same group object new position: " + objct + " " + objct.positionX);
        }

        try
        {
            // verify whether the spatial constrains are still satified after the change
            // constraintSolver.VerifyCurrentLayoutSatisfyAllConstraints(); // removed this as this wont be satisfied anyways because we are moving the pig independent of the novel objects

            // set the solution shot index (this is used to verify the solution works later)
            solutionShotIndex = indexOfMaxNumberOfPathsCutted;
        }
        catch (System.Exception e)
        {
            Debug.Log(e);
            Debug.Log("The most path cutted place does not satisfy the constraints, checking if the initial palcement cut atleast one path");
            if (doesOriginalLayoutSatisfyLastLiesOnPath)
            {
                Debug.Log("yes, the original layout cut paths, keep the initial posision of the obejcts - we have to revert the changes!");
                // reverting the position of the object considered and objects of the same group back to their original position
                gameObjectToMove.positionX = gameObjectToMove.positionX - xCoordinateShift;
                Debug.Log("gameObjectToMove moved back to the initial position: " + gameObjectToMove + " " + gameObjectToMove.positionX);
                // shift the other objects in the same group
                foreach (Object objct in objectsInTheSameGroupToMove)
                {
                    //Debug.Log("same group object old position: " + objct+" " + objct.positionX);
                    objct.positionX -= xCoordinateShift;
                    Debug.Log("same group object moved back to the initial position: " + objct + " " + objct.positionX);
                }
                // set the solution shot index (this is used to verify the solution works later)
                solutionShotIndex = originalLayoutSolutionShotIndex;

            }
            else
            {
                Debug.Log("Exception: the initial palcement cut none of the paths, aborting!");
                throw new System.Exception("could not satisfy the CSP after moving to the best path cut position and the initial position also does not satisfy the last liesOnPath term!");
                //ABSceneManager.Instance.LoadScene("LevelGenerator");
                //ReloadTheGame();
            }

        }

    }

    public void ReloadTheGame()
    {
        if (!reloading)
        {
            reloading = true;
            // destroy donotdestroyonload objects
            var go = new GameObject("Sacrificial Lamb");
            DontDestroyOnLoad(go);

            foreach (var root in go.scene.GetRootGameObjects())
            {
                Debug.Log("destroying the DontDestroyOnLoad object" + root.name);
                Destroy(root);
            }

            // Unload all currently loaded scenes
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name != "LevelGenerator")
                {
                    Debug.Log("unloading the scene: " + scene.name);
                    SceneManager.UnloadSceneAsync(scene);
                }
            }

            // Load the new scene
            SceneManager.LoadScene("LevelGenerator");
        }


    }

    private Tuple<float[], int[]> GetTheNumberOfPathsCuttedInLocations(Object objectA, List<List<Vector2>> pathsOfObjectB)
    {
        /*
         given an object A and the paths of another object B, this function returns the position that A can be placed such that A cuts the most paths of B
         TODO: currently the x positions of the object A is checked and adjusted, advance it to check the y positions as well
         */
        float[] objectAYRange = { objectA.positionY - objectA.size[1] / 2, objectA.positionY + objectA.size[1] / 2 };

        float[] xCoorinatesOfBAtAsYLevel = { -100, -100, -100, -100, -100, -100 };
        int checkingPathIndex = 0;
        foreach (List<Vector2> path in pathsOfObjectB)
        {
            foreach (Vector2 point in path)
            {
                // check y coordinate of the path point and capture the initial point that falls into the y range of A

                // dirty fix for bouncing (for bouncing skip the initial part of the birds path as the pig needs to placed after the bird bounces off the platform)
                if ((scenario.scenarioName == "BouncingBird") & (point.x < -8))
                {
                    continue;
                }

                if ((objectAYRange[0] < point.y) && (point.y < objectAYRange[1]))
                {
                    xCoorinatesOfBAtAsYLevel[checkingPathIndex] = point.x;
                    break;
                }
            }
            checkingPathIndex++;
        }
        Debug.Log("B's paths' x coorditates at " + objectA.GetType().BaseType.Name + "'s y coordinate range: " + string.Join(" ", xCoorinatesOfBAtAsYLevel));

        // order the found x coordinates from least to highest values 
        float[] sortedXCoorinatesOfBAtAsYLevel = new float[xCoorinatesOfBAtAsYLevel.Length];
        xCoorinatesOfBAtAsYLevel.CopyTo(sortedXCoorinatesOfBAtAsYLevel, 0);
        Array.Sort(sortedXCoorinatesOfBAtAsYLevel);

        // considering the width of objectA, check which position will include maximum X coorinates of B
        int numberOfPointsToCheck = xCoorinatesOfBAtAsYLevel.Length;
        int[] numberOfPathsCutted = new int[numberOfPointsToCheck];
        int index = 0;
        while (index < numberOfPointsToCheck)
        {
            if (sortedXCoorinatesOfBAtAsYLevel[index] == -100)
            {
                numberOfPathsCutted[index] = 0;
            }
            else if (index == (numberOfPointsToCheck - 1))
            { // last point, only that point can be cut when A is placed there
                numberOfPathsCutted[index] = 1;
            }
            else
            {
                // assume A's minX is placed at the position of sortedXCoorinatesOfBAtAsYLevel[index] and find out how many other points A's width can cover (to the right)
                int nextPointIndex = index + 1;
                int pointsCoveredCount = 1; // points covered count, start with 1 as sortedXCoorinatesOfBAtAsYLevel[index] is covered by default
                ArrayList allPathsCutPositions = new ArrayList { sortedXCoorinatesOfBAtAsYLevel[index] };
                while (nextPointIndex < numberOfPointsToCheck)
                {
                    if ((sortedXCoorinatesOfBAtAsYLevel[nextPointIndex] - sortedXCoorinatesOfBAtAsYLevel[index]) < objectA.size[0])
                    {
                        pointsCoveredCount++;
                    }
                    else
                    {
                        break; // if it does not cover the current checking point, it will never cover the next point, so stop checking
                    }

                    nextPointIndex++;
                }
                numberOfPathsCutted[index] = pointsCoveredCount;
            }
            index++;
        }

        // reorder the numberOfPathsCutted to match with the paths' index
        int[] numberOfPathsCuttedInPathsOrder = new int[numberOfPointsToCheck];
        int indexOne = 0;
        int indexTwo;

        while (indexOne < numberOfPointsToCheck)
        {
            indexTwo = 0;
            while (indexTwo < numberOfPointsToCheck)
            {
                if (xCoorinatesOfBAtAsYLevel[indexOne] == sortedXCoorinatesOfBAtAsYLevel[indexTwo])
                {
                    numberOfPathsCuttedInPathsOrder[indexOne] = numberOfPathsCutted[indexTwo];
                }
                indexTwo++;
            }
            indexOne++;
        }

        // print stuff
        Debug.Log("width of the object: " + objectA.size[0] + " x position of the object: " + objectA.positionX);
        Debug.Log("sorted y coordinates: " + string.Join(" ", sortedXCoorinatesOfBAtAsYLevel));
        Debug.Log("points covered count: " + string.Join(" ", numberOfPathsCutted));
        Debug.Log("points covered count in paths order: " + string.Join(" ", numberOfPathsCuttedInPathsOrder));

        // return the xCoordinates and the numberOfPathsCutted when A is placed in those x coordinates
        return new Tuple<float[], int[]>(xCoorinatesOfBAtAsYLevel, numberOfPathsCuttedInPathsOrder);
    }


    private Dictionary<LayoutGrammar, int[]> CheckObjectLiesOnPathSatisfaction(List<IDictionary<String, List<Vector2>>> simulationResults, string consideredSequence)
    {

        /*
         This function checks whether the liesOnPath terms are statisfied in the current placement (from the simulation results).
         Currently it verifies the below conditions. For the term liesOnPath(a, b) (=a lies on the path of b):
            1. whether B touches A (and for how many shots)
         */

        Dictionary<LayoutGrammar, int[]> successfulPathsOfAllLiesOnPathTerms = new Dictionary<LayoutGrammar, int[]>(); // a list to save the successful paths (indexes) of each liesOnPath terms

        // get the path observing terms to see who should lie on whose path
        foreach (LayoutGrammar layout in scenario.layouts)
        {

            if ((layout.GetType() == typeof(LiesOnPath))) // get the liesonpath layout terms
            {
                if (consideredSequence == "Novel")
                {
                    // filter only novel layout objects related terms
                    if (!layout.a.gameObject.isNovel & !layout.b.gameObject.isNovel)
                    {
                        continue;
                    }
                }
                else
                {
                    // filter only non novel layout terms
                    if (layout.a.gameObject.isNovel | layout.b.gameObject.isNovel)
                    {
                        continue;
                    }
                }

                Debug.Log(consideredSequence + " LiesOnPath a: " + layout.a.gameObject + " " + layout.a.gameObject.GetType().BaseType.Name + " id: " + layout.a.gameObject.instanceID + " b: " + layout.b.gameObject + " " + layout.b.gameObject.GetType().BaseType.Name + " id: " + layout.b.gameObject.instanceID);

                // ################ checking whether b collides with a ################
                // getting the range of object a (the roatation of the object is disregarded), in the order [x min, x max, y min, y max]
                float objectAWidth = layout.a.gameObject.size[0] / 2;
                float objectAHeight = layout.a.gameObject.size[1] / 2;
                float[] objectARange = { layout.a.gameObject.positionX - objectAWidth, layout.a.gameObject.positionX + objectAWidth, layout.a.gameObject.positionY - objectAHeight, layout.a.gameObject.positionY + objectAHeight };

                float objectAXPosition = layout.a.gameObject.positionX;
                float objectAYPosition = layout.a.gameObject.positionY;
                // finding the simulation data of the object b
                List<List<Vector2>> pathsOfObjectB = utilities.GetPathsFromSimulation(simulationResults, layout.b.gameObject.instanceID);

                // checking the moving paths of b, and determine if they cross the range of a
                int numberOfPathsOfBCrossedA = 0;
                int[] successfulPaths = new int[simulationResults.Count];
                int pathIndexConsidered = 0;
                foreach (List<Vector2> path in pathsOfObjectB)
                {
                    Debug.Log("checking the shot(path) " + string.Join(" ", path));
                    Debug.Log("stationary object position " + objectAXPosition + " " + objectAYPosition);
                    foreach (Vector2 point in path)
                    {
                        // check x and y coordinate
                        // TODO: when colliding, does B go to the position of A (eg: bird B just bounce off A), currently uses the expandingFactor to expand the size of A, think of a better way
                        float expandingFactor = 0.5f;
                        if ((objectARange[0] - expandingFactor < point.x) && (point.x < objectARange[1] + expandingFactor) && (objectARange[2] - expandingFactor < point.y) && (point.y < objectARange[3] + expandingFactor))
                        {
                            numberOfPathsOfBCrossedA++;
                            // Debug.Log(layout.a.gameObject.GetType().BaseType.Name + " is in the path of " + layout.b.gameObject.GetType().BaseType.Name);
                            successfulPaths[pathIndexConsidered] = 1;
                            break;
                        }
                    }

                    pathIndexConsidered++;
                }

                successfulPathsOfAllLiesOnPathTerms.Add(layout, successfulPaths);
                // Debug.Log("number of paths lied on " + layout.a.gameObject.GetType().BaseType.Name + " " + numberOfPathsOfBCrossedA);
                Debug.Log("successful shots: " + string.Join(" ", successfulPaths));
            }
        }

        // throw new Exception("Exception for testing");

        return successfulPathsOfAllLiesOnPathTerms;
    }


    private void SimulateAndRecordTrajectories(Object[] targetObejcts, HashSet<Object> pathObservingObjects)
    {
        /*
            This function simulates the game and record the path of the observing objects
         */
        //Debug.Log("pathObservingObjects[1].instanceID: " + pathObservingObjects[1].instanceID);

        // set the birds target object
        //GameObject birdsTargetGameObject = (GameObject)EditorUtility.InstanceIDToObject(birdsTargetObject.instanceID);
        //Debug.Log("birdsTargetGameObject: " + birdsTargetGameObject.name);
        //TrajectorySimulator.Instance.birdsTargetObject = birdsTargetGameObject;

        // perform the trajectory simulation
        //utilities.GetAllObjects(scenario);
        //TrajectorySimulator.Instance.allObjectsToSimulate = utilities.GetAllObjects(scenario);
        //TrajectorySimulator.Instance.pathObservingObjects = pathObservingObjects;
        //TrajectorySimulator.Instance.birdsTargetObject = birdsTargetObject;
        TrajectorySimulator.Instance.SetParameters(utilities.GetAllObjects(scenario), targetObejcts, pathObservingObjects);
        TrajectorySimulator.Instance.startTrajectorySimulation = true;


    }

    private List<Object> GetObjectsWithSameID(Object gameObject, List<ObjectGrammar> allObjects)
    {
        /*
         Given an object, this function returns all the other objects with the same groupID as the given object
        */
        List<Object> objectsWithSameGroupID = new List<Object>();

        foreach (ObjectGrammar objct in allObjects)
        {
            if (objct.gameObject.groupID == gameObject.groupID)
            {
                // skip the original object (gameObject) 
                if (objct.gameObject != gameObject)
                {
                    objectsWithSameGroupID.Add(objct.gameObject);
                }
            }
        }
        return objectsWithSameGroupID;
    }

    private HashSet<Object> GetPathObeservingObjects()
    {
        /*
            This function return all the objects whose paths are needed to be observed
            i.e. the objects that has liesOnPath terms
         */
        HashSet<Object> observingObjects = new HashSet<Object>();

        foreach (LayoutGrammar layout in scenario.layouts)
        {
            if ((layout.GetType() == typeof(LiesOnPath))) // get the liesonpath layout terms
            {
                Debug.Log("LayoutGrammar Term considered: a" + layout.a.gameObject + " b: " + layout.b.gameObject);
                // the liesonpath 
                observingObjects.Add(layout.b.gameObject);
            }
        }
        return observingObjects;
    }

    private Object GetBirdsTarget()
    {
        /*
            Given a scenario this function returns the first object that the bird hits. TODO: handle the multiple bird scenario 
            assumption: currently the bird hit is in the firstly occuring hit/hitdestroy verbs
        */

        foreach (VerbGrammar verb in scenario.verbs)
        {
            if ((verb.GetType() == typeof(Hit)) | (verb.GetType() == typeof(HitDestroy))) // get hit/hit destroy verbs
            {
                switch (verb)  // check if it is with a bird
                {
                    case Hit hit:
                        if (hit.a.GetType() == typeof(NormalBird))
                        {
                            return hit.b.gameObject;
                        }
                        break;
                    case HitDestroy hitDestroy:
                        if (hitDestroy.a.GetType() == typeof(NormalBird))
                        {
                            return hitDestroy.b.gameObject;
                        }
                        break;
                }
            }
        }
        return null;
    }
}
