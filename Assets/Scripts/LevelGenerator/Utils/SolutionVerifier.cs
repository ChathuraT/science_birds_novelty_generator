using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Linq;
using System.Linq;
using System.Xml.XPath;
using System;
using System.IO;

public class SolutionVerifier : ABSingleton<SolutionVerifier>
{
    /*
     This class is used to facilitate running different solutions for a given scenario and check if the scenario can be solved by those solutions
    */

    public string currentScenrioID;
    public int currentlyExecutingShootingAngleIndex;
    XDocument xmlDoc;
    List<(string, Vector2)> targetObjects; // List of (object name, object location)
    List<(string, Vector2, float, string)> shootingAngles; // List of (object name, object location, shooting angle, trajectry H/L) - too much details just to access for the levelplayoutcome recorder

    // solution's target object info
    private string solutionTargetObject;
    private Vector2 solutionTargetObjectPosition;
    private string solutionShootingAngle;
    private string solutionTrajectory;

    public void setCurrentScenrioID(string scenrioID)
    {
        if (scenrioID != currentScenrioID)
        { // playing level is from a new scenario
            currentScenrioID = scenrioID;
            xmlDoc = XDocument.Load(ABGameWorld.Instance.levelInfoFolder + currentScenrioID + "_info.xml");
            targetObjects = new List<(string, Vector2)>();
            shootingAngles = new List<(string, Vector2, float, string)>();

            // get the target objects and the shooting angles
            getTheTargetObjects();
            generteShootingAngles();
            currentlyExecutingShootingAngleIndex = 0;

            Debug.Log("SolutionVerifier set the scenario id to " + currentScenrioID + " and generated the new shooting angles");

        }
    }

    public float getCurrentShootingAngle()
    {
        Debug.Log("shooting at the object: " + shootingAngles[currentlyExecutingShootingAngleIndex].Item1 + " trajectory: " + shootingAngles[currentlyExecutingShootingAngleIndex].Item4);
        return shootingAngles[currentlyExecutingShootingAngleIndex].Item3;
    }

    public bool determineTheNextPlay()
    {
        /*
         Determine what to do next. If all shooting angles are done move to the next level, otherwise move to the next shooting angle (of the same level)
        */

        if (currentlyExecutingShootingAngleIndex == (shootingAngles.Count - 1)) // all the shooting angles are played, move to the next scenario 
        {
            return true;
        }
        else
        { // more shooting angles are remaining to play, move to the next angle
            currentlyExecutingShootingAngleIndex++;
            return false;
        }
    }

    public void WriteOutcomeFile(LevelPlayOutcome outcomeRecorder, string outcomeRecorderFile)
    {
        /*
            This function writes the outcome of the gameplay in the csv file
        */

        // add the current play data to the outcomeRecorder first
        AddInFoToTheOutcomeRecorder(outcomeRecorder);

        // Create or append a line to the outcomeRecorderFile
        StreamWriter writer = File.AppendText(outcomeRecorderFile);

        // Convert the outcomeRecorder object parameters to comma-separated string
        string csvLine = string.Join(",", outcomeRecorder.scenarioID, outcomeRecorder.targetObject, outcomeRecorder.targetObjectPosition,
            outcomeRecorder.isSolutionTarget, outcomeRecorder.releaseAngle, outcomeRecorder.trajectory, outcomeRecorder.isPassed);

        // Write the string to the file
        writer.WriteLine(csvLine);
        writer.Close();

    }

    private void AddInFoToTheOutcomeRecorder(LevelPlayOutcome outcomeRecorder)
    {

        outcomeRecorder.targetObject = shootingAngles[currentlyExecutingShootingAngleIndex].Item1;
        outcomeRecorder.targetObjectPosition = shootingAngles[currentlyExecutingShootingAngleIndex].Item2.x.ToString() + " " + shootingAngles[currentlyExecutingShootingAngleIndex].Item2.x.ToString();
        outcomeRecorder.isSolutionTarget = checkIfCurrentTargetIsTheSolutionTarget();
        outcomeRecorder.releaseAngle = shootingAngles[currentlyExecutingShootingAngleIndex].Item3.ToString();
        outcomeRecorder.trajectory = shootingAngles[currentlyExecutingShootingAngleIndex].Item4;
    }

    private bool checkIfCurrentTargetIsTheSolutionTarget()
    {
        // compare the object name and the position
        return ((shootingAngles[currentlyExecutingShootingAngleIndex].Item1 == solutionTargetObject) & (shootingAngles[currentlyExecutingShootingAngleIndex].Item2 == solutionTargetObjectPosition));
    }

