using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayoutGrammar
{
    public ObjectGrammar a; public ObjectGrammar b;
}

public class InDirection : LayoutGrammar
{
    public Direction d;

    public InDirection(ObjectGrammar a, ObjectGrammar b, Direction d)
    {
        this.a = a;
        this.b = b;
        this.d = d;
    }

}

public class Touching : LayoutGrammar
{
    public Location d;
    public Touching(ObjectGrammar a, ObjectGrammar b, Location d)
    {
        this.a = a;
        this.b = b;
        this.d = d;
    }

}

public class OnLocation : LayoutGrammar
{
    public Location d;

    public OnLocation(ObjectGrammar a, ObjectGrammar b, Location d)
    {
        this.a = a;
        this.b = b;
        this.d = d;
    }

}

public class Far : LayoutGrammar
{
    public Direction d;

    public Far(ObjectGrammar a, ObjectGrammar b, Direction d)
    {
        this.a = a;
        this.b = b;
        this.d = d;
    }

}

public class PathObstructed : LayoutGrammar
{
    public Direction d;

    public PathObstructed(ObjectGrammar a, ObjectGrammar b, Direction d)
    {
        this.a = a;
        this.b = b;
        this.d = d;
    }

}

public class LiesOnPath : LayoutGrammar
{

    public LiesOnPath(ObjectGrammar a, ObjectGrammar b)
    {
        this.a = a;
        this.b = b;
    }

}
