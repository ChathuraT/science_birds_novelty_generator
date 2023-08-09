using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DimensionGrapgh
{
    // the dimension grapgh has all the objects and the 3 points (lower left ll, centre c, upper right ur) for all the objects and their QSR relationships

    public int[,] xCoordinateDimensionGraph;
    public int[,] yCoordinateDimensionGraph;
    // public List<NQSRRelation> NQSRConstraints; // to store the non-qsr relations that won't be added to the dimensiongraphs

    int attemptsToPopulateQSRRelations = 10; // the code will try to populate the connections for the QSR Relations this many times by varing the possible QSR Relations (as some layoutconstraints can be satisfied with multiple QSR terms)
    int attemptsToRetryWholeProcess = 10; // eventhough populating QSR relations were successful, there can be exceptions when adding within-object intrinsic connections, retrying with different QSR relations can resolve this sometimes, the code will retry the whole process this many times

    LayoutConstraintGraph layoutConstraintGraph;
    int totalNumberOfObjects;

    public DimensionGrapgh(LayoutConstraintGraph layoutConstraintGraph)
    {
        this.layoutConstraintGraph = layoutConstraintGraph;
        totalNumberOfObjects = layoutConstraintGraph.mainObjectAndAllLayoutGrammarsList.Count;

        int attemptsMade = 0;

        while (true)
        {
            //try
            //{
            // initialize the dimension graph considering the QSRRelations of the layout constraints
            InitializeDimensionGraph();
            //PrintDimensionGraph(xCoordinateDimensionGraph);
            //PrintDimensionGraph(yCoordinateDimensionGraph);

            // check and fix ((1 and 3), (3 and 1), (3 and 3)) inconsistencies in the dimension graph at current state (to avoid propagating wrong connections)
            CheckAndFixInconsistency(xCoordinateDimensionGraph, "withinObject");
            CheckAndFixInconsistency(yCoordinateDimensionGraph, "withinObject");

            PrintDimensionGraph(xCoordinateDimensionGraph);
            // add the intrinsic connections to the dimension graph (relations which are not directly included in the graph, but can be inferred from the existing relations)
            AddIntrinsicConnections();

            return;

            //}
            //catch (System.Exception e)
            //{
            //    attemptsMade++;
            //    Debug.Log(e);
            //    Debug.Log("Retrying... could not build the dimension graph. Attempts made: " + attemptsMade);

            //    if (attemptsMade >= attemptsToRetryWholeProcess)
            //    {
            //        throw new System.Exception("Could not build the dimension graph even after " + attemptsMade + " attempts!");
            //    }

            //}
        }
    }

    public void CheckConsistancyAndFixPossible()
    {
        // check the consistency (cycles) of the x and y dimension graphs
        CheckAndFixInconsistency(xCoordinateDimensionGraph, "both");
        CheckAndFixInconsistency(yCoordinateDimensionGraph, "both");
    }
    private void InitializeDimensionGraph()
    {

        // fill the connections for the QSR relations
        bool populatingQSRConnectionsSuccess = false;
        int attemptsTriedToPopulateQSRRelations = 0;
        while (!populatingQSRConnectionsSuccess)
        {

            if (attemptsTriedToPopulateQSRRelations > attemptsToPopulateQSRRelations)
            {
                throw new System.Exception("Could not populate the connections for QSR relations due to conflicts");
            }

            // initialize a 2D array with the size of the number of objects x 5, each object has 5 points, in order  ll, cc, ur, ul and lr
            xCoordinateDimensionGraph = new int[totalNumberOfObjects * 5, totalNumberOfObjects * 5];
            yCoordinateDimensionGraph = new int[totalNumberOfObjects * 5, totalNumberOfObjects * 5];

            populatingQSRConnectionsSuccess = AddConnectionsForQSRRelations();
            attemptsTriedToPopulateQSRRelations++;
        }
        //PrintDimensionGraph(xCoordinateDimensionGraph);
        //PrintDimensionGraph(yCoordinateDimensionGraph);

    }

    private bool AddConnectionsForQSRRelations()
    {
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

                // if the two objects have multiple QSR relations and one of them is a MeetX relation, ignore the other QSR relations (because meetX covers other relations as it has a '=' relation, and if others are included with this from 5-point algebra the are conflicts)
                bool containsMeetXCondition = false;
                foreach (List<QSRRelation> qSRRelationList in objectAndLayoutGrammars.qSRRelations)
                {
                    if (qSRRelationList.Any())
                    {
                        QSRRelation qSRRelation = qSRRelationList[Random.Range(0, qSRRelationList.Count)]; // there are multi-choice relations sometimes, get one out of them as the other choices are also of same type (this is an assumption)

                        if (CheckIfTheConnectionIsAMeetXConnection(qSRRelation))
                        {
                            containsMeetXCondition = true;
                            break;
                        }
                    }
                }

                foreach (List<QSRRelation> qSRRelationList in objectAndLayoutGrammars.qSRRelations)
                {
                    if (qSRRelationList.Any())
                    {
                        Debug.Log("---- QSRRelations: " + Utilities.GetAllPropertiesAndValues(qSRRelationList));
                        // get a qSRRelations randomly, if the DimensionGrapgh has conflicts return false to repeat the dimension graph construction from the begining

                        QSRRelation qSRRelation = qSRRelationList[Random.Range(0, qSRRelationList.Count)];
                        Debug.Log("qSRRelation: " + qSRRelation);

                        // if there is a meetXConnection and the currently checking connection is not that, skip the currently cecking connection as only meetx connection is considered in this case
                        if (containsMeetXCondition & !CheckIfTheConnectionIsAMeetXConnection(qSRRelation))
                        {
                            Debug.Log("There is a MeetX connection, skipping this connection!");
                            continue;
                        }

                        /*
                        // TODO: Change this to above line again

                        // test
                        QSRRelation qSRRelation;
                        if (qSRRelationList.Count > 2)
                        {
                            qSRRelation = qSRRelationList[1];
                        }
                        else
                        {
                            qSRRelation = qSRRelationList[0];
                        }
                        // end test
                        */

                        int[][,] intermediateDimensionGraphs = GetConditionsForQSRRelation(qSRRelation);
                        if (!UpdateDimensionGraph(xCoordinateDimensionGraph, yCoordinateDimensionGraph, mainObjectIndex, secondObjectIndex, intermediateDimensionGraphs))
                        {
                            return false;
                        }
                    }

                    // int[][,] intermediateDimensionGraphs = GetConditionsForQSRRelation(qSRRelation);
                    // UpdateDimensionGraph(xCoordinateDimensionGraph, yCoordinateDimensionGraph, mainObjectIndex, secondObjectIndex, intermediateDimensionGraphs);

                    // PrintDimensionGraph(intermediateDimensionGraphs[0]);
                    // PrintDimensionGraph(intermediateDimensionGraphs[1]);
                }
            }
        }
        return true;
    }

    private bool CheckIfTheConnectionIsAMeetXConnection(QSRRelation qSRRelation)
    {
        return (qSRRelation.GetType() == typeof(MeetNorth) | qSRRelation.GetType() == typeof(MeetWest) | qSRRelation.GetType() == typeof(MeetSouth) | qSRRelation.GetType() == typeof(MeetEast) | qSRRelation.GetType() == typeof(MeetNorthWest) | qSRRelation.GetType() == typeof(MeetSouthWest));
    }


    private void AddIntrinsicConnections()
    {
        /*
         This function adds the intrinsic connections to the dimension graph (relations which are not directly included in the graph, but can be inferred from the existing relations)
         These connections are helpful when solving the constraints 
         */

        // test code
        // totalNumberOfObjects = 2;
        // xCoordinateDimensionGraph = new int[,] { { 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0 }, { 1, 0, 0, 0, 0, 0 } };

        // add the intrinsic connections of the same object (ll->c, ll->ur, c->ur)
        AddInObjectConnections();
        // PrintDimensionGraph(xCoordinateDimensionGraph);
        // PrintDimensionGraph(yCoordinateDimensionGraph);
        //PrintDimensionGraph(yCoordinateDimensionGraph);

        // add the intrinsic connections between objects
        AddWithinObjectConnections(xCoordinateDimensionGraph);
        Debug.Log("adding within object x connections were successful");

        AddWithinObjectConnections(yCoordinateDimensionGraph);
        Debug.Log("adding within object y connections were successful");

        // PrintDimensionGraph(xCoordinateDimensionGraph);
        // PrintDimensionGraph(yCoordinateDimensionGraph);

    }

    private bool AddWithinObjectConnections(int[,] coordinateDimensionGraph)
    {

        //// testing code
        // totalNumberOfObjects = 2;
        // xCoordinateDimensionGraph = new int[,] { { 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 1, 0, 0 }, { 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0 }, { 1, 0, 0, 0, 0, 0 } };
        // PrintDimensionGraph(xCoordinateDimensionGraph);

        bool newConnectionAdded = true;

        // repeat the process until no new connection is added
        while (newConnectionAdded)
        {
            newConnectionAdded = false;

            for (int i = 0; i < totalNumberOfObjects * 5; i++)
            {
                for (int j = 0; j < totalNumberOfObjects * 5; j++)
                {
                    // avoid checking in-object connections - they are are already filled (and inconsistencies are tested later)
                    //if (i == j)
                    //{
                    //    continue;
                    //}

                    if (coordinateDimensionGraph[i, j] != 0)
                    { // if there is a connection

                        // Debug.Log("main connection: " + i + ", " + j);
                        // check the other connections of the jth node
                        for (int k = 0; k < totalNumberOfObjects * 5; k++)
                        {

                            // avoid checking connections between the same connection (diagonal connections) - as they cannot be filled anyways (or should check and decide inconsistency from here itself?)
                            //if (i == k)
                            //{
                            //    continue;
                            //}

                            // Debug.Log("checking connection: " + i + ", " + k);
                            if (coordinateDimensionGraph[j, k] != 0)
                            { // if there is a connection
                                // add the connection to the ith node - get connection function handles if there are any conflicts
                                int newConnection = GetOverwittingConnection(coordinateDimensionGraph[i, k], GetResultingConnection(coordinateDimensionGraph[i, j], coordinateDimensionGraph[j, k]));
                                // Debug.Log("adding new connection to " + i + ", " + k + ": " + newConnection);

                                // if the newConnection is different from the existing one, then it is really a new connection
                                if (newConnection != coordinateDimensionGraph[i, k])
                                {
                                    coordinateDimensionGraph[i, k] = newConnection;
                                    newConnectionAdded = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        // PrintDimensionGraph(coordinateDimensionGraph);
        Debug.Log("adding within object connections were successful");
        return true;
    }

    private bool AddWithinObjectConnectionsNotUsed()
    {

        //// testing code
        totalNumberOfObjects = 2;
        xCoordinateDimensionGraph = new int[,] { { 0, 2, 0, 0, 0, 0 }, { 0, 0, 3, 0, 0, 0 }, { 0, 0, 0, 2, 0, 0 }, { 0, 0, 0, 0, 2, 0 }, { 0, 0, 2, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0 } };
        PrintDimensionGraph(xCoordinateDimensionGraph);

        bool newConnectionAdded = true;

        // repeat the process until no new connection is added
        while (newConnectionAdded)
        {
            newConnectionAdded = false;

            for (int i = 0; i < totalNumberOfObjects; i++)
            {
                for (int p = 0; p < 3; p++) // for i
                {

                    for (int j = 0; j < totalNumberOfObjects; j++)
                    {
                        // avoid checking in-object connections - they are are already filled (and inconsistencies are tested later)
                        if (i == j)
                        {
                            continue;
                        }

                        for (int q = 0; q < 3; q++) // for j
                        {
                            Debug.Log("checking connection " + (i * 3 + p) + ", " + (j * 3 + q));
                            if (xCoordinateDimensionGraph[3 * i + p, 3 * j + q] != 0)
                            { // if there is a connection

                                // Debug.Log("main connection: " + i + ", " + j);
                                // check the other connections of the jth node
                                for (int k = 0; k < totalNumberOfObjects; k++)
                                {
                                    // avoid checking connections between the same object (diagonal connections) - as they cannot be filled anyways (or should check and decide inconsistency from here itself?)
                                    if (i == k)
                                    {
                                        continue;
                                    }

                                    for (int r = 0; r < 3; r++) // for k
                                    {
                                        // Debug.Log("checking connection: " + i + ", " + k);
                                        if (xCoordinateDimensionGraph[j * 3 + q, k * 3 + r] != 0)
                                        { // if there is a connection
                                          // add the connection to the ith node - get connection function handles if there are any conflicts
                                            int newConnection = GetOverwittingConnection(xCoordinateDimensionGraph[i * 3 + p, k * 3 + r], xCoordinateDimensionGraph[j * 3 + q, k * 3 + r]);
                                            Debug.Log("adding new connection to " + (i * 3 + p) + ", " + (k * 3 + r) + ": " + newConnection);

                                            // if the newConnection is different from the existing one, then it is really a new connection
                                            if (newConnection != xCoordinateDimensionGraph[i * 3 + p, k * 3 + r])
                                            {
                                                xCoordinateDimensionGraph[i * 3 + p, k * 3 + r] = newConnection;
                                                newConnectionAdded = true;
                                            }
                                        }

                                    }
                                }
                            }

                        }
                    }
                }
            }

        }

        // PrintDimensionGraph(xCoordinateDimensionGraph);

        Debug.Log("adding within object connections were successful");
        return true;
    }

    private void AddInObjectConnections()
    {

        // fill the intrinsic connections (ll->c, ll->ur, c->ur, and same-node connections) in the dimensionMatrix 
        for (int i = 0; i < totalNumberOfObjects; i++)
        {
            for (int j = 0; j < totalNumberOfObjects; j++)
            {
                if (i == j)
                {
                    // correctness check: intrinsic connections should not be filled by QSRRealations, if they are alredy filled, there's something wrong
                    for (int k = 0; k < 5; k++)
                    {
                        for (int l = 0; l < 5; l++)
                        {
                            if ((xCoordinateDimensionGraph[i * 5 + k, j * 5 + l] != 0) | (yCoordinateDimensionGraph[i * 5 + k, j * 5 + l] != 0))
                            {

                                throw new System.Exception("in-object intrinsic connections are already filled for " + (i + k) + " " + (j + l));
                            }
                        }
                    }

                    // adding the in-object connections
                    // same node x connections
                    xCoordinateDimensionGraph[i * 5, j * 5] = 1;
                    xCoordinateDimensionGraph[i * 5 + 1, j * 5 + 1] = 1;
                    xCoordinateDimensionGraph[i * 5 + 2, j * 5 + 2] = 1;
                    xCoordinateDimensionGraph[i * 5 + 3, j * 5 + 3] = 1;
                    xCoordinateDimensionGraph[i * 5 + 4, j * 5 + 4] = 1;

                    // different node x connections
                    xCoordinateDimensionGraph[i * 5, j * 5 + 1] = 2;
                    xCoordinateDimensionGraph[i * 5, j * 5 + 2] = 2;
                    xCoordinateDimensionGraph[i * 5, j * 5 + 3] = 3;
                    xCoordinateDimensionGraph[i * 5, j * 5 + 4] = 2;

                    xCoordinateDimensionGraph[i * 5 + 1, j * 5 + 2] = 2;
                    xCoordinateDimensionGraph[i * 5 + 1, j * 5 + 4] = 2;

                    xCoordinateDimensionGraph[i * 5 + 3, j * 5 + 1] = 2;
                    xCoordinateDimensionGraph[i * 5 + 3, j * 5 + 2] = 2;
                    xCoordinateDimensionGraph[i * 5 + 3, j * 5 + 4] = 2;

                    xCoordinateDimensionGraph[i * 5 + 4, j * 5 + 2] = 3;


                    // same node y connections
                    yCoordinateDimensionGraph[i * 5, j * 5] = 1;
                    yCoordinateDimensionGraph[i * 5 + 1, j * 5 + 1] = 1;
                    yCoordinateDimensionGraph[i * 5 + 2, j * 5 + 2] = 1;
                    yCoordinateDimensionGraph[i * 5 + 3, j * 5 + 3] = 1;
                    yCoordinateDimensionGraph[i * 5 + 4, j * 5 + 4] = 1;

                    // different node y connections
                    yCoordinateDimensionGraph[i * 5, j * 5 + 3] = 2;
                    yCoordinateDimensionGraph[i * 5 + 1, j * 5 + 3] = 2;
                    yCoordinateDimensionGraph[i * 5 + 2, j * 5 + 3] = 3;
                    yCoordinateDimensionGraph[i * 5 + 4, j * 5 + 3] = 2;

                    yCoordinateDimensionGraph[i * 5 + 4, j * 5] = 3;
                    yCoordinateDimensionGraph[i * 5 + 4, j * 5 + 1] = 2;
                    yCoordinateDimensionGraph[i * 5 + 4, j * 5 + 2] = 2;
                }
            }

        }
        Debug.Log("adding in-object connections were successful");
    }

    private void CheckAndFixInconsistency(int[,] coordinateDimensionGraph, string checkNeeded)
    {
        // check the dimension graph has no cycles by checking whether there are issues with in-object (by checking whether the appropriate values are filled) connections and within-object connections (by checking whether the mirror values are filled)

        //// old testing code
        //totalNumberOfObjects = 2;
        //xCoordinateDimensionGraph = new int[,] { { 0, 0, 0, 3, 0, 2 }, { 0, 0, 0, 0, 1, 0 }, { 0, 0, 0, 2, 0, 0 }, { 3, 0, 0, 0, 0, 0 }, { 0, 0, 2, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0 } };
        //yCoordinateDimensionGraph = new int[,] { { 0, 0, 0, 0, 0, 2 }, { 0, 0, 0, 0, 1, 0 }, { 0, 0, 0, 2, 0, 0 }, { 1, 0, 0, 0, 0, 0 }, { 0, 0, 2, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0 } };
        //AddInObjectConnections();

        // PrintDimensionGraph(dimensionGraph);

        for (int i = 0; i < totalNumberOfObjects; i++)
        {
            for (int j = i; j < totalNumberOfObjects; j++) // iterating over the upper triangle is enough
            {
                // checking in-object connections
                if (i == j) // in in-object connections (only upper triangle should be filled with 2, lower triangle should be 0, and the diagonal should be 1)
                {
                    if (checkNeeded.Equals("inObject") | checkNeeded.Equals("both")) // only perform if the in-object test is requested
                    {

                        for (int p = 0; p < 5; p++)
                        {
                            for (int q = 0; q < 5; q++)
                            {
                                if (q == p)
                                {
                                    if (coordinateDimensionGraph[i * 5 + p, j * 5 + q] != 1)
                                    {
                                        Debug.Log("i:" + i + ", j:" + j + ", p:" + p + ", q:" + q + ", value:" + coordinateDimensionGraph[i * 5 + p, j * 5 + q]);

                                        throw new System.Exception("in-object intrinsic connections diagonal is not 1s!");
                                    }
                                }
                                // below two tests are no longer valid with the 5-point object representation
                                //else if (q < p)
                                //{
                                //    if (coordinateDimensionGraph[i * 3 + p, j * 3 + q] != 0)
                                //    {
                                //        Debug.Log("i:" + i + ", j:" + j + ", p:" + p + ", q:" + q + ", value:" + coordinateDimensionGraph[i * 3 + p, j * 3 + q]);

                                //        throw new System.Exception("in-object intrinsic connections lower triangle is not 0s!");
                                //    }
                                //}
                                //else
                                //{
                                //    if (coordinateDimensionGraph[i * 3 + p, j * 3 + q] != 2)
                                //    {
                                //        Debug.Log("i:" + i + ", j:" + j + ", p:" + p + ", q:" + q + ", value:" + coordinateDimensionGraph[i * 3 + p, j * 3 + q]);
                                //        throw new System.Exception("in-object intrinsic connections upper triangle is not 2s!");
                                //    }
                                //}
                            }
                        }
                    }
                }
                // checking within-object connections
                else
                {
                    if (checkNeeded.Equals("withinObject") | checkNeeded.Equals("both")) // only perform if the within-object test is requested
                    {

                        for (int p = 0; p < 5; p++)
                        {
                            for (int q = 0; q < 5; q++)
                            {
                                int value = coordinateDimensionGraph[i * 5 + p, j * 5 + q];
                                int mirror_value = coordinateDimensionGraph[j * 5 + q, i * 5 + p];

                                // check all conflicting cases: (value and mirror_value) >>> (1 and 2), (2 and 1), (2 and 2), (2 and 3), (3 and 2)
                                if (((value == 1) & (mirror_value == 2)) | ((value == 2) & (mirror_value == 1)) | ((value == 2) & (mirror_value == 2)) | ((value == 2) & (mirror_value == 3)) | ((value == 3) & (mirror_value == 2)))
                                {
                                    Debug.Log("Conflicting connection: i:" + i + ", j:" + j + ", p:" + p + ", q:" + q + ", value:" + value + ", mirror_value: " + mirror_value);
                                    throw new System.Exception("within-object intrinsic connections are conflicting!");
                                }
                                // check all fixable conflicting cases: (value and mirror_value) >>> (1 and 3), (3 and 1), (3 and 3), they can be fixed by changing the value and mirror_value to 1
                                else if (((value == 1) & (mirror_value == 3)) | ((value == 3) & (mirror_value == 1)) | ((value == 3) & (mirror_value == 3)))
                                {
                                    Debug.Log("Fixed conflicting connection: i:" + i + ", j:" + j + ", p:" + p + ", q:" + q + ", value:" + value + ", mirror_value: " + mirror_value);
                                    coordinateDimensionGraph[i * 5 + p, j * 5 + q] = 1;
                                    coordinateDimensionGraph[j * 5 + q, i * 5 + p] = 1;
                                }
                            }
                        }
                    }

                }
            }
        }


        // PrintDimensionGraph(dimensionGraph);

        Debug.Log("Dimension graph is consistant!");

    }

    private bool UpdateDimensionGraph(int[,] xCoordinateDimensionGraph, int[,] yCoordinateDimensionGraph, int mainObjectIndex, int secondObjectIndex, int[][,] intermediateDimensionGraphs)
    {
        // copy the intermediate x and y graphs - they should be of size 6x6
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
                        if (intermediateDimensionGraphs[0][i, j] != 0)
                        {
                            xCoordinateDimensionGraph[mainObjectIndex * 5 + i, secondObjectIndex * 5 + j % 5] = GetOverwittingConnection(xCoordinateDimensionGraph[mainObjectIndex * 5 + i, secondObjectIndex * 5 + j % 5], intermediateDimensionGraphs[0][i, j]);
                        }
                        if (intermediateDimensionGraphs[1][i, j] != 0)
                        {
                            yCoordinateDimensionGraph[mainObjectIndex * 5 + i, secondObjectIndex * 5 + j % 5] = GetOverwittingConnection(yCoordinateDimensionGraph[mainObjectIndex * 5 + i, secondObjectIndex * 5 + j % 5], intermediateDimensionGraphs[1][i, j]);
                        }
                    }
                    else if ((i > 4) & (j < 5))
                    {
                        // disregard no connections (0 values)
                        if (intermediateDimensionGraphs[0][i, j] != 0)
                        {
                            xCoordinateDimensionGraph[secondObjectIndex * 5 + i % 5, mainObjectIndex * 5 + j] = GetOverwittingConnection(xCoordinateDimensionGraph[secondObjectIndex * 5 + i % 5, mainObjectIndex * 5 + j], intermediateDimensionGraphs[0][i, j]);
                        }
                        if (intermediateDimensionGraphs[1][i, j] != 0)
                        {
                            yCoordinateDimensionGraph[secondObjectIndex * 5 + i % 5, mainObjectIndex * 5 + j] = GetOverwittingConnection(yCoordinateDimensionGraph[secondObjectIndex * 5 + i % 5, mainObjectIndex * 5 + j], intermediateDimensionGraphs[1][i, j]);
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

    private int GetOverwittingConnection(int existingConnection, int newConnection)
    {
        /*
        given an existing connection and a new connection that needs to be overwritten check whether which connection (=, <, =<) to keep 
        or notify if the existing connection and the new connection conflicts. New connection cannot be 0
        */

        if (existingConnection == 0)
        {
            return newConnection;
        }
        else if (existingConnection == 1)
        {
            if (newConnection == 2)
            { // = and < conflict
                throw new System.Exception("connection conflict: = and <");
            }
            else if ((newConnection == 1) | (newConnection == 3))
            {// = and = or =<
                return 1;
            }
        }
        else if (existingConnection == 2)
        {
            if (newConnection == 1)
            { // < and = conflict
                throw new System.Exception("connection conflict: < and = ");
            }
            else if ((newConnection == 2) | (newConnection == 3))
            {// < and < or =<
                return 2;
            }
        }
        else if (existingConnection == 3)
        {
            return newConnection;
        }

        throw new System.Exception("could not find the connection!");
    }

    private int GetResultingConnection(int connection1, int connection2)
    {
        /*
        given 2 connections between objects (A and B) and (B and C) return the connection between (A and C), both connections should be non zero
        */

        if (connection1 == 1)
        {
            return connection2;
        }
        else if (connection1 == 2)
        {
            return connection1;
        }
        else if (connection1 == 3)
        {
            if ((connection2 == 1) | (connection2 == 3))
            {
                return connection1;
            }
            else if (connection2 == 2)
            {
                return connection2;
            }
        }

        throw new System.Exception("could not find the connection!");
    }

    private int[][,] GetConditionsForQSRRelation(QSRRelation qSRRelation)
    {
        /* returns a 2D array of size 10*10 with the relations between mainObject and the secondObject
         0 - no connection, 1 - less than connection (<), 2 less than or equal connection (=<)
         
        */

        int[,] intermediateXDimensionGraph = new int[10, 10];
        int[,] intermediateYDimensionGraph = new int[10, 10];
        bool isAFarRelation = false;
        /*
                   A              B  
             ll c ur ul lr  ll c ur ul lr
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

        // Directions
        if (qSRRelation.GetType() == typeof(North))
        {
            intermediateXDimensionGraph[0, 7] = 2;
            intermediateXDimensionGraph[5, 2] = 2;
            intermediateYDimensionGraph[8, 4] = 3;
        }
        else if (qSRRelation.GetType() == typeof(South))
        {
            intermediateXDimensionGraph[0, 7] = 2;
            intermediateXDimensionGraph[5, 2] = 2;
            intermediateYDimensionGraph[3, 9] = 3;
        }
        else if (qSRRelation.GetType() == typeof(East))
        {
            intermediateXDimensionGraph[7, 0] = 3;
            intermediateYDimensionGraph[4, 8] = 2;
            intermediateYDimensionGraph[9, 3] = 2;
        }
        else if (qSRRelation.GetType() == typeof(West))
        {
            intermediateXDimensionGraph[2, 5] = 3;
            intermediateYDimensionGraph[4, 8] = 2;
            intermediateYDimensionGraph[8, 3] = 2;
        }
        else if (qSRRelation.GetType() == typeof(NorthEast))
        {
            intermediateXDimensionGraph[7, 0] = 3;
            intermediateYDimensionGraph[8, 4] = 3;
        }
        else if (qSRRelation.GetType() == typeof(NorthWest))
        {
            intermediateXDimensionGraph[2, 5] = 3;
            intermediateYDimensionGraph[8, 4] = 3;
        }
        else if (qSRRelation.GetType() == typeof(SouthEast))
        {
            intermediateXDimensionGraph[7, 0] = 3;
            intermediateYDimensionGraph[3, 9] = 3;
        }
        else if (qSRRelation.GetType() == typeof(SouthWest))
        {
            intermediateXDimensionGraph[2, 5] = 3;
            intermediateYDimensionGraph[3, 9] = 3;
        }

        // interval
        else if (qSRRelation.GetType() == typeof(MeetNorth))
        {
            intermediateXDimensionGraph[1, 6] = 1;
            intermediateYDimensionGraph[4, 8] = 1;
        }
        else if (qSRRelation.GetType() == typeof(MeetWest))
        {
            intermediateXDimensionGraph[2, 5] = 1;
            intermediateYDimensionGraph[1, 6] = 1;
        }

        else if (qSRRelation.GetType() == typeof(MeetSouth))
        {
            intermediateXDimensionGraph[1, 6] = 1;
            intermediateYDimensionGraph[2, 9] = 1;
        }
        else if (qSRRelation.GetType() == typeof(MeetEast))
        {
            intermediateXDimensionGraph[0, 7] = 1;
            intermediateYDimensionGraph[1, 6] = 1;
        }

        else if (qSRRelation.GetType() == typeof(MeetNorthWest))
        {
            intermediateXDimensionGraph[4, 8] = 1;
            intermediateYDimensionGraph[4, 8] = 1;
        }
        else if (qSRRelation.GetType() == typeof(MeetSouthWest))
        {
            intermediateXDimensionGraph[2, 5] = 1;
            intermediateYDimensionGraph[2, 5] = 1;
        }
        else if (qSRRelation.GetType() == typeof(MeetDuringWest))
        {
            intermediateXDimensionGraph[2, 6] = 3;
            intermediateXDimensionGraph[5, 1] = 3;
            intermediateYDimensionGraph[4, 8] = 1;
        }
        else if (qSRRelation.GetType() == typeof(MeetDuringEast))
        {
            intermediateXDimensionGraph[2, 7] = 3;
            intermediateXDimensionGraph[6, 0] = 3;
            intermediateYDimensionGraph[4, 8] = 1;
        }

        //// distance (dimension grapgh connections are same as the direction connections, additinally non-qualitative constraint for 'far is added')
        /// update 16/03/2023: removed as these terms are no longer used (in the far terms the distance is now handled by the NQSR terms and the direction is handled by the direction terms)
        //if (qSRRelation.GetType() == typeof(FarNorth))
        //{
        //    intermediateXDimensionGraph[3, 2] = 2;
        //    intermediateXDimensionGraph[0, 5] = 2;
        //    intermediateYDimensionGraph[5, 0] = 3;
        //    // isAFarRelation = true;
        //}
        //else if (qSRRelation.GetType() == typeof(FarSouth))
        //{
        //    intermediateXDimensionGraph[0, 5] = 2;
        //    intermediateXDimensionGraph[3, 2] = 2;
        //    intermediateYDimensionGraph[2, 3] = 3;
        //    // isAFarRelation = true;
        //}
        //else if (qSRRelation.GetType() == typeof(FarEast))
        //{
        //    intermediateXDimensionGraph[5, 0] = 3;
        //    intermediateYDimensionGraph[3, 2] = 2;
        //    intermediateYDimensionGraph[0, 5] = 2;
        //    // isAFarRelation = true;
        //}
        //else if (qSRRelation.GetType() == typeof(FarWest))
        //{
        //    intermediateXDimensionGraph[2, 3] = 3;
        //    intermediateYDimensionGraph[0, 5] = 2;
        //    intermediateYDimensionGraph[3, 2] = 2;
        //    // isAFarRelation = true;
        //}
        //else if (qSRRelation.GetType() == typeof(FarNorthEast))
        //{
        //    intermediateXDimensionGraph[5, 0] = 3;
        //    intermediateYDimensionGraph[5, 0] = 3;
        //    // isAFarRelation = true;
        //}
        //else if (qSRRelation.GetType() == typeof(FarNorthWest))
        //{
        //    intermediateXDimensionGraph[2, 3] = 3;
        //    intermediateYDimensionGraph[5, 0] = 3;
        //    // isAFarRelation = true;
        //}
        //else if (qSRRelation.GetType() == typeof(FarSouthEast))
        //{
        //    intermediateXDimensionGraph[5, 0] = 3;
        //    intermediateYDimensionGraph[2, 3] = 3;
        //    // isAFarRelation = true;
        //}
        //else if (qSRRelation.GetType() == typeof(FarSouthWest))
        //{
        //    intermediateXDimensionGraph[2, 3] = 3;
        //    intermediateYDimensionGraph[2, 3] = 3;
        //    // isAFarRelation = true;
        //}

        //if (isAFarRelation)
        //{ // if the relation is a Far relation add the NQSRRelation to the NQSRConstraints list
        //    NQSRConstraints.Add(new FarDistance(mainObjectIndex.ToString() + "_1", secondObjectIndex.ToString() + "_1", whatIsFar));
        //}

        return new[] { intermediateXDimensionGraph, intermediateYDimensionGraph };
    }

    public void PrintDimensionGraph(int[,] dimensionGraph)
    {
        string dimensionMatrixString = "";
        for (int i = 0; i < dimensionGraph.GetLength(0); i++)
        {
            for (int j = 0; j < dimensionGraph.GetLength(1); j++)
            {
                dimensionMatrixString += dimensionGraph[i, j] + "\t";
                if ((j + 1) % 5 == 0)
                    dimensionMatrixString += "\t";
            }
            dimensionMatrixString += "\n";

            if ((i + 1) % 5 == 0)
                dimensionMatrixString += "\n";
        }

        Debug.Log(dimensionMatrixString);
    }
}
