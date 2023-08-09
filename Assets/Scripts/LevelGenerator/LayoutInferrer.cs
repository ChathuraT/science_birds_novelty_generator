using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

public class LayoutInferrer
{

    private Utilities utilities;

    public LayoutInferrer(Utilities utilities)
    {
        this.utilities = utilities;
    }

    public void InferLayout(Scenario scenario)
    {

        List<LayoutGrammar> inferredLayout = new List<LayoutGrammar>();
        List<ObjectGrammar> gameObjects = new List<ObjectGrammar>();

        // get layouts and gameObjects from verbs
        foreach (VerbGrammar verb in scenario.verbs)
        {
            inferredLayout.AddRange(InferFromVerb(verb));

            // get all the fields in the verbGrammar
            FieldInfo[] fields = verb.GetType().GetFields();
            foreach (FieldInfo field in fields)
            {
                // save only the ObjectGrammars
                if (field.FieldType == typeof(ObjectGrammar))
                {
                    // check if this gameobject is already saved, if not save
                    if (!gameObjects.Contains((ObjectGrammar)field.GetValue(verb)))
                    {
                        gameObjects.Add((ObjectGrammar)field.GetValue(verb));
                    }
                }
            }
        }

        // get layouts from constraints
        if (scenario.constraints != null)
        {
            foreach (ConstraintGrammar constraint in scenario.constraints)
            {
                Debug.Log("Constraint grammar considered: " + constraint);
                inferredLayout.AddRange(InferFromConstraint(constraint, scenario.verbs));
            }
        }

        // get layouts from sequence


        // update the scenario with inferred layout and objects
        // the layout terms can be repeated as they are added from multiple grammar terms, therefore remove the repeated layout terms
        scenario.layouts = RemoveRepeatedLayoutTerms(inferredLayout);
        scenario.objects = gameObjects;
    }

    public List<LayoutGrammar> RemoveRepeatedLayoutTerms(List<LayoutGrammar> layoutList)
    {
        /*
            This function removes the duplicated layout terms in a given layout list
         */

        List<LayoutGrammar> filteredLayoutList = new List<LayoutGrammar>();
        foreach (LayoutGrammar layoutGrammar in layoutList)
        {
            bool alreadyAdded = false;
            foreach (LayoutGrammar filteredLayoutGrammar in filteredLayoutList)
            {

                // compare the grammar term, object a, object b and the directin d, only LiesOnPath does not have a direction
                if (layoutGrammar.GetType() == filteredLayoutGrammar.GetType())
                {
                    if ((layoutGrammar.GetType() == typeof(LiesOnPath)) & (layoutGrammar.a == filteredLayoutGrammar.a) & (layoutGrammar.b == filteredLayoutGrammar.b)) // if LiesOnPath only compare a and b
                    {
                        alreadyAdded = true;
                    }
                    else if ((layoutGrammar.a == filteredLayoutGrammar.a) & (layoutGrammar.b == filteredLayoutGrammar.b)) // else compare a, b and d 
                    {   // TODO: d is not compared at the moment ( because d can be a direction or a location, need to compare grammar term wise), or is it not necessary? for the currect scenarios for a given 2 objects the same layout term is applied only once
                        alreadyAdded = true;
                    }
                }
            }
            if (!alreadyAdded)
            {
                filteredLayoutList.Add(layoutGrammar);
            }
        }
        return filteredLayoutList;
    }

