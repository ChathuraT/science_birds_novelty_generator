using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class QSRPredicateInferrer
{
    string scenarioName;
    public int scenarioIndex;

    public QSRPredicateInferrer(string scenarioName, int scenarioIndex)
    {
        this.scenarioName = scenarioName;
        this.scenarioIndex = scenarioIndex;
    }

    public void InferQSRPredicatesForLayoutConstraintGraph(LayoutConstraintGraph layoutConstraintGraph)
    {
        // add the QSRPrediacates (and NQSRPrediacates) to the layoutConstraintGraph

        foreach (MainObjectAndAllLayoutGrammars mainObjectAndAllLayoutGrammars in layoutConstraintGraph.mainObjectAndAllLayoutGrammarsList)
        {
            // Debug.Log("* Main Object: " + mainObjectAndAllLayoutGrammars.mainObjct);
            foreach (ObjectAndLayoutGrammars objectAndLayoutGrammars in mainObjectAndAllLayoutGrammars.objectAndLayoutGrammarsList)
            {

                List<List<QSRRelation>> inferredQSRRelations = new List<List<QSRRelation>>();
                List<List<NQSRRelation>> inferredNQSRRelations = new List<List<NQSRRelation>>();

                // Debug.Log("-- Object: " + objectAndLayoutGrammars.objct);
                foreach (LayoutGrammar layoutGrammar in objectAndLayoutGrammars.layoutGrammars)
                {
                    // infer the QSRPredicates
                    inferredQSRRelations.Add(InferQSRPredicatesOfLayoutTerms(layoutGrammar));
                    // infer the NQSRPredicates - currently only for Far
                    inferredNQSRRelations.Add(InferNQSRPredicatesOfLayoutTerms(layoutGrammar));

                }
                objectAndLayoutGrammars.AddQSRRelations(inferredQSRRelations);
                objectAndLayoutGrammars.AddNQSRRelations(inferredNQSRRelations);
                // Debug.Log("AddQSRRelations" + " " + string.Join(", ", (inferredQSRRelations as IEnumerable<object>).Cast<object>().ToList().ToArray()));
            }
        }

    }


    public List<QSRRelation> InferQSRPredicatesOfLayoutTerms(LayoutGrammar layoutTerm)
    {
        List<QSRRelation> inferredQSRRelations = new List<QSRRelation>();

        switch (layoutTerm)
        {
            case InDirection inDirection:
                inferredQSRRelations = getInDirectionQSRPredicates(inDirection);
                break;
            case Touching touching:
                inferredQSRRelations = getTouchingQSRPredicates(touching);
                break;
            case OnLocation onLocation:
                inferredQSRRelations = getOnLocationQSRPredicates(onLocation);
                break;
            case Far far:
                inferredQSRRelations = getFarQSRPredicates(far);
                break;
            case LiesOnPath liesOnPath:
                // todo
                break;
            case PathObstructed pathObstructed:
                // todo
                break;
            default:
                break;
        }
        return inferredQSRRelations;

    }

    public List<NQSRRelation> InferNQSRPredicatesOfLayoutTerms(LayoutGrammar layoutTerm)
    {
        List<NQSRRelation> inferredNQSRRelations = new List<NQSRRelation>();

        switch (layoutTerm)
        {
            case Far far:
                inferredNQSRRelations = getFarNQSRPredicates(far);
                break;
            default:
                break;
        }
        return inferredNQSRRelations;

    }

    public List<QSRRelation> getInDirectionQSRPredicates(InDirection inDirection)
    {

        List<QSRRelation> qSRRelation = new List<QSRRelation>();
        if (inDirection.d.GetType() == typeof(Above))
        {
            qSRRelation.Add(new North(inDirection.a, inDirection.b)); //TODO: uncomment
            qSRRelation.Add(new NorthEast(inDirection.a, inDirection.b)); //TODO: uncomment
            qSRRelation.Add(new NorthWest(inDirection.a, inDirection.b));
        }
        else if (inDirection.d.GetType() == typeof(Below))
        {
            // dirty fix for bouncing scenario to get more feasible levels
            if (scenarioName == "BouncingBird")
            {
                Debug.Log("bouncing " + inDirection.a.GetType() + " " + inDirection.b.GetType());
                if (inDirection.b.gameObject.GetType().BaseType == typeof(Bird)) // pig is in SouthEast of the bird
                {
                    qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b));
                }
                else if (inDirection.a.gameObject.GetType().BaseType == typeof(Bird)) // bird is in the southwest of the slope
                {
                    qSRRelation.Add(new SouthWest(inDirection.a, inDirection.b));
                }
                else // pig can be in south or southwest or southeast of boucing platform depending on the trajectory used
                {
                    //qSRRelation.Add(new South(inDirection.a, inDirection.b));
                    // qSRRelation.Add(new SouthWest(inDirection.a, inDirection.b)); // for lower trajectories
                    qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b)); // for higher trajectories

                }
            }
            // dirty fix for BouncingBirdFallingObject scenario to get more feasible levels
            else if (scenarioName == "BouncingBirdFallingObject")
            {
                if (inDirection.a.GetType() == typeof(FallableObject) & inDirection.b.GetType() == typeof(Slope))
                { // FallableObject is in the southwest of the slope for lower trajectory shots
                    qSRRelation.Add(new SouthWest(inDirection.a, inDirection.b));
                    // FallableObject is in the SouthEast of the slope for higher trajectory shots
                    // qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b));
                }
                else
                {
                    qSRRelation.Add(new South(inDirection.a, inDirection.b));
                    qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b));
                    qSRRelation.Add(new SouthWest(inDirection.a, inDirection.b));
                }
            }
            else if (scenarioName == "BouncingBirdRollingObject")
            {
                if (inDirection.a.GetType() == typeof(RollableObject) & inDirection.b.GetType() == typeof(Slope))
                {
                    // RollableObject is in the SouthEast of the slope for higher trajectory shots (only higher traj shots are considered in this scenario)
                    qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b));
                }
                else
                {
                    qSRRelation.Add(new South(inDirection.a, inDirection.b));
                    qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b));
                    qSRRelation.Add(new SouthWest(inDirection.a, inDirection.b));
                }
            }
            else if (scenarioName == "FallingRollingObject")// dirty fix for FallingRollingObject scenario to get more feasible levels 
            { // rolling object should be in the SouthEast of the falling object
                qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b));
            }
            else if (scenarioName == "SlidingRollingFallingObject")
            {
                qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b)); // for this scenario, some InDirectionBelow terms were added manually (all of which should be southEast for feasible generation)
            }
            else if (scenarioName == "SlidingFallingRollingObject")
            {
                qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b)); // for this scenario, some InDirectionBelow terms were added manually (all of which should be southEast for feasible generation)
            }
            else if (scenarioName == "RollingRollingRollingObject")
            {
                qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b)); // for this scenario, some InDirectionBelow terms were added manually (all of which should be southEast for feasible generation)
            }
            else if (scenarioName == "RollingRollingFallingObject")
            {
                qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b)); // for this scenario, some InDirectionBelow terms were added manually (all of which should be southEast for feasible generation)
            }
            else if (scenarioName == "RollingObjectNovel")
            {
                qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b)); // for this scenario, some InDirectionBelow terms were added manually (all of which should be southEast for feasible generation)
            }
            else if (scenarioName == "SlidingObjectNovel")
            {
                qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b)); // for this scenario, some InDirectionBelow terms were added manually (all of which should be southEast for feasible generation)
            }
            else if (scenarioName == "FallingObjectNovel")
            {
                qSRRelation.Add(new South(inDirection.a, inDirection.b)); // two falling objects should be one below the other
            }
            else if (scenarioName == "RollingFallingObjectNovel")
            {
                if ((scenarioIndex == 2) | (scenarioIndex == 3) | (scenarioIndex == 4) | (scenarioIndex == 5) | (scenarioIndex == 6))
                {
                    if (inDirection.a.GetType() == typeof(RollableObject) & inDirection.b.GetType() == typeof(RollableObject))
                    {
                        qSRRelation.Add(new South(inDirection.a, inDirection.b));
                        //qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b));
                        //qSRRelation.Add(new SouthWest(inDirection.a, inDirection.b));
                    }
                    else
                    {
                        qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b));
                    }

                }
                else
                {
                    qSRRelation.Add(new South(inDirection.a, inDirection.b)); // two falling objects should be one below the other
                }
            }
            else
            {
                qSRRelation.Add(new South(inDirection.a, inDirection.b));
                qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b));
                qSRRelation.Add(new SouthWest(inDirection.a, inDirection.b));
            }
        }
        else if (inDirection.d.GetType() == typeof(Left))
        {
            qSRRelation.Add(new West(inDirection.a, inDirection.b));
            qSRRelation.Add(new NorthWest(inDirection.a, inDirection.b));
            qSRRelation.Add(new SouthWest(inDirection.a, inDirection.b));
        }
        else if (inDirection.d.GetType() == typeof(Right))
        {
            // dirty fix for RollingSlidingObject scenario to get more feasible levels
            if (scenarioName == "RollingSlidingObject")
            {
                qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b)); // the sliding object should be in the south east of the rolling object to have feasible levels
            }
            else if (scenarioName == "SlidingRollingFallingObject")
            {
                qSRRelation.Add(new East(inDirection.a, inDirection.b)); // the rolling object should be in the east of the sliding object to have feasible levels
            }
            else if (scenarioName == "RollingFallingObjectNovel")
            {
                if ((scenarioIndex == 1) & (inDirection.b.gameObject.isNovel))
                { // there are flat surfaces in some sub scenarios, in them, the falling object should be in the east of the rolling object
                    // qSRRelation.Add(new East(inDirection.a, inDirection.b)); // didnt work
                    qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b)); // the rolling object should be in the east of the sliding object to have feasible levels
                    // qSRRelation.Add(new NorthEast(inDirection.a, inDirection.b));
                }
                else if ((scenarioIndex == 2) | (scenarioIndex == 6))
                {
                    if (inDirection.b.gameObject.isNovel)
                    {
                        qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b)); // novel object is in the inclined ramp
                    }
                    else
                    {
                        qSRRelation.Add(new East(inDirection.a, inDirection.b));
                    }
                }
                else if ((scenarioIndex == 3) | (scenarioIndex == 4) | (scenarioIndex == 5))
                {
                    if (inDirection.b.gameObject.isNovel)
                    {
                        qSRRelation.Add(new East(inDirection.a, inDirection.b)); // novel object is in flat ramp
                    }
                    else
                    {
                        qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b));
                    }
                }
                else
                {
                    qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b)); // the rolling object should be in the east of the sliding object to have feasible levels
                }
            }
            else
            {
                qSRRelation.Add(new East(inDirection.a, inDirection.b));
                qSRRelation.Add(new NorthEast(inDirection.a, inDirection.b));
                qSRRelation.Add(new SouthEast(inDirection.a, inDirection.b));
            }
        }

        return qSRRelation;
    }


    public List<QSRRelation> getTouchingQSRPredicates(Touching touching)
    {

        List<QSRRelation> qSRRelation = new List<QSRRelation>();
        if (touching.d.GetType() == typeof(UpperLeft))
        {
            qSRRelation.Add(new MeetNorthWest(touching.a, touching.b));
        }
        else if (touching.d.GetType() == typeof(CentreLeft))
        {
            qSRRelation.Add(new MeetWest(touching.a, touching.b));
        }
        else if (touching.d.GetType() == typeof(LowerLeft))
        {
            qSRRelation.Add(new MeetSouthWest(touching.a, touching.b));
        }

        return qSRRelation;
    }

    public List<QSRRelation> getOnLocationQSRPredicates(OnLocation onLocation)
    {

        List<QSRRelation> qSRRelation = new List<QSRRelation>();
        if (onLocation.d.GetType() == typeof(OnLeft))
        {
            qSRRelation.Add(new MeetDuringWest(onLocation.a, onLocation.b));
        }
        else if (onLocation.d.GetType() == typeof(OnCentre))
        {
            qSRRelation.Add(new MeetNorth(onLocation.a, onLocation.b));
        }
        else if (onLocation.d.GetType() == typeof(OnRight))
        {
            qSRRelation.Add(new MeetDuringEast(onLocation.a, onLocation.b));
        }

        return qSRRelation;
    }

    public List<QSRRelation> getFarQSRPredicates(Far far)
    {

        List<QSRRelation> qSRRelation = new List<QSRRelation>();
        //if (far.d.GetType() == typeof(Above))
        //{
        //    qSRRelation.Add(new FarNorth(far.a, far.b)); //TODO: uncomment
        //    qSRRelation.Add(new FarNorthEast(far.a, far.b)); //TODO: uncomment
        //    qSRRelation.Add(new FarNorthWest(far.a, far.b));
        //}
        //else if (far.d.GetType() == typeof(Below))
        //{
        //    qSRRelation.Add(new FarSouth(far.a, far.b));
        //    qSRRelation.Add(new FarSouthEast(far.a, far.b));
        //    qSRRelation.Add(new FarSouthWest(far.a, far.b));
        //}
        //else if (far.d.GetType() == typeof(Left))
        //{
        //    qSRRelation.Add(new FarWest(far.a, far.b));
        //    qSRRelation.Add(new FarNorthWest(far.a, far.b));
        //    qSRRelation.Add(new FarSouthWest(far.a, far.b));
        //}
        //else if (far.d.GetType() == typeof(Right))
        //{
        //    qSRRelation.Add(new FarEast(far.a, far.b));
        //    qSRRelation.Add(new FarNorthEast(far.a, far.b));
        //    qSRRelation.Add(new FarSouthEast(far.a, far.b));
        //}

        // 16/03/2023 changed the distance terms (i.e. terms with far)  into direction terms as the distance is handled by NQSR terms

        if (far.d.GetType() == typeof(Above))
        {
            if (scenarioName == "BouncingBirdFallingObject") // dirty fix for RollingSlidingObject scenario to get more feasible levels

            {
                qSRRelation.Add(new NorthEast(far.a, far.b)); // the falling object should be in the NorthEast of the pig to have feasible levels for lower trajectory shots
                // qSRRelation.Add(new NorthWest(far.a, far.b)); // the falling object should be in the NorthWest of the pig to have feasible levels for higher trajectory shots
            }
            else if (scenarioName == "BouncingBirdRollingObject") // dirty fix for BouncingBirdRollingObject scenario to get more feasible levels

            {
                qSRRelation.Add(new NorthWest(far.a, far.b)); // the rolling object should be in the NorthWest of the pig to have feasible levels for higher trajectory shots (only higher traj shots are considered in this scenario)
            }
            else if (scenarioName == "SlidingRollingFallingObject") // dirty fix for SlidingRollingFallingObject scenario to get more feasible levels

            {
                qSRRelation.Add(new NorthWest(far.a, far.b)); // the falling object should be in the NorthWest of the pig to have feasible levels
            }
            else if (scenarioName == "RollingRollingFallingObject") // dirty fix for RollingRollingFallingObject scenario to get more feasible levels

            {
                qSRRelation.Add(new NorthWest(far.a, far.b)); // the falling object should be in the NorthWest of the pig to have feasible levels
            }
            else if (scenarioName == "FallingObject")//  dirty fix for FallingObject scenario to get more feasible levels
            {
                qSRRelation.Add(new NorthWest(far.a, far.b)); // the falling object should be in the NorthWest of the pig to have feasible levels
            }
            else if (scenarioName == "RollingObjectNovel")//  dirty fix for RollingObjectNovel scenario to get more feasible levels
            {
                qSRRelation.Add(new NorthWest(far.a, far.b)); // the falling object should be in the NorthWest of the pig to have feasible levels
            }
            else if (scenarioName == "FallingObjectNovel")//  dirty fix for FallingObjectNovelty scenario to get more feasible levels
            {
                qSRRelation.Add(new NorthWest(far.a, far.b)); // the falling object should be in the NorthWest of the pig to have feasible levels
            }
            else if (scenarioName == "SlidingObjectNovel")//  dirty fix for SlidingObjectNovel scenario to get more feasible levels
            {
                qSRRelation.Add(new NorthWest(far.a, far.b)); // the falling object should be in the NorthWest of the pig to have feasible levels
            }
            else if (scenarioName == "RollingFallingObjectNovel")//  dirty fix for RollingFallingObjectNovel scenario to get more feasible levels
            {
                qSRRelation.Add(new NorthWest(far.a, far.b)); // the falling object should be in the NorthWest of the pig to have feasible levels
            }
            else
            {
                qSRRelation.Add(new North(far.a, far.b));
                qSRRelation.Add(new NorthEast(far.a, far.b));
                qSRRelation.Add(new NorthWest(far.a, far.b));
            }

        }
        else if (far.d.GetType() == typeof(Below))
        {
            qSRRelation.Add(new South(far.a, far.b));
            qSRRelation.Add(new SouthEast(far.a, far.b));
            qSRRelation.Add(new SouthWest(far.a, far.b));
        }

        else if (far.d.GetType() == typeof(Left))
        {
            qSRRelation.Add(new West(far.a, far.b));
            qSRRelation.Add(new NorthWest(far.a, far.b));
            qSRRelation.Add(new SouthWest(far.a, far.b));
        }
        else if (far.d.GetType() == typeof(Right))
        {
            qSRRelation.Add(new East(far.a, far.b));
            qSRRelation.Add(new NorthEast(far.a, far.b));
            qSRRelation.Add(new SouthEast(far.a, far.b));
        }

        return qSRRelation;

    }

    public List<NQSRRelation> getFarNQSRPredicates(Far far)
    {

        List<NQSRRelation> nQSRRelation = new List<NQSRRelation>();
        if (far.d.GetType() == typeof(Above))
        {
            nQSRRelation.Add(new FarNorth(far.a, far.b));
        }
        else if (far.d.GetType() == typeof(Below))
        {
            nQSRRelation.Add(new FarSouth(far.a, far.b));
        }
        else if (far.d.GetType() == typeof(Left))
        {
            nQSRRelation.Add(new FarWest(far.a, far.b));
        }
        else if (far.d.GetType() == typeof(Right))
        {
            nQSRRelation.Add(new FarEast(far.a, far.b));
        }
        return nQSRRelation;

    }
}
