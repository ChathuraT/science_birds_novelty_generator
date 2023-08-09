using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// using Decider.Csp.BaseTypes;
// using Decider.Csp.Integer;
// using Decider.Csp.Global;

public class ConstraintSolverOld // not used class
{
    private List<Object> allObjects;
    private DimensionGrapgh dimensionGraph;
    private LayoutConstraintGraph layoutConstraintGraph;

    private int precisionMultiplier = 100; // the (float) numbers are multiplied with this constant to make them larger integers as all the floats are needed to be represented as ints in the solver, minimum is 100 
    private List<Expression> constraints;
    private Dictionary<string, Variable> variableNamesAndVariableInteger; // to store the string name of the variable and its VariableInteger

    // values related to the NQSR Relations
    private float whatIsFar = 3; // things apart from this value is considered as far (used for Far)

    public ConstraintSolverOld(List<Object> allObjects, LayoutConstraintGraph layoutConstraintGraph, DimensionGrapgh dimensionGraph)
    {
        this.allObjects = allObjects;
        this.dimensionGraph = dimensionGraph;
        this.layoutConstraintGraph = layoutConstraintGraph;
    }

    public void GetVariablesAndConstraintsFromDimensionGraphs(string dimensionConsidered)
    {
        /*
         * This function populates the variables and constraints lists by considering the connections in the dimension graph
         */

        this.variableNamesAndVariableInteger = new Dictionary<string, Variable>();
        this.constraints = new List<Expression>();
        int[,] coordinateDimensionGraph = null;

        if (dimensionConsidered == "x")
        {
            coordinateDimensionGraph = dimensionGraph.xCoordinateDimensionGraph;
        }
        else if (dimensionConsidered == "y")
        {
            coordinateDimensionGraph = dimensionGraph.yCoordinateDimensionGraph;
        }

        // iterate over the dimension graph and findout the constraints
        int totalNumberOfObjects = allObjects.Count;

        for (int mainObjectIndex = 0; mainObjectIndex < totalNumberOfObjects; mainObjectIndex++)
        {
            // first check whether the main object has any conenctions with the other objects
            for (int secondObjectIndex = 0; secondObjectIndex < totalNumberOfObjects; secondObjectIndex++)
            {
                for (int p = 0; p < 3; p++)
                {
                    for (int q = 0; q < 3; q++)
                    {
                        int connection = coordinateDimensionGraph[mainObjectIndex * 3 + p, secondObjectIndex * 3 + q];
                        if (connection != 0)
                        {
                            // create names for the variables using the indexes of the objects
                            string mainObjectPointVariableName = mainObjectIndex.ToString() + "_" + p.ToString();
                            string secondObjectPointVariableName = secondObjectIndex.ToString() + "_" + q.ToString();

                            // add these varibles to the variables list if they are not already there, the lower and higher limits depend on the dimension
                            if (dimensionConsidered == "x") // for x dimension, lowest and highest points are X_MIN_REACHABLE and X_MAX_REACHABLE
                            {
                                if (!variableNamesAndVariableInteger.ContainsKey(mainObjectPointVariableName))
                                {
                                    variableNamesAndVariableInteger.Add(mainObjectPointVariableName, new Variable(mainObjectPointVariableName, (int)Math.Ceiling(Utilities.X_MIN_REACHABLE * precisionMultiplier), (int)Math.Floor(Utilities.X_MAX_REACHABLE * precisionMultiplier)));
                                }
                                if (!variableNamesAndVariableInteger.ContainsKey(secondObjectPointVariableName))
                                {
                                    variableNamesAndVariableInteger.Add(secondObjectPointVariableName, new Variable(secondObjectPointVariableName, (int)Math.Ceiling(Utilities.X_MIN_REACHABLE * precisionMultiplier), (int)Math.Floor(Utilities.X_MAX_REACHABLE * precisionMultiplier)));
                                }
                            }
                            else if (dimensionConsidered == "y") // for y dimension, lowest point is Y_MIN_REACHABLE and the highest point depends on the x_position (therefore x coordinates are needed to be determined first before determining the y coordinates)
                            {
                                if (!variableNamesAndVariableInteger.ContainsKey(mainObjectPointVariableName))
                                {
                                    float yMaxReachable = Utilities.GetMaxYReachable(allObjects[mainObjectIndex].positionX);
                                    // float yMaxReachable = 9; //TODO: remove this and uncomment above
                                    //Debug.Log(allObjects[mainObjectIndex] + "xCoordinate: " + allObjects[mainObjectIndex].positionX + " yMaxReachable: " + yMaxReachable);
                                    variableNamesAndVariableInteger.Add(mainObjectPointVariableName, new Variable(mainObjectPointVariableName, (int)Math.Ceiling(Utilities.Y_MIN_REACHABLE * precisionMultiplier), (int)Math.Floor(yMaxReachable * precisionMultiplier)));
                                }
                                if (!variableNamesAndVariableInteger.ContainsKey(secondObjectPointVariableName))
                                {
                                    float yMaxReachable = Utilities.GetMaxYReachable(allObjects[secondObjectIndex].positionX);
                                    // float yMaxReachable = 9;
                                    variableNamesAndVariableInteger.Add(secondObjectPointVariableName, new Variable(secondObjectPointVariableName, (int)Math.Ceiling(Utilities.Y_MIN_REACHABLE * precisionMultiplier), (int)Math.Floor(yMaxReachable * precisionMultiplier))); ;
                                }
                            }


                            // add the constraint to the constraints list
                            if (mainObjectIndex == secondObjectIndex) // in-object relations: add the constraints considering the size of the object

                            {
                                if (connection == 2) // connection == 1 is ignored as it is the comparison between the same point, not needed
                                {
                                    // Debug.Log(mainObjectPointVariableName + " < " + secondObjectPointVariableName);

                                    if (dimensionConsidered == "x") // for x dimension, width is considered to determine the constraint
                                    {
                                        if ((p == 0) && (q == 2)) // not considered: connection between ll->ur is covered by ll->c and c->ur
                                        { // connection between ll and ur - difference is equal to the width
                                            // Debug.Log("(int)(allObjects[mainObjectIndex].size[0] * precisionMultiplier): " + (int)Math.Ceiling(allObjects[mainObjectIndex].size[0] * precisionMultiplier));
                                            // constraints.Add(new ConstraintInteger(variableNamesAndVariableInteger[mainObjectPointVariableName] + (int)Math.Ceiling(allObjects[mainObjectIndex].size[0] * precisionMultiplier) == variableNamesAndVariableInteger[secondObjectPointVariableName]));

                                        }
                                        else
                                        {// connection between adjacent points - difference is equal to width/2
                                            Debug.Log(mainObjectPointVariableName + " " + (int)Math.Ceiling(Utilities.GetMBRWidthAndHeight(allObjects[mainObjectIndex])[0] * precisionMultiplier) / 2 + " == " + secondObjectPointVariableName);
                                            constraints.Add(new Expression(variableNamesAndVariableInteger[mainObjectPointVariableName], "+", (int)Math.Ceiling(Utilities.GetMBRWidthAndHeight(allObjects[mainObjectIndex])[0] * precisionMultiplier) / 2, "==", variableNamesAndVariableInteger[secondObjectPointVariableName]));

                                        }
                                    }
                                    else if (dimensionConsidered == "y") // for y dimension, height is considered to determine the constraint
                                    {
                                        if ((p == 0) && (q == 2)) // not considered: connection between ll->ur is covered by ll->c and c->ur
                                        { // connection between ll and ur - difference is equal to the width
                                            // Debug.Log(mainObjectPointVariableName + " + " + (int)Math.Ceiling(allObjects[mainObjectIndex].size[1] * precisionMultiplier) + " == " + secondObjectPointVariableName);
                                            // constraints.Add(new ConstraintInteger(variableNamesAndVariableInteger[mainObjectPointVariableName] + (int)Math.Ceiling(allObjects[mainObjectIndex].size[1] * precisionMultiplier) == variableNamesAndVariableInteger[secondObjectPointVariableName]));

                                        }
                                        else
                                        { // connection between adjacent points - difference is equal to width/2
                                            Debug.Log(mainObjectPointVariableName + " + " + (int)Math.Ceiling(Utilities.GetMBRWidthAndHeight(allObjects[mainObjectIndex])[1] * precisionMultiplier) / 2 + " == " + secondObjectPointVariableName);
                                            constraints.Add(new Expression(variableNamesAndVariableInteger[mainObjectPointVariableName], "+", (int)Math.Ceiling(Utilities.GetMBRWidthAndHeight(allObjects[mainObjectIndex])[1] * precisionMultiplier) / 2, "==", variableNamesAndVariableInteger[secondObjectPointVariableName]));

                                        }
                                    }

                                }

                            }
                            else// within-object relations
                            {
                                if (connection == 1)
                                {
                                    Debug.Log(mainObjectPointVariableName + " == " + secondObjectPointVariableName);
                                    constraints.Add(new Expression(variableNamesAndVariableInteger[mainObjectPointVariableName], "==", variableNamesAndVariableInteger[secondObjectPointVariableName]));
                                }
                                else if (connection == 2)
                                {
                                    Debug.Log(mainObjectPointVariableName + " < " + secondObjectPointVariableName);
                                    constraints.Add(new Expression(variableNamesAndVariableInteger[mainObjectPointVariableName], "<", variableNamesAndVariableInteger[secondObjectPointVariableName]));
                                }
                                else if (connection == 3)
                                {
                                    Debug.Log(mainObjectPointVariableName + " <= " + secondObjectPointVariableName);
                                    constraints.Add(new Expression(variableNamesAndVariableInteger[mainObjectPointVariableName], "<=", variableNamesAndVariableInteger[secondObjectPointVariableName]));
                                }
                            }

                        }
                    }
                }
            }
        }
        Debug.Log("Adding variables and constraints completed for " + dimensionConsidered + " dimension graph");
    }

