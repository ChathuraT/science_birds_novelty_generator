using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// using Decider.Csp.BaseTypes;
// using Decider.Csp.Integer;
// using Decider.Csp.Global;

public class ConstraintSolver
{
    /*
        This class converts the relation graphs into a set of constraints that can be fed into the CSP solver. Then solve the constraints using the CSP solver ans assign the locations of the game objects
     */
    public List<Object> allObjects;
    private NQSRAndDimensionGraph relationGraph;

    private int precisionMultiplier = Utilities.precisionMultiplier; // the (float) numbers are multiplied with this constant to make them larger integers as all the floats are needed to be represented as ints in the solver, minimum is 100 
    private List<Expression> constraints;
    private Dictionary<string, Variable> variableNamesAndVariableInteger; // to store the string name of the variable and its VariableInteger

    // values related to the NQSR Relations
    private float whatIsFar = 3; // things apart from this value is considered as far (used for Far)

    public ConstraintSolver(List<Object> allObjects, NQSRAndDimensionGraph relationGraph)
    {
        this.allObjects = allObjects;
        this.relationGraph = relationGraph;
    }


    public void GetVariablesAndConstraintsFromRelationGraph(string dimensionConsidered)
    {
        /*
         * This function populates the variables and constraints lists by considering the connections in the dimension graph
         */

        this.variableNamesAndVariableInteger = new Dictionary<string, Variable>();
        this.constraints = new List<Expression>();
        (int, int)[,] coordinateNQSRGraph = null;

        if (dimensionConsidered == "x")
        {
            coordinateNQSRGraph = relationGraph.xCoordinateRelationGraph;
        }
        else if (dimensionConsidered == "y")
        {
            coordinateNQSRGraph = relationGraph.yCoordinateRelationGraph;
        }

        relationGraph.PrintNQSRGraph(coordinateNQSRGraph);

        int totalNumberOfObjects = allObjects.Count;

        // iterate over the relation graph and findout the constraints
        for (int mainObjectIndex = 0; mainObjectIndex < totalNumberOfObjects; mainObjectIndex++)
        {
            // first check whether the main object has any conenctions with the other objects
            for (int secondObjectIndex = 0; secondObjectIndex < totalNumberOfObjects; secondObjectIndex++)
            {
                for (int p = 0; p < 5; p++)
                {
                    for (int q = 0; q < 5; q++)
                    {
                        if ((mainObjectIndex * 5 + p) == (secondObjectIndex * 5 + q)) // skip same point connections as they are not needed
                        {
                            continue;
                        }
                        //else if ((mainObjectIndex == secondObjectIndex) && (p == 0) && (q == 2)) // skip the connection between ll->ur of the same object is covered by ll->c and c->ur connection
                        //{
                        //    continue;
                        //}

                        int connection = coordinateNQSRGraph[mainObjectIndex * 5 + p, secondObjectIndex * 5 + q].Item1;
                        if (connection != 0) // only consider  points where there is a connection
                        {
                            //// create names for the variables using the indexes of the objects
                            string mainObjectPointVariableName = mainObjectIndex.ToString() + "_" + p.ToString();
                            string secondObjectPointVariableName = secondObjectIndex.ToString() + "_" + q.ToString();

                            //// add these varibles to the variables list if they are not already there, the lower and higher limits depend on the dimension
                            int[] boundries;
                            if (dimensionConsidered == "x") // for x dimension, lowest and highest points are X_MIN_REACHABLE and X_MAX_REACHABLE
                            {
                                if (!variableNamesAndVariableInteger.ContainsKey(mainObjectPointVariableName))
                                {
                                    boundries = getLowerAndUpperBound("x", allObjects[mainObjectIndex]);
                                    variableNamesAndVariableInteger.Add(mainObjectPointVariableName, new Variable(mainObjectPointVariableName, boundries[0], boundries[1]));
                                }
                                if (!variableNamesAndVariableInteger.ContainsKey(secondObjectPointVariableName))
                                {
                                    boundries = getLowerAndUpperBound("x", allObjects[secondObjectIndex]);
                                    variableNamesAndVariableInteger.Add(secondObjectPointVariableName, new Variable(secondObjectPointVariableName, boundries[0], boundries[1]));
                                }
                            }
                            else if (dimensionConsidered == "y") // for y dimension, lowest point is Y_MIN_REACHABLE and the highest point depends on the x_position (therefore x coordinates are needed to be determined first before determining the y coordinates)
                            {
                                if (!variableNamesAndVariableInteger.ContainsKey(mainObjectPointVariableName))
                                {
                                    boundries = getLowerAndUpperBound("y", allObjects[mainObjectIndex]);
                                    // float yMaxReachable = 9; //TODO: remove this and uncomment above
                                    Debug.Log(allObjects[mainObjectIndex] + "xCoordinate: " + allObjects[mainObjectIndex].positionX + " yMaxReachable: " + boundries[1]);
                                    variableNamesAndVariableInteger.Add(mainObjectPointVariableName, new Variable(mainObjectPointVariableName, boundries[0], boundries[1]));
                                }
                                if (!variableNamesAndVariableInteger.ContainsKey(secondObjectPointVariableName))
                                {
                                    boundries = getLowerAndUpperBound("y", allObjects[secondObjectIndex]);
                                    // float yMaxReachable = 9;
                                    variableNamesAndVariableInteger.Add(secondObjectPointVariableName, new Variable(secondObjectPointVariableName, boundries[0], boundries[1])); ;
                                }
                            }

                            //// add the constraint to the constraints list

                            // determine the leftoperator and k considering the value of the offset (item2), the format in the constraints is a +/- k = b, k is a positive integer
                            int k;
                            string leftOperator;
                            int offset = coordinateNQSRGraph[mainObjectIndex * 5 + p, secondObjectIndex * 5 + q].Item2;
                            if (offset < 0)
                            {
                                leftOperator = "-";
                                k = -1 * offset;
                            }
                            else
                            {
                                leftOperator = "+";
                                k = offset;
                            }

                            if (connection == 1)
                            {
                                Debug.Log(mainObjectPointVariableName + " " + leftOperator + " " + k + " == " + secondObjectPointVariableName);
                                constraints.Add(new Expression(variableNamesAndVariableInteger[mainObjectPointVariableName], leftOperator, k, "==", variableNamesAndVariableInteger[secondObjectPointVariableName]));
                            }
                            else if (connection == 2)
                            {
                                Debug.Log(mainObjectPointVariableName + " " + leftOperator + " " + k + " < " + secondObjectPointVariableName);
                                constraints.Add(new Expression(variableNamesAndVariableInteger[mainObjectPointVariableName], leftOperator, k, "<", variableNamesAndVariableInteger[secondObjectPointVariableName]));
                            }
                            else if (connection == 3)
                            {
                                Debug.Log(mainObjectPointVariableName + " " + leftOperator + " " + k + " <= " + secondObjectPointVariableName);
                                constraints.Add(new Expression(variableNamesAndVariableInteger[mainObjectPointVariableName], leftOperator, k, "<=", variableNamesAndVariableInteger[secondObjectPointVariableName]));
                            }
                        }
                    }
                }
            }
        }
        Debug.Log("Completed adding variables and constraints for " + dimensionConsidered + " dimension graph");
    }

