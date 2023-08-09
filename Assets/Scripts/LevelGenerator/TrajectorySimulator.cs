using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System;
using UnityEngine.SocialPlatforms;

public class TrajectorySimulator : MonoBehaviour
{

    /*
        This is the bird shooting simulator for the liesOnPathSolver 
    */

    private Scene parallelScene;
    private PhysicsScene2D parallelPhysicsScene;

    public Vector2 initalVelocity = new Vector2(5f, 0f);
    private int numberOfSimulationSteps = 3000; // original value: 3000
    private int shotsSimulated = 0; // used to determine which (higher/lower) trajectory to use
    private float speedUpFactor = 5f; // speedup the simulation by this factor, by setting the Time.timeScale to this value

    // variables (Objects) that will be set from the SimulationSolver before simulating the trajectories
    public Object[] birdsTargetObjects;
    public List<(Vector2, string)> birdsTargetPoints; // to save the target points of the bird ((x,y coordinate),("H"/"L" trajectory))
    private List<Object> allObjectsToSimulate;
    private HashSet<Object> pathObservingObjects;

    // variables (GameObjects) that will be inferred from the above variables (Objects)
    GameObject birdsTargetGameObject; // GameObject in the ABGameWorldScene
    List<GameObject> allGameObjectsToSimulate; // GameObjects in the ABGameWorldScene

    List<GameObject> pathObservingGameObjectsInParalleScene; // path observing GameObjects in this parallelScene
    List<GameObject> gameObjectsInParallelScene; // all GameObjects in this parallelScene

    public List<IDictionary<String, List<Vector2>>> nonNovelSimulationResults; // to save the paths of the pathObservingGameObjectsInParalleScene for the non-novel path shot simulations
    public List<IDictionary<String, List<Vector2>>> novelSimulationResults; // to save the paths of the pathObservingGameObjectsInParalleScene for the novel path shot simulations

    public bool startTrajectorySimulation = false;
    public bool trajectorySimulationEnded = false;

    private bool mainPhysics = true;
    private static TrajectorySimulator _instance;
    private float simulationTime = 30f;
    int trajPathX = 0;

    public static TrajectorySimulator Instance { get { return _instance; } }
    private void Awake()
    {
        // avoid destroying this object even after moving to another scene
        DontDestroyOnLoad(this.gameObject);

        // set this object to the _instance in order to access this monobehaviour from other objects (e.g. SimulationSolver)
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }

