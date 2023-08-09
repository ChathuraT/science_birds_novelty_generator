/**
* @author Peng Zhang
*
* @date - 04/08/2020 
*/

using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using System.IO;

public class SymbolicGameState
{
    Camera cam;
    public bool devMode;
    private bool useNoise;

    private bool updateNovelList;

    public List<String> novelIDList {private set; get;}

    public static ColorData colorData;

    private enum ObjectType { Pig, Bird, SlingShot, Platform, Ground, Block};
    public SymbolicGameState(bool noise = true, bool updateNovelList = true){
        //System.Console.WriteLine("=== entering SymbolicGameState.SymbolicGameState() ===");
        
        this.cam = Camera.main.gameObject.GetComponent<Camera>();
        // Changing the colorDataFile according to the novelty
        string colorDataFile = "";
        if (ABGameWorld.noveltyTypeForNovelty3 == 1)
        {
            // For GrayScale Novelty
            colorDataFile = ABConstants.colorDataFileNoveltyGrayscale;
        }
        else
        {
            colorDataFile = ABConstants.colorDataFile;
        }    
        string colorHistograms = File.ReadAllText(colorDataFile);
        colorData = JsonUtility.FromJson<ColorData>(colorHistograms);
        colorData.readColorMaps();
        this.devMode = LoadLevelSchema.Instance.devMode;
        this.useNoise = noise;
        this.novelIDList = new List<string>();
        this.updateNovelList = updateNovelList;
    }

