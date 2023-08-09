using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;


public class NovelTemplateBuilder : MonoBehaviour
{
    public Scenario scenario;
    public Utilities utilities;

    public bool novelTemplateBuilderCompleted = false;
    public int novelSolutionShotIndex;

    bool rightForceNovelty = false;
    bool leftForceNovelty = false;
    bool upForceNovelty = false;
    bool downForceNovelty = false;

    bool novelObjectPlaced = false;
    bool levelReloadingStarted = false;
    AsyncOperation levelReloadingCompleted;

    // Start is called before the first frame update
    void Start()
    {
        // avoid destroying this object even after moving to another scene
        DontDestroyOnLoad(this.gameObject);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void StartBuildingNovelTemplate()
    {
        // first place the novel object in the designated location
        PlaceNovelObject();

        // reload the game level after adding the novel object
        StartCoroutine(ReloadLevel(novelObjectPlaced));

        // Play the novel and non novel solution and record their outcomes
        TrajectorySimulator.Instance.trajectorySimulationEnded = false;

        StartCoroutine(SimulateSolutions());

        // Process the simulation results
        StartCoroutine(ProcessSimulationResults());


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

            // check if the non novel path doesn't solve the game
            VerifySolvability(nonNovelSimulationResults, "NonNovel");

            // check if the novel path solves the game
            VerifySolvability(novelSimulationResults, "Novel");

            // novel template building is completed
            novelTemplateBuilderCompleted = true;

        }
    }

    private void VerifySolvability(List<IDictionary<String, List<Vector2>>> simulationResults, string pathName)
    {
        /*
         * This fuction analyses the simulation results and check if it the novel/nonnovel path works after introducing novelty
         * PathName can be "Novel" or "NonNovel"
         */

        Dictionary<LayoutGrammar, int[]> successfulPathsOfAllLiesOnPathTerms = new Dictionary<LayoutGrammar, int[]>(); // a list to save the successful paths (indexes) of each liesOnPath terms

        // get the liesonpth terms and check if the intended term doesn't work
        foreach (LayoutGrammar layout in scenario.layouts)
        {
            float expandingFactor = 0.1f;
            // change the expanding factor for the novel path, as the object moves faser, it is good to have a larger threshold
            if (pathName == "Novel")
            {
                expandingFactor = 0.5f;
            }


            if ((layout.GetType() == typeof(LiesOnPath))) // get the liesonpath layout terms
            {
                if (pathName == "Novel") // novel path
                {
                    // filter only novel layout objects related terms
                    if (!layout.a.gameObject.isNovel & !layout.b.gameObject.isNovel)
                    {
                        continue;
                    }
                }
                else
                { // non novel path

                    // filter only non novel layout terms
                    if (layout.a.gameObject.isNovel | layout.b.gameObject.isNovel)
                    {
                        continue;
                    }
                }

                Debug.Log(pathName + " LiesOnPath a: " + layout.a.gameObject + " " + layout.a.gameObject.GetType().BaseType.Name + " id: " + layout.a.gameObject.instanceID + " b: " + layout.b.gameObject + " " + layout.b.gameObject.GetType().BaseType.Name + " id: " + layout.b.gameObject.instanceID);

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
                Debug.Log("successful shots in novel path: " + string.Join(" ", successfulPaths));
            }
        }

        if (pathName == "Novel") // novel path
        {

            // check if the last liesonpath is satisfied for any shot, if not abort
            if (!Array.Exists<int>(successfulPathsOfAllLiesOnPathTerms.Values.Last(), element => element == 1))
            {
                throw new Exception("The novel path is unsolvable in the novel template");
            }

            // get the index of a shot that solves the novel path
            novelSolutionShotIndex = Array.FindIndex<int>(successfulPathsOfAllLiesOnPathTerms.Values.Last(), element => element == 1);
        }
        else
        { // non novel path

            // check if the last liesonpath is satisfied for any shot, if so abort
            if (Array.Exists<int>(successfulPathsOfAllLiesOnPathTerms.Values.Last(), element => element == 1))
            {
                throw new Exception("The non novel path is still solvable in the novel template");
            }
        }
    }