        nonNovelSimulationResults = new List<IDictionary<String, List<Vector2>>>();
        novelSimulationResults = new List<IDictionary<String, List<Vector2>>>();
    }

    void Start()
    {

        // wait until the gameWorld (scene build index 4) is loaded before doing anything
        if (SceneManager.GetActiveScene().buildIndex != 4)
        {
            StartCoroutine(WaitForSceneLoad(4));
        }

    }
    void Update()
    {
        if (startTrajectorySimulation)
        {
            Debug.Log("start traj simulation");
            Time.timeScale = speedUpFactor; // increase time scale to speedup the simulation -- start from here


            mainPhysics = false;
            startTrajectorySimulation = false;

            StartCoroutine(SimulateBirdShooting());
            // PrintSimulationResults(simulationResults);


        }

        //if (trajectorySimulationEnded)
        //{
        //    Debug.Log("end traj simulation");
        //    mainPhysics = true;
        //    Shoot();
        //    // parallelPhysicsScene.Simulate(Time.fixedDeltaTime);
        //    trajectorySimulationEnded = false;
        //}
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
            Debug.Log("Gameworld is loaded, trajectory simulator starting its work...");

            Physics.autoSimulation = false;
            CreateSceneParameters createSceneParameters = new CreateSceneParameters(LocalPhysicsMode.Physics2D);
            parallelScene = SceneManager.CreateScene("ParallelScene", createSceneParameters);
            parallelPhysicsScene = parallelScene.GetPhysicsScene2D();

            Debug.Log("Parallel scene created!");
        }
    }

    void FixedUpdate()
    {
        if (mainPhysics)
        {
            //Debug.Log(SceneManager.GetActiveScene().buildIndex);
            // SceneManager.GetActiveScene().GetPhysicsScene().Simulate(Time.fixedDeltaTime); // commented on 4/04/2023 change back if needed
        }
    }

    public void SetParameters(List<Object> allObjectsToSimulate, Object[] targetObejcts, HashSet<Object> pathObservingObjects)
    {
        /*
         This function is called to set the parameters of the TrajectorySimlator
         */
        this.allObjectsToSimulate = allObjectsToSimulate;
        this.birdsTargetObjects = targetObejcts;
        this.pathObservingObjects = pathObservingObjects;

        // find the instantiated gameObjects in the GameWorld Scene from these Objects
        allGameObjectsToSimulate = new List<GameObject>();
        // pathObservingGameObjects = new List<GameObject>();
        foreach (Object objct in allObjectsToSimulate)
        {
            allGameObjectsToSimulate.Add((GameObject)EditorUtility.InstanceIDToObject(objct.instanceID));
        }
        //foreach (Object objct in pathObservingObjects)
        //{
        //    pathObservingGameObjects.Add((GameObject)EditorUtility.InstanceIDToObject(objct.instanceID));
        //}
        // birdsTargetGameObject = (GameObject)EditorUtility.InstanceIDToObject(birdsTargetObject.instanceID);

        // calculate the target points of the target object
        CalculateTargtePoints(birdsTargetObjects);
    }

    public void prepareTheParallelSimulation(List<Object> allObjectsToSimulate)
    {
        /*
         This function is called to create a parallel scene and refresh the object list that is going to be simulated in the parallel scene.
         It is used in the novelTemplateBuilder when doing the simulation after introducing the novelty.
        */

        // create a parallel scene to simulate the shots
        Debug.Log("Creating a parallel scene for the simulation...");
        Physics.autoSimulation = false;
        CreateSceneParameters createSceneParameters = new CreateSceneParameters(LocalPhysicsMode.Physics2D);
        parallelScene = SceneManager.CreateScene("ParallelScene", createSceneParameters);
        parallelPhysicsScene = parallelScene.GetPhysicsScene2D();
        Debug.Log("Parallel scene created!");

        // reset the shotsSimulated count and the result storing lists
        shotsSimulated = 0;
        nonNovelSimulationResults = new List<IDictionary<String, List<Vector2>>>();
        novelSimulationResults = new List<IDictionary<String, List<Vector2>>>();

        // find the instantiated gameObjects in the GameWorld Scene from these Objects
        allGameObjectsToSimulate = new List<GameObject>();
        // pathObservingGameObjects = new List<GameObject>();
        foreach (Object objct in allObjectsToSimulate)
        {
            allGameObjectsToSimulate.Add((GameObject)EditorUtility.InstanceIDToObject(objct.instanceID));
        }

    }

    private void CalculateTargtePoints(Object[] targetObject)
    {
        /*
         Given a target objects this function calculates the target points. NN object is targetObject[0] and N obejct is targetObject[1]
         currently only 5 points of the target object are considered (lower left(LT), centre left(LT), upper left(HT/LT), upper middle(HT), upper right(HT)).
         For these 5 points there are possible 6 shots: LT is lower trajectory and HT is higher trajectory <= Assumption: the target object is above the slingshot
         */

        birdsTargetPoints = new List<(Vector2, string)>();

        // NN Object points
        float[] nNMBRWidthAndHeight = Utilities.GetMBRWidthAndHeight(targetObject[0]);
        float nNObjectCentreX = targetObject[0].positionX;
        float nNObjectCenterY = targetObject[0].positionY;
        float nNObjectHalfWidth = nNMBRWidthAndHeight[0] / 2;
        float nNobjectHalfHeight = nNMBRWidthAndHeight[1] / 2;

        // N Object points
        Debug.Log("N Object: " + targetObject[1].ToString());
        float[] nMBRWidthAndHeight = Utilities.GetMBRWidthAndHeight(targetObject[1]);
        float nObjectCentreX = targetObject[1].positionX;
        float nObjectCenterY = targetObject[1].positionY;
        float nObjectHalfWidth = nMBRWidthAndHeight[0] / 2;
        float nObjectHalfHeight = nMBRWidthAndHeight[1] / 2;

        // NN object points

        // lower left(LT)
        // birdsTargetPoints.Add((new Vector2(objectCentreX - objectHalfWidth, objectCenterY - objectHalfHeight), "L"));

        // centre left(LT)
         birdsTargetPoints.Add((new Vector2(nNObjectCentreX - nNObjectHalfWidth, nNObjectCenterY), "L"));

        // upper left(LT)
       // birdsTargetPoints.Add((new Vector2(nNObjectCentreX - nNObjectHalfWidth, nNObjectCenterY + nNobjectHalfHeight), "L"));

        //// upper left(HT) adding another point for upper left for the higher trajectory
        //birdsTargetPoints.Add((new Vector2(objectCentreX - objectHalfWidth, objectCenterY + objectHalfHeight), "H"));

        //// upper middle(HT)
        //birdsTargetPoints.Add((new Vector2(objectCentreX, objectCenterY + objectHalfHeight), "H"));

        //// upper right(HT)
        //birdsTargetPoints.Add((new Vector2(objectCentreX + objectHalfWidth, objectCenterY + objectHalfHeight), "H"));



        // N object points

        // lower left(LT)
        // birdsTargetPoints.Add((new Vector2(objectCentreX - objectHalfWidth, objectCenterY - objectHalfHeight), "L"));

        // centre left(LT)
        birdsTargetPoints.Add((new Vector2(nObjectCentreX - nObjectHalfWidth, nObjectCenterY), "L"));

        // upper left(LT)
        // birdsTargetPoints.Add((new Vector2(nObjectCentreX - nObjectHalfWidth, nObjectCenterY + nObjectHalfHeight), "L"));

        //// upper left(HT) adding another point for upper left for the higher trajectory
        //birdsTargetPoints.Add((new Vector2(objectCentreX - objectHalfWidth, objectCenterY + objectHalfHeight), "H"));

        //// upper middle(HT)
        //birdsTargetPoints.Add((new Vector2(objectCentreX, objectCenterY + objectHalfHeight), "H"));

        //// upper right(HT)
        //birdsTargetPoints.Add((new Vector2(objectCentreX + objectHalfWidth, objectCenterY + objectHalfHeight), "H"));

    }

    IEnumerator SimulateBirdShooting()
    {
        // SceneManager.SetActiveScene(parallelScene);

        foreach ((Vector2, string) targetPoint in birdsTargetPoints)
        {
            // instantiate the objects in the parallel scene
            InstantiateObjects();

            // add the slingshot velocity and make the gameobjects immortal
            foreach (GameObject gameObject in gameObjectsInParallelScene)
            {
                // Debug.Log("object considered: " + gameObject.name);
                if (gameObject.name.Contains("bird_")) // if a bird add birds velocity
                {
                    GetVelocityOfBirdV2(gameObject, targetPoint);

                }
                else // immortalize the objects (except the pig)
                {

                    // since trygetcomponent didn't work, do it in a dirty way!
                    Component[] components = gameObject.GetComponents(typeof(Component));
                    foreach (Component component in components)
                    {
                        if (component.GetType() == typeof(ABBlock))
                        {
                            ((ABBlock)component).setCurrentLife(100000);
                            Debug.Log("Immortalized the object: " + gameObject.name);
                        }
                    }
                }
            }

            // now simulate the paralle scene numberOfSimulationSteps number of steps
            // ShootAndRecord();

            // dictionary to store the positions of game objects in considered simulation steps
            IDictionary<String, List<Vector2>> objectPositionsInSimulationSteps = new Dictionary<String, List<Vector2>>();
            // initialize the dictionary
            foreach (GameObject gameObject in pathObservingGameObjectsInParalleScene)
            {
                String objectIdentifier = gameObject.GetComponent<OriginObjectData>().gameObjectType.ToString() + " " + gameObject.GetComponent<OriginObjectData>().originGameObjectID;
                try
                {
                    // only consider the pathObservingGameObjects that are required to observe for the currently simulating shot (for non novel path: only non novel obejcts, for novel path only novel objects)
                    if (IsShotAndObjectRelatesToNonNovelPath(gameObject) | (IsShotAndObjectRelatesToNovelPath(gameObject))) // check if the object is needed to be oserved in the current shot
                    {
                        objectPositionsInSimulationSteps.Add(objectIdentifier, new List<Vector2>());
                    }
                }
                catch (Exception e)
                { // when the key is already in the list (eg: in the bouncing scenario where the bird's path is observerd 2 times)
                    Debug.Log(e);
                }
            }

            float elapsedTime = 0f;

            while (elapsedTime < simulationTime)
            {
                parallelPhysicsScene.Simulate(Time.fixedDeltaTime); // 0.02 
                foreach (GameObject gameObject in pathObservingGameObjectsInParalleScene)
                {
                    if (gameObject != null)
                    { // only record data if the game object is present

                        gameObject.GetComponent<LineRenderer>().SetPosition(trajPathX, gameObject.transform.position);

                        // only consider the pathObservingGameObjects that are required to observe for the currently simulating shot (for non novel path: only non novel obejcts, for novel path only novel objects)
                        if (IsShotAndObjectRelatesToNonNovelPath(gameObject) | (IsShotAndObjectRelatesToNovelPath(gameObject))) // check if the object is needed to be oserved in the current shot
                        {
                            String objectIdentifier = gameObject.GetComponent<OriginObjectData>().gameObjectType.ToString() + " " + gameObject.GetComponent<OriginObjectData>().originGameObjectID;
                            objectPositionsInSimulationSteps[objectIdentifier].Add(gameObject.transform.position);
                        }
                    }
                }

                elapsedTime += (Time.fixedDeltaTime * Time.timeScale);
                trajPathX++;

                // Yield to allow other processes to execute
                yield return null;
            }

            // save the recorded data into the corresponding list
            if (shotsSimulated < birdsTargetPoints.Count / 2) // non novel path shot
            {
                Debug.Log("NN shot completed: " + shotsSimulated);
                nonNovelSimulationResults.Add(objectPositionsInSimulationSteps);
            }
            else // novel path shot
            {
                Debug.Log("N shot completed: " + shotsSimulated);
                novelSimulationResults.Add(objectPositionsInSimulationSteps);
            }


            // shooting successfully simulated
            shotsSimulated++;
            trajPathX = 0;

            // stop from here for testing
            // throw new Exception("Exception for testing");

            //// save an screenshot of the simulation
            //ScreenCapture.CaptureScreenshot("screenshot_" + shotsSimulated + ".png", 4);

            // Yield to allow other processes to execute
            yield return null;
        }

        // trajectory simulation is finished
        trajectorySimulationEnded = true;
        Time.timeScale = 1; // reset the time scale to original value

        Debug.Log("end traj simulation");
        mainPhysics = true;

    }

    void ShootAndRecord()
    {
        /*
         when everything is ready in the paralle scene, this funcion simualtes the paralle secene numberOfSimulationSteps number of steps and record the outcome
         */

        // dictionary to store the positions of game objects in considered simulation steps
        IDictionary<String, List<Vector2>> objectPositionsInSimulationSteps = new Dictionary<String, List<Vector2>>();
        // initialize the dictionary
        foreach (GameObject gameObject in pathObservingGameObjectsInParalleScene)
        {
            //Debug.Log()
            String objectIdentifier = gameObject.GetComponent<OriginObjectData>().gameObjectType.ToString() + " " + gameObject.GetComponent<OriginObjectData>().originGameObjectID;
            try
            {
                // only consider the pathObservingGameObjects that are required to observe for the currently simulating shot (for non novel path: only non novel obejcts, for novel path only novel objects)
                if (IsShotAndObjectRelatesToNonNovelPath(gameObject) | (IsShotAndObjectRelatesToNovelPath(gameObject))) // check if the object is needed to be oserved in the current shot
                {
                    objectPositionsInSimulationSteps.Add(objectIdentifier, new List<Vector2>());
                }
            }
            catch (Exception e)
            { // when the key is already in the list (eg: in the bouncing scenario where the bird's path is observerd 2 times)
                Debug.Log(e);
            }
        }

        for (int i = 0; i < numberOfSimulationSteps; i++)
        {
            parallelPhysicsScene.Simulate(Time.fixedDeltaTime); // 0.02 
            foreach (GameObject gameObject in pathObservingGameObjectsInParalleScene)
            {
                gameObject.GetComponent<LineRenderer>().SetPosition(i, gameObject.transform.position);

                // only consider the pathObservingGameObjects that are required to observe for the currently simulating shot (for non novel path: only non novel obejcts, for novel path only novel objects)
                if (IsShotAndObjectRelatesToNonNovelPath(gameObject) | (IsShotAndObjectRelatesToNovelPath(gameObject))) // check if the object is needed to be oserved in the current shot
                {
                    String objectIdentifier = gameObject.GetComponent<OriginObjectData>().gameObjectType.ToString() + " " + gameObject.GetComponent<OriginObjectData>().originGameObjectID;
                    objectPositionsInSimulationSteps[objectIdentifier].Add(gameObject.transform.position);
                }
            }
        }

        // save the recorded data into the corresponding list
        if (shotsSimulated < birdsTargetPoints.Count / 2) // non novel path shot
        {
            Debug.Log("NN shot completed: " + shotsSimulated);
            nonNovelSimulationResults.Add(objectPositionsInSimulationSteps);
        }
        else // novel path shot
        {
            Debug.Log("N shot completed: " + shotsSimulated);
            novelSimulationResults.Add(objectPositionsInSimulationSteps);
        }
        // PrintDictionary(objectPositionsInSimulationSteps);
    }

    private bool IsShotAndObjectRelatesToNonNovelPath(GameObject gameObject)
    {

        // the shot has to be related to the novely path (first half of the shots)
        if (shotsSimulated < birdsTargetPoints.Count / 2) // non novel path shot
        {
            // only consider non novel objects
            if (!Utilities.IsObjectNovel(gameObject.GetComponent<OriginObjectData>().originGameObjectID, allObjectsToSimulate))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsShotAndObjectRelatesToNovelPath(GameObject gameObject)
    {

        // the shot has to be related to the novely path (second half of the shots)
        if (shotsSimulated >= birdsTargetPoints.Count / 2) // novel path shot
        {

            // only consider novel objects
            if (Utilities.IsObjectNovel(gameObject.GetComponent<OriginObjectData>().originGameObjectID, allObjectsToSimulate))
            {
                return true;
            }
        }

        return false;
    }

    private void PrintDictionary(IDictionary<GameObject, List<Vector2>> dictionary)
    {
        foreach (KeyValuePair<GameObject, List<Vector2>> kvp in dictionary)
        {
            Debug.Log("Key = " + kvp.Key + " Value = " + string.Join(", ", kvp.Value));
        }

    }
    private void PrintSimulationResults(List<IDictionary<String, List<Vector2>>> simulationResults)
    {
        int shotsPrinted = 0;
        foreach (IDictionary<String, List<Vector2>> shotData in simulationResults)
        {
            Debug.Log("Printing the shot: " + (shotsPrinted + 1));
            foreach (KeyValuePair<String, List<Vector2>> kvp in shotData)
            {
                Debug.Log("Key = " + kvp.Key + " Value = " + string.Join(", ", kvp.Value));
            }
            shotsPrinted++;
        }

    }

    private void GetVelocityOfBirdV2(GameObject bird, (Vector2, string) targetPoint)
    {
        /*
         input (Vector2, string) targetPoint has the x,y target point and the "H" and "L": high/low trajectory as the string
         */
        Debug.Log("target position: " + targetPoint);
        Debug.Log("bird position: " + bird.transform.position);

        float g = -9.81f * 0.48f;  // environment gravity multiplied by the birds launch gravity
        float x = targetPoint.Item1.x - bird.transform.position.x; // target point x relative to the bird position
        float y = targetPoint.Item1.y - bird.transform.position.y; // target point y relative to the bird position

        float v = 9.65f;
        float v2 = v * v;
        float v4 = v2 * v2;
        float x2 = x * x;

        // using higher trajectory/lower trajectory depends trajectory info in the targetPoint
        float theta;
        if (targetPoint.Item2 == "L")
        {
            theta = Mathf.Atan2(v2 - Mathf.Sqrt(v4 - g * (g * x2 - 2 * y * v2)), g * x);
        }
        else
        { // targetPoint.Item2 == "H"
            theta = Mathf.Atan2(v2 + Mathf.Sqrt(v4 - g * (g * x2 - 2 * y * v2)), g * x);
        }
        // adjust the angle as the results from the above equation are mirrored angles
        theta = (float)Math.PI - theta;

        // The bird starts with no gravity, so we must set it
        Debug.Log("theta found: " + theta + "x velocity: " + v * Mathf.Cos(theta) + "y velocity: " + v * Mathf.Sin(theta));

        if (Double.IsNaN(theta))
        { // theta NaN means object is unreacheable
            theta = 0;
        }

        bird.GetComponent<Rigidbody2D>().velocity = new Vector2(v * Mathf.Cos(theta), v * Mathf.Sin(theta)); // this might throw an exception if the values are NaN (happens when the obejct is unreacheable)
        // bird.GetComponent<Rigidbody2D>().velocity = new Vector2(7.9f, 6.1f);
        bird.GetComponent<Rigidbody2D>().gravityScale = bird.GetComponent<ABBird>()._launchGravity;

        // set the layer back to birds to enable collisions
        gameObject.layer = 8;
    }

    private void AddSlingShotForceToTheBird(GameObject bird)
    {
        float dragRadius = 1.0f; // fixed value for all the birds
        Vector3 birdPosition = new Vector3(-13.0f, -2.3f, 1.0f);

        Vector3 slingshotPos = ABGameWorld.Instance.Slingshot().transform.position - ABConstants.SLING_SELECT_POS;
        Vector2 difference = slingshotPos - birdPosition;
        Vector2 direction = difference.normalized;

        bird.GetComponent<ABBird>().IsFlying = true;
        Vector2 releaseVelocity = direction * difference.magnitude / dragRadius * ABConstants.BIRD_MAX_LANUCH_SPEED;

        // add the velocity to the bird
        bird.GetComponent<Rigidbody2D>().velocity = releaseVelocity;
        Debug.Log("Velocity set to the bird: " + bird.name + " value: " + bird.GetComponent<Rigidbody2D>().velocity);
    }
    private void GetVelocityOfBird(GameObject bird, GameObject target)
    {
        Debug.Log("target position: " + target.transform.position);
        float dragRadius = 1.0f; // fixed value for all the birds
        Vector3 birdPosition = new Vector3(-13.0f, -2.3f, 1.0f);

        Vector3 slingshotPos = ABGameWorld.Instance.Slingshot().transform.position - ABConstants.SLING_SELECT_POS;
        Vector2 difference = slingshotPos - birdPosition;
        Vector2 direction = difference.normalized;

        Vector2 releaseVelocity = direction * difference.magnitude / dragRadius * ABConstants.BIRD_MAX_LANUCH_SPEED;
        Vector3 targetBirdPositionDiff = target.transform.position - birdPosition;

        float x = targetBirdPositionDiff.x;
        float y = targetBirdPositionDiff.y;
        float v = releaseVelocity.magnitude;
        float g = 9.81f; // multiply by 0.48f ?

        float v2 = v * v;
        float v4 = v2 * v2;
        float x2 = x * x;

        float theta = Mathf.Atan2(v2 + Mathf.Sqrt(v4 - g * (g * x2 - 2 * y * v2)), g * x);

        // The bird starts with no gravity, so we must set it
        Debug.Log("theta found: " + theta + "x velocity: " + v * Mathf.Cos(theta) + "y velocity: " + v * Mathf.Sin(theta));
        bird.GetComponent<Rigidbody2D>().velocity = new Vector2(v * Mathf.Cos(theta), v * Mathf.Sin(theta));
        // bird.GetComponent<Rigidbody2D>().velocity = new Vector2(7.9f, 6.1f);
        bird.GetComponent<Rigidbody2D>().gravityScale = bird.GetComponent<ABBird>()._launchGravity;

        // set the layer back to birds to enable collisions
        gameObject.layer = 8;
    }

    private void EstimateLaunchPoint(GameObject target)
    {

        float X_OFFSET = 0.45f; // adjust
        float Y_OFFSET = 0.35f; // adjust
        float velocity = 9.5f;
        float slingHeight = 2f; // adjust

        Vector3 slingshotPos = ABGameWorld.Instance.Slingshot().transform.position - ABConstants.SLING_SELECT_POS;

        float refX = slingshotPos.x + X_OFFSET;
        float refY = slingshotPos.y + Y_OFFSET;

        float x = (target.transform.position.x - refX);
        float y = -(target.transform.position.y - refY);

        // gravity
        float g = 0.48f * 9.81f;

        double solutionExistenceFactor = Math.Pow(velocity, 4) - Math.Pow(g, 2) * Math.Pow(x, 2) - 2 * y * g * Math.Pow(velocity, 2);

        // the target point cannot be reached
        Debug.Log("solutionExistenceFactor: " + solutionExistenceFactor);

        if (solutionExistenceFactor < 0)
        {
            Debug.Log("solutionExistenceFactor: " + solutionExistenceFactor);
            // todo
        }

        // solve cos theta from projectile equation

        double cos_theta_1 = Math.Sqrt((Math.Pow(x, 2) * Math.Pow(velocity, 2) - Math.Pow(x, 2) * y * g + Math.Pow(x, 2) * Math.Sqrt(Math.Pow(velocity, 4) - Math.Pow(g, 2) * Math.Pow(x, 2) - 2 * y * g * Math.Pow(velocity, 2))) / (2 * Math.Pow(velocity, 2) * (Math.Pow(x, 2) + Math.Pow(y, 2))));
        double cos_theta_2 = Math.Sqrt((Math.Pow(x, 2) * Math.Pow(velocity, 2) - Math.Pow(x, 2) * y * g - Math.Pow(x, 2) * Math.Sqrt(Math.Pow(velocity, 4) - Math.Pow(g, 2) * Math.Pow(x, 2) - 2 * y * g * Math.Pow(velocity, 2))) / (2 * Math.Pow(velocity, 2) * (Math.Pow(x, 2) + Math.Pow(y, 2))));

        double distanceBetween = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)); // ad-hoc patch

        double theta_1 = Math.Acos(cos_theta_1) + distanceBetween * 0.0001f; //compensate the rounding error
        double theta_2 = Math.Acos(cos_theta_2) + distanceBetween * 0.00005f; //compensate the rounding error

        float mag = slingHeight * 5;
        Debug.Log("X: " + (refX - mag * Math.Cos(theta_1)));
        Debug.Log("Y: " + (refY - mag * Math.Cos(theta_1)));

        Debug.Log("X: " + (refX - mag * Math.Cos(theta_2)));
        Debug.Log("Y: " + (refY - mag * Math.Cos(theta_2)));
    }

    public void LaunchBird(GameObject bird)
    {
        Vector3 birdPosition = new Vector3(-13.0f, -2.3f, 1.0f);

        // Once shot is made set flag in ABGameWorld so it can start recording ground truth if needed
        Debug.Log("ABBird::LaunchBird() : Launching bird");
        Debug.Log("ABBird::LaunchBird() : time scale is " + Time.timeScale);
        ABGameWorld.wasBirdLaunched = true;
        Vector3 slingshotPos = ABGameWorld.Instance.Slingshot().transform.position - ABConstants.SLING_SELECT_POS;
        Vector2 deltaPosFromSlingshot = (birdPosition - slingshotPos);
        //_animator.Play("flying", 0, 0f);

        bird.GetComponent<ABBird>().IsFlying = true;
        bird.GetComponent<ABBird>().IsSelected = false;

        // The bird starts with no gravity, so we must set it
        bird.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        bird.GetComponent<Rigidbody2D>().gravityScale = bird.GetComponent<ABBird>()._launchGravity;

        Vector2 f = -deltaPosFromSlingshot * bird.GetComponent<ABBird>()._launchForce;

        bird.GetComponent<Rigidbody2D>().AddForce(f, ForceMode2D.Impulse);

        // set the layer back to birds to enable collisions
        gameObject.layer = 8;
    }

    private void InstantiateObjects()
    {

        /*
         This function instantiate all the game objects for the parallel scene simulation
         */

        // Remove the objects from the previous simulation
        if (gameObjectsInParallelScene != null)
        {
            foreach (GameObject gameObject in gameObjectsInParallelScene)
            {
                if (gameObject != null)
                {
                    Debug.Log("Destroyed gameobject: " + gameObject.name);
                    Destroy(gameObject);
                }
            }
        }

        gameObjectsInParallelScene = new List<GameObject>();
        pathObservingGameObjectsInParalleScene = new List<GameObject>();

        // get random colors for the line renderers
        Color c1 = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
        Color c2 = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);

        foreach (GameObject gameObject in allGameObjectsToSimulate)
        {
            if (gameObject.name == "Camera" | gameObject.name == "Mountains" | gameObject.name == "Plants" | gameObject.name == "ScoreHUD") // avoid these gameobjects as they are not needed to consider and they dont have rigidbodies
            {
                continue;
            }
            Debug.Log("Making a copy of the object: " + gameObject.name);
            // Debug.Log("Mass of the copyied " + gameObject.name + " object: " + gameObject.GetComponent<Rigidbody2D>().mass);

            // add a line renderer to the game object
            LineRenderer lineRenderer = gameObject.GetComponent<LineRenderer>();
            if (lineRenderer == null)
            { // could not add the line renderer as it is already available in the game object. then get the exsiting one
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }
            lineRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            lineRenderer.startWidth = 0.1f;
            //Color c1 = new Color(1, 0, 0, 1); ; // change these colours later
            //Color c2 = new Color(0, 1, 0, 1);

            lineRenderer.SetColors(c1, c2);
            lineRenderer.positionCount = numberOfSimulationSteps;

            // instantiate the gameobject, adjust mass and move to the parallel scene
            GameObject simulationObject = Instantiate(gameObject);
            simulationObject.AddComponent<OriginObjectData>();
            AdjustMassAndHealthOfBlocks(simulationObject);
            // Debug.Log("Mass of the copyied " + simulationObject.name + " object: " + simulationObject.GetComponent<Rigidbody2D>().mass);

            SceneManager.MoveGameObjectToScene(simulationObject, parallelScene);

            // add the gameobject to list for future reference
            gameObjectsInParallelScene.Add(simulationObject);

            // also add the gameobject to list of pathObservingGameObjects objects if it is a pathObjeservingObject for recording their paths
            foreach (Object objct in pathObservingObjects)
            {
                if (gameObject.GetInstanceID() == objct.instanceID)
                {
                    pathObservingGameObjectsInParalleScene.Add(simulationObject);
                    // save the original object ID for future reference
                    simulationObject.GetComponent<OriginObjectData>().originGameObjectID = objct.instanceID;
                    simulationObject.GetComponent<OriginObjectData>().gameObjectType = objct.GetType();
                }
            }

        }

    }

    void AdjustMassAndHealthOfBlocks(GameObject gameObject)
    {
        /*
            When instantiating the copied blocks in the original scene into the parallel scene, ABBlock script is re-run and it updates the mass and the health points again according to the material of the game object,
            therefore revert that update back.
         */
        ABBlock abBlock = gameObject.GetComponent<ABBlock>();
        if (abBlock) // passed gameobject is a block
        {
            Debug.Log("passed block: " + gameObject.name + " material: " + abBlock._material + "passed mass: " + gameObject.GetComponent<Rigidbody2D>().mass);

            switch (abBlock._material)
            {
                case MATERIALS.wood:
                    //gameObject.GetComponent<Rigidbody2D>().mass /= 0.375f;
                    gameObject.GetComponent<Rigidbody2D>().mass /= 0.05f;
                    abBlock._life /= 0.75f;
                    Debug.Log("passed block: " + gameObject.name + " material: " + abBlock._material + " new mass: " + gameObject.GetComponent<Rigidbody2D>().mass);
                    break;

                case MATERIALS.stone:
                    gameObject.GetComponent<Rigidbody2D>().mass /= 1f;
                    abBlock._life /= 1.25f;
                    break;

                case MATERIALS.ice:
                    gameObject.GetComponent<Rigidbody2D>().mass /= 0.188f;
                    abBlock._life /= 0.4f;
                    break;
                default:
                    break;
            }
        }

    }

    void Shoot()
    {
        birdsTargetGameObject.GetComponent<Rigidbody2D>().velocity += initalVelocity;
    }

}