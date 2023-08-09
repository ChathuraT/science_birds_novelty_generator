// SCIENCE BIRDS: A clone version of the Angry Birds game used for 
// research purposes
// 
// Copyright (C) 2016 - Lucas N. Ferreira - lucasnfe@gmail.com
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>
//

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Linq;

public class ABGameWorld : ABSingleton<ABGameWorld>
{

    static int _levelTimesTried;
    static public float SimulationSpeed = 1.0f;

    //for tracking the sequential id of the simulations in a game instance
    static public int simulationID = 0;

    // novelty type for the novelty level 3 (default -1 means no level 3 novelty is used)
    static public int noveltyTypeForNovelty3 = -1;

    // when set to other than -1, ground truth will be taken every n frames
    static public int takeGroundTruthEveryNthFrames = 5;

    public static int maxBatchGTFrames = 300;
    // when set to true, we will record ground truth every n frames
    static public bool wasBirdLaunched = false;
    static public bool displayTrajectory = false;
    static public bool isRecordingBatchGroundTruthAfterShooting = false;
    static public bool isRecordingBatchGroundTruth = false;
    //Record batch GT option
    //0 : all gts
    //1 : only record bird flying before the collision
    public enum RecordBatchGTOption { COMPLETE, BIRD_BEFORE_COLLISION };
    static public RecordBatchGTOption batchGTOption = RecordBatchGTOption.COMPLETE;

    static public bool isBannerShowing = false;

    static public List<(string, string)> batchGroundTruths = new List<(string, string)>();
    static public List<string> batchScreenshots = new List<string>();

    private bool _levelCleared;

    public List<ABPig> _pigs;
    public List<ABBird> _birds;
    private int framesPassedSinceLastTake = 1;
    private List<ABParticle> _birdTrajectory;

    private ABBird _lastThrownBird;
    public Transform blocksTransform { get; private set; }
    public Transform birdsTransform { get; private set; }
    public Transform plaftformsTransform { get; private set; }
    public Transform slingshotBaseTransform { get; private set; }
    public Transform externalAgentsTransform { get; private set; }
    public Transform noveltiesTransform { get; private set; }

    private GameObject _slingshot;
    public GameObject Slingshot() { return _slingshot; }

    private GameObject _levelFailedBanner;
    public bool LevelFailed()
    {
        if (_levelFailedBanner == null)
        {
            return false;
        }
        return _levelFailedBanner.activeSelf;
    }

    private GameObject _levelClearedBanner;
    public bool LevelCleared()
    {
        if (_levelClearedBanner == null)
        {
            return false;
        }
        return _levelClearedBanner.activeSelf;
    }

    private int _pigsAtStart;
    public int PigsAtStart { get { return _pigsAtStart; } }

    private int _birdsAtStart;
    public int BirdsAtStart { get { return _birdsAtStart; } }

    private int _blocksAtStart;
    public int BlocksAtStart { get { return _blocksAtStart; } }

    public ABGameplayCamera GameplayCam { get; set; }
    public float LevelWidth { get; set; }
    public float LevelHeight { get; set; }

    private bool BirdsScoreUpdated = false;

    //Score get set
    public float LevelScore { get; set; }

    // Game world properties
    public bool _isSimulation;
    public int _timesToGiveUp;
    public float _timeToResetLevel = 1f;
    public int _birdsAmounInARow = 5;
    public int frameRecorded = 0;
    public AudioClip[] _clips;
    public double simulationTimeStep = 0.02;

    public static AssetBundle NOVELTIES;
    public static System.Reflection.Assembly UglyBird_DLL;

    public static ABLevel currentLevel = null;
    public static List<GameObject> _platforms;
    public static List<ABBlock> _blocks;

    // parameters for the level generator
    // set this variable when the level is loading from the level genertor in order to load the level correctly
    public static bool gameWordlLoadedFromLevelGenerator = false;
    // playing bot on or off switch
    private bool playBotSwitch = false;
    // level info folder and output writing file
    public string levelInfoFolder = "Assets\\StreamingAssets\\Levels\\novelty_level_0\\type150\\Levels\\";
    private string outcomeRecorderFile = "Assets\\StreamingAssets\\playbot_outcome.csv";
    // an outcome recorder to record the outcome of the playing bot (if playBotSwitch is enabled)
    LevelPlayOutcome outcomeRecorder;

    // this variable is to avoid loading the next level multiple times after the current level is cleared (due to multiple invokings of the functions)
    private bool nextLevelAlreadyLoading = false;

    // paramters to save the screenshot
    public int captureWidth = 1920;
    public int captureHeight = 1080;
    public int captureQuality = 100;

    void Awake()
    {
        //System.Console.WriteLine("=== entering ABGameWorld.Awake() ===");
        Physics2D.autoSimulation = false;
        isBannerShowing = false;
        blocksTransform = GameObject.Find("Blocks").transform;
        birdsTransform = GameObject.Find("Birds").transform;
        plaftformsTransform = GameObject.Find("Platforms").transform;
        if (GameObject.Find("ExternalAgents") != null)
        {
            externalAgentsTransform = GameObject.Find("ExternalAgents").transform;
        }
        if (GameObject.Find("Novelties") != null)
        {
            noveltiesTransform = GameObject.Find("Novelties").transform;
        }

        _levelFailedBanner = GameObject.Find("LevelFailedBanner").gameObject;
        _levelFailedBanner.gameObject.SetActive(false);

        _levelClearedBanner = GameObject.Find("LevelClearedBanner").gameObject;
        _levelClearedBanner.gameObject.SetActive(false);

        GameplayCam = GameObject.Find("Camera").GetComponent<ABGameplayCamera>();

        //System.Console.WriteLine("=== entering ABGameWorld.Start() ===");

        isBannerShowing = false;
        _pigs = new List<ABPig>();
        _birds = new List<ABBird>();
        _platforms = new List<GameObject>();
        _blocks = new List<ABBlock>();

        _birdTrajectory = new List<ABParticle>();

        _levelCleared = false;

        // If there are objects in the scene, use them to play
        if (blocksTransform.childCount > 0 || birdsTransform.childCount > 0)
        {

            foreach (Transform bird in birdsTransform)
                AddBird(bird.GetComponent<ABBird>());

            foreach (Transform block in blocksTransform)
            {
                ABPig pig = block.GetComponent<ABPig>();
                if (pig != null)
                {
                    _pigs.Add(pig);
                }

            }

        }
        else
        {
            // if the level is loaded from the level generator, skip this line of code
            if (!gameWordlLoadedFromLevelGenerator)
            {
                currentLevel = LevelList.Instance.GetCurrentLevel();
            }

            if (currentLevel != null)
            {
                // check whether the novelty type 3 is used

                CleanCache();
                if (NOVELTIES != null)
                {
                    NOVELTIES.Unload(true);
                    //Debug.Log("asset bundle unloaded");
                }

                if (File.Exists(currentLevel.assetBundleFilePath))
                {
                    NOVELTIES = AssetBundle.LoadFromFile(currentLevel.assetBundleFilePath);
                }
                ClearWorld();

                try
                {
                    TextAsset configFile = NOVELTIES.LoadAsset("level_3_config.txt") as TextAsset;
                    string[] configFileData = configFile.text.Split('\n');

                    // extract the novelty type from the file
                    string noveltyTypeString = Regex.Match(configFileData[0], @"\d+").Value;
                    noveltyTypeForNovelty3 = int.Parse(noveltyTypeString);
                    // UnityEngine.Debug.Log("Level 3 Novelty Type From Asset Bundle: " + noveltyTypeForNovelty3);


                }
                catch (System.Exception)
                {
                    // UnityEngine.Debug.Log("No any evidence for novelty type 3 in the asset bundle");
                    noveltyTypeForNovelty3 = -1;
                }

                DecodeLevel(currentLevel);
                AdaptCameraWidthToLevel();

                _levelTimesTried = 0;

                slingshotBaseTransform = GameObject.Find("slingshot_base").transform;
            }
        }
        int height = Screen.height;
        int width = Screen.width;

        Screen.SetResolution(640, 480, false);

        // immortalize all the blocks to avoid destroying them
        ImmortalizeBlocks();

        // loading the level is done
        nextLevelAlreadyLoading = false;

        UnityEngine.Debug.Log("gameworld loaded: " + SceneManager.GetActiveScene().buildIndex);
    }


