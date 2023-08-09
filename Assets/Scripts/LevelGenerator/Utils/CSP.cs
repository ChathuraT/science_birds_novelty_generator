using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class CSP
{
    /*
     This is a constraint satisfaction problem class.
     Inputs: Add the constraints to the constraints list. The constrains are in the format  eg: a leftOperator k middleOperator b, middleOperator can only be ==, <, and <=, and the leftOperator can only be + or -, k is always a positive integer
     Output: integert values assigned to the 'value' parameter of the variables
    */

    public List<Variable> variables;
    public List<Expression> constraints;
    public string dimension;
    private int undeterminedVariableValue = -10000; // the default value of undetermined variables

    public CSP()
    {
        constraints = new List<Expression>();
    }

    public CSP(List<Variable> variables, List<Expression> constraints, string dimension)
    {
        this.variables = variables;
        this.constraints = constraints;
        this.dimension = dimension;
    }

    public void AddVariable(Expression constaint)
    {
        /*
         * Add variables to the variables list
         */
        constraints.Add(constaint);
    }

    public void AddConstraint(Expression constaint)
    {
        /*
         * Add constraints to the constraints list
         */
        constraints.Add(constaint);
    }


    public void Solve()
    {
        /*
         * This function solves the constraints and determine the values of the variables
         */


        //// finding the variable with least value

        /*
        HashSet<Variable> lessVariables = new HashSet<Variable>();

        // STEP 1: capture all the variables in the less side
        foreach (Expression constraint in constraints)
        {
            // consider only undetermined variables
            if (constraint.leftVariable.value == undeterminedVariableValue)
            {
                // first consider the within object constraints
                // if (constraint.leftVariable.name.Split('_')[0] != constraint.rightVariable.name.Split('_')[0])
                // {
                // in the first iteration capture all the variables in the less (or equal) side of the expression

                // add to the lessVariable/s if already does not exists, this is why a hashset is used
                lessVariables.UnionWith(GetLesserVariable(constraint));

                //}
            }
        }

        // print lessVariables
        Debug.Log("Printing Less Variables");
        foreach (Variable variable in lessVariables)
        {
            Debug.Log("Less Variable: " + variable.name);
        }

        // STEP 2: from the variables in the less side check whether they are in the great side (= right side) of any constraint, if so drop them
        List<Variable> lessVariablesToRemove = new List<Variable>();

        // capture the variables to remove
        foreach (Variable variable in lessVariables)
        {
            // check against all the variables in the right side of the constraints
            foreach (Expression constraint in constraints)
            {
                // check whether the less variable in the right side of the equation
                if (constraint.rightVariable == variable)
                {
                    // if the right variable is greater to the left variable, add it to the lessVariablesToRemove list
                    if (GetGreaterVariable(constraint).Contains(variable))
                    {

                        lessVariablesToRemove.Add(variable);
                    }
                }
            }
        }
        // remove the filtered variables
        foreach (Variable variable in lessVariablesToRemove)
        {
            Debug.Log("Less Variable to Remove: " + variable.name);

            // at the same time remove all the other variables connected with == with the variable
            foreach (Variable variableToRemove in GetEqualVariables(variable))
            {
                Debug.Log(" == Associated Variable to Remove: " + variableToRemove.name);

                lessVariables.Remove(variableToRemove);
            }
        }
        */

        bool valuesAssignedForSomeVariables = true;
        int solvingIterationCount = 0;

        while (valuesAssignedForSomeVariables)
        {
            solvingIterationCount += 1;
            valuesAssignedForSomeVariables = false;

            // Iteration 2: get the new lower points (which are not assigned)
            HashSet<Variable> lessVariables = GetAllLowerPoints();
            foreach (Variable variable in lessVariables)
            {
                Debug.Log("Less Variable: " + variable.name);
            }

            // STEP 3: from the filtered lessVariables infer connections from the within object connections
            List<Variable> lessVariablesToRemove = new List<Variable>();
            foreach (Variable variable in lessVariables)
            {
                bool isALessVariable = VerifyVariableIsLeast(variable);
                if (!isALessVariable)
                {
                    Debug.Log(variable.name + " is a not a less variable... ");
                    lessVariablesToRemove.Add(variable);
                }
                else
                {
                    Debug.Log(variable.name + " is a less variable... ");
                }
            }
            // remove the filtered variables
            foreach (Variable variable in lessVariablesToRemove)
            {
                Debug.Log("Less Variable to Remove: " + variable.name);

                // at the same time remove all the other variables connected with == with the variable (is this necessary?)
                foreach (Variable variableToRemove in GetEqualVariables(variable))
                {
                    Debug.Log(" == Associated Variable to Remove: " + variableToRemove.name);

                    lessVariables.Remove(variableToRemove);
                }
            }

            if (lessVariables.Count > 0)
            {
                // assign the values for the filtered least variables and associated variables (and the in-object connections associated with them)
                AssignValuesToLeastAndAssocVariables(solvingIterationCount, lessVariables);
                valuesAssignedForSomeVariables = true;
            }
            PrintVariableValues();

        }

        Debug.Log("verifying all the constraints are satisfied for the dimension: " + dimension);
        VerifyAllTheConstraintsAreSatisfied();
        // PrintVariableValues();
    }

    private void VerifyAllTheConstraintsAreSatisfied()
    {
        /*
         This function check whether all the constraints in the constraints list are satisfied and if not, throw an exception
         */

        bool allConstraintsSatisfied = true;
        bool constraintIsSatisfied;
        foreach (Expression constraint in constraints) // check all the constraint expressions by evaluating them
        {
            constraintIsSatisfied = false;
            if (constraint.middleOperator == "==")
            {
                if (constraint.leftOperator == "+")
                {
                    if ((constraint.leftVariable.value + constraint.leftConstant) == constraint.rightVariable.value)
                    {
                        constraintIsSatisfied = true;
                    }
                }
                else if (constraint.leftOperator == "-")
                {
                    if ((constraint.leftVariable.value - constraint.leftConstant) == constraint.rightVariable.value)
                    {
                        constraintIsSatisfied = true;
                    }
                }
            }
            else if (constraint.middleOperator == "<")
            {
                if (constraint.leftOperator == "+")
                {
                    if ((constraint.leftVariable.value + constraint.leftConstant) < constraint.rightVariable.value)
                    {
                        constraintIsSatisfied = true;
                    }
                }
                else if (constraint.leftOperator == "-")
                {
                    if ((constraint.leftVariable.value - constraint.leftConstant) < constraint.rightVariable.value)
                    {
                        constraintIsSatisfied = true;
                    }
                }
            }
            else if (constraint.middleOperator == "<=")
            {
                if (constraint.leftOperator == "+")
                {
                    if ((constraint.leftVariable.value + constraint.leftConstant) <= constraint.rightVariable.value)
                    {
                        constraintIsSatisfied = true;
                    }
                }
                else if (constraint.leftOperator == "-")
                {
                    if ((constraint.leftVariable.value - constraint.leftConstant) <= constraint.rightVariable.value)
                    {
                        constraintIsSatisfied = true;
                    }
                }
            }

            if (constraintIsSatisfied) // constraint is satisfied
            {
                // Debug.Log("satisfied constraint: " + constraint.leftVariable.name + " " + constraint.leftOperator + " " + constraint.leftConstant + " " + constraint.middleOperator + " " + constraint.rightVariable.name);
                Debug.Log("satisfied constraint: " + constraint.leftVariable.name + " " + " (" + constraint.leftVariable.value + ") " + constraint.leftOperator + " " + constraint.leftConstant + " " + constraint.middleOperator + " " + constraint.rightVariable.name + " " + " (" + constraint.rightVariable.value + ") ");

            }
            else // constraint is not satisfied
            {
                // Debug.Log("** Unsatisfied constraint: " + constraint.leftVariable.name + " " + constraint.leftOperator + " " + constraint.leftConstant + " " + constraint.middleOperator + " " + constraint.rightVariable.name);
                Debug.Log("** Unsatisfied constraint: " + constraint.leftVariable.name + " " + " (" + constraint.leftVariable.value + ") " + constraint.leftOperator + " " + constraint.leftConstant + " " + constraint.middleOperator + " " + constraint.rightVariable.name + " " + " (" + constraint.rightVariable.value + ") ");

                allConstraintsSatisfied = false;
            }
        }

        if (!allConstraintsSatisfied) // if constraint is not satisfied throw an exception-*
        {
            throw new System.Exception("All constraints of " + dimension + " dimension are not satisfied!");
        }

    }


    public void VerifyAllTheConstraintsAreSatisfiedInALayout(string dimension, int precisionMultiplier, List<Object> allObjects)
    {
        /*
         This function check whether all the constraints in the constraints list are satisfied for a given layout (= determind positions of objects) and if not, throw an exception
         The difference between the VerifyAllTheConstraintsAreSatisfied function and this function is, this function takes all the objects as an input and use the real locations (read from the objects themselves) of the objects.
         Currently this function is used to check whether all the constrainsts are still satisfied after doing adjustments to the gameobjects' positions from the simulation solver
         */

        bool allConstraintsSatisfied = true;
        bool constraintIsSatisfied;
        float leftVariableValue = undeterminedVariableValue;
        float rightVariableValue = undeterminedVariableValue;
        foreach (Expression constraint in constraints) // chek all the constraint expressions by evaluating them
        {

            leftVariableValue = getInObjectRelationValues(allObjects[Convert.ToInt32(constraint.leftVariable.name.Split('_')[0])], Convert.ToInt32(constraint.leftVariable.name.Split('_')[1]), dimension, precisionMultiplier);
            rightVariableValue = getInObjectRelationValues(allObjects[Convert.ToInt32(constraint.rightVariable.name.Split('_')[0])], Convert.ToInt32(constraint.rightVariable.name.Split('_')[1]), dimension, precisionMultiplier);
            string leftObjectName = allObjects[Convert.ToInt32(constraint.leftVariable.name.Split('_')[0])].GetType().Name;
            string rightObjectName = allObjects[Convert.ToInt32(constraint.rightVariable.name.Split('_')[0])].GetType().Name;

            constraintIsSatisfied = false;
            if (constraint.middleOperator == "==")
            {
                if (constraint.leftOperator == "+")
                {
                    if ((leftVariableValue + constraint.leftConstant - rightVariableValue) < 0.001)
                    {
                        constraintIsSatisfied = true;
                    }
                }
                else if (constraint.leftOperator == "-")
                {
                    if ((leftVariableValue - constraint.leftConstant - rightVariableValue) < 0.001)
                    {
                        constraintIsSatisfied = true;
                    }
                }
            }
            else if (constraint.middleOperator == "<")
            {
                if (constraint.leftOperator == "+")
                {
                    if ((leftVariableValue + constraint.leftConstant - rightVariableValue) < 0.001)
                    {
                        constraintIsSatisfied = true;
                    }
                }
                else if (constraint.leftOperator == "-")
                {
                    if ((leftVariableValue - constraint.leftConstant - rightVariableValue) < 0.001)
                    {
                        constraintIsSatisfied = true;
                    }
                }
            }
            else if (constraint.middleOperator == "<=")
            {
                if (constraint.leftOperator == "+")
                {
                    if ((leftVariableValue + constraint.leftConstant - rightVariableValue) <= 0.001)
                    {
                        constraintIsSatisfied = true;
                    }
                }
                else if (constraint.leftOperator == "-")
                {
                    if ((leftVariableValue - constraint.leftConstant - rightVariableValue) <= 0.001)
                    {
                        constraintIsSatisfied = true;
                    }
                }
            }

            if (constraintIsSatisfied) // constraint is satisfied
            {
                Debug.Log("satisfied constraint: " + constraint.leftVariable.name + " " + leftObjectName + " (" + leftVariableValue + ") " + constraint.leftOperator + " " + constraint.leftConstant + " " + constraint.middleOperator + " " + constraint.rightVariable.name + " " + rightObjectName + " (" + rightVariableValue + ") ");
            }
            else // constraint is not satisfied
            {
                Debug.Log("** Unsatisfied constraint: " + constraint.leftVariable.name + " " + leftObjectName + " (" + leftVariableValue + ") " + constraint.leftOperator + " " + constraint.leftConstant + " " + constraint.middleOperator + " " + constraint.rightVariable.name + " " + rightObjectName + " (" + rightVariableValue + ") ");
                allConstraintsSatisfied = false;
            }
        }

        if (!allConstraintsSatisfied) // if constraint is not satisfied throw an exception-*
        {
            throw new System.Exception("All constraints are NOT satisfied after liesOnPathAdjustments for the dimension " + dimension);
        }
        else
        {
            Debug.Log("All constraints are satisfied after liesOnPathAdjustments for the dimension " + dimension);
        }

    }

    private float getInObjectRelationValues(Object gameObject, int pointIndex, string dimension, int precisionMultiplier)
    {

        /*
         given an object this function returns the x or y coordinate of that object's ll, c, ur, ul, or lr positions.
         ll and ur coordinates are calculated from the height and width of the object, similar to how it is done when constructing the constraints
         */

        // Debug.Log("pointIndex " + pointIndex + " dimension " + dimension + " precisionMultiplier " + precisionMultiplier);

        float pointOfInterestValue = undeterminedVariableValue;
        // if the variable is associated with ll ur ul or lr points of the object calculate those points
        int[] multipliedDimensions = Utilities.GetMultipliedObjectDimensions(gameObject); //  { 0 width, 1 height, 2 widthCosa, 3 widthSina, 4 heightCosa, 5 heightSina, 6 mbrHalfWidth, 7 mbrHalfHeight }

        if (dimension == "x") // if it is the x dimension, use the width for the calculations
        {
            switch (pointIndex)
            {
                case 0: // ll point
                    return gameObject.positionX * precisionMultiplier - multipliedDimensions[6];
                case 1: // c point
                    return gameObject.positionX * precisionMultiplier;
                case 2: // ur point
                    return gameObject.positionX * precisionMultiplier + multipliedDimensions[6];
                case 3: // ul point
                    return gameObject.positionX * precisionMultiplier - (multipliedDimensions[6] - multipliedDimensions[5]);
                case 4: // lr point
                    return gameObject.positionX * precisionMultiplier + (multipliedDimensions[6] - multipliedDimensions[5]);
                default:
                    throw new System.Exception("Unknown point in the obejct: " + pointIndex);
            }
        }
        else if (dimension == "y") // if it is the y dimension, use the height for the calculations
        {
            switch (pointIndex)
            {
                case 0: // ll point
                    return gameObject.positionY * precisionMultiplier + (multipliedDimensions[7] - multipliedDimensions[4]);
                case 1: // c point
                    return gameObject.positionY * precisionMultiplier;
                case 2: // ur point
                    return gameObject.positionY * precisionMultiplier - (multipliedDimensions[7] - multipliedDimensions[4]);
                case 3: // ul point
                    return gameObject.positionY * precisionMultiplier + multipliedDimensions[7];
                case 4: // lr point
                    return gameObject.positionY * precisionMultiplier - multipliedDimensions[7];
                default:
                    throw new System.Exception("Unknown point in the obejct: " + pointIndex);
            }
        }
        throw new System.Exception("Unknown dimension: " + dimension);
    }

    private void AssignValuesToLeastAndAssocVariables(int assignmentAttemptCount, HashSet<Variable> selectedVariables)
    {
        /*
            Given a set of least variables assign the values to those variables and the associated (in-object and within object) variables
         */

        if (assignmentAttemptCount == 1)
        {
            // This is the initial assignmnent, therefore we can consider the least value to assign as the variable's lower bound (later change this to have more variations in the placement)
            foreach (Variable variable in selectedVariables)
            {
                // the selectedVariables variables can get assigned when propagating the values of another selectedVariables (eg" selectedVariables[0] might cause propagating values of selectedVariables[1]),
                // therefore do a check here as well to exclude such variables and avoid conflicts when going to assign again
                if (variable.value == undeterminedVariableValue)
                {
                    // variable.value = variable.lowerBound;
                    variable.value = UnityEngine.Random.Range(variable.lowerBound, variable.lowerBound + (variable.upperBound - variable.lowerBound) / 4);
                    Debug.Log("Value assigned for the variable: " + variable.name + ": " + variable.value);
                    // propagate the assigned variable value to the other connected variables
                    PropagatevariableValues();
                }
            }
        }
        else
        {
            //  This is the not the initial assignmnent, therefore there are objects which's location is already determined, 
            //      - find such connections with other already location determined objects and use those connections to determine the value of the undetermined variable

            foreach (Variable variable in selectedVariables)
            {
                // the selectedVariables variables can get assigned when propagating the values of another selectedVariables (eg" selectedVariables[0] might cause propagating values of selectedVariables[1]),
                // therefore do a check here as well to exclude such variables and avoid conflicts when going to assign again
                Debug.Log(variable.name + ": " + variable.value);
                if (variable.value == undeterminedVariableValue)
                {

                    // get all the within object connections the considered object has with other location determined objects
                    string consideredObjectIndex = variable.name.Split('_')[0];
                    List<Expression> potentialConstraints = GetWithinObjectConstraintsWithLocationDeterminedObjects(consideredObjectIndex);

                    // convert all the within object connectins to thieir lower points 
                    List<Expression> resolvedPotentialConstraints = new List<Expression>();
                    foreach (Expression constraint in potentialConstraints)
                    {
                        Debug.Log("potential constraint: " + constraint.leftVariable.name + " " + constraint.leftOperator + " " + constraint.leftConstant + " " + constraint.middleOperator + " " + constraint.rightVariable.name);
                        Expression alblconstraint = ResolveConstraintToLowerPoints(constraint);
                        Debug.Log("Resolved constraint: " + alblconstraint.leftVariable.name + " " + alblconstraint.leftOperator + " " + alblconstraint.leftConstant + " " + alblconstraint.middleOperator + " " + alblconstraint.rightVariable.name);
                        resolvedPotentialConstraints.Add(alblconstraint);
                    }

                    // one side of the potential constraints can be evaluated to a numerical value (as there are already location determined objects),
                    // therefore determine the lowest and highest values for the variable
                    int[] minMaxValues = getMinAndMaxValuesOfVariable(variable, resolvedPotentialConstraints);
                    Debug.Log(variable.name + " Dimenstion: " + dimension + " Min: " + minMaxValues[0] + " Max: " + minMaxValues[1]);
                    // if min is greater than the max, there is some issue, break it from here
                    if (minMaxValues[0] > minMaxValues[1])
                    {
                        throw new System.Exception(variable.name + " variable's possible min value is greater than max value!");
                    }

                    // now determine the value of the variable considering the min and max (for now assign the min value, TODO: for variations change this)
                    variable.value = UnityEngine.Random.Range(minMaxValues[0], minMaxValues[0] + (minMaxValues[1] - minMaxValues[0]) / 4);
                    // variable.value = minMaxValues[0];

                    // propagate the assigned variable value to the other connected variables
                    PropagatevariableValues();

                }
            }
        }

        // PrintVariableValues();
        // propagate the variable values to connected variables
    }

    private int[] getMinAndMaxValuesOfVariable(Variable variable, List<Expression> potentialConstraints)
    {
        /*
         Given a set of constraints (inequilities) of a variable return the max and min value the variable can have
         There cannot be a constraint with == at this point (issue with clustering), therefore throw an exception
         */
        int minValue = variable.lowerBound;
        int maxValue = variable.upperBound;
        int leftConstantWithSign;

        foreach (Expression constraint in potentialConstraints)
        {
            if ((constraint.middleOperator == "<") | (constraint.middleOperator == "<="))
            {
                // get the left constant with its sign
                leftConstantWithSign = constraint.leftConstant;
                if (constraint.leftOperator == "-") // if the leftOperator is - assign the negative sign to the leftConstantWithSign
                {
                    leftConstantWithSign = -1 * leftConstantWithSign;
                }

                if (constraint.leftVariable.name == variable.name)// a + k < b scenario (i.e. a < b - k)
                {
                    // Debug.Log("potential max: " + (constraint.rightVariable.value - leftConstantWithSign));

                    if ((constraint.rightVariable.value - leftConstantWithSign) < maxValue)
                    {
                        maxValue = (constraint.rightVariable.value - leftConstantWithSign);
                    }
                }
                else if (constraint.rightVariable.name == variable.name)// b + k < a scenario (i.e. a > b + k)
                {
                    // Debug.Log("potential min: " + (constraint.leftVariable.value + leftConstantWithSign));

                    if ((constraint.leftVariable.value + leftConstantWithSign) > minValue)
                    {
                        minValue = (constraint.leftVariable.value + leftConstantWithSign);
                        // Debug.Log("min: " + minValue);

                    }

                }
            }
            else if (constraint.middleOperator == "==") // this scenario is not expected (throw an exception)
            {
                throw new System.Exception("There is an issue with object clustering, check again!");
            }

        }


        // add 1 to the min value and minus 1 from the max value to return only the feasible range ("<" and "<=" are treated equally as "<", change in future if needed)
        return new int[] { minValue + 1, maxValue - 1 };
    }

    private void PropagatevariableValues()
    {
        /*
        This function propagate the values of the variables considering the already assigned variables 
        The propagation can only be done for the variables connected with ==
        */

        bool propagationHappened = true;

        while (propagationHappened) // propagate until no propagation occures
        {
            propagationHappened = false;
            foreach (Expression constraint in constraints)
            {
                if (constraint.middleOperator == "==")
                {
                    // if the left variable is determined right variable can be calculated
                    if ((constraint.leftVariable.value != undeterminedVariableValue) & (constraint.rightVariable.value == undeterminedVariableValue))
                    {
                        if (constraint.leftConstant == 0)
                        {
                            constraint.rightVariable.value = constraint.leftVariable.value;
                        }
                        else
                        {
                            if (constraint.leftOperator == "+")
                            {
                                constraint.rightVariable.value = constraint.leftVariable.value + constraint.leftConstant;
                            }
                            else if (constraint.leftOperator == "-")
                            {
                                constraint.rightVariable.value = constraint.leftVariable.value - constraint.leftConstant;
                            }
                        }
                        Debug.Log("propagated variable to " + constraint.rightVariable.name);
                        propagationHappened = true;
                    }

                    // if the right variable is determined left variable can be calculated
                    else if ((constraint.rightVariable.value != undeterminedVariableValue) & (constraint.leftVariable.value == undeterminedVariableValue))
                    {
                        if (constraint.leftConstant == 0)
                        {
                            constraint.leftVariable.value = constraint.rightVariable.value;
                        }
                        else
                        {
                            if (constraint.leftOperator == "+")
                            {
                                constraint.leftVariable.value = constraint.rightVariable.value - constraint.leftConstant;

                            }
                            else if (constraint.leftOperator == "-")
                            {
                                constraint.leftVariable.value = constraint.rightVariable.value + constraint.leftConstant;
                            }
                        }
                        Debug.Log("propagated variable to " + constraint.leftVariable.name);
                        propagationHappened = true;
                    }
                }
            }
        }
    }

    private bool VerifyVariableIsLeast(Variable variable)
    {
        /*
         Given a variable, this function checks whether the variable is the lowest by considering the within object connections of the object associated with that variable and other objects 
         Assumption: the input variable is always a lower left point (for x dimension) or a lower right (for y dimension) i.e. x_0 or x_4 (because, to be lowest it has to be the lower point)
         */
        Debug.Log("variable considered: " + variable.name);

        // get all the within object connections the considered object has with other objects
        string consideredObjectIndex = variable.name.Split('_')[0];
        Debug.Log("variable consideredObjectIndex: " + consideredObjectIndex);

        List<Expression> potentialConstraints = GetWithinObjectConstraintsWithLocationUndeterminedObjects(consideredObjectIndex);

        // resolve the left and right variables to their ll/lr (x_0/4) connections to make comparisons
        foreach (Expression constraint in potentialConstraints)
        {
            // Debug.Log("potential constraint: " + constraint.leftVariable.name + " " + constraint.leftOperator + " " + constraint.leftConstant + " " + constraint.middleOperator + " " + constraint.rightVariable.name);
            Expression alblconstraint = ResolveConstraintToLowerPoints(constraint);
            Debug.Log("Resolved constraint: " + alblconstraint.leftVariable.name + " " + alblconstraint.leftOperator + " " + alblconstraint.leftConstant + " " + alblconstraint.middleOperator + " " + alblconstraint.rightVariable.name);

            // finally using the alblconstraint check the variable is greater, if so return false, else continue to the next potential constraint
            if (GetGreaterVariable(alblconstraint).Contains(variable))
            {
                return false;
            }
        }

        // if variable passed all the k related checks for all the potentialConstraints then return true
        return true;
    }

    private List<Expression> GetWithinObjectConstraintsWithLocationDeterminedObjects(string consideredObjectIndex)
    {
        /*
         Given an object index, returns all the connection that object has with location-determined objects
         */

        List<Expression> withinObjectConstraints = new List<Expression>();

        // capture all the connections the considered object has with other objects (while skipping the already location determined obejcts)
        foreach (Expression constraint in constraints)
        {
            string leftObjectIndex = constraint.leftVariable.name.Split('_')[0];
            string rightObjectIndex = constraint.rightVariable.name.Split('_')[0];

            if (leftObjectIndex == consideredObjectIndex)
            {
                if ((rightObjectIndex != consideredObjectIndex) & (constraint.rightVariable.value != undeterminedVariableValue)) // skip in-object connections and skip alrady determined objects
                {
                    withinObjectConstraints.Add(constraint);
                }
            }
            else if (rightObjectIndex == consideredObjectIndex)
            {
                if ((leftObjectIndex != consideredObjectIndex) & (constraint.leftVariable.value != undeterminedVariableValue)) // skip in-object connections and skip alrady determined objects
                {
                    withinObjectConstraints.Add(constraint);
                }
            }
        }

        return withinObjectConstraints;
    }

    private List<Expression> GetWithinObjectConstraintsWithLocationUndeterminedObjects(string consideredObjectIndex)
    {
        /*
         Given an object index, returns all the connection that object has with other location-undetermined objects
         */
        List<Expression> withinObjectConstraints = new List<Expression>();

        // capture all the connections the considered object has with other objects (while skipping the already location determined obejcts)
        foreach (Expression constraint in constraints)
        {
            string leftObjectIndex = constraint.leftVariable.name.Split('_')[0];
            string rightObjectIndex = constraint.rightVariable.name.Split('_')[0];

            if (leftObjectIndex == consideredObjectIndex)
            {
                if ((rightObjectIndex != consideredObjectIndex) & (constraint.rightVariable.value == undeterminedVariableValue)) // skip in-object connections and skip alrady determined objects
                {
                    // Debug.Log(constraint.rightVariable.name + ": " + constraint.rightVariable.value);
                    withinObjectConstraints.Add(constraint);
                }
            }
            else if (rightObjectIndex == consideredObjectIndex)
            {
                if ((leftObjectIndex != consideredObjectIndex) & (constraint.leftVariable.value == undeterminedVariableValue)) // skip in-object connections and skip alrady determined objects
                {
                    // Debug.Log(constraint.leftVariable.name + ": " + constraint.leftVariable.value);
                    withinObjectConstraints.Add(constraint);
                }
            }
        }
        return withinObjectConstraints;
    }

    private Expression ResolveConstraintToLowerPointsTest(Expression constraintConsidered)
    {

        Variable leftVariable = constraintConsidered.leftVariable;
        Variable rightVariable = constraintConsidered.rightVariable;

        Debug.Log("leftVariable: " + leftVariable.name + " rightVariable: " + rightVariable.name);

        if (dimension == "x")
        {
            // find the a_0 + k == a_input constraint and return
            foreach (Expression constraint in constraints)
            {
                if ((constraint.rightVariable.name == (rightVariable.name.Split('_')[0] + "_0")) & (constraint.leftVariable.name == (leftVariable.name.Split('_')[0] + "_0")))
                {
                    return constraint;
                }
            }
        }
        else if (dimension == "y")
        {
            // find the a_4 + k == a_input constraint and return
            foreach (Expression constraint in constraints)
            {
                if ((constraint.rightVariable.name == (rightVariable.name.Split('_')[0] + "_4")) & (constraint.leftVariable.name == (leftVariable.name.Split('_')[0] + "_4")))
                {
                    return constraint;
                }
            }

        }

        throw new System.Exception("Could not ResolveConstraintToLowerPoints() for the constraint!");

    }

    private Expression ResolveConstraintToLowerPoints(Expression constraint)
    {
        /*
         Given a constraint in the format (a_x + k middleOperator b_y), this function resolve the constraint into the objects lowest points (a_0 and b_0 (if dimension is x) or a_4 and b_4 (if dimension is y))
         and returns the constraint in the format a_0/4 + k middleOperator b_0/4 
         */

        Expression resolvedLeftVariable;
        Expression resolvedRightVariable;
        int resolvedLeftConstant = 0;
        int resolvedRightConstant = 0;

        // resolve the potential constraints to lower points of the both objects (a_0/4 + k middleOperator b_0/4 format)
        Expression a0b0constraint = new Expression();
        a0b0constraint.middleOperator = constraint.middleOperator;

        if (dimension == "x")
        {
            // left variable
            if (constraint.leftVariable.name.Split('_')[1] != "0") // if left varibale is a_1, a_2, a_3 or a_4
            {
                resolvedLeftVariable = GetConnectionWithLowerPoint(constraint.leftVariable.name);
                a0b0constraint.leftVariable = resolvedLeftVariable.leftVariable;
                resolvedLeftConstant = resolvedLeftVariable.leftConstant;
            }
            else // if left varibale is a_0
            {
                a0b0constraint.leftVariable = constraint.leftVariable;
                //resolvedLeftConstant = constraint.leftConstant; // fixed this on 3/05/2023 to the below line as this is wrong
                resolvedLeftConstant = 0;
            }

            // right variable
            if (constraint.rightVariable.name.Split('_')[1] != "0") // if right varibale is b_1, b_2, b_3 or b_4
            {
                resolvedRightVariable = GetConnectionWithLowerPoint(constraint.rightVariable.name);
                a0b0constraint.rightVariable = resolvedRightVariable.leftVariable;
                resolvedRightConstant = resolvedRightVariable.leftConstant;
            }
            else // if left varibale is a_0
            {
                a0b0constraint.rightVariable = constraint.rightVariable;
                resolvedRightConstant = 0;
            }

        }
        else if (dimension == "y")
        {
            // left variable
            if (constraint.leftVariable.name.Split('_')[1] != "4") // if left varibale is a_0, a_1, a_2 or a_3
            {
                resolvedLeftVariable = GetConnectionWithLowerPoint(constraint.leftVariable.name);
                a0b0constraint.leftVariable = resolvedLeftVariable.leftVariable;
                resolvedLeftConstant = resolvedLeftVariable.leftConstant;
            }
            else // if left varibale is a_4
            {
                a0b0constraint.leftVariable = constraint.leftVariable;
                resolvedLeftConstant = constraint.leftConstant;
            }

            // right variable
            if (constraint.rightVariable.name.Split('_')[1] != "4") // if right varibale is a_0, a_1, a_2 or a_3
            {
                resolvedRightVariable = GetConnectionWithLowerPoint(constraint.rightVariable.name);
                a0b0constraint.rightVariable = resolvedRightVariable.leftVariable;
                resolvedRightConstant = resolvedRightVariable.leftConstant;
            }
            else // if left varibale is a_4
            {
                a0b0constraint.rightVariable = constraint.rightVariable;
                resolvedRightConstant = 0;
            }
        }

        // determining the k constant (left constant)
        a0b0constraint.leftConstant = constraint.leftConstant + resolvedLeftConstant - resolvedRightConstant;
        // if the k constant is negative adjust the leftconstant operator and make the k positive
        if (a0b0constraint.leftConstant < 0)
        {
            a0b0constraint.leftConstant = -1 * a0b0constraint.leftConstant;
            a0b0constraint.leftOperator = "-";
        }
        else
        {
            a0b0constraint.leftOperator = "+";
        }

        return a0b0constraint;
    }

    private Expression GetConnectionWithLowerPoint(string variableName)
    {
        /* 
         This function finds the connection between the lower (left\right depedning on the dimension) point (a_0\4) of the same object of the input variable (a_1 a_2 a_3 a_0/4) and return it
         input: a_1 or a_2 or a_3 or either a_0 or a_4 depending on the dimension
         e.g: input a_2 will return the expression a_0 + k == a_2 of the dimention considered is x and will return   a_4 + k == a_2 if the dimention considered is y
         */

        // Debug.Log("input: " + variableName);
        if (dimension == "x")
        {
            // find the a_0 + k == a_input constraint and return
            foreach (Expression constraint in constraints)
            {
                if ((constraint.rightVariable.name == variableName) & (constraint.leftVariable.name == (variableName.Split('_')[0] + "_0")))
                {
                    return constraint;
                }
            }
        }
        else if (dimension == "y")
        {
            // find the a_4 + k == a_input constraint and return
            foreach (Expression constraint in constraints)
            {
                if ((constraint.rightVariable.name == variableName) & (constraint.leftVariable.name == (variableName.Split('_')[0] + "_4")))
                {
                    return constraint;
                }
            }

        }

        throw new System.Exception("Could not GetConnectionWithLowerPoint() for the variable: " + variableName);
    }

    private Expression GetConnectionWithLowerLeftPoint(string variableName)
    {
        /* 
         This function finds the connection between the lower left point (a_0) of the same object of the input variable (a_1 or a_2) and return it
         input: a_1 or a_2, but not a_0
         e.g: input b_2 will return the expression a_0 + k == b_2 
         */

        // capture the a_0 + k == a_1 constraint
        // Debug.Log("input: " + variableName);
        Expression a0a1Constraint = null;
        foreach (Expression constraint in constraints)
        {
            if ((constraint.rightVariable.name == (variableName.Split('_')[0] + "_1")) & (constraint.leftVariable.name == (variableName.Split('_')[0] + "_0")))
            {
                a0a1Constraint = constraint;
                continue;
            }
        }

        if (variableName.Split('_')[1] == "1") // if the input is a_1 return the above expression
        {
            return a0a1Constraint;
        }
        else if (variableName.Split('_')[1] == "2") // if the input is a_2 calcualate the new expression and return
        {
            // find the expression a_1 + k == a_2  
            Expression a1a2Constraint = null;
            foreach (Expression constraint in constraints)
            {
                if ((constraint.rightVariable.name == variableName) & (constraint.leftVariable.name == (variableName.Split('_')[0] + "_1")))
                {
                    a1a2Constraint = constraint;
                    continue;
                }
            }

            // a_0 + k == a_2 is calculated by a_0 + k + k == a_2
            return new Expression(a0a1Constraint.leftVariable, a0a1Constraint.leftOperator, (a0a1Constraint.leftConstant + a1a2Constraint.leftConstant), a0a1Constraint.middleOperator, a1a2Constraint.rightVariable);
        }

        return null;

    }


    private HashSet<Variable> GetAllLowerPoints()
    {
        /*
         This function returns the lower (lower left if the dimension is x, lower right if the dimension is y) points of all the objects in the constraints
         */
        HashSet<Variable> lowerLeftVariables = new HashSet<Variable>();

        foreach (Expression constraint in constraints)
        {
            // consider only undetermined variables
            if (constraint.leftVariable.value == undeterminedVariableValue)
            {
                // get the object points considered in the varibales
                string objectPointInLeftVariable = constraint.leftVariable.name.Split('_')[1]; // the second index in the variable name is the point considered in the object
                string objectPointInRightVariable = constraint.rightVariable.name.Split('_')[1]; // the second index in the variable name is the point considered in the object

                if (dimension == "x") // for x dimension lower point is the lower left point (index 0)
                {
                    if (objectPointInLeftVariable == "0")
                    {
                        lowerLeftVariables.Add(constraint.leftVariable);
                    }
                    if (objectPointInRightVariable == "0")
                    {
                        lowerLeftVariables.Add(constraint.rightVariable);
                    }
                }
                else if (dimension == "y") // for y dimension lower point is the lower right point (index 4)
                {
                    if (objectPointInLeftVariable == "4")
                    {
                        lowerLeftVariables.Add(constraint.leftVariable);
                    }
                    if (objectPointInRightVariable == "4")
                    {
                        lowerLeftVariables.Add(constraint.rightVariable);
                    }
                }
            }
        }
        return lowerLeftVariables;
    }

    private HashSet<Variable> GetEqualVariables(Variable variable)
    {

        /*
         This function returns a list of variables that are connected with == operator (i.e. a == b)in the list of constraines. The input variable itself if also included in the list.
         */
        HashSet<Variable> equalVariable = new HashSet<Variable>();
        // add the input variable itself to the list
        equalVariable.Add(variable);

        foreach (Expression constraint in constraints)
        {
            if ((constraint.middleOperator == "==") & (constraint.leftConstant == 0)) // getting a == b constraints
            {
                if (constraint.leftVariable == variable)
                {
                    equalVariable.Add(constraint.rightVariable);
                }
                else if (constraint.rightVariable == variable)
                {
                    equalVariable.Add(constraint.leftVariable);
                }

            }
        }

        return equalVariable;

    }

    private List<Variable> GetGreaterVariable(Expression expression)
    {
        List<Variable> greaterVariable = new List<Variable>();
        /*
         given an expression, this function returns the greater variable. If the two variables are equal it returns none
         general format of the expression is: "leftVariable leftOperator leftConstant middleOperator rightVariable" eg: a + k middleOperator b
         */
        if (expression.middleOperator == "<" | expression.middleOperator == "<=")
        {
            if (expression.leftConstant == 0) // a < b or a <= b (only < is considered, change in future if needed) types

            {
                greaterVariable.Add(expression.rightVariable);

            }
            else
            {
                if (expression.leftOperator == "+")  // a + k < b or a + k <= b type
                {
                    greaterVariable.Add(expression.rightVariable);
                }
                // a - k < b or a - k <= b types cant determine which is greater, therefore return empty
            }
        }
        else if (expression.middleOperator == "==")
        {
            if (expression.leftConstant == 0) // a == b type, return none
            {
                //greaterVariable.Add(expression.leftVariable);
                //greaterVariable.Add(expression.rightVariable);
            }
            else
            {
                if (expression.leftOperator == "+")  // a + k == b type
                {
                    greaterVariable.Add(expression.rightVariable);
                }
                else if (expression.leftOperator == "-")  // a - k == b type
                {
                    greaterVariable.Add(expression.leftVariable);
                }

            }
        }
        return greaterVariable;
    }

    private List<Variable> GetLesserVariable(Expression expression)
    {
        List<Variable> lesserVariable = new List<Variable>();
        /*
         given an expression, this function returns the lesser variable. If the two variables are equal it returns the both variables
         general format of the expression is: "leftVariable leftOperator leftConstant middleOperator rightVariable" eg: a + k middleOperator b
         */
        if (expression.middleOperator == "<" | expression.middleOperator == "<=")
        {
            if (expression.leftConstant == 0) // a < b or a <= b (only < is considered, change in future if needed) types
            {
                lesserVariable.Add(expression.leftVariable);
            }
            else
            {
                if (expression.leftOperator == "+")  // a + k < b or a + k <= b type
                {
                    lesserVariable.Add(expression.leftVariable);
                }
                // a - k < b or a - k <= b types cant determine which is lesser, therefore return empty
            }
        }
        else if (expression.middleOperator == "==")
        {
            if (expression.leftConstant == 0) // a == b type
            {
                lesserVariable.Add(expression.leftVariable);
                lesserVariable.Add(expression.rightVariable);
            }
            else
            {
                if (expression.leftOperator == "+")  // a + k == b type
                {
                    lesserVariable.Add(expression.leftVariable);
                }
                else if (expression.leftOperator == "-")  // a - k == b type
                {
                    lesserVariable.Add(expression.rightVariable);
                }

            }
        }

        return lesserVariable;

    }

    private void PrintVariableValues()
    {
        /*
         This function prints the values of the variables in the set of constraints. It only prints variables with an assigned value
        */
        Debug.Log("Printing the values of the variables that have been assigned in dimension " + dimension);

        HashSet<Variable> allVariables = new HashSet<Variable>();
        foreach (Expression constraint in constraints)
        {
            allVariables.Add(constraint.leftVariable);
            allVariables.Add(constraint.rightVariable);
        }

        foreach (Variable variable in allVariables)
        {
            if (variable.value != undeterminedVariableValue)
            {
                Debug.Log(variable.name + ": " + variable.value);
            }
            else
            {
                Debug.Log(variable.name + ": undetermined");
            }
        }

        Debug.Log("Printing end ===");
    }
}

