using System.IO;
using System;
using System.Collections.Generic;

public class EvaluationHandler : ABSingleton<EvaluationHandler>
{
    public static string savePath = "./";

    static string agentNamePrefix = "";

    static int evalIndex = 0;

    //the novel object id list of the current frame
    //for batch gt, it is the list of the first frame of the batch gt
    public static List<String> novelIDList = new List<string>();
    //the complete novel object id list of the current game level 
    public static List<String> completeNovelIDList = new List<string>();


    public void savePathUpdate(string configXmlPath){
        //System.Console.WriteLine("=== entering EvaluationHandler.savePathUpdate() ===");
        
        int port = 9000;
        string agentName = "noname";
        var args = System.Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; i++) {
            //UnityEngine.Debug.Log ("ARG " + i + ": " + args [i]);
            if (args [i] == "--port") {
                port = Int32.Parse(args [i + 1]);
                UnityEngine.Debug.Log("Assigned custom port: " + port);
            }

            if(args [i] == "--agent-name"){
                agentName = args [i + 1];
            }
        }
        savePath = "Agent_"+agentName+"_Port_"+port+"_Evaluation_" + DateTime.Now.ToString("yyyyMMddHHmmss");
        bool exists = System.IO.Directory.Exists(savePath);
        //string configXmlPath = ABConstants.STREAMINGASSETS_FOLDER + "/config.xml";

        if(!exists)
            System.IO.Directory.CreateDirectory(savePath);

        //do not copy in this version as the file is too large
        //File.Copy(configXmlPath, savePath+"/config.xml",true);
        