    // Use this for initialization
    void Start()
    {
        // if play bot is on, read the solution from the file
        if (playBotSwitch)
        {
            SolutionPlaySchema();
        }
    }

    public void DecodeLevel(ABLevel currentLevel)
    {
        //System.Console.WriteLine("=== entering ABGameWorld.DecodeLevel() ===");

        isBannerShowing = false;
        CleanCache();

        //DCHEN begin
        // if (NOVELTIES != null) {
        //     NOVELTIES.Unload(true);
        //     UnityEngine.Debug.Log("old asset bundle unloaded");
        // }

        //System.Console.WriteLine("=== ABGameWorld.DecodeLevel(): Debug 1 ===");

        // NOVELTIES = AssetBundle.LoadFromFile(currentLevel.assetBundleFilePath);
        // if (NOVELTIES != null) {
        //     UnityEngine.Debug.Log("new asset bundle loaded");
        //     //var prefab = myLoadedAssetBundle.LoadAsset<GameObject>("MyObject");
        //     //Instantiate(prefab);
        // }

        if (NOVELTIES == null && File.Exists(currentLevel.assetBundleFilePath))
        {
            //System.Console.WriteLine($"ABGameWorld.DecodeLevel(): load assetbundle \"{currentLevel.assetBundleFilePath}\"");
            NOVELTIES = AssetBundle.LoadFromFile(currentLevel.assetBundleFilePath);
        }
        //DCHEN end

        ClearWorld();

        //System.Console.WriteLine("=== ABGameWorld.DecodeLevel(): Debug 2 ===");

        LevelHeight = ABConstants.LEVEL_ORIGINAL_SIZE.y;
        LevelWidth = (float)currentLevel.width * ABConstants.LEVEL_ORIGINAL_SIZE.x;

        Vector3 cameraPos = GameplayCam.transform.position;
        cameraPos.x = currentLevel.camera.x;
        cameraPos.y = currentLevel.camera.y;
        GameplayCam.transform.position = cameraPos;

        GameplayCam._minWidth = currentLevel.camera.minWidth;
        GameplayCam._maxWidth = currentLevel.camera.maxWidth;

        Vector3 landscapePos = ABWorldAssets.LANDSCAPE.transform.position;
        Vector3 backgroundPos = ABWorldAssets.BACKGROUND.transform.position;

        //System.Console.WriteLine("=== ABGameWorld.DecodeLevel(): Debug 3 ===");

        if (currentLevel.width > 1)
        {
            landscapePos.x -= LevelWidth / 4f;
            backgroundPos.x -= LevelWidth / 4f;
        }

        for (int i = 0; i < currentLevel.width; i++)
        {
            GameObject landscape = (GameObject)Instantiate(ABWorldAssets.LANDSCAPE, landscapePos, Quaternion.identity);
            landscape.transform.parent = transform;

            float screenRate = currentLevel.camera.maxWidth / LevelHeight;
            if (screenRate > 2f)
            {

                for (int j = 0; j < (int)screenRate; j++)
                {

                    Vector3 deltaPos = Vector3.down * (LevelHeight / 1.5f + (j * 2f));
                    Instantiate(ABWorldAssets.GROUND_EXTENSION, landscapePos + deltaPos, Quaternion.identity);
                }
            }

            landscapePos.x += ABConstants.LEVEL_ORIGINAL_SIZE.x - 0.01f;

            GameObject background = (GameObject)Instantiate(ABWorldAssets.BACKGROUND, backgroundPos, Quaternion.identity);
            background.transform.parent = GameplayCam.transform;
            backgroundPos.x += ABConstants.LEVEL_ORIGINAL_SIZE.x - 0.01f;
        }

        //System.Console.WriteLine("=== ABGameWorld.DecodeLevel(): Debug 4 ===");

        //Reading the score
        LevelScore = currentLevel.score.highScore;

        Vector2 slingshotPos = new Vector2(currentLevel.slingshot.x, currentLevel.slingshot.y);
        _slingshot = (GameObject)Instantiate(ABWorldAssets.SLINGSHOT, slingshotPos, Quaternion.identity);
        _slingshot.name = "Slingshot";
        _slingshot.transform.parent = transform;

        //System.Console.WriteLine("=== ABGameWorld.DecodeLevel(): Debug 5 ===");

        foreach (BirdData gameObj in currentLevel.birds)
        {


            ////added by DCHEN begin
            //if (ABWorldAssets.BIRDS.ContainsKey(gameObj.type)) {
            //    //System.Console.WriteLine($"ABGameWorld.DecodeLevel(): add bird \"{gameObj.type}\" from ABWorldAssets.BIRDS"); 

            //    AddBird(ABWorldAssets.BIRDS[gameObj.type], ABWorldAssets.BIRDS[gameObj.type].transform.rotation);
            //} else {
            //    //System.Console.WriteLine($"ABGameWorld.DecodeLevel(): add bird \"{gameObj.type}\" from AssetBundle"); 

            //    if (UglyBird_DLL == null) {
            //        string dll_path = Application.dataPath + "/Managed/UglyBird.dll";
            //        //System.Console.WriteLine($"ABGameWorld.DecodeLevel(): load DLL: \"{dll_path}\"");
            //        UglyBird_DLL = System.Reflection.Assembly.LoadFile(dll_path);
            //    }

            //    // string dll_path = Application.dataPath + "/Managed/UglyBird.dll";
            //    // System.Console.WriteLine($"ABGameWorld.Awake(): load DLL: \"{dll_path}\"");
            //    // var dll = System.Reflection.Assembly.LoadFile(dll_path);

            //    GameObject newBird = (GameObject)NOVELTIES.LoadAsset(gameObj.type);
            //    AddBird(newBird, newBird.transform.rotation);
            //}
            //continue;
            ////added by DCHEN end



            if (!gameObj.type.Contains("novel"))
            {
                //System.Console.WriteLine("ABGameWorld.DecodeLevel(): ABWorldAssets.BIRDS.Count=" 
                //+ ABWorldAssets.BIRDS.Count + ", gameObj.type=" + gameObj.type);

                AddBird(ABWorldAssets.BIRDS[gameObj.type], ABWorldAssets.BIRDS[gameObj.type].transform.rotation);
            }

            else
            {
                try
                {
                    GameObject newBird = (GameObject)NOVELTIES.LoadAsset(gameObj.type);
                    string matrialName = "novel_material_" + gameObj.type.Split('_')[2];
                    newBird.GetComponent<PolygonCollider2D>().sharedMaterial = (PhysicsMaterial2D)NOVELTIES.LoadAsset(matrialName);
                    AddBird(newBird, newBird.transform.rotation);
                }
                catch
                {
                    AddBird(ABWorldAssets.BIRDS[gameObj.type], ABWorldAssets.BIRDS[gameObj.type].transform.rotation);
                }
            }

        }

        //System.Console.WriteLine("=== ABGameWorld.DecodeLevel(): Debug 6 ===");

        foreach (OBjData gameObj in currentLevel.pigs)
        {

            Vector2 pos = new Vector2(gameObj.x, gameObj.y);
            Quaternion rotation = Quaternion.Euler(0, 0, gameObj.rotation);


            if (!gameObj.type.Contains("novel"))
            {
                AddPig(ABWorldAssets.PIGS[gameObj.type], pos, rotation);
            }

            else
            {
                try
                {
                    GameObject newPig = (GameObject)NOVELTIES.LoadAsset(gameObj.type);
                    string matrialName = "novel_material_" + gameObj.type.Split('_')[2];
                    newPig.GetComponent<PolygonCollider2D>().sharedMaterial = (PhysicsMaterial2D)NOVELTIES.LoadAsset(matrialName);
                    AddPig(newPig, pos, rotation);
                }
                catch
                {
                    AddPig(ABWorldAssets.PIGS[gameObj.type], pos, rotation);
                }
            }

        }

        //System.Console.WriteLine("=== ABGameWorld.DecodeLevel(): Debug 7 ===");

        foreach (BlockData gameObj in currentLevel.blocks)
        {

            Vector2 pos = new Vector2(gameObj.x, gameObj.y);
            Quaternion rotation = Quaternion.Euler(0, 0, gameObj.rotation);

            if (!gameObj.type.Contains("novel"))
            {
                GameObject block = AddBlock(ABWorldAssets.BLOCKS[gameObj.type], pos, rotation);

                MATERIALS material = (MATERIALS)System.Enum.Parse(typeof(MATERIALS), gameObj.material);

                block.GetComponent<ABBlock>().SetMaterial(material);

            }

            else
            {
                GameObject newBlock = (GameObject)NOVELTIES.LoadAsset(gameObj.type);
                GameObject block = AddBlock(newBlock, pos, rotation);

                string materialName = "novel_material_" + gameObj.type.Split('_')[2];

                block.GetComponent<ABBlock>().SetMaterial(MATERIALS.novelty, materialName);
            }


        }

        //System.Console.WriteLine("=== ABGameWorld.DecodeLevel(): Debug 8 ===");

        foreach (PlatData gameObj in currentLevel.platforms)
        {

            Vector2 pos = new Vector2(gameObj.x, gameObj.y);
            Quaternion rotation = Quaternion.Euler(0, 0, gameObj.rotation);

            if (!gameObj.type.Contains("novel"))
            {
                AddPlatform(ABWorldAssets.PLATFORM, pos, rotation, gameObj.scaleX, gameObj.scaleY);
            }
            else
            {
                GameObject newPlatform = (GameObject)NOVELTIES.LoadAsset(gameObj.type);
                string matrialName = "novel_material_" + gameObj.type.Split('_')[2];
                newPlatform.GetComponent<Rigidbody2D>().sharedMaterial = (PhysicsMaterial2D)NOVELTIES.LoadAsset(matrialName);
                AddPlatform(newPlatform, pos, rotation, gameObj.scaleX, gameObj.scaleY);

            }

        }

        //System.Console.WriteLine("=== ABGameWorld.DecodeLevel(): Debug 9 ===");

        foreach (OBjData gameObj in currentLevel.tnts)
        {

            Vector2 pos = new Vector2(gameObj.x, gameObj.y);
            Quaternion rotation = Quaternion.Euler(0, 0, gameObj.rotation);

            AddBlock(ABWorldAssets.TNT, pos, rotation);
        }

        foreach (ExternalAgentData gameObj in currentLevel.externalagents)
        {

            Vector2 pos = new Vector2(gameObj.x, gameObj.y);
            Quaternion rotation = Quaternion.Euler(0, 0, gameObj.rotation);

            if (!gameObj.type.Contains("novel"))
            {
                GameObject externalAgent = AddExternalAgent(ABWorldAssets.EXTERNALAGENTS[gameObj.type], pos, rotation);

            }

            else
            {
                try
                {
                    GameObject newExternalAgent = (GameObject)NOVELTIES.LoadAsset(gameObj.type);
                    GameObject externalAgent = AddExternalAgent(newExternalAgent, pos, rotation);

                    string matrialName = "novel_material_" + gameObj.type.Split('_')[2];

                    externalAgent.GetComponent<Rigidbody2D>().sharedMaterial = (PhysicsMaterial2D)NOVELTIES.LoadAsset(matrialName);
                }
                catch // when the external agent has novel_object_x name but the game object is not reading from the assetbundle
                {
                    GameObject externalAgent = AddExternalAgent(ABWorldAssets.EXTERNALAGENTS[gameObj.type], pos, rotation);
                }
            }


        }

        foreach (NoveltyData gameObj in currentLevel.novelties)
        {

            Vector2 pos = new Vector2(gameObj.x, gameObj.y);
            Quaternion rotation = Quaternion.Euler(0, 0, gameObj.rotation);

            if (gameObj.type.Contains("Force")) // novelties used in the novelty generator has Force in their name, those novelties comes with an attribute called forceMagnitude
            {
                GameObject novelty = AddNovelty(ABWorldAssets.NOVELTIES[gameObj.type], pos, rotation, gameObj.scaleX, gameObj.scaleY, gameObj.forceMagnitude);
                UnityEngine.Debug.Log("Force related novel object: " + pos + rotation + " scaleX: " + gameObj.scaleX + " scaleY: " + gameObj.scaleY + " forceMagnitude: " + gameObj.forceMagnitude);
            }
            else
            {
                GameObject novelty = AddNovelty(ABWorldAssets.NOVELTIES[gameObj.type], pos, rotation, gameObj.scaleX, gameObj.scaleY);
            }

        }

        //        GameObject wizardTNT= AddExternalAgent(ABWorldAssets.EXTERNALAGENTS["NovelWizardTNT"], new Vector3(3.03f, 4.2656f,0f), Quaternion.identity);
        //GameObject wizardTNT= AddExternalAgent(ABWorldAssets.EXTERNALAGENTS["NovelBirdShooter"], new Vector3(-6.03f, 2.2656f,0f), Quaternion.identity);
        StartWorld();

        //System.Console.WriteLine("=== exiting  ABGameWorld.DecodeLevel() ===");
    }