    public void SolveConstraintsAndDetermineLocations()
    {
        /*
         This function solves the constraints in the x and y dimension graphs and determine the location (x, y coordinates) of the objects and assign them to the objects
        */
        string[] dimensions = { "x", "y" }; // always solve in x and y order as x coordinates are needed to be determine the maximum y coordinates
        foreach (string dimension in dimensions)
        {
            // get varibles and constraints from the dimension graph 
            Debug.Log("Checking QSR Relations for dimension: ==============================================" + dimension);
            GetVariablesAndConstraintsFromRelationGraph(dimension);

            // solve constraints using the CSP class
            Debug.Log("Solving constraints of the dimension: " + dimension);
            CSP cspSolver = new CSP(new List<Variable>(variableNamesAndVariableInteger.Values), constraints, dimension);
            cspSolver.Solve();

            foreach (Variable variable in variableNamesAndVariableInteger.Values)
            {
                Debug.Log("variable: " + variable.name + " value: " + variable.value);
            }

            // assign the solution values to the objects coordinates
            foreach (Variable variable in variableNamesAndVariableInteger.Values)
            {
                // only the centre point is required (which is x_1)
                if (Convert.ToInt32(variable.name.Split('_')[1]) == 1)
                {
                    if (dimension == "x")
                    {
                        allObjects[Convert.ToInt32(variable.name.Split('_')[0])].positionX = (float)variable.value / precisionMultiplier;
                        // Debug.Log(variable.name.Split('_')[0] + " " + allObjects[Convert.ToInt32(variable.name.Split('_')[0])].positionX);
                    }
                    else if (dimension == "y")
                    {
                        allObjects[Convert.ToInt32(variable.name.Split('_')[0])].positionY = (float)variable.value / precisionMultiplier;
                        // Debug.Log(variable.name.Split('_')[0] + " " + allObjects[Convert.ToInt32(variable.name.Split('_')[0])].positionY);
                    }
                }
            }
        }
        // print the objects' final locations
        foreach (Object objct in allObjects)
        {
            Debug.Log(objct.GetType() + " x: " + objct.positionX + " y: " + objct.positionY);
        }
    }