    IEnumerator SimulateSolutions()
    {

        // wait until the reloading is finished
        while (levelReloadingStarted)
        {
            Debug.Log("levelReloadingStarted is still true");
            yield return null;
        }

        Debug.Log("starting to PlaySolutions");

        // refresh the object IDs of the obejcts in the scene (as the gameworld is reloaded previous object ids are now obsolete)
        utilities.GetInstanceIDsOfObjects(scenario.objects);

        // simulate the scene and record the trajectories
        TrajectorySimulator.Instance.prepareTheParallelSimulation(utilities.GetAllObjects(scenario)); // create a parallel scene and refresh the object list which includes the novel objects
        TrajectorySimulator.Instance.startTrajectorySimulation = true;

        //  throw new Exception("NovelTemplateBuilder PlaySolutions");
    }

    IEnumerator ReloadLevel(bool readyToReload)
    {
        // wait till the liesOnpath solver is done
        while (!readyToReload)
        {
            yield return null;
        }

        if (!levelReloadingStarted)
        {
            Debug.Log("reloading the gameworld");

            ABLevel gameLevel = utilities.GetGameLevel(scenario);
            ABGameWorld.currentLevel = gameLevel;
            levelReloadingStarted = true;

            levelReloadingCompleted = SceneManager.LoadSceneAsync("GameWorld");
        }

        while (!levelReloadingCompleted.isDone)
        {
            Debug.Log("still reloading the gameworld");
            yield return null;
        }

        Debug.Log("reloading the gameworld completed inside noveltemplatebuilder!");
        // ImmortalizeBlocksInMainScene();
        levelReloadingStarted = false;
        // Debug.Log("immortalizing the objects completed!");
    }

    void PlaceNovelObject()
    {
        // get the area that needs to cover by the novelty
        (Vector2 minCoordinate, Vector2 maxCoordinate) = DetermineNovelObjectFeasibleRange();

        // calculate the size of the area
        float areaWidth = maxCoordinate.x - minCoordinate.x;
        float areaHeight = maxCoordinate.y - minCoordinate.y;

        // novelty is placed at the centre of the feasible range
        Vector3 position = new Vector3(minCoordinate.x + areaWidth / 2, minCoordinate.y + areaHeight / 2);

        // create the novelty object and add it to the scenario
        ObjectGrammar novelObject = null;
        if (rightForceNovelty)
        {
            novelObject = new NovelObject(new RightForce(scenario.forceMagnitude));
        }
        else if (leftForceNovelty)
        {
            novelObject = new NovelObject(new LeftForce(scenario.forceMagnitude));
        }
        else if (upForceNovelty)
        {
            novelObject = new NovelObject(new UpForce(scenario.forceMagnitude));
        }
        else if (downForceNovelty)
        {
            novelObject = new NovelObject(new DownForce(scenario.forceMagnitude));
        }

        // place the novel object in the middle of the scene
        novelObject.gameObject.positionX = position.x;
        novelObject.gameObject.positionY = position.y;

        // rescale the novel object to cover the required area
        (novelObject.gameObject.scaleX, novelObject.gameObject.scaleY) = RescaleTheNovelObject(novelObject.gameObject.size[0], novelObject.gameObject.size[1], areaWidth, areaHeight);

        Debug.Log("Adding the novel obect");
        scenario.objects.Add(novelObject);
        novelObjectPlaced = true; // used by the level reloading co-routine
    }

    private (float, float) RescaleTheNovelObject(float objectWidth, float objectHeight, float areaWidth, float areaHeight)
    {
        /*
         Considering the scenario, rescale the novel object
         */

        float scaleX;
        float scaleY;
        switch (scenario.scenarioName)
        {
            case "RollingObjectNovel":
                scaleX = (areaWidth - 2) / objectWidth;  // -2 to shrink the horizontal area (to avoid ovarlapping with the objects initial positions - as the feasible range is calculated using the objects' centres)
                scaleY = (areaHeight + 2) / objectHeight;  // +2 to expand the vertical area
                break;
            case "FallingObjectNovel":
                if ((scenario.scenarioIndex == 7) | (scenario.scenarioIndex == 8))
                { // left force (need more y stretching)
                    scaleX = (areaWidth - 1f) / objectWidth;  // -1 is to avoid ovarlapping with the objects as the feasible range is calculated using the objects' centres
                    scaleY = (areaHeight + 2) / objectHeight;
                }
                else
                {
                    scaleX = (areaWidth - 1f) / objectWidth;  // -1 is to avoid ovarlapping with the objects as the feasible range is calculated using the objects' centres
                    scaleY = (areaHeight) / objectHeight;
                }

                break;
            default:
                // throw new Exception("The scenario name is not recognized to rescale the novel object");
                scaleX = (areaWidth - 1f) / objectWidth;
                scaleY = (areaHeight + 0.5f) / objectHeight;
                break;
        }

        return (scaleX, scaleY);
    }