    public List<LayoutGrammar> InferFromVerb(VerbGrammar verb)
    {

        List<LayoutGrammar> inferredLayout = new List<LayoutGrammar>();

        switch (verb)
        {

            case Hit hit:
                inferredLayout.Add(new LiesOnPath(hit.b, hit.a));
                if (hit.d.GetType() == typeof(Right)) // if a is hitting b on b's right, b is in left to a
                    inferredLayout.Add(new InDirection(hit.b, hit.a, new Left()));
                else if (hit.d.GetType() == typeof(Left)) // if a is hitting b on b's left, b is in right to a 
                    inferredLayout.Add(new InDirection(hit.b, hit.a, new Right()));
                else if (hit.d.GetType() == typeof(Above)) // if a is hitting b on b's above, b is in below to a
                    inferredLayout.Add(new InDirection(hit.b, hit.a, new Below()));
                else if (hit.d.GetType() == typeof(Below)) // if a is hitting b on b's below, b is in above to a
                    inferredLayout.Add(new InDirection(hit.b, hit.a, new Above()));
                break;
            case HitDestroy hitDestroy:
                inferredLayout.Add(new LiesOnPath(hitDestroy.b, hitDestroy.a));
                break;
            case Roll roll:
                // if rolling right object is onLeft initially, if rolling left the object is onRight
                if (roll.d.GetType() == typeof(Right))
                    inferredLayout.Add(new InDirection(roll.a, roll.b, new Left()));
                else if (roll.d.GetType() == typeof(Left))
                    inferredLayout.Add(new InDirection(roll.a, roll.b, new Right()));
                break;
            case Fall fall:
                //inferredLayout.Add(new InDirection(fall.a, fall.b, new Above())); //TODO: comment
                inferredLayout.Add(new Far(fall.a, fall.b, new Above())); //TODO: uncomment
                inferredLayout.Add(new LiesOnPath(fall.b, fall.a));
                break;
            case Slide slide:
                // if sliding right object is onLeft initially, if sliding left the object is onRight
                if (slide.d.GetType() == typeof(Right))
                    // inferredLayout.Add(new OnLocation(slide.a, slide.b, new OnLeft()));
                    inferredLayout.Add(new InDirection(slide.a, slide.b, new Left()));
                else if (slide.d.GetType() == typeof(Left))
                    // inferredLayout.Add(new OnLocation(slide.a, slide.b, new OnRight()));
                    inferredLayout.Add(new InDirection(slide.a, slide.b, new Right()));
                break;
            case Bounce bounce:
                inferredLayout.Add(new InDirection(bounce.a, bounce.b, bounce.d));
                break;
            default:
                break;
        }

        return inferredLayout;
    }

    public List<LayoutGrammar> InferFromConstraint(ConstraintGrammar constraint, List<VerbGrammar> verbs)
    {

        List<LayoutGrammar> inferredLayout = new List<LayoutGrammar>();

        switch (constraint)
        {
            case CannotReach cannotReach:
                inferredLayout.Add(new PathObstructed(cannotReach.a, cannotReach.b, new AllDirection(new Direction[] { new Left(), new Right(), new Above(), new Below() })));
                break;
            case CannotReachDirectly cannotReachDirectly:
                inferredLayout.Add(new PathObstructed(cannotReachDirectly.a, cannotReachDirectly.b, cannotReachDirectly.d));
                break;
            case CannotFallMoving cannotFallMoving:
                List<(ObjectGrammar, Location)> connectedObjectsAndLocations = utilities.GetConnectedObjectsForMoving(cannotFallMoving.a, verbs);
                // foreach (ObjectGrammar obj in connectedObjects)
                //    Debug.Log(obj);
                for (int i = 1; i < connectedObjectsAndLocations.Count; i++)
                {
                    Debug.Log("adding touching constraint: " + connectedObjectsAndLocations[i - 1].Item1 + connectedObjectsAndLocations[i].Item1 + connectedObjectsAndLocations[i].Item2);
                    inferredLayout.Add(new Touching(connectedObjectsAndLocations[i - 1].Item1, connectedObjectsAndLocations[i].Item1, connectedObjectsAndLocations[i].Item2));
                }
                break;
            case LiesAtEndOfPath liesAtEndOfPath:
                HashSet<ObjectGrammar> connectedObjectsToMovingObject = utilities.GetObjectsAssociatedWithTheMovement(liesAtEndOfPath.b, liesAtEndOfPath.a, verbs);
                // foreach (ObjectGrammar obj in connectedObjects)
                //    Debug.Log(obj);
                foreach (ObjectGrammar connectedObject in connectedObjectsToMovingObject)
                {
                    Debug.Log("(liesAtEndOfPath resolving): adding inDirection constraint for object: " + connectedObject);
                    inferredLayout.Add(new InDirection(liesAtEndOfPath.a, connectedObject, new Below()));
                }
                break;
            case InDirectionBelow inDirectionBelow:
                inferredLayout.Add(new InDirection(inDirectionBelow.a, inDirectionBelow.b, new Below()));
                break;
            case TouchUpperLeft touchUpperLeft:
                inferredLayout.Add(new Touching(touchUpperLeft.a, touchUpperLeft.b, new UpperLeft()));
                break;
            case TouchLowerLeft touchLowerLeft:
                inferredLayout.Add(new Touching(touchLowerLeft.a, touchLowerLeft.b, new LowerLeft()));
                break;
        }

        return inferredLayout;
    }

