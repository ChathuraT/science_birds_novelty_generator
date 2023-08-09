using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerbGrammar
{
    public ObjectGrammar a; public ObjectGrammar b;
}

public class Hit : VerbGrammar
{
    public Direction d;
    public Hit(ObjectGrammar a, ObjectGrammar b, Direction d)
    {
        this.a = a;
        this.b = b;
        this.d = d;
    }

}

public class HitDestroy : VerbGrammar
{
    public Direction d;
    public HitDestroy(ObjectGrammar a, ObjectGrammar b, Direction d)
    {
        this.a = a;
        this.b = b;
        this.d = d;
    }

}

public class Roll : VerbGrammar
{
    public Direction d;
    public Roll(ObjectGrammar a, ObjectGrammar b, Direction d)
    {
        this.a = a;
        this.b = b;
        this.d = d;
    }

}

public class Fall : VerbGrammar
{
    public Fall(ObjectGrammar a, ObjectGrammar b)
    {
        this.a = a;
        this.b = b;
    }

}

public class Slide : VerbGrammar
{
    public Direction d;
    public Slide(ObjectGrammar a, ObjectGrammar b, Direction d)
    {
        this.a = a;
        this.b = b;
        this.d = d;
    }

}

public class Bounce : VerbGrammar
{
    public Direction d;
    public Bounce(ObjectGrammar a, ObjectGrammar b, Direction d)
    {
        this.a = a;
        this.b = b;
        this.d = d;
    }

}
