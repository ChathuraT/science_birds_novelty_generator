using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Non-qualitative relations

public class NQSRRelation
{

}


public class FarNorth : NQSRRelation
{
    ObjectGrammar a; ObjectGrammar b; float distance;

    public FarNorth(ObjectGrammar a, ObjectGrammar b)
    {
        this.a = a;
        this.b = b;
    }
}

public class FarSouth : NQSRRelation
{
    ObjectGrammar a; ObjectGrammar b; float distance;

    public FarSouth(ObjectGrammar a, ObjectGrammar b)
    {
        this.a = a;
        this.b = b;
    }
}

public class FarWest : NQSRRelation
{
    ObjectGrammar a; ObjectGrammar b; float distance;

    public FarWest(ObjectGrammar a, ObjectGrammar b)
    {
        this.a = a;
        this.b = b;
    }
}

public class FarEast : NQSRRelation
{
    ObjectGrammar a; ObjectGrammar b; float distance;

    public FarEast(ObjectGrammar a, ObjectGrammar b)
    {
        this.a = a;
        this.b = b;
    }
}