    private (Vector2, Vector2) DetermineNovelObjectFeasibleRange()
    {
        // objects' positions where the novelty should be placed in between
        // non novel object range
        Vector2 NNObject1Position = Vector2.zero;
        Vector2 NNObject2Position = Vector2.zero;

        // novel object range
        Vector2 NObject1Position = Vector2.zero;
        Vector2 NObject2Position = Vector2.zero;

        // analyse the novel constraints in the scenario to determine the region that the novelty should be applied
        foreach (NoveltyGrammar noveltyConstraint in scenario.noveltyConstraints)
        {

            Debug.Log("a: " + noveltyConstraint.a.GetType());
            Debug.Log("b: " + noveltyConstraint.b.GetType());
            Debug.Log("noveltyConstraint: " + noveltyConstraint.GetType());

            //////// Right Force Novelty ////////
            if (noveltyConstraint.GetType() == typeof(NotOnRightForce)) // NotOnRightForce is associated with the non novel path
            {
                if ((noveltyConstraint.a.GetType() == typeof(Roll)) & (noveltyConstraint.b.GetType() == typeof(Fall)))
                {
                    // RollingObjectNovel1 scenario
                    // (RollingObjectNovel) roll -> fall interaction - the novelty should be in between the rolling object and the falling
                    NNObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                    NNObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                }
                else if ((noveltyConstraint.a.GetType() == typeof(Fall)) & (noveltyConstraint.b.GetType() == typeof(HitDestroy)))
                {
                    // FallingObjectNove1 and FallingObjectNove2 novel scenarios
                    // (FallingObjectNovel) Fall -> HitDestroy interaction - the novelty should be in between the falling object and the obejct that it falls onto
                    NNObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                    NNObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                }
                else if (noveltyConstraint.a.GetType() == typeof(Slide) & (noveltyConstraint.b.GetType() == typeof(Fall)))
                {
                    // SlidingObjectNovel1 novel scenarios
                    // (SlidingObjectNovel) Slide -> Fall interaction - the novelty should be in between the sliding object and the obejct that it falls onto
                    NNObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                    NNObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                }
                else if ((noveltyConstraint.a.GetType() == typeof(Roll)) & (noveltyConstraint.b.GetType() == typeof(Hit)))
                {
                    // RollingFallingObjectNovel scenario
                    // (RollingFallingObjectNovel) roll -> hit interaction - the novelty should be in between the rolling object and the hitting with the fallable object
                    NNObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                    NNObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                }
            }
            else if (noveltyConstraint.GetType() == typeof(OnlyOnRightForce)) // OnlyOnRightForce is associated with the novel path
            {
                if ((noveltyConstraint.a.GetType() == typeof(Roll)) & (noveltyConstraint.b.GetType() == typeof(HitDestroy)))
                {
                    // RollingObjectNovel1 scenario
                    // (RollingObjectNovel1 scenario) rolling -> hitting interaction - the novelty should be in between the rolling object and the obejct that is being hit
                    NObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                    NObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);

                }
                else if ((noveltyConstraint.a.GetType() == typeof(Fall)) & (noveltyConstraint.b.GetType() == typeof(HitDestroy)))
                {
                    // FallingObjectNove1 and FallingObjectNove2 novel scenarios
                    // (FallingObjectNovel) Fall -> HitDestroy interaction - the novelty should be in between the falling object and the obejct that it falls onto
                    NObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                    NObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                }
                else if ((noveltyConstraint.a.GetType() == typeof(Slide)) & (noveltyConstraint.b.GetType() == typeof(HitDestroy)))
                {
                    // SlidingObjectNovel1 novel scenarios
                    // (SlidingObjectNovel) Slide -> HitDestroy interaction - the novelty should be in between the sliding object and the obejct that it hits
                    NObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                    NObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);

                }
                else if ((noveltyConstraint.a.GetType() == typeof(Roll)) & (noveltyConstraint.b.GetType() == typeof(Hit)))
                {
                    // RollingFallingObjectNovel scenario
                    // (RollingFallingObjectNovel) roll -> hit interaction - the novelty should be in between the rolling object and the hitting with the fallable object
                    NObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                    NObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                }
                rightForceNovelty = true;
            }

