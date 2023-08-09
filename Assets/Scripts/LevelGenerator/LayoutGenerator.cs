using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LayoutGenerator
{

    private Scenario scenario;
    private Utilities utilities;
    private int numberOfAttemptsToRetry = 10; // it will retry this many times to build the dimension graph and solve the constraints before giving up (buidding the dimension graph may result in choosing different QSR relations which may solve the unsolvability of the constraints)

    public LayoutGenerator(Scenario scenario, Utilities utilities)
    {
        this.scenario = scenario;
        this.utilities = utilities;

    }

    public void GenerateLayout()
    {
        // generate the layout of the level considering the constraints in the layout terms

        // build the layout constraint graph
        LayoutConstraintGraph layoutConstraintGraph = new LayoutConstraintGraph(scenario);
        layoutConstraintGraph.PrintLayoutConstraintGraph();

        // using the layout contraint graph, infer the QSR relations for the onLocation, inDirection and touching terms
        QSRPredicateInferrer qSRPredicateInferrer = new QSRPredicateInferrer(scenario.scenarioName, scenario.scenarioIndex);
        qSRPredicateInferrer.InferQSRPredicatesForLayoutConstraintGraph(layoutConstraintGraph);

        int attemptsMade = 0;
        while (true)
        {
            attemptsMade++;

            if (attemptsMade > numberOfAttemptsToRetry)
            {
               Debug.Log("Could not build and solve the dimension graph even after " + attemptsMade + " attempts!");
               return;
            }

            try
            {
                Debug.Log("At the start of the GenerateLayout(), attempt: " + attemptsMade + " numberOfAttemptsToRetry: " + numberOfAttemptsToRetry);

                // build the dimension graphs using the QSR relations
                DimensionGrapgh dimensionGraph = new DimensionGrapgh(layoutConstraintGraph);

                // check the consistency of the dimension graph (currently for onLocation, inDirection and touching terms)
                dimensionGraph.CheckConsistancyAndFixPossible();
                Debug.Log("xCoordinateDimensionGraph");
                dimensionGraph.PrintDimensionGraph(dimensionGraph.xCoordinateDimensionGraph);
                Debug.Log("yCoordinateDimensionGraph");
                dimensionGraph.PrintDimensionGraph(dimensionGraph.yCoordinateDimensionGraph);

                // build the NQSRAndDimensionGraph using the NQSR relations and the QSR relations (=dimension graph), it includes all the constraints associated with both the QSR and NQSR relations
                List<Object> allObjects = utilities.GetAllObjects(layoutConstraintGraph);
                NQSRAndDimensionGraph relationGraph = new NQSRAndDimensionGraph(allObjects, layoutConstraintGraph, dimensionGraph.xCoordinateDimensionGraph, dimensionGraph.yCoordinateDimensionGraph);

                // determine the positions of the gameobjects by solving the NQSRAndDimensionGraphs
                ConstraintSolver constraintSolver = new ConstraintSolver(allObjects, relationGraph);
                constraintSolver.SolveConstraintsAndDetermineLocations();

                // start the simulation solver when the rough locations are determined by solving the NQSRAndDimensionGraphs
                InitializeSimulation(); // initializing the simulation will load the gameworld scene and then the simulation solver will start working
                SimulationSolver.Instance.scenario = scenario;
                SimulationSolver.Instance.utilities = utilities;
                SimulationSolver.Instance.constraintSolver = constraintSolver;
                Debug.Log("assigned scenario and utilities for the simulationsolver!");
                //SimulationSolver.Instance.SimulateAndSolve();

                // CSPSolverTest cpsolver = new CSPSolverTest();

                return;
            }
            catch (System.Exception e)
            {
                Debug.Log(e);
                Debug.Log("Retrying building the dimension graph and solving the constraints. Attempts made: " + attemptsMade + " numberOfAttemptsToRetry: " + numberOfAttemptsToRetry);
            }
        }

    }

    private void InitializeSimulation()
    {
        /*
         initialize the simulation using the objects in the scenario      
         */

        // get the ABGameLevel from the scenario
        ABLevel gameLevel = utilities.GetGameLevel(scenario);

        // immortalize the blocks in the game level (except pigs)


        //utilities.PrintLevel(gameLevel);
        utilities.SimulateLevel(gameLevel);

        //while (SceneManager.GetActiveScene().buildIndex == 4) {
        //    Debug.Log("waiting until GetActiveScene().buildIndex == 4");
        //    break;
        //}

    }

    private void DetermineObjectLocationsFromDimensionGraph(LayoutConstraintGraph layoutConstraintGraph, DimensionGrapgh dimensionGraph)
    {
        /*
         This function determines the position (x,y coordinates) of the game objects in the game level space. 
         Input is a consistant dimension graph and the determined locations will be updated in the Objects's positionX, positionY variables
        */

        int mainObjectIndex = -1; // row
        int secondObjectIndex; // column

        foreach (MainObjectAndAllLayoutGrammars mainObjectAndAllLayoutGrammars in layoutConstraintGraph.mainObjectAndAllLayoutGrammarsList)
        {
            Debug.Log("* Main Object: " + mainObjectAndAllLayoutGrammars.mainObjct);
            mainObjectIndex++;
            secondObjectIndex = -1;

            // skip birds
            if (mainObjectAndAllLayoutGrammars.mainObjct.GetType().BaseType == typeof(Bird))
            {
                Debug.Log("-- Skipped Object: " + mainObjectAndAllLayoutGrammars.mainObjct);
                continue;
            }

            // test code - remove
            if (mainObjectAndAllLayoutGrammars.mainObjct.GetType() == typeof(BasicSmall))
            {
                Debug.Log("-- Found the pig: " + mainObjectAndAllLayoutGrammars.mainObjct.associatedPrefab);
                Debug.Log(string.Join(", ", mainObjectAndAllLayoutGrammars.mainObjct.size));

            }

            bool mainObjectLocationSet = false;
            bool secondObjectLocationSet = false;

            foreach (ObjectAndLayoutGrammars objectAndLayoutGrammars in mainObjectAndAllLayoutGrammars.objectAndLayoutGrammarsList)
            {
                Debug.Log("-- Object: " + objectAndLayoutGrammars.objct);
                secondObjectIndex++;

                // skip same object connections
                if (mainObjectIndex == secondObjectIndex)
                {
                    continue;
                }

                // check if there are connections between the two objects
                bool objectsConnected = false;
                for (int p = 0; p < 3; p++)
                {
                    for (int q = 0; q < 3; q++)
                    {
                        if (dimensionGraph.xCoordinateDimensionGraph[mainObjectIndex * 3 + p, secondObjectIndex * 3 + q] != 0)
                        {
                            objectsConnected = true;
                            // check if the main object has been already placed, if not place it in a free space
                            if (!mainObjectLocationSet & (mainObjectAndAllLayoutGrammars.mainObjct.positionX == -100))
                            {
                                // set the mainobject's initial location
                                if (utilities.GetAnUnoccupiedReachableSpace(layoutConstraintGraph, mainObjectAndAllLayoutGrammars.mainObjct))
                                {
                                    mainObjectLocationSet = true;
                                    Debug.Log("Succesfully placed the main object: " + mainObjectAndAllLayoutGrammars.mainObjct);

                                }
                                else
                                {
                                    throw new System.Exception("Could not place the main object!");
                                }
                            }

                        }
                    }
                }

                // if the there are connections between the two objects satisfy them, handling both (mainObject -> secondObject) and (secondObject -> mainObject) connections
                if (objectsConnected)
                {
                    Debug.Log("Placing " + objectAndLayoutGrammars.objct + " in relative to the " + mainObjectAndAllLayoutGrammars.mainObjct);

                    // check whether there are (connections between ur and ll positions) or (= connections), if so the object can be directly placed as no additional connections are needed to check
                    if (dimensionGraph.xCoordinateDimensionGraph[mainObjectIndex * 3 + 2, secondObjectIndex * 3 + 0] != 0) // (mainObject->secondObject ur and ll positions)
                    {
                        // to do

                        secondObjectLocationSet = true;
                    }
                    else if (dimensionGraph.xCoordinateDimensionGraph[secondObjectIndex * 3 + 2, mainObjectIndex * 3 + 0] != 0) // (secondObject->mainObject ur and ll positions)
                    {
                        // to do
                        secondObjectLocationSet = true;
                    }
                    else if (!secondObjectLocationSet)   // (mainObject->secondObject = connections)
                    {
                        for (int p = 0; p < 3; p++)
                        {
                            for (int q = 0; q < 3; q++)
                            {
                                // check if there are = connections (only one = can be there)
                                if (dimensionGraph.xCoordinateDimensionGraph[mainObjectIndex * 3 + p, secondObjectIndex * 3 + q] == 1)
                                {
                                    // to do
                                    secondObjectLocationSet = true;
                                }
                            }
                        }
                    }
                    else if (!secondObjectLocationSet)   // (secondObject->mainObject = connections)
                    {
                        for (int p = 0; p < 3; p++)
                        {
                            for (int q = 0; q < 3; q++)
                            {
                                // check if there are = connections (only one = can be there)
                                if (dimensionGraph.xCoordinateDimensionGraph[secondObjectIndex * 3 + p, mainObjectIndex * 3 + q] == 1)
                                {
                                    // to do
                                    secondObjectLocationSet = true;
                                }
                            }
                        }
                    }

                    // if not yet secondObjectLocationSet then find a using other connections

                }

            }
        }
    }

}