    private void ImmortalizeBlocks() {

        // immortalize all the blocks to avoid distruction
        foreach (ABBlock block in _blocks)
        {
            block.setCurrentLife(100000);
        }

    }
    private void SolutionPlaySchema()
    {
        /*
         The procedure for playing the solution of the level
         */


        SolutionVerifier.Instance.setCurrentScenrioID(currentLevel.id.scenarioID);

        // create a new level play outcome object to save the level play data to write into the csv
        outcomeRecorder = new LevelPlayOutcome();
        outcomeRecorder.scenarioID = currentLevel.id.scenarioID;

        // first immortalize all the blocks (as the levels are generated by first immortalizing them)
        foreach (ABBlock block in _blocks)
        {
            block.setCurrentLife(100000);
        }

        // get the release angle of the solution
        // float releaseAngle = GetSolutionReleaseAngle();
        float releaseAngle = SolutionVerifier.Instance.getCurrentShootingAngle();

        // play it
        playSolution(releaseAngle);
    }

    private float GetSolutionReleaseAngle()
    {
        /*
            This function reads the solution of the current level from its info file 
        */

        // find the info file of the current level using the level ID
        UnityEngine.Debug.Log("current level ID: " + currentLevel.id.scenarioID);

        // Load the levelinfo XML file
        XDocument xmlDoc = XDocument.Load(levelInfoFolder + currentLevel.id.scenarioID + "_info.xml");

        // Get the value of a sub-node (solution->releaseAngle)
        string releaseAngle = xmlDoc.Descendants("solution").Elements("releaseAngle").FirstOrDefault()?.Value;
        UnityEngine.Debug.Log("releaseAngle: " + releaseAngle);

        // save important infomation into the outcomeRecorder
        AddSolutionTargetInfoToTheOutcomeRecorder(xmlDoc);
        outcomeRecorder.isSolutionTarget = true; // this function returns a release angle to the solution target
        outcomeRecorder.releaseAngle = releaseAngle;
        outcomeRecorder.trajectory = xmlDoc.Descendants("solution").Elements("trajectory").FirstOrDefault()?.Value;

        return float.Parse(releaseAngle);
    }