    //public List<LayoutGrammar> InferFromSequence(List<VerbGrammar> verbs)
    //{

    //    /* not implemented
    //     Infer layout terms from the sequence. Currently considering the below cases
    //     1) Hit -> Roll -> Fall : object being hit and the rolling surface should be touching
    //     */
    //    List<LayoutGrammar> inferredLayout = new List<LayoutGrammar>();

    //    int index = 0;
    //    int verbCount = verbs.Count;

    //    while (index < verbCount)
    //    {
    //        if (verbs[index].GetType() == typeof(Hit))
    //        {
    //            if ((index + 2) <= verbCount)
    //            {
    //                if ((verbs[index + 1].GetType() == typeof(Roll)) & (verbs[index + 2].GetType() == typeof(Fall)))
    //                {

    //                }
    //            }
    //        }

    //        index++;

    //    }

    //    return inferredLayout;
    //}


    public void AddSupportPlatforms(Scenario scenario)
    {
        /*
         * This function checks whether there are any unsupported objects - if there are blocks/pigs that do not have an onLocation Layout Grammar they are unsupported, add platforms below them
         */

        // get all onLocation grammar terms
        List<OnLocation> onLocationTerms = new List<OnLocation>();
        foreach (LayoutGrammar layout in scenario.layouts)
        {
            if (layout.GetType() == typeof(OnLocation))
            {
                onLocationTerms.Add((OnLocation)layout);
            }
        }


        // iterate all gameobjects, find ones that needs support and add platforms
        List<ObjectGrammar> newBasePlatforms = new List<ObjectGrammar>();
        foreach (ObjectGrammar objectTerm in scenario.objects)
        {
            // skip birds and platforms as they do not need any support
            if ((objectTerm.gameObject.GetType().BaseType != typeof(Bird)) & (objectTerm.gameObject.GetType().BaseType != typeof(Plat)))
            {
                bool objectIsSupported = false;

                // check if the gameObject is linked with an existing onLocation layout grammar
                foreach (OnLocation onLocationTerm in onLocationTerms)
                {
                    if (onLocationTerm.a.gameObject == objectTerm.gameObject)
                    {
                        objectIsSupported = true;
                    }
                }

                // if it is not linked to an existing OnLocation grammar, that means it is not supported, add a base platform
                if (!objectIsSupported)
                {
                    FlatSurface basePlatform = new FlatSurface();

                    // scale the platform according to the width of the object it supports
                    basePlatform.gameObject.scaleX = Utilities.GetMBRWidthAndHeight(objectTerm.gameObject)[0] / basePlatform.gameObject.size[0];
                    basePlatform.gameObject.scaleY = 0.3f; // make it thinner

                    basePlatform.gameObject.groupID = objectTerm.gameObject.groupID;
                    newBasePlatforms.Add(basePlatform);

                    // add onLocation term to the layout
                    scenario.layouts.Add(new OnLocation(objectTerm, basePlatform, new OnCentre()));
                }
            }
        }

        // add all new baseplatforms to the scenario
        scenario.objects.AddRange(newBasePlatforms);
    }
}