    public void VerifyCurrentLayoutSatisfyAllConstraints()
    {
        /*
         This function checks whether a given layout (all objects with determined positions) satisfies all the constraints
         */

        string[] dimensions = { "x", "y" };

        foreach (string dimension in dimensions)
        {
            // get varibles and constraints from the dimension graph 
            GetVariablesAndConstraintsFromRelationGraph(dimension);

            Debug.Log("checking: " + dimension + " constraints");

            // verify the constraints using the CSP class
            CSP cspSolver = new CSP(new List<Variable>(variableNamesAndVariableInteger.Values), constraints, dimension);
            cspSolver.VerifyAllTheConstraintsAreSatisfiedInALayout(dimension, precisionMultiplier, allObjects);
        }
    }

    public int[] getLowerAndUpperBound(String dimension, Object objectConsidered)
    {

        int minBoundry = 0;
        int maxBoundry = 0;

        // if the mainObject is a bird, adjust the min max according to the slingshot position
        if (dimension == "x")
        {
            if (objectConsidered.GetType().BaseType == typeof(Bird))
            {
                //minBoundry = (int)Math.Ceiling(Utilities.SLIGSHOT_POSITION[0] * precisionMultiplier); // TODO uncomment these two lines and comment the below two lines for bouncing
                //maxBoundry = (int)Math.Floor(Utilities.SLIGSHOT_POSITION[0] * precisionMultiplier) + 100;

                minBoundry = (int)Math.Ceiling(Utilities.X_MIN_REACHABLE * precisionMultiplier);
                maxBoundry = (int)Math.Floor(Utilities.X_MAX_REACHABLE * precisionMultiplier);

                Debug.Log("A bird is considered: " + typeof(Bird) + "XminBoundry: " + minBoundry + ", XmaxBoundry: " + maxBoundry);
            }
            else
            { // otherwise use  use X_MIN_REACHABLE and X_MAX_REACHABLE
                minBoundry = (int)Math.Ceiling(Utilities.X_MIN_REACHABLE * precisionMultiplier);
                maxBoundry = (int)Math.Floor(Utilities.X_MAX_REACHABLE * precisionMultiplier);
            }

        }
        else if (dimension == "y")
        {
            if (objectConsidered.GetType().BaseType == typeof(Bird))
            {
                //minBoundry = (int)Math.Ceiling(Utilities.SLIGSHOT_POSITION[1] * precisionMultiplier); // TODO uncomment these two lines and comment the below two lines for bouncing
                //maxBoundry = (int)Math.Floor(Utilities.SLIGSHOT_POSITION[1] * precisionMultiplier) + 100;

                minBoundry = (int)Math.Ceiling(Utilities.Y_MIN_REACHABLE * precisionMultiplier);
                maxBoundry = (int)Math.Floor(Utilities.GetMaxYReachable(objectConsidered.positionX) * precisionMultiplier);

                Debug.Log("A bird is considered: " + typeof(Bird) + "YminBoundry: " + minBoundry + ", YmaxBoundry: " + maxBoundry);

            }
            else
            { // otherwise use  use X_MIN_REACHABLE and X_MAX_REACHABLE
                minBoundry = (int)Math.Ceiling(Utilities.Y_MIN_REACHABLE * precisionMultiplier);
                maxBoundry = (int)Math.Floor(Utilities.GetMaxYReachable(objectConsidered.positionX) * precisionMultiplier);
            }
        }
        return new int[] { minBoundry, maxBoundry };
    }

}