    private void AddSolutionTargetInfoToTheOutcomeRecorder(XDocument xmlDoc)
    {
        /*
            This function adds the solution's target object's info to the outcomeRecorder
        */
        outcomeRecorder.targetObject = xmlDoc.Descendants("solution").Elements("targetObject").Elements("scenarioObjectName").FirstOrDefault()?.Value;
        outcomeRecorder.targetObjectPosition = xmlDoc.Descendants("solution").Elements("targetObject").Elements("position").Elements("x").FirstOrDefault()?.Value + " " +
            xmlDoc.Descendants("solution").Elements("targetObject").Elements("position").Elements("y").FirstOrDefault()?.Value;
    }


    private void WriteOutcomeFile()
    {
        /*
            This function writes the outcome of the gameplay in to the csv file
        */

        // Create or append a line to the outcomeRecorderFile
        StreamWriter writer = File.AppendText(outcomeRecorderFile);

        // Convert the outcomeRecorder object parameters to comma-separated string
        string csvLine = string.Join(",", outcomeRecorder.scenarioID, outcomeRecorder.targetObject, outcomeRecorder.targetObjectPosition,
            outcomeRecorder.isSolutionTarget, outcomeRecorder.releaseAngle, outcomeRecorder.trajectory, outcomeRecorder.isPassed);

        // Write the string to the file
        writer.WriteLine(csvLine);
        writer.Close();

    }


    private void playSolution(float shootingAngle)
    {
        /*
            given the release angle of the bird, this function shoots the bird 
        */

        float v = 9.65f; // velocity applied on the bird when the sling is fully stretched

        // set the parameters of bird shooting
        _birds[0].IsFlying = true;
        _birds[0].IsSelected = false;

        // The bird starts with no gravity, so we must set it (_birds[0] is the bird on the sling)
        _birds[0].GetComponent<Rigidbody2D>().velocity = new Vector2(v * Mathf.Cos(shootingAngle), v * Mathf.Sin(shootingAngle));
        _birds[0].GetComponent<Rigidbody2D>().gravityScale = _birds[0].GetComponent<ABBird>()._launchGravity;

        // set the layer back to birds to enable collisions
        gameObject.layer = 8;


    }

    private IEnumerator GTTest(bool useNoise)
    {
        //System.Console.WriteLine("=== entering ABGameWorld.GTTest() ===");

        yield return new WaitForEndOfFrame();
        string savePath = "./gt/";
        Stopwatch stopwatch_overall = new Stopwatch();
        stopwatch_overall.Reset();
        stopwatch_overall.Start();
        SymbolicGameState gt = new SymbolicGameState(useNoise, false);
        //string screenimage = gt.GetScreenshotStr();
        string gtdebug = gt.GetGTJson();
        UnityEngine.Debug.Log("total: " + stopwatch_overall.ElapsedTicks + " ms: " + stopwatch_overall.ElapsedMilliseconds);
        System.IO.File.WriteAllText(savePath + "GTData.json", gtdebug);
    }