    public void GetVariablesAndConstraintsFromNQSRRelations(string dimensionConsidered)
    {
        /*
         * This function populates the variables and constraints lists by considering the connections in the NQSRRelations
         * currently only Far related NQSRRelations are handled
         */

        // fill the connections for the QSR relations
        int mainObjectIndex = -1;
        int secondObjectIndex = -1;

        foreach (MainObjectAndAllLayoutGrammars mainObjectAndAllLayoutGrammars in layoutConstraintGraph.mainObjectAndAllLayoutGrammarsList)
        {
            Debug.Log("* Main Object: " + mainObjectAndAllLayoutGrammars.mainObjct);
            mainObjectIndex++;
            secondObjectIndex = -1;

            foreach (ObjectAndLayoutGrammars objectAndLayoutGrammars in mainObjectAndAllLayoutGrammars.objectAndLayoutGrammarsList)
            {
                Debug.Log("-- Object: " + objectAndLayoutGrammars.objct);
                secondObjectIndex++;

                foreach (List<NQSRRelation> nQSRRelationList in objectAndLayoutGrammars.nQSRRelations)
                {
                    if (nQSRRelationList.Any())
                    {
                        Debug.Log("---- NQSRRelations: " + Utilities.GetAllPropertiesAndValues(nQSRRelationList));

                        NQSRRelation nQSRRelation = nQSRRelationList[UnityEngine.Random.Range(0, nQSRRelationList.Count)]; // get a nQSRRelation randomly, however currently only one nQSRRelation is available
                        // create names for the variables using the indexes of the objects
                        string mainObjectPointVariableName = mainObjectIndex.ToString() + "_1";
                        string secondObjectPointVariableName = secondObjectIndex.ToString() + "_1";

                        if (dimensionConsidered == "x")
                        {
                            if (nQSRRelation.GetType() == typeof(FarWest))
                            {
                                // check and add the variable name to the variableNamesAndVariableInteger dict if it is not already available
                                CheckAndAddVariableName(dimensionConsidered, mainObjectPointVariableName, secondObjectPointVariableName);
                                // todo
                            }
                            else if (nQSRRelation.GetType() == typeof(FarEast))
                            {
                                // check and add the variable name to the variableNamesAndVariableInteger dict if it is not already available
                                CheckAndAddVariableName(dimensionConsidered, mainObjectPointVariableName, secondObjectPointVariableName);
                                // todo
                            }
                        }

                        if (dimensionConsidered == "y")
                        {
                            if (nQSRRelation.GetType() == typeof(FarNorth))
                            {
                                // check and add the variable name to the variableNamesAndVariableInteger dict if it is not already available
                                CheckAndAddVariableName(dimensionConsidered, mainObjectPointVariableName, secondObjectPointVariableName);
                                // add the constraint
                                Debug.Log("Adding FarAbove constraint for y axis");
                                Debug.Log(variableNamesAndVariableInteger[secondObjectPointVariableName].name + " + " + (int)Math.Ceiling(whatIsFar * precisionMultiplier) + " < " + variableNamesAndVariableInteger[mainObjectPointVariableName].name);
                                Expression newConstraint = new Expression(variableNamesAndVariableInteger[secondObjectPointVariableName], "+", (int)Math.Ceiling(whatIsFar * precisionMultiplier), "<", variableNamesAndVariableInteger[mainObjectPointVariableName]);
                                propagateNQSRRelations(newConstraint);
                                constraints.Add(newConstraint);


                            }
                            else if (nQSRRelation.GetType() == typeof(FarSouth))
                            {
                                // check and add the variable name to the variableNamesAndVariableInteger dict if it is not already available
                                CheckAndAddVariableName(dimensionConsidered, mainObjectPointVariableName, secondObjectPointVariableName);
                                // todo
                            }
                        }

                    }
                }
            }

        }
    }

