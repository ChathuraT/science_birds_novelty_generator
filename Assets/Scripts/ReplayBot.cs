using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class ReplayBot : ABSingleton<ReplayBot>
{
    public string replayLogPath;
    private StreamReader logFile;
    bool requestLock = false;
    bool isReplaying;
    
    private void Init()
    {
        isReplaying = false;
        replayLogPath = null;

        string[] args = System.Environment.GetCommandLineArgs();

        //System.Console.WriteLine("Start(): args.Length=" + args.Length);

        int replayArgPos = Array.IndexOf(args, "--replay");
        if (replayArgPos > -1)
        {

            if (args.Length > replayArgPos + 1)
            {
                replayLogPath = args[replayArgPos + 1];
                if (!File.Exists(replayLogPath))
                {
                    UnityEngine.Debug.LogError("The replay log file not existed, the game will exit");
                    Application.Quit();
                }
                else {
                    isReplaying = true;
                }
            }
            else
            {
                UnityEngine.Debug.LogError("The replay log path is not specified, the game will exit");
                Application.Quit();
            }
            // in replay mode
            // update args with the first line in replay mode
            this.logFile = File.OpenText(replayLogPath);

        }
    }

    IEnumerator Start(){
        yield return new WaitForSecondsRealtime(3);
        Init();
        string line;
        //The first line has been read by the InitGameWithLogArgs function.
        //The starting line in the while loop should be the 
        //second line which contains the request from the agent.
        while (isReplaying) {

            while(requestLock)
            {
                UnityEngine.Debug.Log("wait for lock");
                yield return new WaitForSecondsRealtime(1);
            }

            line = logFile.ReadLine();
            if (line == null) {
                UnityEngine.Debug.Log("replay finished!");
                break;
            }
            else {
                UnityEngine.Debug.Log(line);
            }
            string[] args = line.Split(',');
            string request = args[0];

            switch (this.GetGameState()) {
                case "NoveltyLikelihoodReportRequest":
                    this.ReportNoveltyLikelihood();
                    UnityEngine.Debug.Log("ReportNoveltyLikelihood " + this.GetGameState());
                    break;
                default:
                    break;
            }


            switch (request) {
                case "ReadyForNewSet":
                    this.ReadyForNewSet();
                    UnityEngine.Debug.Log("ReadyForNewSet " + this.GetGameState());
                    break;
                case "ShootAndRecordGroundTruth":
                    StartCoroutine(this.ShootAndRecord(args));
                    UnityEngine.Debug.Log("ShootAndRecordGroundTruth " + this.GetGameState());
                    break;
                case "TapShoot":
                    this.TapShoot(args);
                    UnityEngine.Debug.Log("TapShoot " + this.GetGameState());
                    break;
                case "ReportNoveltyLikelihood":
                    this.ReportNoveltyLikelihood();
                    UnityEngine.Debug.Log("ReportNoveltyLikelihood " + this.GetGameState());
                    break;
                case "SpeedUpGameSimulation":
                    this.SpeedUpGameSimulation(args);
                    UnityEngine.Debug.Log("SpeedUpGameSimulation " + this.GetGameState());
                    break;
                case "SelectNextAvailableLevel":
                    StartCoroutine(this.LoadNextAvailableLevel());
                    UnityEngine.Debug.Log("SelectNextAvailableLevel " + this.GetGameState());
                    break;
                default:
                    UnityEngine.Debug.LogWarning("request not recognised " + this.GetGameState());
                    break;
            }
        }
    }


    private void SpeedUpGameSimulation(string[] args) {
        float simSpeed = float.Parse(args[2]);

        UnityEngine.Debug.Log("SIM_SPEED = " + simSpeed);

        // Make sure speed is within Unity boundaries 0-50
        //String optionalError = "{}";
        if (simSpeed > 50.0f)
        {
            simSpeed = 50.0f;
            //    optionalError = "{\"error\": \"Simulation speed must be within 0.0 to 50.0.\"}";
        }
        else if (simSpeed <= 0.0f)
        {
            simSpeed = 0.00001f; // setting speed to 0 will freeze the game completely
                                 // optionalError = "{\"error\": \"Simulation speed must be within 0.00001 to 50.0.\"}";
        }

        ABGameWorld.SimulationSpeed = simSpeed;

    }

    private IEnumerator ShootAndRecord(string[] args) {
        //ensure the number of args is greater than 8
        requestLock = true;
        Assert.IsTrue(args.Length>8);

        LoadLevelSchema.Instance.updateLikelihoodRequestFlag();
        float dragX = float.Parse(args[2]);
        float dragY = float.Parse(args[3]);

        float dragDX = float.Parse(args[4]);
        float dragDY = float.Parse(args[5]);

        float tapTime = float.Parse(args[6]);

        int groundTruthTakeFrequency = int.Parse(args[7]);
        int batchGTOption = int.Parse(args[8]);

        ABGameWorld.takeGroundTruthEveryNthFrames = groundTruthTakeFrequency;
        ABGameWorld.batchGTOption = (ABGameWorld.RecordBatchGTOption)batchGTOption;

        // Scale tapTime by time 
        tapTime = tapTime / ABGameWorld.SimulationSpeed;

        Vector2 dragPos = new Vector2(dragX, dragY);
        Vector2 deltaPos = new Vector2(dragDX, Screen.height - dragDY);

        //unpause the game
        //            UnityEngine.Debug.Log("AIBirdsConnnection::ShootAndRecordGroundTruth : unpause game");
        Time.timeScale = 1.0f;

        RepresentationNovelty.addNoveltyToActionSpaceIfNeeded(dragX, dragY, dragDX, dragDY);
        HUD.Instance.shootDone = false;
        HUD.Instance.SimulateInputEvent = 1;
        HUD.Instance.SimulateInputPos = dragPos;
        HUD.Instance.SimulateInputDelta = deltaPos;
        HUD.Instance.SetTapTime(tapTime);
        // Start recording
        ABGameWorld.isRecordingBatchGroundTruthAfterShooting = true;
        // Wait until shot is over
        //            UnityEngine.Debug.Log("Waiting for a shot to be completed ...");

        while (!HUD.Instance.shootDone)
        {
            //UnityEngine.Debug.Log("waiting for the shot to be completed. shootDone is " + HUD.Instance.shootDone);
            //UnityEngine.Debug.Log("mouse sim step " + HUD.Instance.SimulateInputEvent);
            if (ABGameWorld.Instance.LevelCleared() || ABGameWorld.Instance.LevelFailed())
            {
                HUD.Instance.SimulateInputEvent = 0;
                HUD.Instance.shootDone = true;
            }
            yield return new WaitForEndOfFrame();
        }

        UnityEngine.Debug.Log("ReplayBot.ShootAndRecord: shoot done!");
        //wait until the shoot and record is done
        while (ABGameWorld.isRecordingBatchGroundTruthAfterShooting) {
            UnityEngine.Debug.Log("ReplayBot.ShootAndRecord: recording ... ");
            yield return new WaitForEndOfFrame();
        }

        UnityEngine.Debug.Log("ReplayBot.ShootAndRecord: recording done!");


        requestLock = false;
    }

    private IEnumerator TapShoot(string[] args) {

        LoadLevelSchema.Instance.nInteractions++;
        LoadLevelSchema.Instance.nOverallInteractions++;

        Vector3 slingshotPos = ABGameWorld.Instance.Slingshot().transform.position - ABConstants.SLING_SELECT_POS;
        Vector3 slingScreenPos = Camera.main.WorldToScreenPoint(slingshotPos);

        LoadLevelSchema.Instance.updateLikelihoodRequestFlag();
        float dragX = float.Parse(args[2]);
        float dragY = float.Parse(args[3]);

        float dragDX = float.Parse(args[4]);
        float dragDY = float.Parse(args[5]);

        float tapTime = float.Parse(args[6]);
        UnityEngine.Debug.Log("TAP = " + tapTime);
        // Scale tapTime by time 
        tapTime = tapTime / ABGameWorld.SimulationSpeed;


        //no need to convert the coordinate of the drag pt since it is from the unity ingame coordinate but not from the agent 
        Vector2 dragPos = new Vector2(dragX, dragY);
        //this one is from the agent so it still needs to be converted
        Vector2 deltaPos = new Vector2(dragDX, Screen.height - dragDY);

        UnityEngine.Debug.Log("POS = " + dragPos);
        UnityEngine.Debug.Log("DRAG = " + deltaPos);

        //unpause the game
        Time.timeScale = 1.0f;

        RepresentationNovelty.addNoveltyToActionSpaceIfNeeded(dragX, dragY, dragDX, dragDY);
        HUD.Instance.shootDone = false;
        HUD.Instance.SimulateInputEvent = 1;

        //for one tap shoot without dragging, this should be the origin point of the bird on the sling  
        HUD.Instance.SimulateInputPos = dragPos;

        //then this should come from the agent's request
        HUD.Instance.SimulateInputDelta = deltaPos;
        HUD.Instance.SetTapTime(tapTime);


        while (!HUD.Instance.shootDone)
        {
            //UnityEngine.Debug.Log("waiting for the shot to be completed. shootDone is " + HUD.Instance.shootDone);
            //UnityEngine.Debug.Log("mouse sim step " + HUD.Instance.SimulateInputEvent);
            if (ABGameWorld.Instance.LevelCleared() || ABGameWorld.Instance.LevelFailed())
            {
                HUD.Instance.SimulateInputEvent = 0;
                HUD.Instance.shootDone = true;
            }
            yield return new WaitForEndOfFrame();
        }
    }

    private void BatchGroundTruth() { 
    
    }

    private void ReportNoveltyLikelihood() {
        LoadLevelSchema.Instance.noveltyLikelihoodReportRequest = false;
    }

    private void ReadyForNewSet() {
//        yield return new WaitForEndOfFrame();

        //do not update levels file in is isCheckpoint state right after isNewTrial 
        //as they are updated when isNewTrial is true
        if (!LoadLevelSchema.Instance.doNotUpdateLevels)
        {
            if (!LoadLevelSchema.Instance.firstLoad)
            {
                LoadLevelSchema.Instance.updateLevelSet();
            }
            else
            {
                LoadLevelSchema.Instance.firstLoad = false;
            }
        }
        else
        {
            LoadLevelSchema.Instance.doNotUpdateLevels = false;
        }

        LoadLevelSchema.Instance.isCheckPoint = false;
        LoadLevelSchema.Instance.resumeTraining = false;

        if (LoadLevelSchema.Instance.isNewTrial)
        {
            LoadLevelSchema.Instance.isNewTrial = false;
            //send the actual level set game state again after agent is ready for a new trial
            LoadLevelSchema.Instance.isCheckPoint = true;
            LoadLevelSchema.Instance.doNotUpdateLevels = true;
        }
        if (LoadLevelSchema.Instance.needResetTimer)
        {
            LoadLevelSchema.Instance.overallStopwatch.Reset();
            LoadLevelSchema.Instance.overallStopwatch.Start();
            LoadLevelSchema.Instance.needResetTimer = false;
        }

        if (LoadLevelSchema.Instance.resumeTraining)
        {
            LevelList.Instance.CurrentIndex = LoadLevelSchema.Instance.prevLevelIndex;
        }
        else
        {
            LevelList.Instance.CurrentIndex = -1;
        }
        //        LoadLevelSchema.Instance.overallStopwatch.Start();
        LoadLevelSchema.Instance.checkpointWatch.Reset();
        LoadLevelSchema.Instance.checkpointWatch.Start();
        //        LevelLoader.LoadXmlLevel(LoadLevelSchema.Instance.currentXmlFiles[0]);
        //        ABSceneManager.Instance.LoadScene("MainMenu");

        string timeLimit, timeLeft, interactionLimit, interactionLeft, mode, seqOrSet, nLevels, attemptPerLevel, noveltyInfo;
        timeLimit = LoadLevelSchema.Instance.timeLimit.ToString();
        timeLeft = LoadLevelSchema.Instance.timeLeft.ToString();
        interactionLimit = LoadLevelSchema.Instance.interactionLimit.ToString();
        interactionLeft = LoadLevelSchema.Instance.interactionLeft.ToString();
        if (LoadLevelSchema.Instance.isTesting)
        {
            mode = "testing";
        }
        else
        {
            mode = "training";
        }
        if (LoadLevelSchema.Instance.isSequence)
        {
            seqOrSet = "seq";
        }
        else
        {
            seqOrSet = "set";
        }

        nLevels = LoadLevelSchema.Instance.currentXmlFiles.Length.ToString();
        attemptPerLevel = LoadLevelSchema.Instance.attemptsLimitPerLevel.ToString();
        TrialInfo trial = LoadLevelSchema.Instance.trials[LoadLevelSchema.Instance.currentTrialIndex];
        if (trial.notifyNovelty)
        {
            noveltyInfo = "1";
        }
        else
        {
            noveltyInfo = "0";
        }

        ABSceneManager.Instance.LoadScene("MainMenu");

    }

    private bool IsPlaying() {
        string currentScence = SceneManager.GetActiveScene().name;

        if (currentScence == "GameWorld")
        {

            if (ABGameWorld.Instance.LevelCleared())
            {

                currentScence = "LevelCleared";
            }
            else if (ABGameWorld.Instance.LevelFailed())
            {

                currentScence = "LevelFailed";
            }
            else if (!ABGameWorld.Instance.IsLevelStable())
            {

                currentScence = "LevelUnstable";
            }
        }

        if (currentScence != "LevelUnstable" && currentScence != "GameWorld")
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    private IEnumerator LoadNextAvailableLevel() {
        requestLock = true;
        TrialInfo trial = LoadLevelSchema.Instance.trials[LoadLevelSchema.Instance.currentTrialIndex];
        GameLevelSetInfo gameLevelSet;
        if (LoadLevelSchema.Instance.isTesting)
        {
            gameLevelSet = trial.testLevelSets[trial.currentTestSetIndex];
        }
        else
        {
            gameLevelSet = trial.trainingSet;
        }
        if (IsPlaying())
        {//if skip levels while the level is not finished
            EvaluationHandler.Instance.RecordEvaluationScore("Playing");
        }
        if (gameLevelSet.availableLevelList.Count > 0)
        {

            int currentLevelIndex = LevelList.Instance.CurrentIndex;
            LinkedListNode<int> currentNode;
            LinkedListNode<int> nextNode;
            if (currentLevelIndex < 0)
            {
                currentLevelIndex = 0;
            }
            //repeat the level is the current level's allowed number of attempts is greater than zero 
            if (gameLevelSet.nLevelEntryList[currentLevelIndex] > 0)
            {
                nextNode = gameLevelSet.availableLevelList.Find(currentLevelIndex);
            }

            else
            {
                if (currentLevelIndex >= 0)
                {
                    currentNode = gameLevelSet.availableLevelList.Find(currentLevelIndex);
                    nextNode = currentNode.Next;
                }
                else
                {//if the levelindex is inited to -1, read the first level
                    currentNode = gameLevelSet.availableLevelList.First;
                    nextNode = currentNode;
                }
            }
            if (nextNode == null)
            {
                nextNode = gameLevelSet.availableLevelList.First;
            }

            //delayed remove to prevent current node to be null       
            if (gameLevelSet.nLevelEntryList[currentLevelIndex] == 0)
            {
                gameLevelSet.availableLevelList.Remove(currentLevelIndex);
            }

            int levelIndex = nextNode.Value;

            UnityEngine.Debug.Log("Level index:" + levelIndex);

            LevelList.Instance.SetLevel(levelIndex);
            gameLevelSet.nLevelEntryList[levelIndex]--;


            //do not need delayed remove if only one level is left
            if (gameLevelSet.nLevelEntryList[levelIndex] == 0 && gameLevelSet.availableLevelList.Count == 1)
            {
                gameLevelSet.availableLevelList.Remove(levelIndex);
            }

            UnityEngine.Debug.Log("loading next available level " + levelIndex.ToString());
            //Time.timeScale = ABGameWorld.SimulationSpeed;
            ABSceneManager.Instance.LoadScene("GameWorld");

            var currentScence = SceneManager.GetActiveScene().name;
            while (currentScence != "GameWorld")
            {
                currentScence = SceneManager.GetActiveScene().name;
                UnityEngine.Debug.Log("wait for level loading " + levelIndex.ToString());
                UnityEngine.Debug.Log("game scene " + currentScence);
                yield return new WaitForSeconds(1);
            }
            yield return new WaitForSeconds(0.2f);
            //speed up zoom
            //Time.timeScale = 50.0f;
            try
            {
                HUD.Instance.CameraZoom(15f);
            }
            catch
            {
                UnityEngine.Debug.LogWarning("zooming out after loading level failed, check if camera object is null");
            }
            yield return new WaitForSeconds(2f);
            //switch back time scale
            //Time.timeScale = 1.0f;
            //isZoomingCompleted = true;

            LevelList.Instance.CurrentIndex = levelIndex;

            //uncomment the below three lines if loading a game is considered as an interaction
            //LoadLevelSchema.Instance.nInteractions++;
            //LoadLevelSchema.Instance.nOverallInteractions++;
            //LoadLevelSchema.Instance.updateLikelihoodRequestFlag();

            SymbolicGameState gt = new SymbolicGameState(false);
            //make sure the scene is loaded
            for (int i = 0; i < 3; i++)
            {
                gt.GetGTJson();
                yield return new WaitForSeconds(0.2f);
            }
            EvaluationHandler.novelIDList = gt.novelIDList;
            EvaluationHandler.completeNovelIDList = gt.novelIDList;
        }

        else
        {
            UnityEngine.Debug.LogWarning("loading next available level : no available level");
            trial.loadLevelAfterFinished = true;
        }

        requestLock = false;
    }

    private string GetGameState() {
        string currentScence = SceneManager.GetActiveScene().name;

        if (currentScence == "GameWorld")
        {

            if (ABGameWorld.Instance.LevelCleared())
            {
                currentScence = "LevelCleared";
            }
            else if (ABGameWorld.Instance.LevelFailed())
            {
                currentScence = "LevelFailed";
            }
            else if (!ABGameWorld.Instance.IsLevelStable())
            {

                currentScence = "LevelUnstable";
            }
        }

        //wait until a stable state to update the evaluation related states
        //if(currentScence != "LevelUnstable"){

        //report novelty likelihood has priority
        if (LoadLevelSchema.Instance.noveltyLikelihoodReportRequest)
        {
            currentScence = "NoveltyLikelihoodReportRequest";
        }
        else if (LoadLevelSchema.Instance.isNewTrial)
        {
            currentScence = "NewTrial";
        }

        else if (LoadLevelSchema.Instance.evaluationTerminating)
        {
            currentScence = "EvaluationTerminated";
        }
        //wait until the level is solved 
        //GameWorld means playing 
        else if (currentScence != "LevelUnstable" && currentScence != "GameWorld")
        {

            //ask the agent to report novelty likelihood first, before any new set/termination states
            if (LoadLevelSchema.Instance.isCheckPoint)
            {
                if (LoadLevelSchema.Instance.isTesting)
                {
                    currentScence = "NewTestSet";
                }
                else
                {
                    currentScence = "NewTrainingSet";
                }
            }

            else if (LoadLevelSchema.Instance.resumeTraining)
            {
                currentScence = "ResumeTraining";
            }
        }

        return currentScence;
    }

    public static string[] GetInitArgs(string path) {
        StreamReader file = new StreamReader(path);
        string line = file.ReadLine();
        string[] args = null;

        if (line != null)
        {
            args = line.Split(' ');
        }
        UnityEngine.Debug.Log("ReplayBot: args read from file : " + args);
        return args;
    }

    void Update() { 
    
    }


}