    public string GetGTJson(){
        this.novelIDList = new List<string>();
        string gtJson = "[{\"type\": \"FeatureCollection\",\"features\": [";
        
        //Get ground
        if (GameObject.Find("Ground") != null && cam != null)
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground.GetComponent<BoxCollider2D>() != null)
            {
                string groundID = ground.GetInstanceID().ToString();

                var Screenmax = cam.WorldToScreenPoint(ground.GetComponent<BoxCollider2D>().bounds.max);
                int yind = (int)Mathf.Max(Mathf.Round(Screenmax.y), 0.0f);
                
                yind = Screen.height - yind;
                GTGround gtGround = new GTGround(groundID, "Ground",yind);
                
                gtJson = gtJson + gtGround.ToJsonString(this.devMode) + ",";
            }
        }

        //Get sling shot
        GameObject[] Slingshot = GameObject.FindGameObjectsWithTag("Slingshot");
        float xmax=0,xmin=0,ymax=0,ymin=0;
        foreach (GameObject gameObject in Slingshot)
        {
            if (gameObject.name == "slingshot_back")
            {
                Vector3 objectBoundmin = cam.WorldToScreenPoint(gameObject.GetComponent<Renderer>().bounds.min);
                Vector3 objectBoundmax = cam.WorldToScreenPoint(gameObject.GetComponent<Renderer>().bounds.max);

                xmax = Mathf.Round(objectBoundmax.x);
                //if (xmax < 0) { xmax = 0; }
                ymin = Mathf.Round(objectBoundmin.y);
                //if (ymin < 0) { ymin = 0; }
            }
            else if (gameObject.name == "slingshot_front")
            {
                Vector3 objectBoundmin = cam.WorldToScreenPoint(gameObject.GetComponent<Renderer>().bounds.min);
                Vector3 objectBoundmax = cam.WorldToScreenPoint(gameObject.GetComponent<Renderer>().bounds.max);

                ymax = Mathf.Round(objectBoundmax.y);
                //if (ymax < 0) { ymax = 0; }
                xmin = Mathf.Round(objectBoundmin.x);
                //if (xmin < 0) { xmin = 0; }
            }
        }
        Vector2[] slingshotBound = new Vector2[4];
        slingshotBound[0] = new Vector2(xmin, Screen.height - ymin);
        slingshotBound[1] = new Vector2(xmin, Screen.height - ymax);
        slingshotBound[2] = new Vector2(xmax, Screen.height - ymax);
        slingshotBound[3] = new Vector2(xmax, Screen.height - ymin);
                
        bool hasZeroWidthOrHeight = isWidthOrHeightZero(slingshotBound);
        if(hasZeroWidthOrHeight){
            UnityEngine.Debug.LogWarning("the height or width of the sling is zero ");
            UnityEngine.Debug.LogWarning(slingshotBound);
            UnityEngine.Debug.LogWarning("sling has to be kept");
        }
        //reset
        hasZeroWidthOrHeight = false;

        List<Vector2[]> slingPaths = new List<Vector2[]>();
        slingPaths.Add(slingshotBound);
        string slingID = Slingshot[0].GetInstanceID().ToString();
        //colorMaps[] SlingShotColors = ColorPoints(vert,false);
        float slingHP = float.MaxValue;
        GTObject gtSling = new GTObject(slingID,"Slingshot",null,slingPaths,slingHP);
        gtJson = gtJson + gtSling.ToJsonString(this.devMode) + ",";

        //Get trajectory points
        GameObject[] trajectoryPts = GameObject.FindGameObjectsWithTag("Trajectory");
        string trajID = getTrajID();
        List<Vector2> screenLocationList = new List<Vector2>();
        Vector2[] screenLocation;

        for (int i=0; i<trajectoryPts.Length; i++)
        {

            GameObject gameObject = trajectoryPts[i];
            //if(!gameObject.GetComponent<Renderer>().isVisible)
            //{
            //    UnityEngine.Debug.Log("invisible traj index" + i);
            //    continue;
            //}

            // do not get trajectory id from the trajectory dots. They are not stable 
            //            if(i==0){
            //                trajID = gameObject.GetInstanceID().ToString();
            //            }
            var screenPosition = cam.WorldToScreenPoint(gameObject.transform.position);
            Vector2 point = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            if (useNoise)
            {
                point = GTNoise.ApplyPositionNoise(point);
            }

            screenLocationList.Add(point);
        }
        screenLocation = screenLocationList.ToArray();
        //only include trajectory when there is at lease one trajectory point detected
        if (screenLocation.Length > 0){
            GTTrajectory gtTrajectory = new GTTrajectory(trajID,"Trajectory", screenLocation);
            gtJson = gtJson + gtTrajectory.ToJsonString(this.devMode)+",";
        }

        //Get pigs
        GameObject[] pigsSmall = GameObject.FindGameObjectsWithTag("PigSmall");
        GameObject[] pigBig = GameObject.FindGameObjectsWithTag("PigBig");
        GameObject[] pigMedium = GameObject.FindGameObjectsWithTag("PigMedium");

        GameObject[] pigs = pigsSmall.Concat(pigBig).Concat(pigMedium).ToArray();
        foreach (GameObject gameObject in pigs)
        {   
            bool outOfBound = false;
            string blockName = gameObject.GetComponent<SpriteRenderer>().sprite.name;


            bool isNovelObj = false;
            string objName = gameObject.name;
            if (objName.Contains("novel"))
            {
                isNovelObj = true;
                blockName = gameObject.name;
                blockName = blockName.Replace("(Clone)", "").Trim();
                ABLevel currentLevel = LevelList.Instance.GetCurrentLevel();
                string assetBundlePath = currentLevel.assetBundleFilePath;

                string[] strArray = assetBundlePath.Split(new string[] { "type" }, StringSplitOptions.None);

                string noveltyTypeStr = strArray[strArray.Length - 1].Split(new string[] { "AssetBundle" }, StringSplitOptions.None)[0];
                noveltyTypeStr = noveltyTypeStr.Substring(0, noveltyTypeStr.Length - 1);//remove the file separator

                string noveltyLevelStr = strArray[strArray.Length - 2].Split(new string[] { "novelty_level_" }, StringSplitOptions.None).Last();
                noveltyLevelStr = noveltyLevelStr.Substring(0, noveltyLevelStr.Length - 1);//remove the file separator

                blockName = blockName + "_" + noveltyLevelStr + "_" + noveltyTypeStr;

            }

            if (gameObject.name == "BasicSmall(Clone)" || gameObject.name == "BasicMedium(Clone)" || gameObject.name == "BasicBig(Clone)" || gameObject.name.Contains("novel"))
            {

                float hp = gameObject.GetComponent<ABGameObject>().getCurrentLife();
                string pigID = "";

                pigID = gameObject.GetInstanceID().ToString();
                //string pigID = gameObject.GetInstanceID().ToString();
                int pathCount = gameObject.GetComponent<PolygonCollider2D>().pathCount;
                List<Vector2[]> paths = new List<Vector2[]>();
                //Vector2[] allObjPoints = null;
//                ObjectContour[] noisyPaths = new ObjectContour[pathCount];
                for (int i = 0; i < pathCount; i++ ){
                    Vector2[] objPoints = gameObject.GetComponent<PolygonCollider2D>().GetPath(i);
                                        //vertices that is in unity coordinate system
                    //the origin is buttom-left corner 
                    Vector3[] screenPoints = new Vector3[objPoints.Length];
                    
                    Vector2[] noisePoints = new Vector2[objPoints.Length];

                    for (int j = 0; j < objPoints.Length; j++)
                    {
                        screenPoints[j] = cam.WorldToScreenPoint(gameObject.transform.TransformPoint(objPoints[j]));
                        objPoints[j] = new Vector2(Mathf.Round(screenPoints[j].x), Mathf.Round(Screen.height - screenPoints[j].y));                        
                    }

                    //skip this object if it is out of the screen
                    if(IsOutOfBound(objPoints)){
                        outOfBound = true;
                        break;
                    }
                    /*for getting colour map from screen shot 
                    if (i == 0)
                    {
                        allObjPoints = objPoints;
                    }
                    else
                    {
                        allObjPoints = allObjPoints.Concat(objPoints).ToArray();
                    }

                    if (this.useNoise){
                        objPoints = ApplyNoise(objPoints);                    
                    }
                    */
                    paths.Add(objPoints);
                    hasZeroWidthOrHeight = isWidthOrHeightZero(objPoints);
                    if(hasZeroWidthOrHeight){
                        UnityEngine.Debug.LogWarning(blockName + " width or height is zero. This pig is excluded from the gt");
                        UnityEngine.Debug.LogWarning(objPoints);
                        break;
                    }

                }
                if(outOfBound){
                    continue;
                }
                if(hasZeroWidthOrHeight){
                    //reset
                    hasZeroWidthOrHeight = false;
                    continue;
                }

                ColorEntry[] objColors = ColorDataLookUp(blockName,this.useNoise);

                string objectType = "Object";
                if(this.devMode){
                    objectType = blockName;
                }

                /*for getting colour map from screen shot 
                if (blockName.Contains("novel")) {
                    UnityEngine.Debug.LogWarning("obj pts length " + allObjPoints.Length);
                    objColors = null;
                    objColors = ColorPoints(gameObject, allObjPoints, false);
                }
                */
                GTObject pig = new GTObject(pigID, objectType, objColors, paths, hp);
                gtJson = gtJson + pig.ToJsonString(this.devMode) + ',';
                if(isNovelObj&&updateNovelList){
                    novelIDList.Add(pigID);
                }
            }
        }

        //Get birds
        GameObject[] birds = GameObject.FindGameObjectsWithTag("Bird");
        foreach (GameObject gameObject in birds)
        {
            string blockName = gameObject.GetComponent<SpriteRenderer>().sprite.name;
            

            bool isNovelObj = false;
            string objName = gameObject.name;
            if(objName.Contains("novel")){
                isNovelObj = true;
            }
            
            string blockMeterial = "";
            PolygonCollider2D polygonCollider= gameObject.GetComponent<PolygonCollider2D>();
            
            if(polygonCollider!=null&&polygonCollider.sharedMaterial!=null){
                blockMeterial = polygonCollider.sharedMaterial.ToString();
            }
            else{
            //    UnityEngine.Debug.Log("birds poly collider found, but material is null");
            }

            //Find out novel objects
            //No exclusive objects with different names have been found
            //But some novel objects with different name format
            //may occur in the future
            if(blockMeterial.Contains("novel")){
                isNovelObj = true;
            }
            
            //ad-hoc code for distinguishing the novel bird that uses pig sprite  
            if(blockName=="pig_basic_small_2"){
                blockName = "novel_object_0_1_9";
            }

            if (gameObject.GetComponent<SpriteRenderer>().color.a != 0)
            {   
                bool outOfBound = false;
                float hp = gameObject.GetComponent<ABGameObject>().getCurrentLife();
                //filter out invisible objects
                // if(!gameObject.GetComponent<Renderer>().isVisible){
                //     continue;
                // }
                string birdID = "";

                birdID = gameObject.GetInstanceID().ToString();
                //string birdID = gameObject.GetInstanceID().ToString();
                int pathCount = gameObject.GetComponent<PolygonCollider2D>().pathCount;
                List<Vector2[]> paths = new List<Vector2[]>();
                for(int i = 0; i < pathCount; i++ ){
                    Vector2[] objPoints = gameObject.GetComponent<PolygonCollider2D>().GetPath(i);
                    Vector3[] screenPoints = new Vector3[objPoints.Length];
                    Vector2[] noisePoints = new Vector2[objPoints.Length];

                    for (int j = 0; j < objPoints.Length; j++)
                    {
                        screenPoints[j] = cam.WorldToScreenPoint(gameObject.transform.TransformPoint(objPoints[j]));
                        objPoints[j] = new Vector2(Mathf.Round(screenPoints[j].x), Mathf.Round(Screen.height - screenPoints[j].y));
                    }
                    //skip this object if it is out of the screen
                    if(IsOutOfBound(objPoints)){
                        outOfBound = true;
                        break;
                    }
                    
                    hasZeroWidthOrHeight = isWidthOrHeightZero(objPoints);
                    if(hasZeroWidthOrHeight){
                        UnityEngine.Debug.LogWarning("Bird " + blockName + " width or height is zero. Birds have to be kept in the gt");
                        UnityEngine.Debug.LogWarning(objPoints);
                    }

                    paths.Add(objPoints);
                }

                if(outOfBound){
                    continue;
                }
                ColorEntry[] objColors = ColorDataLookUp(blockName,false);
                
                string objectType = "Object";
                if(this.devMode){
                    objectType = blockName;
                }
                
                GTObject bird = new GTObject(birdID,objectType,objColors,paths, hp);
                gtJson = gtJson + bird.ToJsonString(this.devMode) + ',';
                if(isNovelObj&&updateNovelList){
                    novelIDList.Add(birdID);
                }
            }

        }

        //Get blocks
        GameObject[] cricleBlocks = GameObject.FindGameObjectsWithTag("Circle");
        GameObject[] triangleBlocks = GameObject.FindGameObjectsWithTag("Triangle");
        GameObject[] haloSquareBlocks = GameObject.FindGameObjectsWithTag("SquareHole");
        GameObject[] novelBlocks = GameObject.FindGameObjectsWithTag("Block");
        GameObject[] blocks = cricleBlocks.Concat(triangleBlocks).Concat(haloSquareBlocks).Concat(novelBlocks).ToArray();
        foreach (GameObject gameObject in blocks)
        {   
            bool outOfBound = false;
            if(gameObject.tag!="Circle"&&gameObject.tag!="Triangle"&&gameObject.tag!="SquareHole"&&gameObject.tag!="Block"){continue;}
            if (gameObject.name.Equals("BasicSmall(Clone)")){continue;} //pigs are removed 
            string blockName = gameObject.GetComponent<SpriteRenderer>().sprite.name;
            bool isNovelObj = false;

            string objName = gameObject.name;
            if (objName.Contains("novel"))
            {
                isNovelObj = true;
                blockName = gameObject.name;
                blockName = blockName.Replace("(Clone)", "").Trim();
                ABLevel currentLevel = LevelList.Instance.GetCurrentLevel();
                string assetBundlePath = currentLevel.assetBundleFilePath;

                string[] strArray = assetBundlePath.Split(new string[] { "type" }, StringSplitOptions.None);

                string noveltyTypeStr = strArray[strArray.Length - 1].Split(new string[] { "AssetBundle" }, StringSplitOptions.None)[0];
                noveltyTypeStr = noveltyTypeStr.Substring(0, noveltyTypeStr.Length - 1);//remove the file separator

                string noveltyLevelStr = strArray[strArray.Length - 2].Split(new string[] { "novelty_level_" }, StringSplitOptions.None).Last();
                noveltyLevelStr = noveltyLevelStr.Substring(0, noveltyLevelStr.Length - 1);//remove the file separator

                blockName = blockName + "_" + noveltyLevelStr + "_" + noveltyTypeStr;

            }


            //adjust the name of the novel objects

            if (gameObject.tag == "Block"){
                blockName = gameObject.name;
                blockName = blockName.Replace("(Clone)","").Trim();
                ABLevel currentLevel = LevelList.Instance.GetCurrentLevel();
                string assetBundlePath = currentLevel.assetBundleFilePath;
                
                string[] strArray = assetBundlePath.Split(new string[] { "type" }, StringSplitOptions.None);
                
                string noveltyTypeStr = strArray[strArray.Length-1].Split(new string[] { "AssetBundle" }, StringSplitOptions.None)[0];
                noveltyTypeStr = noveltyTypeStr.Substring(0, noveltyTypeStr.Length - 1);//remove the file separator

                string noveltyLevelStr = strArray[strArray.Length-2].Split(new string[] { "novelty_level_" }, StringSplitOptions.None).Last();
                noveltyLevelStr = noveltyLevelStr.Substring(0, noveltyLevelStr.Length - 1);//remove the file separator

                blockName = blockName + "_" + noveltyLevelStr + "_" + noveltyTypeStr;
            }

            string blockID = "";
            blockID = gameObject.GetInstanceID().ToString();

            List<Vector2[]> paths = new List<Vector2[]>();
            //string blockID = gameObject.GetInstanceID().ToString();
            float hp = gameObject.GetComponent<ABGameObject>().getCurrentLife();
            if(gameObject.GetComponent<PolygonCollider2D>()!=null){
                int pathCount = gameObject.GetComponent<PolygonCollider2D>().pathCount;
                for(int i = 0; i < pathCount; i++ ){
                    Vector2[] objPoints = gameObject.GetComponent<PolygonCollider2D>().GetPath(i);
                    Vector3[] screenPoints = new Vector3[objPoints.Length];
                    Vector2[] noisePoints = new Vector2[objPoints.Length];
                    
                    for (int j = 0; j < objPoints.Length; j++)
                    {
                        screenPoints[j] = cam.WorldToScreenPoint(gameObject.transform.TransformPoint(objPoints[j]));
                        objPoints[j] = new Vector2(Mathf.Round(screenPoints[j].x), Mathf.Round(Screen.height - screenPoints[j].y));                        
                    }
                    //skip this object if it is out of the screen
                    if(IsOutOfBound(objPoints)){
                        outOfBound = true;
                        break;
                    }                
                    if(this.useNoise){
                        objPoints = ApplyNoise(objPoints);
                    }
                                        
                    hasZeroWidthOrHeight = isWidthOrHeightZero(objPoints);
                    if(hasZeroWidthOrHeight){
                        UnityEngine.Debug.LogWarning("block " + blockName + " width or height is zero. Block has been removed from gt");
                        UnityEngine.Debug.LogWarning(objPoints);
                        break;
                    }

                    paths.Add(objPoints);

                }

            }
            else{//assume otherwise the block uses box collider
                Vector3[] corners = new Vector3[4];
                Vector3[] screenPoints = new Vector3[4];
                Vector2[] objPoints = new Vector2[4];

                if (gameObject.GetComponent<RectTransform>() == null)
                {
                    gameObject.AddComponent<RectTransform>();
                }
                gameObject.GetComponent<RectTransform>().GetWorldCorners(corners);
                for (int i = 0; i < 4; i++)
                {
                    screenPoints[i] = cam.WorldToScreenPoint(corners[i]);
                    objPoints[i] = new Vector2(Mathf.Round(screenPoints[i].x), Mathf.Round(Screen.height - screenPoints[i].y));
                }
                //skip this object if it is out of the screen
                if(IsOutOfBound(objPoints)){
                    continue;
                }
                if(this.useNoise){
                    objPoints = ApplyNoise(objPoints);
                }
                hasZeroWidthOrHeight = isWidthOrHeightZero(objPoints);
                if(hasZeroWidthOrHeight){
                    UnityEngine.Debug.LogWarning("block " + blockName + " width or height is zero. Block has been removed from gt");
                    UnityEngine.Debug.LogWarning(objPoints);
                    break;
                }
                paths.Add(objPoints);

            }

            if(outOfBound){
                continue;
            }
            if(hasZeroWidthOrHeight){
                hasZeroWidthOrHeight = false;
                continue;
            }
            ColorEntry[] objColors = ColorDataLookUp(blockName,this.useNoise);

            string objectType = "Object";
            if(this.devMode){
                objectType = blockName;
            }

            GTObject block = new GTObject(blockID, objectType, objColors, paths, hp);
            gtJson = gtJson + block.ToJsonString(this.devMode) + ',';
            if(isNovelObj&&updateNovelList){
                novelIDList.Add(blockID);
            }
        }

        //external agents
        GameObject[] externalAgents = GameObject.FindGameObjectsWithTag("ExternalAgent");

        foreach(GameObject gameObject in externalAgents) {
            bool outOfBound = false;
            string blockName = gameObject.GetComponent<SpriteRenderer>().sprite.name;
            List<Vector2[]> paths = new List<Vector2[]>();

            bool isNovelObj = false;

            string objName = gameObject.name;
            if (objName.Contains("novel"))
            {
                isNovelObj = true;
            }

            if (gameObject.GetComponent<PolygonCollider2D>() != null)
            {
                int pathCount = gameObject.GetComponent<PolygonCollider2D>().pathCount;
                for (int i = 0; i < pathCount; i++)
                {
                    Vector2[] objPoints = gameObject.GetComponent<PolygonCollider2D>().GetPath(i);
                    Vector3[] screenPoints = new Vector3[objPoints.Length];
                    Vector2[] noisePoints = new Vector2[objPoints.Length];

                    for (int j = 0; j < objPoints.Length; j++)
                    {
                        screenPoints[j] = cam.WorldToScreenPoint(gameObject.transform.TransformPoint(objPoints[j]));
                        objPoints[j] = new Vector2(Mathf.Round(screenPoints[j].x), Mathf.Round(Screen.height - screenPoints[j].y));
                    }
                    //skip this object if it is out of the screen
                    if (IsOutOfBound(objPoints))
                    {
                        outOfBound = true;
                        break;
                    }
                    if (this.useNoise)
                    {
                        objPoints = ApplyNoise(objPoints);
                    }

                    hasZeroWidthOrHeight = isWidthOrHeightZero(objPoints);
                    if (hasZeroWidthOrHeight)
                    {
                        UnityEngine.Debug.LogWarning("block " + blockName + " width or height is zero. Block has been removed from gt");
                        UnityEngine.Debug.LogWarning(objPoints);
                        break;
                    }

                    paths.Add(objPoints);

                }

            }
            else
            {//assume otherwise the block uses box collider
                Vector3[] corners = new Vector3[4];
                Vector3[] screenPoints = new Vector3[4];
                Vector2[] objPoints = new Vector2[4];

                if (gameObject.GetComponent<RectTransform>() == null)
                {
                    gameObject.AddComponent<RectTransform>();
                }
                gameObject.GetComponent<RectTransform>().GetWorldCorners(corners);
                for (int i = 0; i < 4; i++)
                {
                    screenPoints[i] = cam.WorldToScreenPoint(corners[i]);
                    objPoints[i] = new Vector2(Mathf.Round(screenPoints[i].x), Mathf.Round(Screen.height - screenPoints[i].y));
                }
                //skip this object if it is out of the screen
                if (IsOutOfBound(objPoints))
                {
                    continue;
                }
                if (this.useNoise)
                {
                    objPoints = ApplyNoise(objPoints);
                }
                hasZeroWidthOrHeight = isWidthOrHeightZero(objPoints);
                if (hasZeroWidthOrHeight)
                {
                    UnityEngine.Debug.LogWarning("block " + blockName + " width or height is zero. Block has been removed from gt");
                    UnityEngine.Debug.LogWarning(objPoints);
                    break;
                }
                paths.Add(objPoints);

            }

            if (outOfBound)
            {
                continue;
            }
            if (hasZeroWidthOrHeight)
            {
                hasZeroWidthOrHeight = false;
                continue;
            }

            string blockID = "";

            blockID = gameObject.GetInstanceID().ToString();
            //string blockID = gameObject.GetInstanceID().ToString();
            ABGameObject abGameObject = gameObject.GetComponent<ABGameObject>();
            float hp;
            if (abGameObject != null)
            {
                hp = abGameObject.getCurrentLife();
            }
            else
            {//return hp as MaxValue if the object is unbreakable  
                hp = float.MaxValue;
            }

            ColorEntry[] objColors = ColorDataLookUp(blockName, this.useNoise);

            //testing only for get color map from screenshot
            //ColorEntry[] objColors = this.ColorPoints(gameObject,objPoints,false);

            string objectType = "Object";
            if (this.devMode)
            {   
                objectType = blockName;
            }
            GTObject externalAgent = new GTObject(blockID, objectType, objColors, paths, hp);
            gtJson = gtJson + externalAgent.ToJsonString(this.devMode) + ',';
            if (isNovelObj && updateNovelList)
            {
                novelIDList.Add(blockID);
            }

        }


        //other blocks
        GameObject[] rectangleBlocks = GameObject.FindGameObjectsWithTag("Rect");
        GameObject[] TNTBlocks = GameObject.FindGameObjectsWithTag("TNT");
        GameObject[] Platforms = GameObject.FindGameObjectsWithTag("Platform");
        GameObject[] allRectBlocks = rectangleBlocks.Concat(TNTBlocks).Concat(Platforms).ToArray();
        foreach (GameObject gameObject in allRectBlocks)
        {
            string blockName = gameObject.GetComponent<SpriteRenderer>().sprite.name;
            List<Vector2[]> paths = new List<Vector2[]>();
            Vector3[] v = new Vector3[4];
            Vector3[] screenPoints = new Vector3[4];
            Vector2[] objPoints = new Vector2[4];
            
            bool isNovelObj = false;

            string objName = gameObject.name;
            if (objName.Contains("novel"))
            {
                isNovelObj = true;
                blockName = gameObject.name;
                blockName = blockName.Replace("(Clone)", "").Trim();
                ABLevel currentLevel = LevelList.Instance.GetCurrentLevel();
                string assetBundlePath = currentLevel.assetBundleFilePath;

                string[] strArray = assetBundlePath.Split(new string[] { "type" }, StringSplitOptions.None);

                string noveltyTypeStr = strArray[strArray.Length - 1].Split(new string[] { "AssetBundle" }, StringSplitOptions.None)[0];
                noveltyTypeStr = noveltyTypeStr.Substring(0, noveltyTypeStr.Length - 1);//remove the file separator

                string noveltyLevelStr = strArray[strArray.Length - 2].Split(new string[] { "novelty_level_" }, StringSplitOptions.None).Last();
                noveltyLevelStr = noveltyLevelStr.Substring(0, noveltyLevelStr.Length - 1);//remove the file separator

                blockName = blockName + "_" + noveltyLevelStr + "_" + noveltyTypeStr;

            }
            /*
                        ABBlock abblock = gameObject.GetComponent<ABBlock>();
                        if(abblock!= null && abblock._material==MATERIALS.novelty){
                            isNovelObj = true;
                        //    UnityEngine.Debug.Log("blocks material is novel");
                        }
                        else{
                            Collider2D collider = gameObject.GetComponent<Collider2D>();
                            if(collider.sharedMaterial!=null){
                                if(collider.sharedMaterial.ToString().Contains("novel")){
                                    isNovelObj = true;
                                }
                            }
                        }*/
            if (gameObject.GetComponent<RectTransform>() == null)
            {
                gameObject.AddComponent<RectTransform>();
            }
            gameObject.GetComponent<RectTransform>().GetWorldCorners(v);
            for (int i = 0; i < 4; i++)
            {
                screenPoints[i] = cam.WorldToScreenPoint(v[i]);
                objPoints[i] = new Vector2(Mathf.Round(screenPoints[i].x), Mathf.Round(Screen.height - screenPoints[i].y));
            }
            //skip this object if it is out of the screen
            if(IsOutOfBound(objPoints)){
                continue;
            }
            if(this.useNoise){
                objPoints = ApplyNoise(objPoints);
            }

            hasZeroWidthOrHeight = isWidthOrHeightZero(objPoints);
            if(hasZeroWidthOrHeight){
                UnityEngine.Debug.LogWarning("block " + blockName + " width or height is zero. Block has been removed from gt");
                UnityEngine.Debug.LogWarning(objPoints);
                hasZeroWidthOrHeight = false;
                continue;
            }

            paths.Add(objPoints);
            string blockID = "";

            blockID = gameObject.GetInstanceID().ToString();
            //string blockID = gameObject.GetInstanceID().ToString();
            ABGameObject abGameObject = gameObject.GetComponent<ABGameObject>();
            float hp;
            if(abGameObject!=null){
                hp = abGameObject.getCurrentLife();
            } 
            else{//return hp as MaxValue if the object is unbreakable  
                hp = float.MaxValue;
            }

            ColorEntry[] objColors = ColorDataLookUp(blockName,this.useNoise);
            string objectType = "Object";
            if(this.devMode){
                if(blockName.Equals("effects_21")){
                    objectType = "Platform";                        
                }

                else if (blockName.Equals("effects_34")){
                    objectType = "TNT";
                }
                else{
                    objectType = blockName;
                }
            }                   
            GTObject rectBlock = new GTObject(blockID, objectType, objColors, paths, hp);
            gtJson = gtJson + rectBlock.ToJsonString(this.devMode)+ ',';
            if(isNovelObj&&updateNovelList){
                novelIDList.Add(blockID);
            }
        }
        //remove last ,
        gtJson = gtJson.Remove(gtJson.Length-1,1);

        gtJson += "]}]";//end features 
        gtJson = RepresentationNovelty.addNoveltyToJSONGroundTruthIfNeeded(gtJson);

        return gtJson;
    }

    public static bool isWidthOrHeightZero(Vector2[] pts){
        bool hasZeroWidthOrHeight = false;
        float maxX = 0;
        float maxY = 0;
        float minX = float.MaxValue;
        float minY = float.MaxValue;

        foreach(Vector2 pt in pts){
            if (pt.x > maxX){
                maxX = pt.x;
            }
            if(pt.y > maxY){
                maxY = pt.y;
            }
            if(pt.x < minX){
                minX = pt.x;
            }
            if(pt.y < minY){
                minY = pt.y;
            }
        }

        if(maxX == minX || maxY==minY){
            hasZeroWidthOrHeight = true;
        }
        
        return hasZeroWidthOrHeight;
    }

    //Apply noise for a list of points
    public Vector2[] ApplyNoise(Vector2[] coords)
    {
        Vector2[] points = new Vector2[coords.Length];

        //preserve the shape of the object
        points = GTNoise.ApplyPositionNoise(coords);

        /*uncomment if do not need shape preservation
        for (int i = 0; i < coords.Length; i++)
        {
            points[i] = GTNoise.ApplyPositionNoise(coords[i]);
        }
        */        
        return points;
    }
    public ColorEntry[] ColorDataLookUp(string objectType,bool useNoise)
    {   

        ColorEntry[] colorMap = null;

        bool colorMapExist = colorData.colorMaps.TryGetValue(objectType, out colorMap);
        if(!colorMapExist){
            UnityEngine.Debug.LogWarning("The color map for Object "+ objectType + " cannot be found!");
            return null;
        }

        if (useNoise){
            float totalPercent = 0f;
            //Noise is adjusted by ensuring that it will always sums to 100%
            //No value should be negative
            for (int i=0; i < colorMap.Length; i++)
            {
                //add noise at 1%
                int noiseDegree = 1;
                colorMap[i].percent = colorMap[i].percent*(1.0f+Mathf.Abs(UnityEngine.Random.Range(noiseDegree* (-1),noiseDegree)/100f));
                if(colorMap[i].percent<=0){
                    //make sure the percentage is positive
                    colorMap[i].percent = 1e-4F;
                }
                totalPercent += colorMap[i].percent;
            }
            //To ensure total percentage sums up to 100
            for (int i =0; i < colorMap.Length; i++)
            {
                colorMap[i].percent = colorMap[i].percent/totalPercent;
            }
        }
        return colorMap;
    }

    //check out of bound vertices
    //naive method that needs to be refined 
    public bool IsOutOfBound(Vector2[] points)
    {
        //return false;//do not check now

        float groundLevel = 0;
        if (GameObject.Find("Ground") != null){
            groundLevel = this.cam.WorldToScreenPoint(GameObject.Find("Ground").GetComponent<BoxCollider2D>().bounds.max).y;
        }

        //adjust origin to top-left
        //This is to gurantee the coordination system is consistent to the original chrome angry birds framework 
        groundLevel = Screen.height - groundLevel;

        int leftOutCount = 0;
        int rightOutCount = 0;
        int buttomOutCount = 0;
        int topOutCount = 0;

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i].x <= 0)
            {
                leftOutCount += 1;
                if(leftOutCount >= points.Length){
                    return true;
                }
            }
            if (points[i].x >= Screen.width)
            {
                rightOutCount += 1;
                if(rightOutCount >= points.Length){
                    return true;
                }
            }
            if (points[i].y <= 0)
            {
                topOutCount += 1;
                if(topOutCount >= points.Length){
                    return true;
                }                
            }
            if (points[i].y >= Mathf.Min(Screen.height,groundLevel))
            {
                buttomOutCount += 1;
                if(buttomOutCount >= points.Length){
                    return true;
                }                
            }
        }
        return false;
    }


    //To obtain colors from Screenshot
    Texture2D TakeScreenshot()
    {
        Texture2D screenimage;

        screenimage = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, true);
        screenimage.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, true);
        screenimage.Apply();