    void FixedUpdate()
    {
        for (int i = 0; i < SimulationSpeed; i++)
        {
            Physics2D.Simulate(Time.fixedDeltaTime);
            simulationID++;

            ABBird currentBird = GetCurrentBird();

            if (currentBird != null)
            {
                ABParticleSystem currentBirdParticleSystem = currentBird.getBirdParticalSystem();
                if (currentBirdParticleSystem._shootParticles)
                {
                    if (currentBirdParticleSystem._systemLifetime > 0)
                    {
                        currentBirdParticleSystem._selfDestructionTimer += Time.fixedDeltaTime;
                        if (currentBirdParticleSystem._selfDestructionTimer >= currentBirdParticleSystem._systemLifetime)
                        {
                            currentBirdParticleSystem._shootParticles = false;
                            ABGameWorld.displayTrajectory = false;
                        }
                    }

                    currentBirdParticleSystem._shootingTimer += Time.fixedDeltaTime;

                    if (currentBirdParticleSystem._shootingTimer >= currentBirdParticleSystem._shootingRate)
                    {

                        currentBirdParticleSystem.ShootParticle();
                        currentBirdParticleSystem._shootingTimer = 0f;
                    }
                }

            }



            if (wasBirdLaunched == true && isRecordingBatchGroundTruthAfterShooting == true)
            {
                if (framesPassedSinceLastTake >= takeGroundTruthEveryNthFrames)
                {
                    // Every n-frames take ground truth once the shot is made
                    //UnityEngine.Debug.Log("recording a frame");
                    bool devMode = LoadLevelSchema.Instance.devMode;
                    bool noisyBatchGT = LoadLevelSchema.Instance.noisyBatchGT;
                    SymbolicGameState gt;
                    bool updateNovelList = false;

                    //if the option from batch gt request is BIRD_BEFORE_COLLISION,
                    //record until the flying bird collides with other objects
                    if (currentBird != null && batchGTOption == RecordBatchGTOption.BIRD_BEFORE_COLLISION && currentBird.IsCollided)
                    {
                        //under the above condition
                        //any frames after bird's collision will not be recorded
                        //but the pause time will still be the same as normal batch gt
                        framesPassedSinceLastTake = 1;
                        frameRecorded += 1;
                    }
                    else
                    {
                        //only update novel id list from the first frame of batch gts 
                        if (batchGroundTruths.Count == 0)
                        {
                            updateNovelList = true;
                        }
                        if (noisyBatchGT)
                        {
                            //always use noisy batch gt if noisyBatchGT set to true 
                            gt = new SymbolicGameState(true, updateNovelList);
                        }
                        else
                        {
                            gt = new SymbolicGameState(!devMode, updateNovelList);
                        }

                        string gtjson = gt.GetGTJson();
                        //do not take screenshot in this version for efficiency
                        //string image = gt.GetScreenshotStr();
                        string image = "";
                        batchGroundTruths.Add((image, gtjson));

                        //only update novel id list from the first frame of batch gts 
                        if (batchGroundTruths.Count == 1)
                        {
                            EvaluationHandler.novelIDList = gt.novelIDList;
                        }
                        framesPassedSinceLastTake = 1; // 1 since every n-th frame
                        frameRecorded += 1;

                    }
                }
                else
                {
                    framesPassedSinceLastTake += 1;
                }
            }

            else if (isRecordingBatchGroundTruth)
            {
                if (framesPassedSinceLastTake >= takeGroundTruthEveryNthFrames)
                {
                    bool updateNovelList = false;
                    bool devMode = LoadLevelSchema.Instance.devMode;
                    bool noisyBatchGT = LoadLevelSchema.Instance.noisyBatchGT;
                    SymbolicGameState gt;
                    //only update novel id list from the first frame of batch gts 
                    if (batchGroundTruths.Count == 0)
                    {
                        updateNovelList = true;
                    }
                    if (noisyBatchGT)
                    {
                        //always use noisy batch gt if noisyBatchGT set to true 
                        gt = new SymbolicGameState(true, updateNovelList);
                    }
                    else
                    {
                        gt = new SymbolicGameState(!devMode, updateNovelList);
                    }

                    string gtjson = gt.GetGTJson();
                    //do not take screenshot in this version for efficiency
                    //string image = gt.GetScreenshotStr();
                    string image = "";
                    batchGroundTruths.Add((image, gtjson));

                    //only update novel id list from the first frame of batch gts 
                    if (batchGroundTruths.Count == 1)
                    {
                        EvaluationHandler.novelIDList = gt.novelIDList;
                    }
                    framesPassedSinceLastTake = 1; // 1 since every n-th frame
                    frameRecorded += 1;
                }
                else
                {
                    framesPassedSinceLastTake += 1;
                }
            }



            //            UnityEngine.Debug.Log("ABGameWorld.FixedUpdate: frame recorded is " + frameRecorded);
            // Check if the level is stable and stop recording the ground truth if needed
            if (((IsLevelStable() && wasBirdLaunched) || frameRecorded >= maxBatchGTFrames) && isRecordingBatchGroundTruthAfterShooting)
            {
                wasBirdLaunched = false;
                framesPassedSinceLastTake = 1; // reset
                isRecordingBatchGroundTruthAfterShooting = false;
                //                UnityEngine.Debug.Log("ABGameWorld.FixedUpdate: Finished recording batch gt after shooting");
                frameRecorded = 0;
                //pause the game if no Win or Lose condition is met
                if (ABGameWorld.Instance._birds.Count > 0 && ABGameWorld.Instance._pigs.Count > 0)
                {
                    Time.timeScale = 0.0f;
                }
            }

            // record exactly the number of frames requested
            if (frameRecorded >= maxBatchGTFrames && isRecordingBatchGroundTruth)
            {
                framesPassedSinceLastTake = 1; // reset
                isRecordingBatchGroundTruth = false;
                //                UnityEngine.Debug.Log("ABGameWorld.FixedUpdate: Finished recording batch gt");
                frameRecorded = 0;
                //pause the game if no Win or Lose condition is met
                if (ABGameWorld.Instance._birds.Count > 0 && ABGameWorld.Instance._pigs.Count > 0)
                {
                    Time.timeScale = 0.0f;
                }
            }

            // Check if birds was thrown, if it died and swap them when needed
            ManageBirds();
            TakeAction();
        }
    }

    private float trajTimer;
    // Update is called once per frame