    private void getTheSolutionTargetObjecatInfo()
    {
        /*
           find the intended solutions' target object information
         */

        solutionTargetObject = xmlDoc.Descendants("solution").Elements("targetObject").Elements("scenarioObjectName").FirstOrDefault()?.Value;
        solutionShootingAngle = xmlDoc.Descendants("solution").Elements("releaseAngle").FirstOrDefault()?.Value;
        solutionTrajectory = xmlDoc.Descendants("solution").Elements("trajectory").FirstOrDefault()?.Value;
        float x = float.Parse(xmlDoc.Descendants("solution").Elements("targetObject").Elements("position").Elements("x").FirstOrDefault()?.Value);
        float y = float.Parse(xmlDoc.Descendants("solution").Elements("targetObject").Elements("position").Elements("y").FirstOrDefault()?.Value);
        solutionTargetObjectPosition = new Vector2(x, y);
    }


    private void getTheTargetObjects()
    {
        /*
            read the scenario info file and read the potential target object to the bird (the distraction objects and the other dynamic objects (i.e. excludes platforms) in the layout)
         */

        Debug.Log("Finding the target objects");

        // first find the solution target obejct's info, it is needed to exclude from the target obejcts
        getTheSolutionTargetObjecatInfo();

        // Get the scenarioObject->layoutObjects and scenarioObject->distractionObjects
        var layoutObjects = xmlDoc.Root.Elements("scenarioObjects").Elements("layoutObjects").Elements("ScenarioObject");
        var distractionObjects = xmlDoc.Root.Elements("scenarioObjects").Elements("distractionObjects").Elements("ScenarioObject");
        string objectName;
        float x, y;
        foreach (var scenarioObject in layoutObjects.Concat(distractionObjects))
        {
            objectName = scenarioObject.Elements("scenarioObjectName").FirstOrDefault()?.Value;
            // skip birds and platforms (for now)
            if (objectName != "Platform" & objectName != "BirdRed")
            {
                Debug.Log(objectName);
                x = float.Parse(scenarioObject.Elements("position").Elements("x").FirstOrDefault()?.Value);
                y = float.Parse(scenarioObject.Elements("position").Elements("y").FirstOrDefault()?.Value);
                targetObjects.Add((objectName, new Vector2(x, y)));
            }
        }

        // for bouncing related scenarios (BouncingBird and BouncingBirdFallingObject) the target object is a platform, in such cases add the solution's platform specifically
        if (currentScenrioID.Contains("BouncingBird"))
        {
            targetObjects.Add((solutionTargetObject, solutionTargetObjectPosition));
        }
    }

    private void generteShootingAngles()
    {
        /*
         This function generates the release angles for the targetObjects. One target object is attacked with 2 trajectories high and low, except the cases where the object is not reachable
         The function also adds the solution shooting angle as well
         */

        var bird = ABGameWorld.Instance._birds[0]; // get the bird on the sling

        float g = -9.81f * 0.48f;  // environment gravity multiplied by the birds launch gravity

        float v = 9.65f;
        float v2 = v * v;
        float v4 = v2 * v2;

        float lowerTrajTheta, higherTrajTheta;
        foreach (var targetObject in targetObjects)
        {
            Debug.Log("object considered: " + targetObject.Item1);

            float x = targetObject.Item2.x - bird.transform.position.x; // target point x relative to the bird position
            float y = targetObject.Item2.y - bird.transform.position.y; // target point y relative to the bird position
            float x2 = x * x;

            // for heigher trajectory
            lowerTrajTheta = Mathf.Atan2(v2 - Mathf.Sqrt(v4 - g * (g * x2 - 2 * y * v2)), g * x);

            // for lower trajectory
            higherTrajTheta = Mathf.Atan2(v2 + Mathf.Sqrt(v4 - g * (g * x2 - 2 * y * v2)), g * x);

            // adjust the angle as the results from the above equation are mirrored angles
            lowerTrajTheta = (float)Math.PI - lowerTrajTheta;
            higherTrajTheta = (float)Math.PI - higherTrajTheta;

            // if angles exist (if the objects are unreachable, angles dosen't exist), add them to the shootingAngles dictionary
            if (!float.IsNaN(lowerTrajTheta))
            {
                shootingAngles.Add((targetObject.Item1, targetObject.Item2, lowerTrajTheta, "L"));
                Debug.Log("shooting angle added - lowerTrajTheta: " + lowerTrajTheta);

            }
            if (!float.IsNaN(higherTrajTheta))
            {
                shootingAngles.Add((targetObject.Item1, targetObject.Item2, higherTrajTheta, "H"));
                Debug.Log("shooting angle added - higherTrajTheta: " + higherTrajTheta);

            }
        }

        // add the shooting angle of the solution as well
        shootingAngles.Add((solutionTargetObject, solutionTargetObjectPosition, float.Parse(solutionShootingAngle), solutionTrajectory));
    }
}

