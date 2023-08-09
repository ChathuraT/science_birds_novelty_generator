using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class LevelGenerator : MonoBehaviour
{
    public Utilities utilities;

    // Start is called before the first frame update
    void Start()
    {
        // avoid destroying this object even after moving to another scene
        DontDestroyOnLoad(this.gameObject);

        this.utilities = new Utilities();

        GenerationSchema();

        // end the play - remove this later
        // EditorApplication.isPlaying = false;

    }

    // Update is called once per frame
    void Update()
    {

    }

    void GenerationSchema()
    {

        LayoutInferrer layoutInferer = new LayoutInferrer(utilities);

        // get a scenario 
        Scenario scenario = GetScenario();

        // infer the layout grammar and add groupIDs for the objects (maintain the function calling order - InferLayout (to fill the scenario.gameObects array)  -> AddGroupIDs (gameObects array is needed to add the group IDs) -> RefineLayout (group IDs are needed to determine the groupID of the newly introudced objects))
        layoutInferer.InferLayout(scenario);
        utilities.AddGroupIDs(scenario);
        // add supporting platforms for the objects without any support
        layoutInferer.AddSupportPlatforms(scenario);

        // initialize the layoutGenerator
        LayoutGenerator layoutGenerator = new LayoutGenerator(scenario, utilities);

        layoutGenerator.GenerateLayout();

    }

    Scenario GetScenario()
    {

        // return a predefined scenario

        // Scenario scenario = new PredefinedScenarios().RollingObjectNovel1();
        // Scenario scenario = new PredefinedScenarios().RollingObjectNovel2();

        // Scenario scenario = new PredefinedScenarios().FallingObjectNovel1();
        // Scenario scenario = new PredefinedScenarios().FallingObjectNovel2();
        // Scenario scenario = new PredefinedScenarios().FallingObjectNove13();
        // Scenario scenario = new PredefinedScenarios().FallingObjectNovel4();
        // Scenario scenario = new PredefinedScenarios().FallingObjectNovel5();
        // Scenario scenario = new PredefinedScenarios().FallingObjectNovel6();
        // Scenario scenario = new PredefinedScenarios().FallingObjectNovel7();
        // Scenario scenario = new PredefinedScenarios().FallingObjectNovel8();

        // Scenario scenario = new PredefinedScenarios().SlidingObjectNovel1();
        // Scenario scenario = new PredefinedScenarios().SlidingObjectNovel2();


        Scenario scenario = new PredefinedScenarios().RollingFallingObjectNovel1();
        // Scenario scenario = new PredefinedScenarios().RollingFallingObjectNovel2();
        // Scenario scenario = new PredefinedScenarios().RollingFallingObjectNovel3();
        // Scenario scenario = new PredefinedScenarios().RollingFallingObjectNovel4();
        // Scenario scenario = new PredefinedScenarios().RollingFallingObjectNovel5();
        //Scenario scenario = new PredefinedScenarios().RollingFallingObjectNovel6();




        // Scenario scenario = new PredefinedScenarios().SingleForce(); // no need to test for both higher and lower trjectories
        // Scenario scenario = new PredefinedScenarios().SingleForceTopBlocked(); // no need to test for higher trjectories
        // Scenario scenario = new PredefinedScenarios().SingleForceLeftBlocked(); // no need to test for lower trjectories

        // Scenario scenario = new PredefinedScenarios().MultipleForces();
        // Scenario scenario = new PredefinedScenarios().MultipleForcesTopBlocked();
        // Scenario scenario = new PredefinedScenarios().MultipleForcesLeftBlocked();


        // Scenario scenario = new PredefinedScenarios().RollingObject();
        // Scenario scenario = new PredefinedScenarios().FallingObject();
        // Scenario scenario = new PredefinedScenarios().SlidingObject(); // only low trajectories are necessary
        // Scenario scenario = new PredefinedScenarios().BouncingBird(); // simulate separately for higher and lower trajectories (as the liesOnPath solver favours the most path cutting position and it misses the variations) -  trajectorySimulator.CalculateTargtePoints() for LT only have centre left(LT) and upper left(LT) points, for HT all 3 points, QSRPredicateInferrer.getInDirectionQSRPredicates(), constraintSolver.getLowerAndUpperBound() change the bounds of the bird, scenarios.cs change the size of the slope, pathobstructed solver adjust topWallStartAdjustment


        // Scenario scenario = new PredefinedScenarios().RollingFallingObject(); 
        // Scenario scenario = new PredefinedScenarios().RollingSlidingObject(); 
        // Scenario scenario = new PredefinedScenarios().FallingRollingObject();
        // Scenario scenario = new PredefinedScenarios().SlidingRollingObject();
        // Scenario scenario = new PredefinedScenarios().BouncingBirdFallingObject(); // set NQSRAndDimensionGraph whatIsFar value to 1, simulate separately for higher and lower trajectories (trajectorySimulator CalculateTargtePoints() functions, QSRPredicateInferrer getInDirectionQSRPredicates() and getFarQSRPredicates()), scenario.cs change the fallableObject accordingly, set the variable.value assignments to the least values in the function CSP.AssignValuesToLeastAndAssocVariables()
        // Scenario scenario = new PredefinedScenarios().BouncingBirdRollingObject(); //[no feasible levels] set the variable.value assignments to the least values in the function CSP.AssignValuesToLeastAndAssocVariables(), set NQSRAndDimensionGraph whatIsFar value to 1, simulate ONLY for higher trajectories (trajectorySimulator GetVelocityOfBirdV2() and CalculateTargtePoints() functions, QSRPredicateInferrer getInDirectionQSRPredicates() and getFarQSRPredicates())


        // Scenario scenario = new PredefinedScenarios().SlidingRollingFallingObject();
        // Scenario scenario = new PredefinedScenarios().SlidingFallingRollingObject();
        // Scenario scenario = new PredefinedScenarios().RollingRollingRollingObject();
        // Scenario scenario = new PredefinedScenarios().RollingRollingFallingObject();

        return scenario;
    }

}