    void Update()
    {
        //System.Console.WriteLine("=== entering ABGameWorld.Update() ===");

        //string gtdebug = gttest.GetGTJson();



        if (Input.GetKeyDown(KeyCode.Return))
        {
            string folderPath = "Assets/StreamingAssets/Screenshots/"; // the path of your project folder

            if (!System.IO.Directory.Exists(folderPath)) // if this path does not exist yet
                System.IO.Directory.CreateDirectory(folderPath);  // it will get created

            var screenshotName =
                                    "Screenshot_" +
                                    System.DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss") + // puts the current time right into the screenshot name
                                    ".png"; // put youre favorite data format here
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folderPath, screenshotName), 1); // takes the sceenshot, the "2" is for the scaled resolution, you can put this to 600 but it will take really long to scale the image up
            UnityEngine.Debug.Log(folderPath + screenshotName); // You get instant feedback in the console
        }

        //if (Input.GetKeyDown(KeyCode.Return))
        //{
        //    bool useNoise = true;
        //    StartCoroutine(GTTest(useNoise));
        //}

        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    bool useNoise = false;
        //    StartCoroutine(GTTest(useNoise));
        //}

    }

    public bool IsObjectOutOfWorld(Transform abGameObject, Collider2D abCollider)
    {

        //System.Console.WriteLine("=== entering ABGameWorld.IsObjectOutOfWorld() ===");

        Vector2 halfSize = abCollider.bounds.size / 2f;

        if (abGameObject.position.x - halfSize.x > LevelWidth / 2f ||
           abGameObject.position.x + halfSize.x < -LevelWidth / 2f)

            return true;

        return false;
    }

    void ManageBirds()
    {
        //System.Console.WriteLine("=== entering ABGameWorld.ManageBirds() ===");

        if (_birds.Count == 0)
            return;

        // Move next bird to the slingshot
        if (_birds[0].JumpToSlingshot)
            _birds[0].SetBirdOnSlingshot();

        //		int birdsLayer = LayerMask.NameToLayer("Birds");
        //		int blocksLayer = LayerMask.NameToLayer("Blocks");
        //		if(_birds[0].IsFlying || _birds[0].IsDying)
        //			
        //			Physics2D.IgnoreLayerCollision(birdsLayer, blocksLayer, false);
        //		else 
        //			Physics2D.IgnoreLayerCollision(birdsLayer, blocksLayer, true);
    }

    public ABBird GetCurrentBird()
    {
        //System.Console.WriteLine("=== entering ABGameWorld.GetCurrentBird() ===");

        if (_birds.Count > 0)
            return _birds[0];

        return null;
    }

    public void RecordOutcomeAndGoToNextLevel()
    {
        /*
         This function record the outcome of the current play and loads the next level for the playbot
         */

        if (!nextLevelAlreadyLoading) // to avoid multiple callings of this function when it is already called (happens due to 'invokings')
        {
            nextLevelAlreadyLoading = true;

            // first record the outcome of the current play
            SolutionVerifier.Instance.WriteOutcomeFile(outcomeRecorder, outcomeRecorderFile);

            // now go to the next level (/play)
            isBannerShowing = false;

            if (SolutionVerifier.Instance.determineTheNextPlay()) // true means go to next next level
            {
                if (LevelList.Instance.NextLevel() == null)
                    ABSceneManager.Instance.LoadScene("MainMenu");
                else
                    ABSceneManager.Instance.LoadScene(SceneManager.GetActiveScene().name);
            }
            else // false means reload the existing level, so another shooting angle will be tried
            {
                ABSceneManager.Instance.LoadScene(SceneManager.GetActiveScene().name);
            }
        }


    }

    public void ResetLevel()
    {
        //System.Console.WriteLine("=== entering ABGameWorld.ResetLevel() ===");

        isBannerShowing = false;
        if (_levelFailedBanner.activeSelf)
            _levelTimesTried++;

        ABSceneManager.Instance.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void AddTrajectoryParticle(ABParticle trajectoryParticle)
    {
        //System.Console.WriteLine("=== entering ABGameWorld.AddTrajectoryParticle() ===");

        _birdTrajectory.Add(trajectoryParticle);
    }

    public void RemoveLastTrajectoryParticle()
    {
        //System.Console.WriteLine("=== entering ABGameWorld.RemoveLastTrajectoryParticle() ===");

        foreach (ABParticle part in _birdTrajectory)
            part.Kill();
    }

    public void AddBird(ABBird readyBird)
    {
        //System.Console.WriteLine("=== entering ABGameWorld.AddBird(1) ===");

        if (_birds.Count == 0)
            readyBird.GetComponent<Rigidbody2D>().gravityScale = 0f;

        if (readyBird != null)
            _birds.Add(readyBird);
    }

    public GameObject AddBird(GameObject original, Quaternion rotation)
    {

        //System.Console.WriteLine("=== entering ABGameWorld.AddBird(2) ===");

        Vector3 birdsPos = _slingshot.transform.position - ABConstants.SLING_SELECT_POS;

        if (_birds.Count >= 1)
        {

            birdsPos.y = _slingshot.transform.position.y;

            for (int i = 0; i < _birds.Count; i++)
            {

                if ((i + 1) % _birdsAmounInARow == 0)
                {

                    float coin = (Random.value < 0.5f ? 1f : -1);
                    birdsPos.x = _slingshot.transform.position.x + (Random.value * 0.5f * coin);
                }

                birdsPos.x -= original.GetComponent<SpriteRenderer>().bounds.size.x * 1.75f;
            }
        }

        GameObject newGameObject = (GameObject)Instantiate(original, birdsPos, rotation);
        Vector3 scale = newGameObject.transform.localScale;
        scale.x = original.transform.localScale.x;
        scale.y = original.transform.localScale.y;
        newGameObject.transform.localScale = scale;

        newGameObject.transform.parent = birdsTransform;
        newGameObject.name = "bird_" + _birds.Count;

        ABBird bird = newGameObject.GetComponent<ABBird>();
        bird.SendMessage("InitSpecialPower", SendMessageOptions.DontRequireReceiver);

        if (_birds.Count == 0)
            bird.GetComponent<Rigidbody2D>().gravityScale = 0f;

        if (bird != null)
            _birds.Add(bird);

        return newGameObject;
    }

    public GameObject AddPig(GameObject original, Vector3 position, Quaternion rotation, float scale = 1f)
    {
        //System.Console.WriteLine("=== entering ABGameWorld.AddPig() ===");

        GameObject newGameObject = AddBlock(original, position, rotation, scale);

        ABPig pig = newGameObject.GetComponent<ABPig>();
        if (pig != null)
            _pigs.Add(pig);

        return newGameObject;
    }

    public GameObject AddPlatform(GameObject original, Vector3 position, Quaternion rotation, float scaleX = 1f, float scaleY = 1f)
    {
        //System.Console.WriteLine("=== entering ABGameWorld.AddPlatform() ===");

        GameObject platform = AddBlock(original, position, rotation, scaleX, scaleY);
        platform.transform.parent = plaftformsTransform;

        _platforms.Add(platform);

        return platform;
    }

    public GameObject AddBlock(GameObject original, Vector3 position, Quaternion rotation, float scaleX = 1f, float scaleY = 1f)
    {
        //System.Console.WriteLine("=== entering ABGameWorld.AddBlock() ===");

        GameObject newGameObject = (GameObject)Instantiate(original, position, rotation);
        newGameObject.transform.parent = blocksTransform;

        Vector3 newScale = newGameObject.transform.localScale;
        newScale.x = scaleX;
        newScale.y = scaleY;
        newGameObject.transform.localScale = newScale;

        ABBlock block = newGameObject.GetComponent<ABBlock>();
        if (block != null) // normal block
        {
            _blocks.Add(block);
            // UnityEngine.Debug.Log("added a block");

        }

        return newGameObject;
    }

    public GameObject AddExternalAgent(GameObject original, Vector3 position, Quaternion rotation, float scaleX = 1f, float scaleY = 1f)
    {

        GameObject newGameObject = (GameObject)Instantiate(original, position, rotation);
        newGameObject.transform.parent = externalAgentsTransform;

        Vector3 newScale = newGameObject.transform.localScale;
        newScale.x = scaleX;
        newScale.y = scaleY;
        newGameObject.transform.localScale = newScale;

        return newGameObject;
    }


    public GameObject AddNovelty(GameObject original, Vector3 position, Quaternion rotation, float scaleX = 1f, float scaleY = 1f)
    {

        GameObject newGameObject = (GameObject)Instantiate(original, position, rotation);
        newGameObject.transform.parent = noveltiesTransform;

        Vector3 newScale = newGameObject.transform.localScale;
        newScale.x = scaleX;
        newScale.y = scaleY;
        newGameObject.transform.localScale = newScale;

        return newGameObject;
    }

    public GameObject AddNovelty(GameObject original, Vector3 position, Quaternion rotation, float scaleX = 1f, float scaleY = 1f, float forceMagnitude = 2f)
    {

        GameObject newGameObject = (GameObject)Instantiate(original, position, rotation);
        newGameObject.transform.parent = noveltiesTransform;

        Vector3 newScale = newGameObject.transform.localScale;
        newScale.x = scaleX;
        newScale.y = scaleY;
        newGameObject.transform.localScale = newScale;

        newGameObject.GetComponent<Force>().forceMagnitude = forceMagnitude;

        return newGameObject;
    }



    private void ShowLevelFailedBanner()
    {
        //System.Console.WriteLine("=== entering ABGameWorld.ShowLevelFailedBanner() ===");

        //MaxScoreUpdate();
        if (_levelCleared)
            return;

        if (!IsLevelStable())
        {

            Invoke("ShowLevelFailedBanner", 1f);
        }
        else
        { // Player lost the game

            // avoid multiple invoking of the function adding multiple scores
            if (!BirdsScoreUpdated)
            {
                // add points to the remaining birds
                ScoreHud.Instance.SpawnScorePoint(10000 * (int)_birds.Count, transform.position);
                BirdsScoreUpdated = true;

                //For evaluation purpose
                EvaluationHandler.Instance.RecordEvaluationScore("Fail");
            }

            HUD.Instance.gameObject.SetActive(false);


            if (_levelTimesTried < _timesToGiveUp - 1)
            {

                _levelFailedBanner.SetActive(true);
                Text[] TextFieldsInBanner = _levelFailedBanner.GetComponentsInChildren<Text>();
                TextFieldsInBanner[0].text = "Level Failed!";
                TextFieldsInBanner[1].text = "Score: " + HUD.Instance.GetScore().ToString();
            }
            else
            {

                _levelClearedBanner.SetActive(true);
                Text[] TextFieldsInBanner = _levelClearedBanner.GetComponentsInChildren<Text>();
                TextFieldsInBanner[0].text = "Level Failed!";
                TextFieldsInBanner[1].text = "Score: " + HUD.Instance.GetScore().ToString();

            }
            isBannerShowing = true;

            // if playbot is playing write the output of the current play and continue to the next level
            if (playBotSwitch)
            {
                UnityEngine.Debug.Log("current play is failed moving to the next play");
                outcomeRecorder.isPassed = false;
                RecordOutcomeAndGoToNextLevel();
            }
        }
    }

    private void ShowLevelClearedBanner()
    {
        //System.Console.WriteLine("=== entering ABGameWorld.ShowLevelClearedBanner() ===");

        if (!IsLevelStable())
        {
            Invoke("ShowLevelClearedBanner", 1f);
        }
        else
        { // Player won the game

            // avoid multiple invoking of the function adding multiple scores
            if (!BirdsScoreUpdated)
            {
                // add points to the remaining birds
                ScoreHud.Instance.SpawnScorePoint(10000 * (int)_birds.Count, transform.position);
                BirdsScoreUpdated = true;

                //For evaluation purpose
                EvaluationHandler.Instance.RecordEvaluationScore("Pass");
            }

            HUD.Instance.gameObject.SetActive(false);

            _levelClearedBanner.SetActive(true);
            Text[] TextFieldsInBanner = _levelClearedBanner.GetComponentsInChildren<Text>();
            TextFieldsInBanner[0].text = "Level Cleared!";
            TextFieldsInBanner[1].text = "Score: " + HUD.Instance.GetScore().ToString();
            //MaxScoreUpdate();
            isBannerShowing = true;


        }

    }

    public void KillPig(ABPig pig)
    {
        //System.Console.WriteLine("=== entering ABGameWorld.KillPig() ===");

        _pigs.Remove(pig);

        if (_pigs.Count == 0)
        {

            // ScoreHud.Instance.SpawnScorePoint(10000*(uint)_birds.Count, transform.position); 

            // Check if player won the game
            if (!_isSimulation)
            {
                _levelCleared = true;
                Invoke("ShowLevelClearedBanner", _timeToResetLevel);

                // if playbot is playing write the output of the current play and continue to the next level
                if (playBotSwitch)
                {
                    UnityEngine.Debug.Log("current play is passed moving to the next play");
                    outcomeRecorder.isPassed = true;
                    RecordOutcomeAndGoToNextLevel();
                }

            }

            return;
        }
    }

    public void nextbirdonsling()
    {
        //System.Console.WriteLine("=== entering ABGameWorld.nextbirdonsling() ===");

        /*
        if (!IsLevelStable())
        {
            Invoke("nextbirdonsling", 1f);
        }
        else
        {
            _birds[0].GetComponent<Rigidbody2D>().gravityScale = 0f;
            _birds[0].JumpToSlingshot = true;
        }*/
        _birds[0].GetComponent<Rigidbody2D>().gravityScale = 0f;
        _birds[0].JumpToSlingshot = true;

    }

    public void KillBird(ABBird bird)
    {
        System.Console.WriteLine("=== entering ABGameWorld.KillBird() ===");

        if (!_birds.Contains(bird))
            return;

        _birds.Remove(bird);

        if (_birds.Count == 0)
        {

            // Check if player lost the game
            if (!_isSimulation)
            {
                Invoke("ShowLevelFailedBanner", _timeToResetLevel);
            }

            return;
        }
        nextbirdonsling();
    }

    public int GetPigsAvailableAmount()
    {

        return _pigs.Count;
    }

    public int GetBirdsAvailableAmount()
    {

        return _birds.Count;
    }

    public int GetBlocksAvailableAmount()
    {

        int blocksAmount = 0;

        foreach (Transform b in blocksTransform)
        {

            if (b.GetComponent<ABPig>() == null)

                for (int i = 0; i < b.GetComponentsInChildren<Rigidbody2D>().Length; i++)
                    blocksAmount++;
        }

        return blocksAmount;
    }

    public bool IsLevelStable()
    {

        return GetLevelStability() == 0f;
    }

    //calculating stability regardless of the external agents
    public float GetLevelStability()
    {
        //System.Console.WriteLine("=== entering ABGameWorld.GetLevelStability() ===");

        float totalVelocity = 0f;

        foreach (Transform b in blocksTransform)
        {

            Rigidbody2D[] bodies = b.GetComponentsInChildren<Rigidbody2D>();

            foreach (Rigidbody2D body in bodies)
            {

                if (!IsObjectOutOfWorld(body.transform, body.GetComponent<Collider2D>()))
                    totalVelocity += body.velocity.magnitude;
            }
        }

        foreach (Transform b in birdsTransform)
        {

            Rigidbody2D[] bodies = b.GetComponentsInChildren<Rigidbody2D>();

            foreach (Rigidbody2D body in bodies)
            {

                if (!IsObjectOutOfWorld(body.transform, body.GetComponent<Collider2D>()))
                    totalVelocity += body.velocity.magnitude;
            }
        }

        foreach (Transform b in externalAgentsTransform)
        {

            Rigidbody2D[] bodies = b.GetComponentsInChildren<Rigidbody2D>();

            foreach (Rigidbody2D body in bodies)
            {
                if (!IsObjectOutOfWorld(body.transform, body.GetComponent<Collider2D>()))
                {
                    //                    UnityEngine.Debug.Log("ABGameWorld.GetLevelStability : " + "found external agent and added velocity");
                    totalVelocity += body.velocity.magnitude;
                    //ad-hoc solution for considering external agnets movements
                    //will update when there is a sopiscated solution for including non-physical movements of objects
                    //totalVelocity += 1;
                }

            }
        }

        return totalVelocity;
    }

    public List<GameObject> BlocksInScene()
    {

        List<GameObject> objsInScene = new List<GameObject>();

        foreach (Transform b in blocksTransform)
            objsInScene.Add(b.gameObject);

        return objsInScene;
    }

    public Vector3 DragDistance()
    {

        Vector3 selectPos = (_slingshot.transform.position - ABConstants.SLING_SELECT_POS);
        return slingshotBaseTransform.transform.position - selectPos;
    }

    public void SetSlingshotBaseActive(bool isActive)
    {

        slingshotBaseTransform.gameObject.SetActive(isActive);
    }

    public void ChangeSlingshotBasePosition(Vector3 position)
    {

        slingshotBaseTransform.transform.position = position;
    }

    public void ChangeSlingshotBaseRotation(Quaternion rotation)
    {

        slingshotBaseTransform.transform.rotation = rotation;
    }

    public bool IsSlingshotBaseActive()
    {

        return slingshotBaseTransform.gameObject.activeSelf;
    }

    public Vector3 GetSlingshotBasePosition()
    {

        return slingshotBaseTransform.transform.position;
    }

    public void StartWorld()
    {
        //System.Console.WriteLine("=== entering ABGameWorld.StartWorld() ===");

        _pigsAtStart = GetPigsAvailableAmount();
        _birdsAtStart = GetBirdsAvailableAmount();
        _blocksAtStart = GetBlocksAvailableAmount();

        //For evaluation purposes
        //EvaluationHandler.Instance.RecordStartData();
    }

    public void ClearWorld()
    {
        //System.Console.WriteLine("=== entering ABGameWorld.ClearWorld() ===");

        foreach (Transform b in blocksTransform)
            Destroy(b.gameObject);

        _pigs.Clear();

        foreach (Transform b in birdsTransform)
            Destroy(b.gameObject);

        _birds.Clear();
    }

    private void AdaptCameraWidthToLevel()
    {
        //System.Console.WriteLine("=== entering ABGameWorld.AdaptCameraWidthToLevel() ===");

        Collider2D[] bodies = blocksTransform.GetComponentsInChildren<Collider2D>();

        if (bodies.Length == 0)
            return;

        // Adapt the camera to show all the blocks		
        float levelLeftBound = -LevelWidth / 2f;
        float groundSurfacePos = LevelHeight / 2f;

        float minPosX = Mathf.Infinity;
        float maxPosX = -Mathf.Infinity;
        float maxPosY = -Mathf.Infinity;

        // Get position of first non-empty stack
        for (int i = 0; i < bodies.Length; i++)
        {
            float minPosXCandidate = bodies[i].transform.position.x - bodies[i].bounds.size.x / 2f;
            if (minPosXCandidate < minPosX)
                minPosX = minPosXCandidate;

            float maxPosXCandidate = bodies[i].transform.position.x + bodies[i].bounds.size.x / 2f;
            if (maxPosXCandidate > maxPosX)
                maxPosX = maxPosXCandidate;

            float maxPosYCandidate = bodies[i].transform.position.y + bodies[i].bounds.size.y / 2f;
            if (maxPosYCandidate > maxPosY)
                maxPosY = maxPosYCandidate;
        }

        float cameraWidth = Mathf.Abs(minPosX - levelLeftBound) +
            Mathf.Max(Mathf.Abs(maxPosX - minPosX), Mathf.Abs(maxPosY - groundSurfacePos)) + 0.5f;

        GameplayCam.SetCameraWidth(cameraWidth);
    }

    public void MaxScoreUpdate()
    {
        //System.Console.WriteLine("=== entering ABGameWorld.MaxScoreUpdate() ===");

        float totalScore = HUD.Instance.GetScore();

        //read the highScore from Xml file
        float currentScore = LevelScore;

        int currentIndex = LevelList.Instance.CurrentIndex;
        //        string[] levelFilePaths = ABLevelUpdate.levelPath.ToArray();
        string[] levelFilePaths = LoadLevelSchema.Instance.currentLevelPaths;
        if (totalScore > currentScore)
        {
            string xmldata = levelFilePaths[currentIndex];
            string levelText = LevelLoader.ReadXmlLevel(xmldata);
            string newText = "<Score highScore =\"" + totalScore.ToString() + "\">";
            string oldText = "<Score highScore =\"" + currentScore.ToString() + "\">";
            levelText = levelText.Replace(oldText, newText);

            File.WriteAllText(xmldata, levelText);
        }

    }


    public static void CleanCache()
    {
        //System.Console.WriteLine("=== entering ABGameWorld.CleanCache() ===");

        if (Caching.ClearCache())
        {
            //UnityEngine.Debug.Log("Successfully cleaned the cache.");
        }
        else
        {
            //UnityEngine.Debug.Log("Cache is being used.");
        }
    }

    public void TakeAction()
    {

        bool isMouseControlling = true;
        float simulateDragTime = 1;
        Vector3 _inputPos = Input.mousePosition;

        if (HUD.Instance.SimulateInputEvent > 0)
        {
            _inputPos = HUD.Instance.SimulateInputPos;

            //disable mouse interaction when the agent is playing
            isMouseControlling = false;
        }

        if (HUD.Instance.SimulateInputEvent == 1)
        {
            //To ensure the human player still can play the game
            //Unpaused when mouse click is detected
            Time.timeScale = 1.0f;
            HUD.Instance.ClickDown(_inputPos);

            if (!isMouseControlling)
                HUD.Instance.SimulateInputEvent++;
        }
        else if (HUD.Instance.SimulateInputEvent == 2)
        {

            for (int i = 0; i < ABGameWorld.SimulationSpeed; i++)
            {
                if (HUD.Instance.SimulateInputEvent > 0 && !isMouseControlling)
                    HUD.Instance.Drag(HUD.Instance.SimulateInputDelta);
                else
                    HUD.Instance.Drag(_inputPos);
            }
            HUD.Instance.simulatedDragTimer += Time.fixedDeltaTime;
            if (HUD.Instance.simulatedDragTimer >= simulateDragTime)
            {

                if (!isMouseControlling)
                    HUD.Instance.SimulateInputEvent++;

                HUD.Instance.simulatedDragTimer = 0f;
            }
        }
        else if (HUD.Instance.SimulateInputEvent == 3)
        {
            HUD.Instance.ClickUp();

            if (!isMouseControlling)
            {
                HUD.Instance.tapTimer = 0;
                HUD.Instance.SimulateInputEvent++;
            }
        }
        else if (HUD.Instance.SimulateInputEvent >= 4)
        {

            //tap time is in ms, we need to convert fixedDeltaTime to ms too
            HUD.Instance.tapTimer += Time.fixedDeltaTime * 1000;


            // Wait for tap time
            //do not tap if tap time <= 0
            if (HUD.Instance.SimulatedTapTime <= 0)
            {
                HUD.Instance.SimulateInputEvent = 0;
                HUD.Instance.shootDone = true;
            }
            else if (HUD.Instance.tapTimer >= HUD.Instance.SimulatedTapTime)
            {
                //              UnityEngine.Debug.Log("in tap time block");
                if (HUD.Instance.selectedBird && HUD.Instance.selectedBird.IsInFrontOfSlingshot() &&
                HUD.Instance.selectedBird == ABGameWorld.Instance.GetCurrentBird() &&
                !HUD.Instance.selectedBird.IsDying && !HUD.Instance.usedSpecialPower)
                {
                    HUD.Instance.usedSpecialPower = true;
                    HUD.Instance.selectedBird.SendMessage("SpecialAttack", SendMessageOptions.DontRequireReceiver);

                }

                HUD.Instance.SimulateInputEvent = 0;
                HUD.Instance.shootDone = true;
            }

        }

        string currentScence = SceneManager.GetActiveScene().name;

        if (currentScence == "GameWorld")
        {

            if (ABGameWorld.Instance.LevelCleared() || ABGameWorld.Instance.LevelFailed())
            {
                HUD.Instance.SimulateInputEvent = 0;
                HUD.Instance.shootDone = true;
            }
        }
    }

    public bool getLevelClearedStatus()
    { //25/04/2022 chathura: added this function to access the level cleared status from outside of this script
        return _levelCleared;
    }

}