//        screenimage = RepresentationNovelty.addNoveltyToScreenshotIfNeeded(screenimage);
        return screenimage;
    }

    public string GetScreenshotStr()
    {
        //yield return new WaitForEndOfFrame();
        Texture2D screenimage = TakeScreenshot();

        byte[] byteArray = screenimage.EncodeToPNG();
        return System.Convert.ToBase64String(byteArray);
    }

    //low chance to be duplicated
    //a check maybe unnecessary
    public string getTrajID(){
        
        string trajID = "2147483647";
        return trajID ;
    }

    //This function obtains colors from the screenshot
    //Quantize the color and returns the colormap
    public ColorEntry[] ColorPoints(GameObject gameObject, Vector2[] objPoints, bool use_noise)
    {

        Texture2D screenimage = TakeScreenshot();
        //Check if gameobject has the collider
        Collider2D objCollider;
        if (gameObject.GetComponent<PolygonCollider2D>() != null)
        {
            objCollider = gameObject.GetComponent<PolygonCollider2D>();
        }
        else
        {
            objCollider = gameObject.GetComponent<BoxCollider2D>();
        }

        //Obtaining the bounding box for various colliders 
        var min_x = objPoints[0].x;
        var max_x = objPoints[0].x;
        var min_y = Screen.height - objPoints[0].y;
        var max_y = Screen.height - objPoints[0].y;

        for (int i = 1; i < objPoints.Length; i++)
        {
            min_x = Mathf.Min(objPoints[i].x, min_x);
            max_x = Mathf.Max(objPoints[i].x, max_x);
            min_y = Mathf.Min(Screen.height - objPoints[i].y, min_y);
            max_y = Mathf.Max(Screen.height - objPoints[i].y, max_y);
        }

        var width = (int)(max_x - min_x);
        var height = (int)(max_y - min_y);

        //Obtaining colors from the screenshot
        Color[] colorcoords = new Color[width * height];
        int index = 0;
        for (int x = (int)min_x; x < (int)max_x; x++)
        {
            for (int y = (int)min_y; y < (int)max_y; y++)
            {
                Vector3 point = new Vector3(x, y, 0);
                //point conversion to check overlap
                point = cam.ScreenToWorldPoint(point);
                Vector2 point_tocheck = new Vector2(point.x, point.y);
                if (objCollider.OverlapPoint(point_tocheck))
                {
                    colorcoords[index] = screenimage.GetPixel(x, y);
//                    UnityEngine.Debug.Log("colour " + colorcoords[index]);
                    index++;
                }
            }
        }
        UnityEngine.Debug.Log("colorcoords count " + colorcoords.Length);

        //Color Quantization
        Dictionary<int, int> histogram = new Dictionary<int, int>();
        for (int i = 0; i < colorcoords.Length; i++)
        {
            if(colorcoords[i] == Color.clear) {
                UnityEngine.Debug.Log("colorcoords color " + i + " is clear");
                UnityEngine.Debug.Log("colorcoords color " + i + " is " + colorcoords[i]);
            }
            if (colorcoords[i] != Color.clear)
            {
                UnityEngine.Debug.Log("colorcoords color " + i + " is not clear");
                UnityEngine.Debug.Log("colorcoords color " + i + " is " + colorcoords[i]);

                //convert 24-bit color to 8-bit as RRRGGGBB
                Color color = colorcoords[i];
                byte eight_bit_color = 0b_0000_0000;
                byte three_bit_red = (byte)((int)(color.r * 8) << 5);
                byte three_bit_green = (byte)((int)(color.g * 8) << 2);
                byte two_bit_blue = (byte)(int)(color.b * 4);

                eight_bit_color = (byte)(three_bit_red | three_bit_green | two_bit_blue);

                if (histogram.ContainsKey(eight_bit_color))
                {
                    histogram[eight_bit_color]++;
                }
                else
                {
                    histogram[eight_bit_color] = 1;
                }
            }
        }

        float totalcount = 0.00f;
        //int length_of_histogram = 0;
        int noise_added = 0;
        float noise_amount = 0f;
        int noise_degree = 0;

        //Add noise of +/- 2% of the actual color count 
        ColorEntry[] cols = new ColorEntry[histogram.Count];
        if (use_noise)
        {
            noise_added = 1;
            //Add noise according to novelty
            if (ABGameWorld.noveltyTypeForNovelty3 == 12)
            {
                noise_degree = 5;
            }
            else
            {
                noise_degree = 2;
            }
        }
        int ind = 0;
        foreach (int key in histogram.Keys)
        {
            noise_amount = noise_added * UnityEngine.Random.insideUnitCircle.x * histogram[key] * noise_degree / 100;
            int count = histogram[key] + (int)noise_amount;
            cols[ind].color = key;
            cols[ind].percent = Mathf.Abs(histogram[key] + (int)noise_amount);
            ind += 1;
            totalcount += count;
        }

        for (int i = 0; i < cols.Length; i++)
        {
            cols[i].percent = cols[i].percent / totalcount;
        }

        return cols;
    }

}