            //////// Down Force Novelty ////////
            if (noveltyConstraint.GetType() == typeof(NotOnDownForce)) // NotOnDownForce is associated with the non novel path
            {
                if ((noveltyConstraint.a.GetType() == typeof(Roll)) & (noveltyConstraint.b.GetType() == typeof(HitDestroy)))
                {
                    // (RollingObjectNovel) roll -> fall interaction - the novelty should be in between the rolling object and the falling

                    NNObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                    NNObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                }
                else if ((noveltyConstraint.a.GetType() == typeof(Fall)) & (noveltyConstraint.b.GetType() == typeof(HitDestroy)))
                {
                    // FallingObjectNovel3 and FallingObjectNove4 and RollingFallingObjectNovel3  scenarios
                    // (FallingObjectNovel) Fall -> HitDestroy interaction - the novelty should be in between the falling object and the obejct that it falls onto
                    NNObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                    NNObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                }
                else if ((noveltyConstraint.a.GetType() == typeof(Slide)) & (noveltyConstraint.b.GetType() == typeof(HitDestroy)))
                {
                    // SlidingObjectNovel2 novel scenarios
                    // (SlidingObjectNovel) Slide -> HitDestroy interaction - the novelty should be in between the sliding object and the obejct that it hits
                    NNObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                    NNObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                }
                else if ((noveltyConstraint.a.GetType() == typeof(Roll)) & (noveltyConstraint.b.GetType() == typeof(Hit)))
                {
                    // RollingFallingObjectNovel scenario
                    // (RollingFallingObjectNovel) roll -> hit interaction - the novelty should be in between the rolling object and the hitting with the fallable object
                    NNObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                    NNObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                }
            }
            else if (noveltyConstraint.GetType() == typeof(OnlyOnDownForce)) // OnlyOnRightForce is associated with the novel path
            {
                if ((noveltyConstraint.a.GetType() == typeof(Roll)) & (noveltyConstraint.b.GetType() == typeof(Fall)))
                {
                    // (RollingObjectNovel) rolling -> hitting interaction - the novelty should be in between the rolling object and the obejct that is being hit
                    NObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                    NObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                }
                else if ((noveltyConstraint.a.GetType() == typeof(Fall)) & (noveltyConstraint.b.GetType() == typeof(HitDestroy)))
                {
                    // FallingObjectNove3 and FallingObjectNove4 and RollingFallingObjectNovel3 scenarios
                    // (FallingObjectNovel)Fall->HitDestroy interaction - the novelty should be in between the falling object and the obejct that it falls onto
                    NObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                    NObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                }
                else if ((noveltyConstraint.a.GetType() == typeof(Slide)) & (noveltyConstraint.b.GetType() == typeof(Fall)))
                {
                    // SlidingObjectNovel2 novel scenarios
                    // (SlidingObjectNovel) Slide -> Fall interaction - the novelty should be in between the sliding object and the obejct that it falls onto
                    NObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                    NObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                }
                else if ((noveltyConstraint.a.GetType() == typeof(Roll)) & (noveltyConstraint.b.GetType() == typeof(Hit)))
                {
                    // RollingFallingObjectNovel scenario
                    // (RollingFallingObjectNovel) roll -> hit interaction - the novelty should be in between the rolling object and the hitting with the fallable object
                    NObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                    NObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                }
                downForceNovelty = true;
            }

