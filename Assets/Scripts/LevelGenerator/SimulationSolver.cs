using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;

public class SimulationSolver : MonoBehaviour
{
    /*
         This class is used to solve the layout terms that require physical simulations (eg: liesOnPath)
    */

    public Scenario scenario;
    public Utilities utilities;
    public ConstraintSolver constraintSolver;
    private static SimulationSolver _instance;

    private float speedUpFactor = 5; // speedup the simulation by this factor

    private bool liesOnPathSolverCompleted = false;
    private bool gameWorldReloadedAfterLiesOnPathSolved = false;
    private bool allConstraintTermsSolved = false;
    private bool loopSolutionExecution = false;

    private bool pathObstructedSolverCompleted = false;
    private bool novelTemplateBuilderCompleted = false;
    private bool gameWorldReloadedAfterPathObstructedSolved = false;
    private bool levelReloadingStarted = false;
    private bool curretlyASolutionIsPlaying = false;
    private bool completedSolutionReplayFunction = false;
    private bool levelFileWriteCompleted = false;
    private bool nonNovelSolutionPlayed = false;

    // bools for reloading the game level -  for ReloadGameWorld()
    private bool levelReloadingEnded = false;


    AsyncOperation levelReloadingCompleted;
    public int nonNovelSolutionShotIndex = -1;
    public int novelSolutionShotIndex = -1;
    public List<(Vector2, string)> birdsTargetPoints;
    Object[] birdsTargetObjects;

    public static SimulationSolver Instance { get { return _instance; } }