    private void propagateNQSRRelations(Expression newConstraint)
    {
        /*
            This function propagates the NQSR constraints to possible all the other objects 
        */

        foreach (Expression constraint in constraints) // chek all the constraints
        {


        }

    }

    private void CheckAndAddVariableName(string dimensionConsidered, string mainObjectPointVariableName, string secondObjectPointVariableName)
    {
        /*
          This function add the varibles to the variables list (variableNamesAndVariableInteger) if they are not already there, the lower and higher limits depend on the dimension
          x locations of the objects should have been determined first to add variable for the y dimension
         */

        if (dimensionConsidered == "x") // for x dimension, lowest and highest points are X_MIN_REACHABLE and X_MAX_REACHABLE
        {
            if (!variableNamesAndVariableInteger.ContainsKey(mainObjectPointVariableName))
            {
                variableNamesAndVariableInteger.Add(mainObjectPointVariableName, new Variable(mainObjectPointVariableName, (int)Math.Ceiling(Utilities.X_MIN_REACHABLE * precisionMultiplier), (int)Math.Floor(Utilities.X_MAX_REACHABLE * precisionMultiplier)));
            }
            if (!variableNamesAndVariableInteger.ContainsKey(secondObjectPointVariableName))
            {
                variableNamesAndVariableInteger.Add(secondObjectPointVariableName, new Variable(secondObjectPointVariableName, (int)Math.Ceiling(Utilities.X_MIN_REACHABLE * precisionMultiplier), (int)Math.Floor(Utilities.X_MAX_REACHABLE * precisionMultiplier)));
            }
        }
        else if (dimensionConsidered == "y") // for y dimension, lowest point is Y_MIN_REACHABLE and the highest point depends on the x_position (therefore x coordinates are needed to be determined first before determining the y coordinates)
        {
            if (!variableNamesAndVariableInteger.ContainsKey(mainObjectPointVariableName))
            {
                float yMaxReachable = Utilities.GetMaxYReachable(allObjects[Convert.ToInt32(mainObjectPointVariableName.Split('_')[0])].positionX);
                variableNamesAndVariableInteger.Add(mainObjectPointVariableName, new Variable(mainObjectPointVariableName, (int)Math.Ceiling(Utilities.Y_MIN_REACHABLE * precisionMultiplier), (int)Math.Floor(yMaxReachable * precisionMultiplier)));
            }
            if (!variableNamesAndVariableInteger.ContainsKey(secondObjectPointVariableName))
            {
                float yMaxReachable = Utilities.GetMaxYReachable(allObjects[Convert.ToInt32(secondObjectPointVariableName.Split('_')[0])].positionX);
                variableNamesAndVariableInteger.Add(secondObjectPointVariableName, new Variable(secondObjectPointVariableName, (int)Math.Ceiling(Utilities.Y_MIN_REACHABLE * precisionMultiplier), (int)Math.Floor(yMaxReachable * precisionMultiplier)));
            }
        }
    }