        int trialIndex = LoadLevelSchema.Instance.currentTrialIndex;
        string informed = getInformedInfo();
        agentNamePrefix = "Agent_"+agentName+"_Trial_" + trialIndex.ToString() + "_" + informed + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" ;
    }

    public void UpdateEvaluationFileInfo(string trialIndex, string trialRunIndex, string gameSetIndex, string mode){
        //agentNamePrefix = "trial_" + trialIndex + "_gameset_"+ gameSetIndex  + "_trialrun_ " + trialRunIndex +"_"+ mode + "_" ;

        //System.Console.WriteLine("=== entering EvaluationHandler.UpdateEvaluationFileInfo() ===");

        string informed = getInformedInfo();
        string agentName = "noname";
        var args = System.Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; i++) {
            //UnityEngine.Debug.Log ("ARG " + i + ": " + args [i]);
         
            if(args [i] == "--agent-name"){
                agentName = args [i + 1];
            }
        }
        agentNamePrefix = "Agent_"+agentName+"_Trial_" + trialIndex + "_" + informed+ "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" ;
    }

    public void RecordEvaluationScore(string state)
    {

        //System.Console.WriteLine("=== entering EvaluationHandler.RecordEvaluationScore() ===");

        evalIndex += 1;

        float totalScore = HUD.Instance.GetScore();
        int currentLevelIndex = LevelList.Instance.CurrentIndex + 1;
        int birdsRemaing = ABGameWorld.Instance._birds.Count;
        int pigsRemaining = ABGameWorld.Instance._pigs.Count;

        int birdsAtStart = ABGameWorld.Instance.BirdsAtStart;
        int pigsAtStart = ABGameWorld.Instance.PigsAtStart;

        int trialIndex = LoadLevelSchema.Instance.currentTrialIndex;
        string playStatus;
        bool isTesting = LoadLevelSchema.Instance.isTesting;
        if(isTesting){playStatus = "test";}
        else{playStatus = "training";}
        string levelPath = LoadLevelSchema.Instance.currentLevelPaths[LevelList.Instance.CurrentIndex];

        string levelStatus;
        if (state == "Pass")
        {
            levelStatus = "Pass";
        }
        else if(state == "Playing"){
            levelStatus = "Playing";
        }
        else
        {
            levelStatus = "Fail";
        }
        
        string informed = getInformedInfo();

        string novelty = "novel";
        if (levelPath.Substring((levelPath.Length - 5), 1) == "0")
        {
            novelty = "basic";
        }

        string evalData = string.Format("{0},{1},{2},{3},{4},{5},{6},{7}, {8},{9},{10},{11},{12}\n", 
            evalIndex, currentLevelIndex, totalScore, levelStatus, birdsRemaing, pigsRemaining, birdsAtStart,pigsAtStart, trialIndex, levelPath, playStatus,informed,novelty);

        if (File.Exists(savePath + "/" + agentNamePrefix + "EvaluationData.csv"))
        {
            File.AppendAllText(savePath + "/" + agentNamePrefix + "EvaluationData.csv", evalData);
            //Debug.Log("Score: " + totalScore + ", Level: " + currentIndex);
        }
        else{
            string evalHead = string.Format("{0},{1},{2},{3},{4},{5},{6},{7}, {8},{9},{10},{11},{12}\n",
             "EvaluationIndex", "LevelIndex", "Score", "LevelStatus","birdsRemaining","pigsRemaining","birdsAtStart","pigsAtStart", "trial", "levelName", "playStatus","informed","novelty");
            File.WriteAllText(savePath + "/" + agentNamePrefix + "EvaluationData.csv", evalHead);
            File.AppendAllText(savePath + "/" + agentNamePrefix + "EvaluationData.csv", evalData);
        }
    }

    public void RecordNoveltyLikelihood(double noveltyLikelihood, double nonNoveltyLikelihood, string noveltyIds, int noveltyLevel, string noveltyDescription,string trueNovelIDList, string trueCompleteNovelIDList){
        //System.Console.WriteLine("=== entering EvaluationHandler.RecordNoveltyLikelihood() ===");

        int currentInteractionNumber;
        
        if(noveltyDescription!=null){
            //replace any commas with spaces to avoid the break of csv format
            noveltyDescription = noveltyDescription.Replace("," , "");
        }
        
        if(!LoadLevelSchema.Instance.isTesting){
            currentInteractionNumber = LoadLevelSchema.Instance.nOverallInteractions;
        }
        else{
            currentInteractionNumber = LoadLevelSchema.Instance.nInteractions;
        }

        int currentLevelIndex = LevelList.Instance.CurrentIndex + 1;//start from 1
        int nInteractions = LoadLevelSchema.Instance.nInteractions; 

        int trialIndex = LoadLevelSchema.Instance.currentTrialIndex;
        string playStatus;
        bool isTesting = LoadLevelSchema.Instance.isTesting;
        if(isTesting){playStatus = "test";}
        else{playStatus = "training";}
        string levelPath = LoadLevelSchema.Instance.currentLevelPaths[LevelList.Instance.CurrentIndex];

        string informed = getInformedInfo();
        string novelty = "novel";
        if (levelPath.Substring((levelPath.Length - 5), 1) == "0")
        {
            novelty = "basic";
        }

        string evalData; 
        if(evalIndex == currentLevelIndex){//if the game is finished (won or lost), the eval index has been updated to the next game, thus adjust it.
            evalData = string.Format("{0},{1},{2},{3},{4}, {5},{6},{7},{8},{9},{10},{11},{12},{13},{14}\n", evalIndex-1, currentLevelIndex, nInteractions,noveltyLikelihood,nonNoveltyLikelihood,noveltyIds,noveltyLevel,noveltyDescription,trueNovelIDList,trueCompleteNovelIDList,trialIndex, levelPath, playStatus,informed,novelty);
        }
        else{
            evalData = string.Format("{0},{1},{2},{3},{4}, {5},{6},{7},{8},{9},{10},{11},{12},{13},{14}\n", evalIndex, currentLevelIndex, nInteractions,noveltyLikelihood,nonNoveltyLikelihood,noveltyIds,noveltyLevel,noveltyDescription,trueNovelIDList,trueCompleteNovelIDList,trialIndex, levelPath, playStatus,informed,novelty);
        }
        if (File.Exists(savePath + "/" + agentNamePrefix + "Likelihood.csv"))
        {
            File.AppendAllText(savePath + "/" + agentNamePrefix + "Likelihood.csv", evalData);
        }
        else
        {
            string evalHead = string.Format("{0},{1},{2},{3},{4}, {5},{6},{7},{8},{9}, {10}, {11}, {12},{13},{14}\n", 
            "EvaluationIndex", "LevelIndex","InteractionIndex","HasNoveltyLikelihood", "NoNoveltyLikelihood","novel object IDs", "novelty level","novelty description", "true novel IDs","complete true novel object IDs","trial", "levelName", "playStatus","informed","novelty");
            File.WriteAllText(savePath + "/" + agentNamePrefix + "Likelihood.csv", evalHead);
            File.AppendAllText(savePath + "/" + agentNamePrefix + "Likelihood.csv", evalData);
        }
    }

    private string getInformedInfo(){
        //System.Console.WriteLine("=== entering EvaluationHandler.getInformedInfo() ===");

        string informed;
        int trialIndex = LoadLevelSchema.Instance.currentTrialIndex;
        TrialInfo trial = LoadLevelSchema.Instance.trials[trialIndex];
        if(trial.notifyNovelty){
            informed = "informed";
        }
        else{
            informed = "uninformed";
        }
        return informed;
    }
    /*
    static int evalCount = 0;
    public void RecordStartData()
    {
        evalCount += 1;
        int currentLevelIndex = LevelList.Instance.CurrentIndex + 1;
        int birdsAtStart = ABGameWorld.Instance.BirdsAtStart;
        int pigsAtStart = ABGameWorld.Instance.PigsAtStart;
        string startData = string.Format("{0},{1},{2},{3}\n", evalCount, currentLevelIndex, birdsAtStart, pigsAtStart);
        if (File.Exists(savePath + "/" + agentNamePrefix + "InteractionData.csv"))
        {
            File.AppendAllText(savePath + "/" + agentNamePrefix + "InteractionData.csv", startData);
        }
        else{
            string startHead = string.Format("{0},{1},{2},{3}\n", "EvaluationIndex", "LevelIndex", "BirdsAtStart", "PigsAtStart");
            File.WriteAllText(savePath + "/" + agentNamePrefix + "InteractionData.csv", startHead);
            File.AppendAllText(savePath + "/" + agentNamePrefix + "InteractionData.csv", startData);
        }
    }
    */
}

