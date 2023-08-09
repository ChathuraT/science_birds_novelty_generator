using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Reflection;
using System.Linq;
using System.ComponentModel;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using System.Xml;

public class Utilities
{
    public bool simulationStarted = false;

    // reachable level space with some offset. It is a rectangled triangle, the 3 points are,
    public static float X_MIN_REACHABLE = -8;
    public static float X_MAX_REACHABLE = 9;
    public static float Y_MIN_REACHABLE = -3.5f;
    public static float[] SLIGSHOT_POSITION = { -12f, -2f }; // birds location on slingshot (roughly)
    public static float GROUND_LEVEL_Y = -3.5f; // ground level y coordinate
    private string LEVEL_SAVE_DIRECTORY = "Assets\\StreamingAssets\\GeneratedLevels\\";

    // the (float) numbers are multiplied with this constant to make them larger integers as all the floats are needed to be represented as ints in the solver, minimum is 100 
    public static int precisionMultiplier = 100;

    // object gropupID for the pathObstructing walls
    public static int pathObstructingWallsGroupID = 50;

    public ABLevel GetGameLevel(Scenario scenario)
    {
        ABLevel gameLevel = InstantiateGameLevel();

        /*
        List<ObjectGrammar> allObjects = new List<ObjectGrammar>();

        
        // traverse through VerbGrammar and LayoutGrammar and findout game objects
        foreach (VerbGrammar verb in scenario.verbs)
        {
            // get all the fields in the verbGrammar
            FieldInfo[] fields = verb.GetType().GetFields();
            foreach (FieldInfo field in fields)
            {
                // save only the ObjectGrammars
                if (field.FieldType == typeof(ObjectGrammar))
                {
                    allObjects.Add((ObjectGrammar)field.GetValue(verb));
                }
            }
        }
        */

        // add filtered gameobjects into correct lists
        foreach (ObjectGrammar objectTerm in scenario.objects)
        {
            string gameObjectType = objectTerm.gameObject.GetType().ToString();
            System.Type gameObjectBaseType = objectTerm.gameObject.GetType().BaseType;
            float rotation = objectTerm.gameObject.rotation;
            float xPosition = objectTerm.gameObject.positionX;
            float yPosition = objectTerm.gameObject.positionY;
            float scaleX = objectTerm.gameObject.scaleX;
            float scaleY = objectTerm.gameObject.scaleY;
            string material = objectTerm.gameObject.material;
            float forceMagnitude = objectTerm.gameObject.forceMagnitude;

            Debug.Log(gameObjectType);

            if (gameObjectBaseType == typeof(Block))
            {
                gameLevel.blocks.Add(new BlockData(gameObjectType, rotation, xPosition, yPosition, material));
            }
            else if (gameObjectBaseType == typeof(Plat))
            {
                gameLevel.platforms.Add(new PlatData(gameObjectType, rotation, xPosition, yPosition, scaleX, scaleY));

            }
            else if (gameObjectBaseType == typeof(Pig))
            {
                gameLevel.pigs.Add(new OBjData(gameObjectType, rotation, xPosition, yPosition));
            }
            else if (gameObjectBaseType == typeof(Bird))
            {
                gameLevel.birds.Add(new BirdData(gameObjectType));
            }
            else if (gameObjectBaseType == typeof(NovelForceObject))
            {
                gameLevel.novelties.Add(new NoveltyData(gameObjectType, rotation, xPosition, yPosition, material, scaleX, scaleY, forceMagnitude));
            }
        }

        return gameLevel;
    }

    public ABLevel InstantiateGameLevel()
    {

        ABLevel newGameLevel = new ABLevel();

        // assign the level width, camera, and sligshot
        newGameLevel.width = 2;
        newGameLevel.camera = new CameraData(25, 35, 0, -1);
        newGameLevel.slingshot = new SlingData(-12.0f, -2.5f);

        // create lists for the gameobjects
        newGameLevel.birds = new List<BirdData>();
        newGameLevel.pigs = new List<OBjData>();
        newGameLevel.blocks = new List<BlockData>();
        newGameLevel.platforms = new List<PlatData>();
        newGameLevel.novelties = new List<NoveltyData>();

        return newGameLevel;
    }

    public void PrintLevel(ABLevel gameLevel)
    {
        Debug.Log("--- start printing the game level ---");

        foreach (BirdData bird in gameLevel.birds)
        {
            Debug.Log(bird.type);
        }
        foreach (OBjData pig in gameLevel.pigs)
        {
            Debug.Log(pig.type + " x:" + pig.x + " y:" + pig.y);
        }
        foreach (OBjData block in gameLevel.blocks)
        {
            Debug.Log(block.type + " x:" + block.x + " y:" + block.y);
        }
        foreach (OBjData platform in gameLevel.platforms)
        {
            Debug.Log(platform.type + " x:" + platform.x + " y:" + platform.y);
        }

        Debug.Log("--- end printing the game level ---");

    }

    public void PrintScenario(Scenario scenario)
    {
        Debug.Log("--- start printing the scenario ---");

        Debug.Log("--- printing the verbs ---");
        foreach (VerbGrammar verb in scenario.verbs)
        {
            Debug.Log(GetAllPropertiesAndValues(verb));
        }
        if (scenario.constraints != null)
        {
            Debug.Log("--- printing the constraints ---");

            foreach (ConstraintGrammar constraint in scenario.constraints)
            {
                Debug.Log(GetAllPropertiesAndValues(constraint));
            }
        }
        if (scenario.layouts != null)
        {
            Debug.Log("--- printing the layouts ---");

            foreach (LayoutGrammar layout in scenario.layouts)
            {
                Debug.Log(GetAllPropertiesAndValues(layout));
            }
        }
        if (scenario.objects != null)
        {
            Debug.Log("--- printing the gameObjects ---");

            foreach (ObjectGrammar objectTerm in scenario.objects)
            {
                Debug.Log(GetAllPropertiesAndValues(objectTerm));
            }
        }

        Debug.Log("--- end printing the scenario ---");

    }