    public void SolveConstraintsAndDetermineLocations()
    {
        /*
         This function solves the constraints in the x and y dimension graphs and determine the location (x, y coordinates) of the objects and assign them to the objects
        */
        string[] dimentions = { "x", "y" }; // always solve in x and y order as x coordinates are needed to be determine the maximum y coordinates
        foreach (string dimension in dimentions)
        {
            // get varibles and constraints from the dimension graph 
            Debug.Log("Checking QSR Relations for dimension: " + dimension);
            GetVariablesAndConstraintsFromDimensionGraphs(dimension); // always do this first and then get the NQSR relations, as when adding NQSR relations, they are propagated using 
            // Debug.Log("Checking NQSR Relations for dimension: " + dimension);
            GetVariablesAndConstraintsFromNQSRRelations(dimension);

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

    public void SolveConstraints(string dimensionConsidered)
    {
        /* 
         * This function fomulates a given dimension graph into a set of variables and constraints and use the Decider library to solve the constraints programming problem.
         * Input: string dimensionConsidered which is either "x" or "y"
         * Output: feasible location coordinates for the objects
         */

        CSP cspSolver = new CSP(new List<Variable>(variableNamesAndVariableInteger.Values), constraints, dimensionConsidered);
        cspSolver.Solve();
        foreach (Variable variable in variableNamesAndVariableInteger.Values)
        {
            Debug.Log("variable: " + variable.name + " value: " + variable);
        }

        /*
        // build the model using already filled (GetVariablesAndConstraints() has to be called first) variables and constraints and solve the model
        var state = new StateInteger(variableNamesAndVariableInteger.Values, constraints);
        if (state.Search() == StateOperationResult.Unsatisfiable)
        {
            throw new ApplicationException("Cannot find solution satifying all the constraints!");
        }

        // prtint the runtime and solution
        Debug.Log($"Runtime:\t{state.Runtime}\nBacktracks:\t{state.Backtracks}\n");
        foreach (Variable variable in variableNamesAndVariableInteger.Values)
        {
            Debug.Log("variable: " + variable.name + " value: " + variable);
        }


        
        var a = new VariableInteger("0_0", -800, 900);
        var b = new VariableInteger("0_1", -800, 900);
        var c = new VariableInteger("0_2", -800, 900);


        var constraints = new List<IConstraint>
                {
                    // new AllDifferentInteger(new [] { s, e, n, d, m, o, r, y }),
                    // new ConstraintInteger(a == a),
                    new ConstraintInteger(a  + (int)(0.44*100)/2 == b),
                    new ConstraintInteger(a + (int)(0.44*100) == c),
                    // new ConstraintInteger(b == b),
                    new ConstraintInteger(b + (int)(0.44*100)/2 == c),
                    // new ConstraintInteger(c == c)
                };

        var variables = new[] { a, b, c };
        var state = new StateInteger(variables, constraints);

        if (state.Search() == StateOperationResult.Unsatisfiable)
            throw new ApplicationException("Cannot find solution satifying all the constraints!");

        Debug.Log($"Runtime:\t{state.Runtime}\nBacktracks:\t{state.Backtracks}\n");

        Debug.Log($"a: {a}, b: {b}, c: {c}");
        

        constraints = new List<IConstraint>
               {
                   // new AllDifferentInteger(new [] { s, e, n, d, m, o, r, y }),
                   new ConstraintInteger(new VariableInteger("0_0", (int)Utilities.X_MIN_REACHABLE * 10, (int)Utilities.X_MAX_REACHABLE * 10) == new VariableInteger("0_0", (int)Utilities.X_MIN_REACHABLE * 10, (int)Utilities.X_MAX_REACHABLE * 10)),
                   new ConstraintInteger(new VariableInteger("0_0", (int)Utilities.X_MIN_REACHABLE * 10, (int)Utilities.X_MAX_REACHABLE * 10) < new VariableInteger("0_1", (int)Utilities.X_MIN_REACHABLE * 10, (int)Utilities.X_MAX_REACHABLE * 10)),
                   new ConstraintInteger(new VariableInteger("0_0", (int)Utilities.X_MIN_REACHABLE * 10, (int)Utilities.X_MAX_REACHABLE * 10) < new VariableInteger("0_2", (int)Utilities.X_MIN_REACHABLE * 10, (int)Utilities.X_MAX_REACHABLE * 10)),
                   new ConstraintInteger(new VariableInteger("0_1", (int)Utilities.X_MIN_REACHABLE * 10, (int)Utilities.X_MAX_REACHABLE * 10) == new VariableInteger("0_1", (int)Utilities.X_MIN_REACHABLE * 10, (int)Utilities.X_MAX_REACHABLE * 10)),
                   new ConstraintInteger(new VariableInteger("0_1", (int)Utilities.X_MIN_REACHABLE * 10, (int)Utilities.X_MAX_REACHABLE * 10) < new VariableInteger("0_2", (int)Utilities.X_MIN_REACHABLE * 10, (int)Utilities.X_MAX_REACHABLE * 10)),
                   new ConstraintInteger(new VariableInteger("0_2", (int)Utilities.X_MIN_REACHABLE * 10, (int)Utilities.X_MAX_REACHABLE * 10) == new VariableInteger("0_2", (int)Utilities.X_MIN_REACHABLE * 10, (int)Utilities.X_MAX_REACHABLE * 10))
               };
       variables = new[] { new VariableInteger("0_0", (int)Utilities.X_MIN_REACHABLE * 10, (int)Utilities.X_MAX_REACHABLE * 10), new VariableInteger("0_1", (int)Utilities.X_MIN_REACHABLE * 10, (int)Utilities.X_MAX_REACHABLE * 10), new VariableInteger("0_2", (int)Utilities.X_MIN_REACHABLE * 10, (int)Utilities.X_MAX_REACHABLE * 10) };

    */


    }

}