            //////// Up Force Novelty ////////
            if (noveltyConstraint.GetType() == typeof(NotOnUpForce)) // NotOnUpForce is associated with the non novel path
            {
                //if (noveltyConstraint.a.GetType() == typeof(Roll))
                //{
                //    if (noveltyConstraint.b.GetType() == typeof(Fall)) // (RollingObjectNovel) roll -> fall interaction - the novelty should be in between the rolling object and the falling
                //    {
                //        NNObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                //        NNObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                //    }
                //}
                //else
                if ((noveltyConstraint.a.GetType() == typeof(Fall)) & (noveltyConstraint.b.GetType() == typeof(HitDestroy)))
                {
                    // FallingObjectNove5 and FallingObjectNove6 and RollingFallingObjectNovel5 scenarios
                    // (FallingObjectNovel) Fall -> HitDestroy interaction - the novelty should be in between the falling object and the obejct that it falls onto
                    NNObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                    NNObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                }
            }
            else if (noveltyConstraint.GetType() == typeof(OnlyOnUpForce)) // OnlyOnUpForce is associated with the novel path
            {
                //if (noveltyConstraint.a.GetType() == typeof(Roll))
                //{
                //    if (noveltyConstraint.b.GetType() == typeof(HitDestroy)) // (RollingObjectNovel) rolling -> hitting interaction - the novelty should be in between the rolling object and the obejct that is being hit
                //    {
                //        NObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                //        NObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                //    }
                //}
                //else 
                if ((noveltyConstraint.a.GetType() == typeof(Fall)) & (noveltyConstraint.b.GetType() == typeof(HitDestroy)))
                {
                    // FallingObjectNove5 and FallingObjectNove6 and RollingFallingObjectNovel5 scenarios
                    // (FallingObjectNovel)Fall->HitDestroy interaction - the novelty should be in between the falling object and the obejct that it falls onto
                    NObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                    NObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                }
                upForceNovelty = true;
            }

            //////// Left Force Novelty ////////
            if (noveltyConstraint.GetType() == typeof(NotOnLeftForce)) // NotOnLeftForce is associated with the non novel path
            {
                //if (noveltyConstraint.a.GetType() == typeof(Roll))
                //{
                //    if (noveltyConstraint.b.GetType() == typeof(Fall)) // (RollingObjectNovel) roll -> fall interaction - the novelty should be in between the rolling object and the falling
                //    {
                //        NNObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                //        NNObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                //    }
                //}
                //else
                if ((noveltyConstraint.a.GetType() == typeof(Fall)) & (noveltyConstraint.b.GetType() == typeof(HitDestroy)))
                {
                    // FallingObjectNove7 and FallingObjectNove8 and RollingFallingObjectNovel4 scenarios
                    // (FallingObjectNovel) Fall -> HitDestroy interaction - the novelty should be in between the falling object and the obejct that it falls onto
                    NNObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                    NNObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                }

            }
            else if (noveltyConstraint.GetType() == typeof(OnlyOnLeftForce)) // OnlyOnLeftForce is associated with the novel path
            {
                //if (noveltyConstraint.a.GetType() == typeof(Roll))
                //{
                //    if (noveltyConstraint.b.GetType() == typeof(HitDestroy)) // (RollingObjectNovel) rolling -> hitting interaction - the novelty should be in between the rolling object and the obejct that is being hit
                //    {
                //        NObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                //        NObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                //    }
                //}
                //else 
                if ((noveltyConstraint.a.GetType() == typeof(Fall)) & (noveltyConstraint.b.GetType() == typeof(HitDestroy)))
                {
                    // FallingObjectNove7 and FallingObjectNove8 and RollingFallingObjectNovel4 scenarios
                    // (FallingObjectNovel)Fall->HitDestroy interaction - the novelty should be in between the falling object and the obejct that it falls onto
                    NObject1Position = new Vector2(noveltyConstraint.a.a.gameObject.positionX, noveltyConstraint.a.a.gameObject.positionY);
                    NObject2Position = new Vector2(noveltyConstraint.b.b.gameObject.positionX, noveltyConstraint.b.b.gameObject.positionY);
                }
                
                leftForceNovelty = true;
            }
        }

        Debug.Log("Range of the object: " + NNObject1Position + NNObject2Position + NObject1Position + NObject2Position);

        // calculate the possible rough bounding box for placing the novelty (only considering the centre points of the objects - not adjusted for their size)
        // x values - assumes that the object1 is in left of the object2
        float minX = Mathf.Max(NNObject1Position.x, NObject1Position.x);
        float maxX = Mathf.Min(NNObject2Position.x, NObject2Position.x);
        // y values - consider the lowest and highest values of the 4 objects - change if needed
        float minY = Mathf.Min(NNObject1Position.y, NNObject2Position.y, NObject1Position.y, NObject2Position.y);
        float maxY = Mathf.Max(NNObject1Position.y, NNObject2Position.y, NObject1Position.y, NObject2Position.y);

        (Vector2, Vector2) boundingBox = (new Vector2(minX, minY), new Vector2(maxX, maxY));
        return boundingBox;
    }

}
