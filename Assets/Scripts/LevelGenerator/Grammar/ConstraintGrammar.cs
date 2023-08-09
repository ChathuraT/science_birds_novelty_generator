using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstraintGrammar
{

}
public class CannotReach : ConstraintGrammar
{
    public ObjectGrammar a; public ObjectGrammar b;

    public CannotReach(ObjectGrammar a, ObjectGrammar b)
    {
        this.a = a;
        this.b = b;
    }
}
public class CannotReachDirectly : ConstraintGrammar
{
    public ObjectGrammar a; public ObjectGrammar b; public Direction d;

    public CannotReachDirectly(ObjectGrammar a, ObjectGrammar b, Direction d)
    {
        this.a = a;
        this.b = b;
        this.d = d;
    }
}

public class CannotFallMoving : ConstraintGrammar
{
    public ObjectGrammar a;

    public CannotFallMoving(ObjectGrammar a)
    {
        this.a = a;
    }
}

// some constraints to make some scenarios work - think to fix them later using other verbs/constraints
public class LiesAtEndOfPath : ConstraintGrammar // for bouncing scenario
{
    public ObjectGrammar a; public ObjectGrammar b;

    public LiesAtEndOfPath(ObjectGrammar a, ObjectGrammar b)
    {
        this.a = a;
        this.b = b;
    }
}

public class InDirectionBelow : ConstraintGrammar // for SlidingRollingObject scenario 
{
    public ObjectGrammar a; public ObjectGrammar b;

    public InDirectionBelow(ObjectGrammar a, ObjectGrammar b)
    {
        this.a = a;
        this.b = b;
    }
}

// constraints for novelty generation
public class TouchUpperLeft : ConstraintGrammar // for RollingObjectNovel scenario
{
    public ObjectGrammar a; public ObjectGrammar b;

    public TouchUpperLeft(ObjectGrammar a, ObjectGrammar b)
    {
        this.a = a;
        this.b = b;
    }
}

public class TouchLowerLeft : ConstraintGrammar // for RollingObjectNovel scenario
{
    public ObjectGrammar a; public ObjectGrammar b;

    public TouchLowerLeft(ObjectGrammar a, ObjectGrammar b)
    {
        this.a = a;
        this.b = b;
    }
}


//public class InDirectionSouthEast : ConstraintGrammar // for RollingObjectNovel scenario
//{
//    public ObjectGrammar a; public ObjectGrammar b;

//    public InDirectionSouthEast(ObjectGrammar a, ObjectGrammar b)
//    {
//        this.a = a;
//        this.b = b;
//    }
//}