public class Expression
{

    /*
     This class is used to define expressions (inequalities) for the constraint solver. The expressions are in the format "leftVariable leftOperator leftConstant middleOperator rightVariable" eg: a + k < b
     k is a positive integer
    */
    public Variable leftVariable;
    public Variable rightVariable;
    public int leftConstant;
    public string leftOperator;
    public string middleOperator;

    public Expression()
    {
    }

    public Expression(Variable leftVariable, string leftOperator, int leftConstant, string middleOperator, Variable rightVariable)
    {
        this.rightVariable = rightVariable;
        this.leftVariable = leftVariable;
        this.leftConstant = leftConstant;
        this.leftOperator = leftOperator;
        this.middleOperator = middleOperator;
    }


    public Expression(Variable leftVariable, string middleOperator, Variable rightVariable)
    {
        this.rightVariable = rightVariable;
        this.leftVariable = leftVariable;
        this.leftConstant = 0;
        this.leftOperator = "+";
        this.middleOperator = middleOperator;
    }
}

public class Variable
{
    /*
     This class is used to define variables for the constraints
    */

    public string name;
    public int lowerBound;
    public int upperBound;
    public int value;
    private int undeterminedVariableValue = -10000; // the default value of undetermined variables

    public Variable(string name, int lowerBound, int upperBound, int value)
    {
        this.name = name;
        this.lowerBound = lowerBound;
        this.upperBound = upperBound;
        this.value = value;
    }

    public Variable(string name, int lowerBound, int upperBound)
    {
        this.name = name;
        this.lowerBound = lowerBound;
        this.upperBound = upperBound;
        this.value = undeterminedVariableValue;
    }
}