    private void Awake()
    {
        // avoid destroying this object even after moving to another scene
        DontDestroyOnLoad(this.gameObject);

        // set this object to the _instance in order to access this monobehaviour from other objects (e.g. LayoutGenerator)
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    void Start()
    {
        // wait until the gameWorld (scene build index 4) is loaded before doing anything
        if (SceneManager.GetActiveScene().buildIndex != 4)
        {
            StartCoroutine(WaitForSceneLoad(4));
        }
    }

    IEnumerator WaitForSceneLoad(int sceneNumber)
    {
        while (SceneManager.GetActiveScene().buildIndex != sceneNumber)
        {
            yield return null;
        }

        // okay, gameworld loaded let's start the job
        if (SceneManager.GetActiveScene().buildIndex == sceneNumber)
        {
            Debug.Log("Gameworld is loaded, simulation solver starting its work...");
            SimulationSolverSchema();
        }
    }

    public void SimulationSolverSchema()
    {

        // throw new Exception("Reached upto the SimulationSolverSchema");

        // first solve the liesOnPath terms
        StartCoroutine(SolveLiesOnPath());

        // throw new Exception("SimulationSolverSchema: Ended SolveLiesOnPath");

        // redo the simulation after adjusting the gameobjects
        StartCoroutine(ReloadGameWorldAfterSolvingLiesOnPath());

        StartCoroutine(ReplayNonNovelSlolution()); // start from here as ArgumentNullException: Value cannot be null error comes

        // build the novel template from the non novel template
        StartCoroutine(BuildNovelTemplate());

        // solve the pathObstructed Terms
        StartCoroutine(SolvePathObstructedAndAddDistractions());

        // redo the simulation after adding the platform walls
        StartCoroutine(ReloadGameWorldAfterSolvingPathObstructed());

        // replay the solution
        StartCoroutine(ReplayNovelSlolution());

        // write the scenario into a level file and write the scenario info file
        StartCoroutine(WriteLevelAndLevelInfo());
    }

    IEnumerator ReloadGameWorld()
    {
        // wait till the liesOnpath solver is done
        while (levelReloadingStarted)
        {
            Debug.Log("Level reloading is happening, waiting until it is done!");
            yield return null;
        }

        if (!levelReloadingStarted)
        {
            Debug.Log("reloading the gameworld");

            ABLevel gameLevel = utilities.GetGameLevel(scenario);
            ABGameWorld.currentLevel = gameLevel;
            //ABSceneManager.Instance.LoadScene("GameWorld");
            levelReloadingStarted = true;

            levelReloadingCompleted = SceneManager.LoadSceneAsync("GameWorld");
        }

        while (!levelReloadingCompleted.isDone)
        {
            Debug.Log("still reloading the gameworld");
            yield return null;
        }

        Debug.Log("reloading the gameworld after solving path obstructed completed!");
        ImmortalizeBlocksInMainScene();
        levelReloadingStarted = false;
        // levelReloadingEnded = true;
    }

    IEnumerator WriteLevelAndLevelInfo()
    {
        /*
         This function writes the novel and non novel level xml file and level info file
         */

        // wait until all the constraint terms are solved
        while (!completedSolutionReplayFunction)
        {
            yield return null;
        }
        Debug.Log("level replaying is done, writting the level files and the level info file");

        // write the non novel level file
        utilities.WriteLevelFileFromScenario(scenario.objects, scenario.scenarioInfo.nonNovelScenarioID, "NonNovel");
        // write the novel level file
        utilities.WriteLevelFileFromScenario(scenario.objects, scenario.scenarioInfo.novelScenarioID, "Novel");

        // write the scenario info file
        utilities.WriteLevelInfoFile(scenario.scenarioInfo);

        Debug.Log("Level and level info files writting completed!");
        levelFileWriteCompleted = true;
    }

    IEnumerator ReplayNonNovelSlolution()
    {
        /*
         This function replays the solution
         */

        // wait until all the constraint terms are solved
        while (!gameWorldReloadedAfterLiesOnPathSolved)
        {
            yield return null;
        }

        Debug.Log("allConstraintTermsSolved, no solution is curretly replaying, level reloadings are done, so starting to replay the solution");
        //curretlyASolutionIsPlaying = true;

        // speedup the simulation
        Time.timeScale = speedUpFactor;

        Debug.Log("Solution shot index: " + nonNovelSolutionShotIndex + " birdsTargetPoints: " + string.Join(" ", birdsTargetPoints));

        // get the bird (assumption: only one bird is in the game level)
        GameObject bird = GameObject.FindGameObjectsWithTag("Bird")[0];
        ShootBird(bird, nonNovelSolutionShotIndex, "NonNovel");

        yield return new WaitForSeconds(20); // wait x seconds until the shot effects are finished

        // save whether the task was solved by executing the intended solution TODO: adjust the above x or speedup the simulation as sometimes x seconds is not enough for the pig to get killed
        scenario.scenarioInfo.nonNovelSolvability = ABGameWorld.Instance.getLevelClearedStatus();
        Debug.Log("solvability " + scenario.scenarioInfo.nonNovelSolvability);

        // bring back the simulation to the original speed
        Time.timeScale = 1;

        //curretlyASolutionIsPlaying = false;
        nonNovelSolutionPlayed = true;
        Debug.Log("End of the replay solution, after x seconds! ");

        // reload the level and replay the solution - uncomment below to replay the solution infinitely
        // StartCoroutine(ReloadGameWorld());
        // StartCoroutine(ReplaySolution());

    }

    IEnumerator ReplayNovelSlolution()
    {
        /*
         This function replays the solution
         */

        // wait until all the constraint terms are solved
        while (!allConstraintTermsSolved | (curretlyASolutionIsPlaying | levelReloadingStarted))
        {
            yield return null;
        }

        Debug.Log("allConstraintTermsSolved, no solution is curretly replaying, level reloadings are done, so starting to replay the solution");
        curretlyASolutionIsPlaying = true;

        // speedup the simulation
        Time.timeScale = speedUpFactor;

        Debug.Log("Solution shot index: " + novelSolutionShotIndex + " birdsTargetPoints: " + string.Join(" ", birdsTargetPoints));

        // get the bird (assumption: only one bird is in the game level)
        GameObject bird = GameObject.FindGameObjectsWithTag("Bird")[0];
        ShootBird(bird, novelSolutionShotIndex, "Novel");

        yield return new WaitForSeconds(20); // wait x seconds until the shot effects are finished

        // save whether the task was solved by executing the intended solution TODO: adjust the above x or speedup the simulation as sometimes x seconds is not enough for the pig to get killed
        scenario.scenarioInfo.novelSolvability = ABGameWorld.Instance.getLevelClearedStatus();
        Debug.Log("solvability " + scenario.scenarioInfo.novelSolvability);

        // bring back the simulation to the original speed
        Time.timeScale = 1;

        curretlyASolutionIsPlaying = false;
        Debug.Log("End of the replay solution, after x seconds! ");
        completedSolutionReplayFunction = true;

        // reload the level and replay the solution - uncomment below to replay the solution infinitely
        // StartCoroutine(ReloadGameWorld());
        // StartCoroutine(ReplaySolution());

    }

    IEnumerator SolveLiesOnPath()
    {
        /*
         This function calls the LiesOnPathSolver and solves the liesOnPathTerms
         */
        GameObject liesOnPathSolverGameObject = new GameObject();
        LiesOnPathSolver liesOnPathSolver = liesOnPathSolverGameObject.AddComponent<LiesOnPathSolver>();
        liesOnPathSolver.scenario = scenario;
        liesOnPathSolver.utilities = utilities;
        liesOnPathSolver.constraintSolver = constraintSolver;
        liesOnPathSolver.SolveLiesOnPathTerms();

        while (liesOnPathSolver.liesOnPathSolverCompleted != true)
        {
            yield return null;
        }

        Debug.Log("liesOnPath simulation completed!");
        // get the solution shot index from the liesOnPathSolver
        nonNovelSolutionShotIndex = liesOnPathSolver.solutionShotIndex;
        // get the birds birdsTargetObject and target points list from the liesOnPathSolver
        birdsTargetPoints = liesOnPathSolver.birdsTargetPoints;
        birdsTargetObjects = liesOnPathSolver.birdsTargetObjects;

        // yield return new WaitForSeconds(4); // wait x seconds until the shot effects are finished

        liesOnPathSolverCompleted = true;
    }

    IEnumerator SolvePathObstructedAndAddDistractions()
    {
        /*
        This function calls the PathObstructedSolver and solves the pathObstructed terms
        */

        while (!novelTemplateBuilderCompleted)
        {
            yield return null;
        }

        Debug.Log("starting PathObstructedSolver");
        GameObject pathObstructedSolverGameObject = new GameObject();
        PathObstructedSolverAndAddDistractions pathObstructedSolver = pathObstructedSolverGameObject.AddComponent<PathObstructedSolverAndAddDistractions>();
        pathObstructedSolver.scenario = scenario;
        pathObstructedSolver.utilities = utilities;

        pathObstructedSolver.SolvePathObstructedTermsAndAddDistractions();

        while (pathObstructedSolver.pathObstructedSolverCompleted != true)
        {
            // Debug.Log("still solving the pathObstructed terms");
            yield return null;
        }
        Debug.Log("pathObstructed solver completed!");
        // this is the place which considered as the end of the generation process, record the timestamp at this point
        scenario.scenarioInfo.generationEndTimeTicks = System.DateTime.UtcNow.Ticks;

        pathObstructedSolverCompleted = true;
        // this is the end of solving all the constraint terms
        allConstraintTermsSolved = true;
    }

    IEnumerator BuildNovelTemplate()
    {
        /*
        This function bulilds the novel template from the non novel template
        */

        while (!nonNovelSolutionPlayed)
        {
            yield return null;
        }

        Debug.Log("starting NovelTemplateBuilder");
        GameObject novelTemplateBuilderGameObject = new GameObject();
        NovelTemplateBuilder novelTemplateBuilder = novelTemplateBuilderGameObject.AddComponent<NovelTemplateBuilder>();
        novelTemplateBuilder.scenario = scenario;
        novelTemplateBuilder.utilities = utilities;

        novelTemplateBuilder.StartBuildingNovelTemplate();

        while (novelTemplateBuilder.novelTemplateBuilderCompleted != true)
        {
            // Debug.Log("still building the novel template");
            yield return null;
        }
        Debug.Log("novelTemplateBuilderCompleted solver completed!");

        novelSolutionShotIndex = novelTemplateBuilder.novelSolutionShotIndex + birdsTargetPoints.Count / 2; // birdsTargetPoints.Count/2 is added as the novel target point is accessed from the middle of the list (first half for non novel and second half for novel)
        novelTemplateBuilderCompleted = true;

    }

    IEnumerator ExecuteTheSolution()
    {
        /*
        This function executes the solution shot 
        */

        while (!gameWorldReloadedAfterLiesOnPathSolved)
        {
            yield return null;
        }

        Physics.autoSimulation = false;

        Debug.Log("inside ExecuteTheSolution");
        Debug.Log("Solution shot index: " + novelSolutionShotIndex + " birdsTargetPoints: " + string.Join(" ", birdsTargetPoints));

        // get the bird (assumption: only one bird is in the game level)
        GameObject bird = GameObject.FindGameObjectsWithTag("Bird")[0];
        ShootBird(bird, nonNovelSolutionShotIndex, "NonNovel");

    }

    IEnumerator ReloadGameWorldAfterSolvingLiesOnPath()
    {
        // wait till the liesOnpath solver is done
        while (liesOnPathSolverCompleted != true)
        {
            yield return null;
        }

        if (!levelReloadingStarted)
        {
            Debug.Log("reloading the gameworld");

            ABLevel gameLevel = utilities.GetGameLevel(scenario);
            ABGameWorld.currentLevel = gameLevel;
            //ABSceneManager.Instance.LoadScene("GameWorld");
            levelReloadingStarted = true;

            levelReloadingCompleted = SceneManager.LoadSceneAsync("GameWorld");
        }

        while (!levelReloadingCompleted.isDone)
        {
            Debug.Log("still reloading the gameworld");
            yield return null;
        }

        Debug.Log("reloading the gameworld completed!");
        ImmortalizeBlocksInMainScene();
        gameWorldReloadedAfterLiesOnPathSolved = true;
        levelReloadingStarted = false;
        // Debug.Log("immortalizing the objects completed!");
    }

    IEnumerator ReloadGameWorldAfterSolvingPathObstructed()
    {
        // wait till the liesOnpath solver is done
        while (pathObstructedSolverCompleted != true)
        {
            yield return null;
        }

        Debug.Log("pathObstructedSolver is completed, reloading the game level");

        if (!levelReloadingStarted)
        {
            Debug.Log("reloading the gameworld");

            ABLevel gameLevel = utilities.GetGameLevel(scenario);
            ABGameWorld.currentLevel = gameLevel;
            //ABSceneManager.Instance.LoadScene("GameWorld");
            levelReloadingStarted = true;

            levelReloadingCompleted = SceneManager.LoadSceneAsync("GameWorld");
        }

        while (!levelReloadingCompleted.isDone)
        {
            Debug.Log("still reloading the gameworld");
            yield return null;
        }

        Debug.Log("reloading the gameworld after solving path obstructed completed!");
        ImmortalizeBlocksInMainScene();
        gameWorldReloadedAfterPathObstructedSolved = true;
        levelReloadingStarted = false;
    }

    public void ImmortalizeBlocksInMainScene()
    {
        //if (TryGetComponent<ABBlock>(out ABBlock blockScript)) // blocks
        //{
        //    blockScript._life = 10000;
        //    Debug.Log("Life set to the object: " + gameObject.name + " value: " + blockScript._life);
        //}

        ABBlock[] allGameObjects = UnityEngine.Object.FindObjectsOfType<ABBlock>();
        foreach (ABBlock block in allGameObjects)
        {
            block.GetComponent<ABBlock>().setCurrentLife(100000);
            Debug.Log("found block: " + block.gameObject.name + " set health points to 100000");
        }
    }

    private void ShootBird(GameObject bird, int solutionShotIndex, string pathName)
    {
        /*
            The bird is shot by giving it the velocity 
        */
        Object birdsTargetObject = (pathName == "NonNovel") ? birdsTargetObjects[0] : birdsTargetObjects[1]; // first target object is non novel and second is novel

        (Vector2, string) targetPoint = birdsTargetPoints[solutionShotIndex];

        Debug.Log("target position: " + targetPoint);
        Debug.Log("bird position: " + bird.transform.position);

        float g = -9.81f * 0.48f;  // environment gravity multiplied by the birds launch gravity
        float x = targetPoint.Item1.x - bird.transform.position.x; // target point x relative to the bird position
        float y = targetPoint.Item1.y - bird.transform.position.y; // target point y relative to the bird position

        float v = 9.65f;
        float v2 = v * v;
        float v4 = v2 * v2;
        float x2 = x * x;

        // using higher trajectory/lower trajectory depends on the info in the targetPoint
        float theta;
        if (targetPoint.Item2 == "L")
        { // L for heigher trajectory
            theta = Mathf.Atan2(v2 - Mathf.Sqrt(v4 - g * (g * x2 - 2 * y * v2)), g * x);
        }
        else
        { // H for lower trajectory
            theta = Mathf.Atan2(v2 + Mathf.Sqrt(v4 - g * (g * x2 - 2 * y * v2)), g * x);
        }
        // adjust the angle as the results from the above equation are mirrored angles
        theta = (float)Math.PI - theta;
        Debug.Log("theta found: " + theta + "x velocity: " + v * Mathf.Cos(theta) + "y velocity: " + v * Mathf.Sin(theta));

        // save info about the solution to write into the file =============
        // target object info
        ScenarioObject scenarioTargetObject = new ScenarioObject();
        scenarioTargetObject.scenarioObjectName = birdsTargetObject.GetType().ToString();
        scenarioTargetObject.position = new Coordinate();
        scenarioTargetObject.position.x = birdsTargetObject.positionX;
        scenarioTargetObject.position.y = birdsTargetObject.positionY;
        // target point info
        Coordinate scenarioTargetPoint = new Coordinate();
        scenarioTargetPoint.x = targetPoint.Item1.x;
        scenarioTargetPoint.y = targetPoint.Item1.y;


        if (pathName == "NonNovel")
        {
            scenario.scenarioInfo.nonNovelSolution = new NonNovelSolution();
            scenario.scenarioInfo.nonNovelSolution.targetObject = scenarioTargetObject;
            scenario.scenarioInfo.nonNovelSolution.targetPoint = scenarioTargetPoint;
            scenario.scenarioInfo.nonNovelSolution.trajectory = targetPoint.Item2;
            scenario.scenarioInfo.nonNovelSolution.releaseAngle = theta;
            scenario.scenarioInfo.nonNovelSolution.shotIndex = solutionShotIndex;
        }
        else if (pathName == "Novel")
        {
            scenario.scenarioInfo.novelSolution = new NovelSolution();
            scenario.scenarioInfo.novelSolution.targetObject = scenarioTargetObject;
            scenario.scenarioInfo.novelSolution.targetPoint = scenarioTargetPoint;
            scenario.scenarioInfo.novelSolution.trajectory = targetPoint.Item2;
            scenario.scenarioInfo.novelSolution.releaseAngle = theta;
            scenario.scenarioInfo.novelSolution.shotIndex = solutionShotIndex;
        }


        // The bird starts with no gravity, so we must set it
        bird.GetComponent<Rigidbody2D>().velocity = new Vector2(v * Mathf.Cos(theta), v * Mathf.Sin(theta));
        bird.GetComponent<Rigidbody2D>().gravityScale = bird.GetComponent<ABBird>()._launchGravity;

        // set the layer back to birds to enable collisions
        gameObject.layer = 8;
    }


}
