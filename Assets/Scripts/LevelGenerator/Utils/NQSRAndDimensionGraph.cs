using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class NQSRAndDimensionGraph
{
    /*
     This class stores all the QSR and NQSR relations of the game objects (QSR relations are added from the previously constructed dimension graph).
     First the Dimension Graph is needed to be constructed to contruct the NQSRGraph relations are propagated based on the Dimension Graph
     */

    LayoutConstraintGraph layoutConstraintGraph;
    private List<Object> allObjects;

    public (int, int)[,] xCoordinateRelationGraph;
    public (int, int)[,] yCoordinateRelationGraph;
    private int totalNumberOfObjects;
    private int precisionMultiplier = Utilities.precisionMultiplier; // the (float) numbers are multiplied with this constant to make them larger integers as all the floats are needed to be represented as ints in the solver, minimum is 100 

    // quantative values associated with NQSR relations
    int whatIsFar = 1; // things apart from this value is considered as far (used for Far) - original  whatIsFar = 2;

    public NQSRAndDimensionGraph(List<Object> allObjects, LayoutConstraintGraph layoutConstraintGraph, int[,] xCoordinateDimensionGraph, int[,] yCoordinateDimensionGraph)
    {
        // propagate the NQSR graphs with the values in the coordinate garaphs
        this.layoutConstraintGraph = layoutConstraintGraph;
        this.allObjects = allObjects;
        totalNumberOfObjects = xCoordinateDimensionGraph.GetLength(0) / 5;
        InitializeNQSRGraph(xCoordinateDimensionGraph, yCoordinateDimensionGraph);
    }

    private void InitializeNQSRGraph(int[,] xCoordinateDimensionGraph, int[,] yCoordinateDimensionGraph)
    {
        /*
         * This function initialize and propagates the values of the NQSR grapghs using the correspoinding dimension graphs
         */

        // initialize a 2D array with the size of the number of objects x 3, each object has 3 points, in order  ll, cc and ur
        xCoordinateRelationGraph = new (int, int)[totalNumberOfObjects * 5, totalNumberOfObjects * 5];
        yCoordinateRelationGraph = new (int, int)[totalNumberOfObjects * 5, totalNumberOfObjects * 5];
        Debug.Log(totalNumberOfObjects);
        for (int i = 0; i < totalNumberOfObjects * 5; i++)
        {
            for (int j = 0; j < totalNumberOfObjects * 5; j++)
            {
                xCoordinateRelationGraph[i, j].Item1 = xCoordinateDimensionGraph[i, j];
                yCoordinateRelationGraph[i, j].Item1 = yCoordinateDimensionGraph[i, j];
            }
        }

        //  add the in-object connections (resolve the qualitative values to quantitative values)
        AddConnectionsForInObjectRelations();

        PrintNQSRGraph(xCoordinateRelationGraph);
        PrintNQSRGraph(yCoordinateRelationGraph);
        // add the connections for the NQSR relations
        AddConnectionsForNQSRRelations();
        PrintNQSRGraph(xCoordinateRelationGraph);
        PrintNQSRGraph(yCoordinateRelationGraph);

        // propagate the connections 
        Debug.Log("propagating x dimension connections  ");
        PropagateConnections(xCoordinateRelationGraph);
        Debug.Log("propagating y dimension connections  ");
        PropagateConnections(yCoordinateRelationGraph);
        PrintNQSRGraph(xCoordinateRelationGraph);
        PrintNQSRGraph(yCoordinateRelationGraph);

    }

    private bool PropagateConnections((int, int)[,] coordinateNQSRGraph)
    {
        /*
         This function propagates the constraints in the NQSRGraph
       */

        bool newConnectionAdded = true;

        // repeat the process until no new connection is added
        while (newConnectionAdded)
        {
            newConnectionAdded = false;

            for (int i = 0; i < totalNumberOfObjects * 5; i++)
            {
                for (int j = 0; j < totalNumberOfObjects * 5; j++)
                {
                    if (coordinateNQSRGraph[i, j].Item2 != 0)
                    { // if there is a connection with a value
                        // Debug.Log("checking main connection " + i + ", " + j);
                        // Debug.Log("main connection: " + i + ", " + j);
                        // check the other connections of the jth row
                        for (int k = 0; k < totalNumberOfObjects * 5; k++)
                        {
                            // Debug.Log("checking connection: " + i + ", " + k);
                            if (coordinateNQSRGraph[j, k].Item1 != 0) // if there is a connection (there is a connection between (i, j) and (j, k) )
                            {
                                // avoid checking connections between the same point (diagonal connections)
                                if (j == k)
                                {
                                    continue;
                                }

                                // Debug.Log("there is a connection at " + j + ", " + k + ": " + coordinateNQSRGraph[j, k]);

                                // get the resulting connection
                                (int, int) resultingConnection = GetResultingConnection(coordinateNQSRGraph[i, j], coordinateNQSRGraph[j, k]);

                                // Debug.Log("new connection to add " + i + ", " + k + ": " + resultingConnection);
                                // Debug.Log("existing connection " + i + ", " + k + ": " + coordinateNQSRGraph[i, k]);

                                // add the resultingConnection to the ith node, taking into consideration the existing connection in the ith node
                                (int, int) newConnection = GetOverwrittingConnection(coordinateNQSRGraph[i, k], resultingConnection);

                                // Debug.Log("overwritting connection to add " + i + ", " + k + ": " + newConnection);

                                // if the newConnection is different from the existing one, then it is really a new connection
                                if (newConnection != coordinateNQSRGraph[i, k])
                                {
                                    coordinateNQSRGraph[i, k] = newConnection;
                                    newConnectionAdded = true;
                                }
                            }

                        }
                    }


                    //if ((coordinateNQSRGraph[i, j].Item1 == 1) & (coordinateNQSRGraph[i, j].Item2 == 0)) // if (i, j) is a '==' connection we can propagate more, and for now only constant == 0 is handled
                    //{
                    //    // check the other connections of the ith column
                    //    for (int k = 0; k < totalNumberOfObjects * 5; k++)
                    //    {
                    //        if (coordinateNQSRGraph[k, j].Item1 != 0) // if there is a connection (there is connection between (i, j) and (k, j))
                    //        {
                    //            // avoid checking connections between the same point (diagonal connections)
                    //            if (j == k)
                    //            {
                    //                continue;
                    //            }

                    //            Debug.Log("the connection at " + i + ", " + j + ": " + coordinateNQSRGraph[i, j]);
                    //            Debug.Log("there is a connection at " + k + ", " + j + ": " + coordinateNQSRGraph[k, j]);
                    //            // get the resulting connection
                    //            (int, int) resultingConnection = coordinateNQSRGraph[k, j];

                    //            Debug.Log("new connection to add " + k + ", " + i + ": " + resultingConnection);
                    //            Debug.Log("existing connection " + k + ", " + i + ": " + coordinateNQSRGraph[k, i]);

                    //            // add the resultingConnection to the k,i node, taking into consideration the existing connection
                    //            (int, int) newConnection = GetOverwrittingConnection(coordinateNQSRGraph[k, i], resultingConnection);

                    //            Debug.Log("overwritting connection to add " + k + ", " + i + ": " + newConnection);

                    //            // if the newConnection is different from the existing one, then it is really a new connection
                    //            if (newConnection != coordinateNQSRGraph[k, i])
                    //            {
                    //                coordinateNQSRGraph[k, i] = newConnection;
                    //                newConnectionAdded = true;
                    //            }
                    //        }

                    //    }
                    //}


                }
            }
        }

        // PrintDimensionGraph(coordinateDimensionGraph);
        Debug.Log("propagating connections finished ");
        return true;
    }

    private (int, int) GetResultingConnection((int, int) connection1, (int, int) connection2)
    {
        /*  
            given 2 connections between objects (a and b) and (b and c) return the connection between (a and c), assumption: both the connections item1 is non-zero (i.r. it is =, <, <=)
            a + k1 operator1 b and b + k2 operator2 c outputs a + (k1 + k2) operator3 c
        */

        (int, int) resultingConnection;

        // the operator of the resultingConnection is determind depending  on the connection1 and connection2 operators (see table in notes)
        if ((connection1.Item1 == 2) | (connection2.Item1 == 2))
        {
            resultingConnection.Item1 = 2;
        }
        else if ((connection1.Item1 == 1) & (connection2.Item1 == 1))
        {
            resultingConnection.Item1 = 1;
        }
        else
        {
            resultingConnection.Item1 = 3;
        }

        resultingConnection.Item2 = connection1.Item2 + connection2.Item2;

        return resultingConnection;
    }

    private void AddConnectionsForInObjectRelations()
    {
        /*
         * This function populates the NQSRGraphs by considering the in-object connections (in-object connections are also considered as NQSR as they are associated with a quantitative value, height/width of the object)
         */
        for (int i = 0; i < totalNumberOfObjects; i++)
        {
            for (int j = 0; j < totalNumberOfObjects; j++)
            {
                if (i == j)
                {
                    //// x dimension
                    //change the operator to == (this happens because now we add the offset and hence the connection becomes a + k == b type from the type a < b)
                    xCoordinateRelationGraph[i * 5, j * 5 + 1].Item1 = 1;
                    xCoordinateRelationGraph[i * 5, j * 5 + 2].Item1 = 1;
                    xCoordinateRelationGraph[i * 5, j * 5 + 3].Item1 = 1;
                    xCoordinateRelationGraph[i * 5, j * 5 + 4].Item1 = 1;

                    xCoordinateRelationGraph[i * 5 + 1, j * 5 + 2].Item1 = 1;
                    xCoordinateRelationGraph[i * 5 + 1, j * 5 + 4].Item1 = 1;

                    xCoordinateRelationGraph[i * 5 + 3, j * 5 + 1].Item1 = 1;
                    xCoordinateRelationGraph[i * 5 + 3, j * 5 + 2].Item1 = 1;
                    xCoordinateRelationGraph[i * 5 + 3, j * 5 + 4].Item1 = 1;

                    xCoordinateRelationGraph[i * 5 + 4, j * 5 + 2].Item1 = 1;

                    // add the offset values
                    int[] multipliedDimensions = Utilities.GetMultipliedObjectDimensions(allObjects[i]); //returns { 0 width, 1 height, 2 widthCosa, 3 widthSina, 4 heightCosa, 5 heightSina, 6 mbrHalfWidth, 7 mbrHalfHeight };

                    xCoordinateRelationGraph[i * 5, j * 5 + 1].Item2 = multipliedDimensions[6]; //ll and c
                    xCoordinateRelationGraph[i * 5, j * 5 + 2].Item2 = multipliedDimensions[6] * 2; //ll and ur
                    xCoordinateRelationGraph[i * 5, j * 5 + 3].Item2 = multipliedDimensions[5]; //ll and ul
                    xCoordinateRelationGraph[i * 5, j * 5 + 4].Item2 = multipliedDimensions[2]; //ll and lr

                    xCoordinateRelationGraph[i * 5 + 1, j * 5 + 2].Item2 = multipliedDimensions[6]; //c and ur
                    xCoordinateRelationGraph[i * 5 + 1, j * 5 + 4].Item2 = multipliedDimensions[6] - multipliedDimensions[5]; //c and lr

                    xCoordinateRelationGraph[i * 5 + 3, j * 5 + 1].Item2 = multipliedDimensions[6] - multipliedDimensions[5]; //ul and c
                    xCoordinateRelationGraph[i * 5 + 3, j * 5 + 2].Item2 = multipliedDimensions[2]; //ul and ur
                    xCoordinateRelationGraph[i * 5 + 3, j * 5 + 4].Item2 = multipliedDimensions[2] - multipliedDimensions[5]; //ul and lr

                    xCoordinateRelationGraph[i * 5 + 4, j * 5 + 2].Item2 = multipliedDimensions[5]; //lr and ur

                    // old===
                    //xCoordinateRelationGraph[i * 3, j * 3 + 1].Item1 = 1;
                    //xCoordinateRelationGraph[i * 3, j * 3 + 2].Item1 = 1;
                    //xCoordinateRelationGraph[i * 3 + 1, j * 3 + 2].Item1 = 1;
                    //// add the offset values
                    //xCoordinateRelationGraph[i * 3, j * 3 + 1].Item2 = (int)Math.Ceiling(Utilities.GetMBRWidthAndHeight(allObjects[i])[0] * precisionMultiplier / 2);
                    //xCoordinateRelationGraph[i * 3, j * 3 + 2].Item2 = (int)Math.Ceiling(Utilities.GetMBRWidthAndHeight(allObjects[i])[0] * precisionMultiplier);
                    //xCoordinateRelationGraph[i * 3 + 1, j * 3 + 2].Item2 = (int)Math.Ceiling(Utilities.GetMBRWidthAndHeight(allObjects[i])[0] * precisionMultiplier / 2);


                    // y dimension { 0 width, 1 height, 2 widthCosa, 3 widthSina, 4 heightCosa, 5 heightSina, 6 mbrHalfWidth, 7 mbrHalfHeight };
                    //change the operator to == (this happens because now we add the offset and hence the connection becomes a + k == b type from the type a < b)
                    yCoordinateRelationGraph[i * 5, j * 5 + 3].Item1 = 1;
                    yCoordinateRelationGraph[i * 5 + 1, j * 5 + 3].Item1 = 1;
                    yCoordinateRelationGraph[i * 5 + 2, j * 5 + 3].Item1 = 1;
                    yCoordinateRelationGraph[i * 5 + 4, j * 5 + 3].Item1 = 1;

                    yCoordinateRelationGraph[i * 5 + 4, j * 5].Item1 = 1;
                    yCoordinateRelationGraph[i * 5 + 4, j * 5 + 1].Item1 = 1;
                    yCoordinateRelationGraph[i * 5 + 4, j * 5 + 2].Item1 = 1;

                    // add the offset values
                    yCoordinateRelationGraph[i * 5, j * 5 + 3].Item2 = multipliedDimensions[4]; //ll and ul
                    yCoordinateRelationGraph[i * 5 + 1, j * 5 + 3].Item2 = multipliedDimensions[7]; //c and ul
                    yCoordinateRelationGraph[i * 5 + 2, j * 5 + 3].Item2 = multipliedDimensions[3]; //ur and ul
                    yCoordinateRelationGraph[i * 5 + 4, j * 5 + 3].Item2 = multipliedDimensions[7] * 2; //lr and ul

                    yCoordinateRelationGraph[i * 5 + 4, j * 5].Item2 = multipliedDimensions[3]; //lr and ll
                    yCoordinateRelationGraph[i * 5 + 4, j * 5 + 1].Item2 = multipliedDimensions[7]; //lr and c
                    yCoordinateRelationGraph[i * 5 + 4, j * 5 + 2].Item2 = multipliedDimensions[4]; //lr and ur


                    // old===
                    ////change the operator to == (this happens because now we add the offset and hence the connection becomes a + k == b type from the type a < b)
                    //yCoordinateRelationGraph[i * 3, j * 3 + 1].Item1 = 1;
                    //yCoordinateRelationGraph[i * 3, j * 3 + 2].Item1 = 1;
                    //yCoordinateRelationGraph[i * 3 + 1, j * 3 + 2].Item1 = 1;
                    ////add the offset values
                    //yCoordinateRelationGraph[i * 3, j * 3 + 1].Item2 = (int)Math.Ceiling(Utilities.GetMBRWidthAndHeight(allObjects[i])[1] * precisionMultiplier / 2);
                    //yCoordinateRelationGraph[i * 3, j * 3 + 2].Item2 = (int)Math.Ceiling(Utilities.GetMBRWidthAndHeight(allObjects[i])[1] * precisionMultiplier);
                    //yCoordinateRelationGraph[i * 3 + 1, j * 3 + 2].Item2 = (int)Math.Ceiling(Utilities.GetMBRWidthAndHeight(allObjects[i])[1] * precisionMultiplier / 2);
                }
            }
        }
    }

    private bool AddConnectionsForNQSRRelations()
    {
        /*
         * This function populates the NQSRGraphs by considering the connections in the NQSRRelations
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

                        // get the conditions for NQSRRelation
                        (int, int)[][,] intermediateNQSRGraphs = GetConditionsForNQSRRelation(nQSRRelation);

                        //update NQSRRelation Graph
                        if (!UpdateNQSRGraph(mainObjectIndex, secondObjectIndex, intermediateNQSRGraphs))
                        {
                            return false;
                        }
                    }
                }
            }

        }
        return true;
    }

    private bool UpdateNQSRGraph(int mainObjectIndex, int secondObjectIndex, (int, int)[][,] intermediateNQSRGraphs)
    {
        // copy the intermediate x and y graphs - they should be of size 10x10
        try
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    // split the intermediate dimension graph into partitions and fill the x and y CoordinateDimensionGraphs
                    // intermediate dimension graph does not contain connection within the same object (i.e. A-A and B-B entries can be distregarded)

                    if ((i < 5) & (j > 4))
                    {
                        // disregard no connections (0 values)
                        if (intermediateNQSRGraphs[0][i, j].Item1 != 0)
                        {
                            xCoordinateRelationGraph[mainObjectIndex * 5 + i, secondObjectIndex * 5 + j % 5] = GetOverwrittingConnection(xCoordinateRelationGraph[mainObjectIndex * 5 + i, secondObjectIndex * 5 + j % 5], intermediateNQSRGraphs[0][i, j]);
                        }
                        if (intermediateNQSRGraphs[1][i, j].Item1 != 0)
                        {
                            yCoordinateRelationGraph[mainObjectIndex * 5 + i, secondObjectIndex * 5 + j % 5] = GetOverwrittingConnection(yCoordinateRelationGraph[mainObjectIndex * 5 + i, secondObjectIndex * 5 + j % 5], intermediateNQSRGraphs[1][i, j]);
                        }
                    }
                    else if ((i > 4) & (j < 5))
                    {
                        // disregard no connections (0 values)
                        if (intermediateNQSRGraphs[0][i, j].Item1 != 0)
                        {
                            xCoordinateRelationGraph[secondObjectIndex * 5 + i % 5, mainObjectIndex * 5 + j] = GetOverwrittingConnection(xCoordinateRelationGraph[secondObjectIndex * 5 + i % 5, mainObjectIndex * 5 + j], intermediateNQSRGraphs[0][i, j]);
                        }
                        if (intermediateNQSRGraphs[1][i, j].Item1 != 0)
                        {
                            yCoordinateRelationGraph[secondObjectIndex * 5 + i % 5, mainObjectIndex * 5 + j] = GetOverwrittingConnection(yCoordinateRelationGraph[secondObjectIndex * 5 + i % 5, mainObjectIndex * 5 + j], intermediateNQSRGraphs[1][i, j]);
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            // there had been an issue with the dimension graph update, return false
            Debug.Log(e);
            return false;
        }
        return true;
    }

    private (int, int) GetOverwrittingConnectionOld((int, int) existingConnection, (int, int) newConnection)
    {
        /* 
         Given an existing and a new connection between same nodes, this function returns the connection that can be used to overwrite both
         */

        int existingOperator = existingConnection.Item1;
        int newOperator = newConnection.Item1;
        int overWrittingOperator = existingOperator;

        if (existingOperator == 0)
        {
            overWrittingOperator = newOperator;
        }
        else if (existingOperator == 1)
        {
            if (newOperator == 2)
            { // = and <
                if (existingConnection.Item2 > newConnection.Item2) // check if the k is lower in the newConnection, if so ok, else conflicting
                {
                    overWrittingOperator = 1;
                }
                else
                {
                    throw new System.Exception("connection conflict: = and <");
                }

            }
            else if ((newOperator == 1) | (newOperator == 3))
            {// = and = or =<
                overWrittingOperator = 1;
            }
        }
        else if (existingOperator == 2)
        {
            if (newOperator == 1)
            { // < and =
                if (existingConnection.Item2 < newConnection.Item2)
                { // check if the k is higher in the newConnection, if so ok, else conflicting
                    overWrittingOperator = 1;
                }
                else
                {
                    throw new System.Exception("connection conflict: < and = ");
                }
            }
            else if ((newOperator == 2) | (newOperator == 3))
            {// < and < or =<
                if (existingConnection.Item2 < newConnection.Item2)
                { // check if k is higher in the newConnection, if so overWrittingOperator operator is the new operator (as newConnection will be the overWritting connection)
                    overWrittingOperator = newOperator;
                }
                else
                {// if k is higher in the existingConnection, the overWrittingOperator operator is the old operator (as existingConnection will be the overWritting connections)
                    overWrittingOperator = existingOperator;
                }
            }
        }
        else if (existingOperator == 3)
        {
            overWrittingOperator = newOperator;
        }

        return (overWrittingOperator, GetOverwrittingValue(existingConnection.Item2, newConnection.Item2));

    }

    private (int, int) GetOverwrittingConnection((int, int) existingConnection, (int, int) newConnection)
    {
        /* 
         Given an existing and a new connection between same nodes, this function returns the connection that can be used to overwrite them
         */

        int existingOperator = existingConnection.Item1;
        int newOperator = newConnection.Item1;
        int existingK = existingConnection.Item2;
        int newK = newConnection.Item2;

        if (existingOperator == 0) // there is no existing connection
        {
            return newConnection;
        }

        else if (existingOperator == 1) // existing connection is =
        {
            if (newOperator == 1)
            {
                if (existingK == newK)
                {
                    return existingConnection;
                }
                else
                {
                    throw new Exception("connection conflict - existingConnection: " + existingConnection + ", newConnection: " + newConnection);
                }
            }
            else if (newOperator == 2)
            {
                if (existingK > newK)
                {
                    return existingConnection;
                }
                else
                {
                    throw new Exception("connection conflict - existingConnection: " + existingConnection + ", newConnection: " + newConnection);
                }
            }
            else if (newOperator == 3)
            {
                if ((existingK == newK) | (existingK > newK))
                {
                    return existingConnection;
                }
                else
                {
                    throw new Exception("connection conflict - existingConnection: " + existingConnection + ", newConnection: " + newConnection);
                }
            }
        }


        else if (existingOperator == 2) // existing connection is <
        {
            if (newOperator == 1)
            {
                if (existingK < newK)
                {
                    return newConnection;
                }
                else
                {
                    throw new Exception("connection conflict - existingConnection: " + existingConnection + ", newConnection: " + newConnection);
                }
            }
            else if (newOperator == 2)
            {
                if ((existingK == newK) | (existingK > newK))
                {
                    return existingConnection;
                }
                else if (existingK < newK)
                {
                    return newConnection;
                }
            }
            else if (newOperator == 3)
            {
                if ((existingK == newK) | (existingK > newK))
                {
                    return existingConnection;
                }
                else if (existingK < newK)
                {
                    return newConnection;
                }
            }
        }

        else if (existingOperator == 3) // existing connection is <=
        {
            if (newOperator == 1)
            {
                if ((existingK == newK) | (existingK < newK))
                {
                    return newConnection;
                }
                else
                {
                    throw new Exception("connection conflict - existingConnection: " + existingConnection + ", newConnection: " + newConnection);
                }
            }
            else if (newOperator == 2)
            {
                if ((existingK == newK) | (existingK < newK))
                {
                    return newConnection;
                }
                else if (existingK > newK)
                {
                    return existingConnection;
                }
            }
            else if (newOperator == 3)
            {
                if ((existingK == newK) | (existingK > newK))
                {
                    return existingConnection;
                }
                else if (existingK < newK)
                {
                    return newConnection;
                }
            }
        }

        throw new Exception("GetOverwrittingConnection() connections did not fall into any criteria tested - existingConnection: " + existingConnection + ", newConnection: " + newConnection);
    }

    private int GetOverwrittingValue(int existingValue, int newValue)
    {
        /*
        given an existing value and a new value in a connection, that needs to be overwritten check whether which value to keep 
        since the connections are in a + k ==/</<= b format (where k can be a positive or negative integer, but left operator is always positive), just need to return the max(existingValue, newValue)
        */
        if (existingValue >= newValue)
        {
            return existingValue;
        }
        else if (existingValue < newValue)
        {
            return newValue;
        }

        throw new System.Exception("Issues with determing the value!");
    }

    private int GetOverwrittingOperator(int existingOperator, int newOperator)
    {
        /*
        given an existing operator and a new operator that needs to be overwritten check whether which operator (=, <, =<) to keep 
        or notify if the existing connection and the new connection conflicts. New connection cannot be 0
        */

        if (existingOperator == 0)
        {
            return newOperator;
        }
        else if (existingOperator == 1)
        {
            if (newOperator == 2)
            { // = and < conflict
                throw new System.Exception("connection conflict: = and <");
            }
            else if ((newOperator == 1) | (newOperator == 3))
            {// = and = or =<
                return 1;
            }
        }
        else if (existingOperator == 2)
        {
            if (newOperator == 1)
            { // < and = conflict
                throw new System.Exception("connection conflict: < and = ");
            }
            else if ((newOperator == 2) | (newOperator == 3))
            {// < and < or =<
                return 2;
            }
        }
        else if (existingOperator == 3)
        {
            return newOperator;
        }

        throw new System.Exception("could not find the connection!");
    }

    private (int, int)[][,] GetConditionsForNQSRRelation(NQSRRelation qSRRelation)
    {
        /* returns a 2D array of size 10*10 with the relations between mainObject and the secondObject
         0 - no connection, 1 - connection without equal (<), 2 connectionn with equal (=<)
         Also, if the qSRRelation has a NQSRRelation (for Far relations) it is added to the NQSRConstraints list
        */

        (int, int)[,] intermediateXNQSRGraph = new (int, int)[10, 10];
        (int, int)[,] intermediateYNQSRGraph = new (int, int)[10, 10];
        /*
                   A            B  
             ll c ur ul lr ll c ur ul lr
          ll
          c
        A ur
          ul
          lr
          ll
          c
        B ur
          ul
          lr
        */

        if (qSRRelation.GetType() == typeof(FarWest))
        {
            intermediateXNQSRGraph[2, 5].Item1 = 2; // ll and ur
            intermediateXNQSRGraph[2, 5].Item2 = whatIsFar * precisionMultiplier;
        }
        else if (qSRRelation.GetType() == typeof(FarEast))
        {
            intermediateXNQSRGraph[7, 0].Item1 = 2;
            intermediateXNQSRGraph[7, 0].Item2 = whatIsFar * precisionMultiplier;
        }
        else if (qSRRelation.GetType() == typeof(FarNorth))
        {
            Debug.Log("inside far north");
            intermediateYNQSRGraph[8, 4].Item1 = 2;
            intermediateYNQSRGraph[8, 4].Item2 = whatIsFar * precisionMultiplier; // changing this value to take structures closer doesnt work (as sometimes the values are propagated to the objects with far terms from other objects when satisfying the CSP)
        }
        else if (qSRRelation.GetType() == typeof(FarSouth))
        {
            intermediateYNQSRGraph[3, 9].Item1 = 2;
            intermediateYNQSRGraph[3, 9].Item2 = whatIsFar * precisionMultiplier;
        }
        return new[] { intermediateXNQSRGraph, intermediateYNQSRGraph };
    }

    public void PrintNQSRGraph((int, int)[,] coordinateNQSRGraph)
    {
        /*
         This function prints a given NQSR grapgh
         */
        string nQSRGraphString = "";
        for (int i = 0; i < coordinateNQSRGraph.GetLength(0); i++)
        {
            for (int j = 0; j < coordinateNQSRGraph.GetLength(1); j++)
            {
                nQSRGraphString += coordinateNQSRGraph[i, j] + "\t";
                if ((j + 1) % 5 == 0)
                    nQSRGraphString += "\t";
            }
            nQSRGraphString += "\n";

            if ((i + 1) % 5 == 0)
                nQSRGraphString += "\n";
        }
        Debug.Log(nQSRGraphString);
    }
}