    public static string GetAllPropertiesAndValues(object obj)
    {
        string allFieldsAndValues = obj.ToString() + " -> ";

        if (obj.GetType().IsGenericType) // handling List<QSRRelation> in objectAndLayoutGrammars.qSRRelations
        {
            foreach (object element in (IList)obj)
            {
                allFieldsAndValues += element.ToString() + " (";

                foreach (FieldInfo field in element.GetType().GetFields())
                {
                    string name = field.Name;
                    object value = field.GetValue(element);
                    allFieldsAndValues += " " + name + ": " + value + " ";
                }

                allFieldsAndValues += ")  ";
            }
        }
        else
        {
            foreach (FieldInfo field in obj.GetType().GetFields())
            {
                string name = field.Name;
                object value = field.GetValue(obj);
                // expand if the value is a list 
                if (value is IList && value.GetType().IsGenericType)
                {
                    allFieldsAndValues += " " + string.Join(", ", (value as IEnumerable<object>).Cast<object>().ToList().ToArray());
                }
                else
                {
                    allFieldsAndValues += " " + name + ": " + value;
                }
            }
        }
        return allFieldsAndValues;
    }

    public void StartSimulation()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.Log("Playing");
        }
        else
        {
            Debug.Log("Not playing");
            EditorApplication.OpenScene("Assets/Scenes/Menus/MainMenu.unity");
            Debug.Log("11");

            //EditorApplication.isPlaying = true;
            Debug.Log("22");


            //LevelList.Instance.SetLevel(1);


        }


    }

    public void SimulateLevel(ABLevel gameLevel)
    {
        Debug.Log("simulating level");

        //Button shootButton = GameObject.Find("PlayButton").GetComponent<Button>(); ;
        //shootButton.onClick.Invoke();

        //ABLevelSelect lvlselcet = new ABLevelSelect();
        //lvlselcet.LoadNextScene("LevelSelectMenu");
        //ABSceneManager.Instance.LoadScene("LevelSelectMenu");

        //string[] allXmlFiles = LoadLevelSchema.Instance.currentXmlFiles;
        //Debug.Log(allXmlFiles[0]);
        //LevelList.Instance.LoadLevelsFromSource(allXmlFiles);

        // LevelList.Instance.SetLevel(0);
        ABGameWorld.currentLevel = gameLevel;

        ABGameWorld.gameWordlLoadedFromLevelGenerator = true; // line 198 in ABGameWorld.cs  currentLevel = LevelList.Instance.GetCurrentLevel(); line has not to be executed when the level is loading from the level generator

        ABSceneManager.Instance.LoadScene("GameWorld");


        Debug.Log("GameWorld scene loaded");

        //if (SceneManager.GetActiveScene().buildIndex != 6)
        //{
        //    StartCoroutine("waitForSceneLoad", 6);
        //}

        //Debug.Log("gameworld is loaded");
        //this.simulationStarted = true;

    }

    public void AddNewObject()
    {

        ABGameWorld.Instance.AddBlock(ABWorldAssets.BLOCKS["SquareHole"], new Vector2(2, 2), Quaternion.Euler(0, 0, 0));

    }

    public void CheckDirectReacheability()
    {


    }

    public void CheckTrajectory()
    {

        ABBird bird = ABGameWorld.Instance._birds[0];

        int numSteps = 500; // for example
        float timeDelta = 0.02f; // for example
        float _dragRadius = bird._dragRadius;
        float _launchGravity = bird._launchGravity;
        Vector3 position = new Vector3(-12.8f, -2.4f, 1.0f); // full stretch

        // trajectory visualization
        LineRenderer lineRenderer = bird.GetComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        Color c1 = Color.white;
        Color c2 = new Color(1, 1, 1, 0);
        lineRenderer.SetColors(c1, c2);
        //lineRenderer.materials[0] = new Material("Blue");
        lineRenderer.SetVertexCount(numSteps);

        Vector3 slingshotPos = ABGameWorld.Instance.Slingshot().transform.position - ABConstants.SLING_SELECT_POS;
        Vector2 difference = slingshotPos - position;
        Vector2 direction = difference.normalized;

        // The launch directly set the velocity of the bird, but not add a force.
        Vector2 releaseVelocity = direction * difference.magnitude / _dragRadius * ABConstants.BIRD_MAX_LANUCH_SPEED;
        Vector3 gravity = new Vector3(0, -9.8f * _launchGravity, 0);

        Vector3 velocity = releaseVelocity;
        for (int i = 0; i < numSteps; ++i)
        {
            // lineRenderer.SetPosition(i, position);

            position += velocity * timeDelta + 0.5f * gravity * timeDelta * timeDelta;
            velocity += gravity * timeDelta;

            lineRenderer.SetPosition(i, position);

        }

    }

    public List<Object> GetAllObjects(LayoutConstraintGraph layoutConstraintGraph)
    {
        List<Object> allObjects = new List<Object>();

        foreach (MainObjectAndAllLayoutGrammars mainObjectAndAllLayoutGrammars in layoutConstraintGraph.mainObjectAndAllLayoutGrammarsList)
        {
            allObjects.Add(mainObjectAndAllLayoutGrammars.mainObjct);
        }

        return allObjects;
    }

    public List<Object> GetAllObjects(Scenario scenario)
    {
        /*
            Returns all the game objects in the scenaro 
            LayoutInferrer.InferLayout should be called before calling this function
         */

        List<Object> allObjects = new List<Object>();

        foreach (ObjectGrammar objct in scenario.objects)
        {
            allObjects.Add(objct.gameObject);
        }

        return allObjects;
    }

    public void GetInstanceIDsOfObjects(List<ObjectGrammar> objects)
    {
        /*
            Given a list of objects, assign the instance IDs of those objects. 
            The instance IDs are captured considering the locatin of the objects.
            For the birds, since the ID cannot be captured from the location, it is determined by the order. Assumption: the birds are in the same order in the scenario.gameobjects and instantiated allGameObjects (TODO verify)
        */

        // get the object IDs of the pathObservingObjects from the simulation. The location of the objects are used for this
        Debug.Log("Finding the instance IDs of the path observing objects");
        GameObject[] allGameObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        // Debug.Log("current scene index start: " + SceneManager.GetActiveScene().buildIndex);

        int searchingBirdCount = 0; // number of birds searched
        foreach (ObjectGrammar objectGrammar in objects)
        {
            Object objctSearching = objectGrammar.gameObject;
            if (objctSearching.GetType() == typeof(BirdRed))
            {
                searchingBirdCount += 1;
            }
            Debug.Log(objctSearching.GetType() + " ---------------------------------");
            int numberOfBirdsFound = 0;
            foreach (GameObject objct in allGameObjects)
            {
                // debug.Log("found object " + objct.name + " searching object " + objctSearching.GetType() + " instance id is: " + objct.GetInstanceID() + " x: " + objct.transform.position.x + " " + objctSearching.positionX + " y: " + objct.transform.position.y + " " + objctSearching.positionY);
                // Debug.Log(Mathf.Abs(Mathf.Round(objct.transform.position.x * 100) - observingObjct.positionX * 100) < 0.001);
                // Debug.Log(Mathf.Abs(Mathf.Round(objct.transform.position.y * 100) - observingObjct.positionY * 100) < 0.001);
                if (objct.name.Contains("bird_"))
                { // handle the birds separately as they cannot be captured from the locations, bird ID is determined considering the order
                    numberOfBirdsFound += 1;
                    if ((objctSearching.GetType() == typeof(BirdRed)) & (searchingBirdCount == numberOfBirdsFound))
                    {
                        Debug.Log("object found at the position of " + objctSearching.GetType() + " is of type " + objct.name + " and the instance id is: " + objct.GetInstanceID());
                        objctSearching.instanceID = objct.GetInstanceID();
                    }
                }
                else if ((Mathf.Abs(objct.transform.position.x - objctSearching.positionX) < 0.1) & (Mathf.Abs(objct.transform.position.y - objctSearching.positionY) < 0.1))
                {
                    // Debug.Log(Mathf.Round(objct.transform.position.x * 100) + " " + observingObjct.positionX * 100);
                    // Debug.Log(Mathf.Round(objct.transform.position.y * 100) + " " + observingObjct.positionY * 100);
                    Debug.Log("object found at the position of " + objctSearching.GetType() + " is of type " + objct.name + " and the instance id is: " + objct.GetInstanceID());
                    objctSearching.instanceID = objct.GetInstanceID();
                }
            }
        }

        // verify whether all the gameobjects have been assigned with the instance ID
        foreach (ObjectGrammar objectGrammar in objects)
        {
            if (objectGrammar.gameObject.instanceID == 0)
            {
                throw new System.Exception("Could not find the instance of all the objects! eg: " + objectGrammar.gameObject.GetType());
            }
        }
        // Debug.Log("current scene index end: " + SceneManager.GetActiveScene().buildIndex);
    }


    public List<(ObjectGrammar, Location)> GetConnectedObjectsForMoving(ObjectGrammar movingObject, List<VerbGrammar> verbs)
    {

        /*
         This function returns the connected objects of a given movingObject for its motion (in order to satisfy the cannotFallMoving constraint, eventualy used for the touching terms)
         Also it returns the location the objects where they should be connected.
         Returns  List<(ObjectGrammar, Location)> connectedObjectsAndLocations list.
         The connected locations  depend on the previous verb the movingObject is associated with. Current rules to determine the connected location are:
            -- If it is a Hit (except a bird hit) - the movingObject and the other object has to be connected at lowerLeft relative to the other object(assuming the moving object is moving to the right side)
            -- If it is rolling/ sliding -  the movingObject and the other object has to be connected with UpperLeft (assuming the moving object is moving to the right side)
             // TODO: for rolling in a flat surface the location would be centreLeft

         Assumptions: the verbs are in the sequential order in which the movingObject moves. The motion of the movingObject starts with a Hit to the movingObject. The rolling object is being hit only once (but it can hit others in its motion).
         (TODO: The motion ends by destroyiong the movingObject from a HitDestroy or otherwise all the pigs are HitDestroyed)
         */

        List<(ObjectGrammar, Location)> connectedObjectsAndLocations = new List<(ObjectGrammar, Location)>(); // save the connected objects and the locations they should be connected considering the verbs
        FieldInfo[] fields;

        // get all the verbs linked with the movingGameObject
        List<VerbGrammar> linkedVerbsToMovingObject = new List<VerbGrammar>();
        foreach (VerbGrammar verb in verbs)
        {
            // get all the fields in the verbGrammar
            fields = verb.GetType().GetFields();
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType == typeof(ObjectGrammar))
                {
                    if ((ObjectGrammar)field.GetValue(verb) == movingObject)
                    {
                        linkedVerbsToMovingObject.Add(verb);
                    }
                }
            }
        }

        // get the connected objects of the movement
        bool motionStartDetected = false;
        foreach (VerbGrammar verb in linkedVerbsToMovingObject)
        {
            // step2: after the motion started, the possible verbs are roll, slide, hit and hitDestroy (only rolling and sliding are there in the exising predefined scenarios, which needs extracting connected objects)
            if (motionStartDetected)
            {
                if (verb.GetType() == typeof(Roll))
                {
                    // movingObject should be the one that is being rolled
                    if (((Roll)verb).a.gameObject == movingObject.gameObject)
                    {
                        connectedObjectsAndLocations.Add((((Roll)verb).b, new UpperLeft()));
                    }
                }
                else if (verb.GetType() == typeof(Slide))
                {
                    // movingObject should be the one that is being slid
                    if (((Slide)verb).a.gameObject == movingObject.gameObject)
                    {
                        connectedObjectsAndLocations.Add((((Slide)verb).b, new UpperLeft()));
                    }
                }
                else if (verb.GetType() == typeof(Hit))
                {
                    // if the movingObject is the one that hit the other object
                    if (((Hit)verb).a.gameObject == movingObject.gameObject)
                    {
                        connectedObjectsAndLocations.Add((((Hit)verb).b, new LowerLeft()));
                    }
                }
                else if (verb.GetType() == typeof(HitDestroy))
                {
                    // movingObject should be the one that hitDestroy the other object
                    if (((HitDestroy)verb).a.gameObject == movingObject.gameObject)
                    {
                        connectedObjectsAndLocations.Add((((HitDestroy)verb).b, new LowerLeft()));
                    }
                }
            }

            // step1: the motion should start from a hit
            if (!motionStartDetected & (verb.GetType() == typeof(Hit)))
            {
                // movingObject should be the one that is being hit, not that hits the other object
                if (((Hit)verb).b.gameObject == movingObject.gameObject)
                {
                    // store the movingObject itself in the connectedObjects (connected location is set to null as it is not important here beacause this is the start of the motion)
                    connectedObjectsAndLocations.Add((movingObject, null));

                    // if the other hit object is a bird ignore it, else store in the connectedObjects
                    //if (((Hit)verb).a.gameObject.GetType().BaseType != typeof(Bird))
                    //{
                    //    Debug.Log(((Hit)verb).b.gameObject.GetType());
                    //    connectedObjectsAndLocations.Add((((Hit)verb).a, null));
                    //}
                    motionStartDetected = true;
                }
            }
        }
        return connectedObjectsAndLocations;

    }

    public HashSet<ObjectGrammar> GetObjectsAssociatedWithTheMovement(ObjectGrammar movingObject, ObjectGrammar objectLiesAtEndOfPath, List<VerbGrammar> verbs)
    {
        /*  
        This function determines the objects that are associated with the movingObject, so that the objectLiesAtEndOfPath can be placed without interferring the prior movemnt of the movingObject
        This is used to resolve the liesAtEndOfPath terms into inDirection terms (objectLiesAtEndOfPath is inDirection below to all the other objects that the movingObject associated with to avoid interfering the movement of movingObject)
        */

        List<(ObjectGrammar, Location)> associatedObjectsToMovingObject = new List<(ObjectGrammar, Location)>(); // save the connected objects and the locations they should be connected considering the verbs
        FieldInfo[] fields;

        // get all the verbs linked with the movingGameObject
        HashSet<VerbGrammar> linkedVerbsToMovingObject = new HashSet<VerbGrammar>();
        foreach (VerbGrammar verb in verbs)
        {
            // get all the fields in the verbGrammar
            fields = verb.GetType().GetFields();
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType == typeof(ObjectGrammar))
                {
                    if ((ObjectGrammar)field.GetValue(verb) == movingObject)
                    {
                        linkedVerbsToMovingObject.Add(verb);
                    }
                }
            }
        }

        // get all the objects associated with the above verbs
        HashSet<ObjectGrammar> linkedObjectsToMovingObject = new HashSet<ObjectGrammar>();
        foreach (VerbGrammar verb in linkedVerbsToMovingObject)
        {
            if (verb.a.gameObject != objectLiesAtEndOfPath.gameObject) // skip adding the objectLiesAtEndOfPath to the set
            {
                linkedObjectsToMovingObject.Add(verb.a);
            }
            if (verb.b.gameObject != objectLiesAtEndOfPath.gameObject) // skip adding the objectLiesAtEndOfPath to the set
            {
                linkedObjectsToMovingObject.Add(verb.b);
            }
        }

        return linkedObjectsToMovingObject;
    }

    public static float GetMaxYReachable(float x)
    {
        /*
         This function returns the maximum posible y location of a given x coordinate considering the parabolic reachable area of the bird
         */

        float launchGravity = 0.48f; // for red bird
        Vector3 gravity = new Vector3(0, 9.8f * launchGravity, 0);
        Vector2 origin = new Vector2(-12.65f, -2.2f); // the origin of the bird when fully strectched (calculated using the pos of the bird in sling + adding an average (-0.5) for the stretch as dragRadius is 1)
        float releaseVelocity = 10; // this is fixed for the full stretch
        float transformed_x = x - origin.x; // transform from the world coordinates to a coordinates where origin is the release point

        /*
        float dragRadius = 1f; // for red bird
        Vector3 position = new Vector3(-12.8f, -2.4f, 1.0f); // full stretch of the sling
        Vector3 slingshotPositionInLevel = new Vector3(-12f, -2.5f, 0f); // fixed slingshot position
        Vector3 slingshotPos = slingshotPositionInLevel - ABConstants.SLING_SELECT_POS;
        Vector2 difference = slingshotPos - position;
        Vector2 direction = difference.normalized;
        Vector2 releaseVelocity = direction * difference.magnitude / dragRadius * ABConstants.BIRD_MAX_LANUCH_SPEED;

        Debug.Log("dragRadius: " + dragRadius + " " + "launchGravity: " + launchGravity + " " + "difference: " + difference + " " + "releaseVelocity: " + releaseVelocity + " " + "gravity: " + gravity);
        */


        // due to the parabolic motion of the bird the y should be; y ≤ (v^2/(2g))−(gx^2/(2v^2)) math can be found at: [https://physics.stackexchange.com/a/63142]
        float transformed_maxY = (releaseVelocity * releaseVelocity / (2 * gravity.y)) - (gravity.y * transformed_x * transformed_x / (2 * releaseVelocity * releaseVelocity));
        float maxY = transformed_maxY + origin.y; // trasnform from the release point coordinate system to the world coordinates

        // recuce the maxY by 0.5 to be on the safe side (to account for not having the exact release point) and return 
        // Debug.Log("xLocation, maxYLocation: " + x + " " + (maxY - 0.5f));
        return maxY - 0.5f;

    }

    public bool GetAnUnoccupiedReachableSpace(LayoutConstraintGraph layoutConstraintGraph, Object objct)
    {
        /*
            check all the objects in the scenario to find out an unoccupied space to place the objct. Function will return a feasible x and y coordinates for the objct to place.
        */
        bool findingSpaceSuccess = false;
        int maximumAttemps = 10;
        int attemptsTried = 0;

        // defining the necessary variables
        float xLocation;
        float yLocation;
        float objectMinX;
        float objectMaxX;
        float objectMinY;
        float objectMaxY;

        Object mainObject;
        float mainObjectMinX;
        float mainObjectMaxX;
        float mainObjectMinY;
        float mainObjectMaxY;

        while (!findingSpaceSuccess)
        {
            if (attemptsTried > maximumAttemps)
            {
                return false;
            }

            findingSpaceSuccess = true;
            attemptsTried++;

            // get a random x location in the reachable x range
            xLocation = UnityEngine.Random.Range(X_MIN_REACHABLE + objct.size[0] / 2, X_MAX_REACHABLE - objct.size[0] / 2);
            // get a random y location in the reachable y range for the selected x location
            yLocation = UnityEngine.Random.Range(Y_MIN_REACHABLE + objct.size[1] / 2, GetMaxYReachable(xLocation) - objct.size[1] / 2);
            Debug.Log("x: " + xLocation + ", y: " + yLocation);

            // get the bounding box of the objct
            objectMinX = xLocation - objct.size[0] / 2;
            objectMaxX = xLocation + objct.size[0] / 2;
            objectMinY = yLocation - objct.size[1] / 2;
            objectMaxY = yLocation + objct.size[1] / 2;

            // check whether it overlaps with the exisiting objects
            foreach (MainObjectAndAllLayoutGrammars mainObjectAndAllLayoutGrammars in layoutConstraintGraph.mainObjectAndAllLayoutGrammarsList)
            {
                mainObject = mainObjectAndAllLayoutGrammars.mainObjct;
                if (mainObject.positionX != -100)// check if the position determined or not
                {
                    // get the bounding box of the objct
                    mainObjectMinX = mainObject.positionX - mainObject.size[0] / 2;
                    mainObjectMaxX = mainObject.positionX + mainObject.size[0] / 2;
                    mainObjectMinY = mainObject.positionY - mainObject.size[1] / 2;
                    mainObjectMaxY = mainObject.positionY + mainObject.size[1] / 2;

                    // check of the 2 objects overlap
                    if (DoesRectanglesOverlap(new float[] { objectMinX, objectMaxX, objectMinY, objectMaxY }, new float[] { mainObjectMinX, mainObjectMaxX, mainObjectMinY, mainObjectMaxY }))
                    {
                        findingSpaceSuccess = false;
                        break;
                    }
                }
            }

        }
        return true;
    }

    private bool DoesRectanglesOverlap(float[] rectangle1, float[] rectangle2)
    {
        /*
        Check whether 2 objects overlap in the level space and return true if they do and false otherwise
        Inputs are the rectangles represented as float arrays [objectMinX, objectMaxX, objectMinY, objectMaxY]
        */

        if (rectangle1[0] < rectangle2[1] && rectangle1[1] > rectangle2[0] && rectangle1[2] > rectangle2[3] && rectangle1[3] < rectangle2[2])
        {
            return true;
        }
        return false;
    }

    public static float[] GetMBRWidthAndHeight(Object objct)
    {
        /*
         * Given an objct this function calculates the width and the height of the Minimal Bounding Rectangle comsidering the rotation of the object. Any object is considered as a ractangle
         * Return float [] {width of MBR, Height of MBR}
         */

        float ObjectRotationInRad = Mathf.Deg2Rad * -1 * objct.rotation; // object's rotation is multiplied by -1 as it measured counter clockwise in unity

        float MBRWidth = objct.size[0] * objct.scaleX * Mathf.Cos(ObjectRotationInRad) + objct.size[1] * objct.scaleY * Mathf.Sin(ObjectRotationInRad);
        float MBRHeight = objct.size[0] * objct.scaleX * Mathf.Sin(ObjectRotationInRad) + objct.size[1] * objct.scaleY * Mathf.Cos(ObjectRotationInRad);

        // treat the platforms differently, as they can be resized and rotated, hence MBR does not give the exact boundries
        //if (objct.GetType() == typeof(Platform))
        //{
        //    Debug.Log("MBRWidth, MBRHeight " + MBRWidth + " " + MBRHeight);

        //}

        Debug.Log(objct + " MBRWidth, MBRHeight " + MBRWidth + " " + MBRHeight);
        return new float[] { MBRWidth, MBRHeight };

    }

    public static List<(float, float)> GetMBRWidthAndHeight(List<Object> obejcts)
    {
        /*
         * Given a list of objcts this function calculates the width and the height of the Minimal Bounding Rectangle comsidering the rotation of all the object. Any object is considered as a ractangle
         * Return (float,float) {(width of MBR object 1, Height of MBR object 1), (width of MBR object 2, Height of MBR object 2), ...}
         */

        List<(float, float)> mBRWidthAndHeights = new List<(float, float)>();
        foreach (Object objct in obejcts)
        {
            float ObjectRotationInRad = Mathf.Deg2Rad * -1 * objct.rotation; // object's rotation is multiplied by -1 as it measured counter clockwise in unity

            float MBRWidth = objct.size[0] * objct.scaleX * Mathf.Cos(ObjectRotationInRad) + objct.size[1] * objct.scaleY * Mathf.Sin(ObjectRotationInRad);
            float MBRHeight = objct.size[0] * objct.scaleX * Mathf.Sin(ObjectRotationInRad) + objct.size[1] * objct.scaleY * Mathf.Cos(ObjectRotationInRad);

            // treat the platforms differently, as they can be resized and rotated, hence MBR does not give the exact boundries
            if (objct.GetType() == typeof(Platform))
            {
                Debug.Log("MBRWidth, MBRHeight " + MBRWidth + " " + MBRHeight);

            }
            mBRWidthAndHeights.Add((MBRWidth, MBRHeight));

        }


        return mBRWidthAndHeights;

    }

    public static int[] GetMultipliedObjectDimensions(Object objct)
    {
        /*
         * Given a object with rotation a, this function returns the width, height, widthCosa, widthSina, heightCosa, heightSina, mbrHalfWidth, mbrHalfHeight
         * The returned values are multiplied by the precisionMultiplier 
         * Return int [] { width, height, widthCosa, widthSina, heightCosa, heightSina, mbrHalfWidth, mbrHalfHeight}
         */

        float ObjectRotationInRad = Mathf.Deg2Rad * -1 * objct.rotation; // object's rotation is multiplied by -1 as it measured counter clockwise in unity

        int width = (int)(System.Math.Round((objct.size[0] * objct.scaleX * precisionMultiplier) / 2, MidpointRounding.AwayFromZero) * 2); // rounding is to make the decimals into the nearest even integer number
        int height = (int)(System.Math.Round((objct.size[1] * objct.scaleY * precisionMultiplier) / 2, MidpointRounding.AwayFromZero) * 2);

        int widthCosa = (int)(System.Math.Round((width * Mathf.Cos(ObjectRotationInRad)) / 2, MidpointRounding.AwayFromZero) * 2);
        int widthSina = (int)(System.Math.Round((width * Mathf.Sin(ObjectRotationInRad)) / 2, MidpointRounding.AwayFromZero) * 2);
        int heightCosa = (int)(System.Math.Round((height * Mathf.Cos(ObjectRotationInRad)) / 2, MidpointRounding.AwayFromZero) * 2);
        int heightSina = (int)(System.Math.Round((height * Mathf.Sin(ObjectRotationInRad)) / 2, MidpointRounding.AwayFromZero) * 2);

        float MBRWidth = widthCosa + heightSina;
        float MBRHeight = widthSina + heightCosa;
        int mbrHalfWidth = (int)System.Math.Ceiling(MBRWidth / 2);
        int mbrHalfHeight = (int)System.Math.Ceiling(MBRHeight / 2);


        //int width = (int)(System.Math.Round(objct.size[0] * objct.scaleX, 2) * precisionMultiplier);
        //int height = (int)(System.Math.Round(objct.size[1] * objct.scaleY, 2) * precisionMultiplier);

        //int widthCosa = (int)(System.Math.Round(objct.size[0] * Mathf.Cos(ObjectRotationInRad), 2) * precisionMultiplier);
        //int widthSina = (int)(System.Math.Round(objct.size[0] * Mathf.Sin(ObjectRotationInRad), 2) * precisionMultiplier);
        //int heightCosa = (int)(System.Math.Round(objct.size[1] * Mathf.Cos(ObjectRotationInRad), 2) * precisionMultiplier);
        //int heightSina = (int)(System.Math.Round(objct.size[1] * Mathf.Sin(ObjectRotationInRad), 2) * precisionMultiplier);

        //float MBRWidth = widthCosa + heightSina;
        //float MBRHeight = widthSina + heightCosa;
        //int mbrHalfWidth = (int)System.Math.Ceiling(MBRWidth / 2);
        //int mbrHalfHeight = (int)System.Math.Ceiling(MBRHeight / 2);


        // treat the platforms differently, as they can be resized and rotated, hence MBR does not give the exact boundries
        if (objct.GetType() == typeof(Platform))
        {
            // Debug.Log("scaleY: " + objct.scaleY + " rotation: " + objct.rotation + " cos: " + Mathf.Cos(ObjectRotationInRad) + " Sin: " + Mathf.Sin(ObjectRotationInRad) + " width: " + width + " height: " + height + " widthCosa: " + widthCosa + " widthSina: " + widthSina + " heightCosa: " + heightCosa + " heightSina: " + heightSina + " mbrHalfWidth: " + mbrHalfWidth + " mbrHalfHeight: " + mbrHalfHeight);
        }

        return new int[] { width, height, widthCosa, widthSina, heightCosa, heightSina, mbrHalfWidth, mbrHalfHeight };

    }

    public void AddLayoutObjectsToScenarioInfo(Scenario scenario)
    {
        /*
         This function will add the current objects in the layout to the ScenarioInfo class which will be written to a txt file at the end of the generation
        */

        scenario.scenarioInfo.scenarioObjects = new ScenarioObjects();
        List<ScenarioObject> layoutObjectsList = new List<ScenarioObject>();

        foreach (ObjectGrammar objectGrammar in scenario.objects)
        {
            ScenarioObject scenarioObject = new ScenarioObject();
            scenarioObject.scenarioObjectName = objectGrammar.gameObject.GetType().ToString();
            scenarioObject.position = new Coordinate();
            scenarioObject.position.x = objectGrammar.gameObject.positionX;
            scenarioObject.position.y = objectGrammar.gameObject.positionY;
            layoutObjectsList.Add(scenarioObject);
        }
        scenario.scenarioInfo.scenarioObjects.layoutObjects = layoutObjectsList;
    }

    public void AddDistractionObjectsToScenarioInfo(Scenario scenario, List<ObjectGrammar> distractionObjects)
    {
        /*
         This function will add the distraction objects in the scenario to the ScenarioInfo class which will be written to a txt file at the end of the generation
        */
        List<ScenarioObject> distractionObjectsList = new List<ScenarioObject>();

        foreach (ObjectGrammar objectGrammar in distractionObjects)
        {
            ScenarioObject scenarioObject = new ScenarioObject();
            scenarioObject.scenarioObjectName = objectGrammar.gameObject.GetType().ToString();
            scenarioObject.position = new Coordinate();
            scenarioObject.position.x = objectGrammar.gameObject.positionX;
            scenarioObject.position.y = objectGrammar.gameObject.positionY;
            distractionObjectsList.Add(scenarioObject);
        }
        scenario.scenarioInfo.scenarioObjects.distractionObjects = distractionObjectsList;
    }

    public void WriteLevelFileFromScenario(List<ObjectGrammar> scenarioObjects, string levelName, string version)
    {
        /*
            Given the objects in the scenario, this function writes the level xml file. version is whether it is a Novel level or a NonNovel level
        */

        Debug.Log("Writing the level file");

        string path = LEVEL_SAVE_DIRECTORY + levelName + ".xml";

        // number of decimal places needed to be written
        int precisionPoints = 4;

        StringBuilder output = new StringBuilder();
        XmlWriterSettings ws = new XmlWriterSettings();
        ws.Indent = true;

        using (XmlWriter writer = XmlWriter.Create(output, ws))
        {
            writer.WriteStartElement("Level");
            writer.WriteAttributeString("width", "2");

            writer.WriteStartElement("Camera");
            writer.WriteAttributeString("x", "0");
            writer.WriteAttributeString("y", "-1");
            writer.WriteAttributeString("minWidth", "25");
            writer.WriteAttributeString("maxWidth", "35");
            writer.WriteEndElement();

            writer.WriteStartElement("Score");
            writer.WriteAttributeString("highScore", "0");
            writer.WriteEndElement();

            writer.WriteStartElement("ScenarioID");
            writer.WriteAttributeString("scenarioID", levelName); // levelID will be written here
            writer.WriteEndElement();

            writer.WriteStartElement("Birds");
            foreach (ObjectGrammar gameObjectGrammar in scenarioObjects)
            {
                if (gameObjectGrammar.gameObject.GetType().BaseType == typeof(Bird))
                {
                    writer.WriteStartElement("Bird");
                    writer.WriteAttributeString("type", gameObjectGrammar.gameObject.GetType().ToString());
                    writer.WriteEndElement();
                }


            }
            writer.WriteEndElement();

            writer.WriteStartElement("Slingshot");
            writer.WriteAttributeString("x", "-12.0");
            writer.WriteAttributeString("y", "-2.5");
            writer.WriteEndElement();



            writer.WriteStartElement("GameObjects");

            foreach (ObjectGrammar gameObjectGrammar in scenarioObjects)
            {
                Object gameObject = gameObjectGrammar.gameObject;
                string gameObejctType = gameObject.GetType().ToString();
                string x = Math.Round(gameObject.positionX, precisionPoints).ToString();
                string y = Math.Round(gameObject.positionY, precisionPoints).ToString();
                string rotation = Math.Round(gameObject.rotation, precisionPoints).ToString();
                string forceMagnitude = Math.Round(gameObject.forceMagnitude, precisionPoints).ToString(); // only used for novel objects

                if (gameObject.GetType().BaseType == typeof(Block))
                {
                    writer.WriteStartElement("Block");
                    writer.WriteAttributeString("type", gameObejctType);
                    writer.WriteAttributeString("material", gameObject.material);
                    writer.WriteAttributeString("x", x);
                    writer.WriteAttributeString("y", y);
                    writer.WriteAttributeString("rotation", rotation);
                    writer.WriteEndElement();
                }
                else if (gameObject.GetType().BaseType == typeof(Pig))
                {
                    writer.WriteStartElement("Pig");
                    writer.WriteAttributeString("type", gameObejctType);
                    writer.WriteAttributeString("x", x);
                    writer.WriteAttributeString("y", y);
                    writer.WriteAttributeString("rotation", rotation);
                    writer.WriteEndElement();

                }
                else if (gameObject.GetType().BaseType == typeof(Plat))
                {
                    string scaleX = Math.Round(gameObject.scaleX, precisionPoints).ToString();
                    string scaleY = Math.Round(gameObject.scaleY, precisionPoints).ToString();

                    writer.WriteStartElement("Platform");
                    writer.WriteAttributeString("type", gameObejctType);
                    writer.WriteAttributeString("x", x);
                    writer.WriteAttributeString("y", y);
                    writer.WriteAttributeString("rotation", rotation);
                    writer.WriteAttributeString("scaleX", scaleX);
                    writer.WriteAttributeString("scaleY", scaleY);
                    writer.WriteEndElement();
                }
                else if (gameObject.GetType().BaseType == typeof(NovelForceObject))
                {
                    if (version == "NonNovel")
                    {
                        continue; // write novel objects depending on the version
                    }
                    string scaleX = Math.Round(gameObject.scaleX, precisionPoints).ToString();
                    string scaleY = Math.Round(gameObject.scaleY, precisionPoints).ToString();

                    writer.WriteStartElement("Novelty");
                    writer.WriteAttributeString("type", gameObejctType);
                    writer.WriteAttributeString("x", x);
                    writer.WriteAttributeString("y", y);
                    writer.WriteAttributeString("scaleX", scaleX);
                    writer.WriteAttributeString("scaleY", scaleY);
                    writer.WriteAttributeString("forceMagnitude", forceMagnitude);
                    writer.WriteEndElement();
                }
            }

        }

        StreamWriter streamWriter = new StreamWriter(path);
        streamWriter.WriteLine(output.ToString());
        streamWriter.Close();
    }

    public void WriteLevelInfoFile(ScenarioInfo scenarioInfo)
    {
        /*
            Given a ScenarioInfo object, this function serializes that object to an xml and write it to a file
        */

        XmlSerializer serializer = new XmlSerializer(typeof(ScenarioInfo));
        using (FileStream stream = new FileStream(LEVEL_SAVE_DIRECTORY + scenarioInfo.nonNovelScenarioID + "_info.xml", FileMode.Create))
        {
            serializer.Serialize(stream, scenarioInfo);
        }
    }

    public void AddGroupIDs(Scenario scenario)
    {
        // this function group the objects in a scenario (objects that move together) and add IDs. Note: Birds are also assigned with IDs, modify according to the necessity in future

        // step 1: considering the verbs that the objects are linked  (currently only roll and slide verbs have objects that can be grouped together) 

        int newID;

        foreach (VerbGrammar verb in scenario.verbs)
        {
            // TODO: handle conflicts (if the gameobject is already assigned with an id)/ if they are really in the same group when there is a complex movement (eg: bird rolling on a > falling > rolling on b> ...)

            if (verb.GetType() == typeof(Roll))
            {
                // check for conflicts
                if ((((Roll)verb).a.gameObject.groupID != 0) | (((Roll)verb).b.gameObject.groupID != 0))
                {
                    Debug.Log("Warning! overwriting an exising index");
                }

                newID = getNextGroupID(scenario.objects);
                ((Roll)verb).a.gameObject.groupID = newID;
                ((Roll)verb).b.gameObject.groupID = newID;
            }
            else if (verb.GetType() == typeof(Slide))
            {
                // check for conflicts
                if ((((Slide)verb).a.gameObject.groupID != 0) | (((Slide)verb).b.gameObject.groupID != 0))
                {
                    Debug.Log("Warning! overwriting an exising index");
                }

                newID = getNextGroupID(scenario.objects);
                ((Slide)verb).a.gameObject.groupID = newID;
                ((Slide)verb).b.gameObject.groupID = newID;
            }

        }

        // step 2: if a groupID hasn't already been assigned to a gameOject add a newID
        foreach (ObjectGrammar objct in scenario.objects)
        {
            if (objct.gameObject.groupID == 0)
            {  // default groupID is 0
                objct.gameObject.groupID = getNextGroupID(scenario.objects);
            }
        }

    }

    private int getNextGroupID(List<ObjectGrammar> gameObjects)
    {
        // this function returns the next unused ID for a given set of gameobjects

        List<int> usedGroupIDs = new List<int>();
        foreach (ObjectGrammar objctTerm in gameObjects)
        {
            if (!usedGroupIDs.Contains(objctTerm.gameObject.groupID))
            {
                usedGroupIDs.Add(objctTerm.gameObject.groupID);
            }
        }

        return usedGroupIDs.Max() + 1;
    }

    public static bool IsObjectNovel(int gameObjectID, List<Object> allObjects)
    {
        /*
         Given an object ID return whether that object is associated with the novelty solution path
         */

        // find the object with gameObjectID in the Scenarios object list
        foreach (Object gameObject in allObjects)
        {
            if (gameObject.instanceID == gameObjectID)
            {
                return gameObject.isNovel;
            }
        }
        throw new Exception("Could not find an object with the given object id in the allObjects list!");
    }

    public List<List<Vector2>> GetPathsFromSimulation(List<IDictionary<String, List<Vector2>>> simulationResults, int objectID)
    {
        /*
         This function returns all the paths from the simulation of a given objectID
        */

        List<List<Vector2>> allPathsOfObject = new List<List<Vector2>>();

        foreach (IDictionary<string, List<Vector2>> shotData in simulationResults)
        {
            foreach (KeyValuePair<string, List<Vector2>> kvp in shotData)
            {
                if (Int32.Parse(kvp.Key.Split(' ')[1]) == objectID)
                {
                    // Debug.Log("Found path for object: Key = " + kvp.Key + " Value = " + string.Join(", ", kvp.Value));
                    allPathsOfObject.Add(kvp.Value);
                }
            }
        }
        return allPathsOfObject;
    }

}
