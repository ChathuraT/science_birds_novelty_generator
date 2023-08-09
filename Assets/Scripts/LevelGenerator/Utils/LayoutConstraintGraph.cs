using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LayoutConstraintGraph
{
    public List<MainObjectAndAllLayoutGrammars> mainObjectAndAllLayoutGrammarsList;

    public LayoutConstraintGraph(Scenario scenario)
    {
        // instantiate the LayoutConstraintGraph and populate the grapgh with layout constraints, QSRPredicates are not populated (they are populated from the QSRPredicateInferrer)
        InstantiateLayoutConstraintGraph(scenario);
    }

    private void InstantiateLayoutConstraintGraph(Scenario scenario)
    {

        this.mainObjectAndAllLayoutGrammarsList = new List<MainObjectAndAllLayoutGrammars>();

        // instantiate ObjectAndAllLayoutGrammars for all the main objects in the scenario
        foreach (ObjectGrammar mainObjct in scenario.objects)
        {
            List<ObjectAndLayoutGrammars> objectAndLayoutGrammarsList = new List<ObjectAndLayoutGrammars>();

            // populate all the objects for the objectAndLayoutGrammars
            foreach (ObjectGrammar objct in scenario.objects)
            {
                ObjectAndLayoutGrammars objectAndLayoutGrammars = new ObjectAndLayoutGrammars(objct.gameObject, new List<LayoutGrammar>());
                objectAndLayoutGrammarsList.Add(objectAndLayoutGrammars);
            }
            this.mainObjectAndAllLayoutGrammarsList.Add(new MainObjectAndAllLayoutGrammars(mainObjct.gameObject, objectAndLayoutGrammarsList));
        }

        // populate the layout constraint grapgh from the data in the scenario
        PopulateLayoutConstraintGraph(scenario);
    }

    private void PopulateLayoutConstraintGraph(Scenario scenario)
    {

        foreach (LayoutGrammar layout in scenario.layouts)
        {
            if (layout.GetType() == typeof(InDirection))
            {
                AddLayoutGrammarToMainObject(((InDirection)layout).a.gameObject, ((InDirection)layout).b.gameObject, layout);
            }
            else if (layout.GetType() == typeof(OnLocation))
            {
                AddLayoutGrammarToMainObject(((OnLocation)layout).a.gameObject, ((OnLocation)layout).b.gameObject, layout);
            }
            else if (layout.GetType() == typeof(PathObstructed))
            {
                AddLayoutGrammarToMainObject(((PathObstructed)layout).a.gameObject, ((PathObstructed)layout).b.gameObject, layout);
            }
            else if (layout.GetType() == typeof(LiesOnPath))
            {
                AddLayoutGrammarToMainObject(((LiesOnPath)layout).a.gameObject, ((LiesOnPath)layout).b.gameObject, layout);
            }
            else if (layout.GetType() == typeof(Far))
            {
                AddLayoutGrammarToMainObject(((Far)layout).a.gameObject, ((Far)layout).b.gameObject, layout);
            }
            else if (layout.GetType() == typeof(Touching))
            {
                AddLayoutGrammarToMainObject(((Touching)layout).a.gameObject, ((Touching)layout).b.gameObject, layout);

                /* for (int i = 0; i < ((Touching)layout).objcts.Count - 1; i++)
                    {
                        // for touching terms break the touching list into pairs and add to the graph (eg: touching<a><b><c> is decomposed to touching<a><b> and touching<b><c>)
                        Touching decomposedTouching = new Touching(new List<ObjectGrammar> { ((Touching)layout).objcts[i], ((Touching)layout).objcts[i + 1] });
                        AddLayoutGrammarToMainObject(decomposedTouching.objcts[0].gameObject, decomposedTouching.objcts[1].gameObject, decomposedTouching);
                    } */
            }
        }

    }

    public void AddLayoutGrammarToMainObject(Object mainObjct, Object secondObjct, LayoutGrammar layoutGrammar)
    {

        // check the ObjectAndLayoutGrammars to find the objectAndLayoutGrammarsList that is associated with the main object
        foreach (MainObjectAndAllLayoutGrammars mainObjectAndAllLayoutGrmmars in mainObjectAndAllLayoutGrammarsList)
        {
            if (mainObjectAndAllLayoutGrmmars.mainObjct == mainObjct)
            {
                // now check for the objectAndLayoutGrammarsList to find the ObjectAndLayoutGrammars that is associated with the second game object in the verb
                foreach (ObjectAndLayoutGrammars objectAndLayoutGrammars in mainObjectAndAllLayoutGrmmars.objectAndLayoutGrammarsList)
                {
                    if (objectAndLayoutGrammars.objct == secondObjct)
                    {

                        // add the Layout term to this list
                        objectAndLayoutGrammars.AddLayout(layoutGrammar);

                    }

                }

                break;
            }

        }
    }

    public void PrintLayoutConstraintGraph()
    {
        foreach (MainObjectAndAllLayoutGrammars mainObjectAndAllLayoutGrammars in mainObjectAndAllLayoutGrammarsList)
        {
            Debug.Log("* Main Object: " + mainObjectAndAllLayoutGrammars.mainObjct);
            foreach (ObjectAndLayoutGrammars objectAndLayoutGrammars in mainObjectAndAllLayoutGrammars.objectAndLayoutGrammarsList)
            {
                Debug.Log("-- Object: " + objectAndLayoutGrammars.objct);
                foreach (LayoutGrammar layoutGrammar in objectAndLayoutGrammars.layoutGrammars)
                {
                    Debug.Log("---- Layout: " + Utilities.GetAllPropertiesAndValues(layoutGrammar));
                }
                foreach (List<QSRRelation> qSRRelationList in objectAndLayoutGrammars.qSRRelations)
                {
                    if (qSRRelationList.Any())
                    {
                        Debug.Log("---- QSRRelations: " + Utilities.GetAllPropertiesAndValues(qSRRelationList));
                    }
                }

            }
        }
    }
}

public class ObjectAndLayoutGrammars
{
    public Object objct;
    public List<LayoutGrammar> layoutGrammars;
    public List<List<QSRRelation>> qSRRelations; // one layout grammar term can have a set of QSRRelations (with OR connection - where satisfying one is enough)
    public List<List<NQSRRelation>> nQSRRelations; // one layout grammar term can have a set of NQSRRelations (with OR connection - where satisfying one is enough) - still only having one is handled

    public ObjectAndLayoutGrammars(Object objct, List<LayoutGrammar> layoutGrammars)
    {
        this.objct = objct;
        this.layoutGrammars = layoutGrammars;
        this.qSRRelations = new List<List<QSRRelation>>();
        this.nQSRRelations = new List<List<NQSRRelation>>();
    }

    public void AddLayout(LayoutGrammar layoutGrammar)
    {
        this.layoutGrammars.Add(layoutGrammar);
    }

    public void AddQSRRelations(List<List<QSRRelation>> qSRRelations)
    {
        this.qSRRelations.AddRange(qSRRelations);
    }
    public void AddNQSRRelations(List<List<NQSRRelation>> nQSRRelations)
    {
        this.nQSRRelations.AddRange(nQSRRelations);
    }
}

public class MainObjectAndAllLayoutGrammars
{
    public Object mainObjct;
    public List<ObjectAndLayoutGrammars> objectAndLayoutGrammarsList;

    public MainObjectAndAllLayoutGrammars(Object mainObjct, List<ObjectAndLayoutGrammars> objectAndLayoutGrammarsList)
    {
        this.mainObjct = mainObjct;
        this.objectAndLayoutGrammarsList = objectAndLayoutGrammarsList;
    }
}
