using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QSRRelation
{
    public ObjectGrammar a; public ObjectGrammar b;

    public QSRRelation()
    {
    }

    public QSRRelation(ObjectGrammar a, ObjectGrammar b)
    {
        this.a = a;
        this.b = b;
    }
}

/*
 Qualitative Direction Calculus - 8 Direction Relations
     */

public class North : QSRRelation
{
    public North(ObjectGrammar a, ObjectGrammar b) : base(a, b)
    {

    }
}

public class South : QSRRelation
{
    public South(ObjectGrammar a, ObjectGrammar b) : base(a, b)
    {

    }
}

public class East : QSRRelation
{
    public East(ObjectGrammar a, ObjectGrammar b) : base(a, b)
    {

    }
}

public class West : QSRRelation
{
    public West(ObjectGrammar a, ObjectGrammar b) : base(a, b)
    {

    }
}

public class NorthEast : QSRRelation
{
    public NorthEast(ObjectGrammar a, ObjectGrammar b) : base(a, b)
    {

    }
}

public class NorthWest : QSRRelation
{
    public NorthWest(ObjectGrammar a, ObjectGrammar b) : base(a, b)
    {

    }
}

public class SouthEast : QSRRelation
{
    public SouthEast(ObjectGrammar a, ObjectGrammar b) : base(a, b)
    {

    }
}

public class SouthWest : QSRRelation
{
    public SouthWest(ObjectGrammar a, ObjectGrammar b) : base(a, b)
    {

    }
}

/*
 Qualitative Interval Algebra
     */


public class MeetNorth : QSRRelation
{
    public MeetNorth(ObjectGrammar a, ObjectGrammar b) : base(a, b)
    {

    }
}

public class MeetWest : QSRRelation
{
    public MeetWest(ObjectGrammar a, ObjectGrammar b) : base(a, b)
    {

    }
}

public class MeetSouth : QSRRelation
{
    public MeetSouth(ObjectGrammar a, ObjectGrammar b) : base(a, b)
    {

    }
}

public class MeetEast : QSRRelation
{
    public MeetEast(ObjectGrammar a, ObjectGrammar b) : base(a, b)
    {

    }
}

public class MeetNorthWest : QSRRelation
{
    public MeetNorthWest(ObjectGrammar a, ObjectGrammar b) : base(a, b)
    {

    }
}

public class MeetSouthWest : QSRRelation
{
    public MeetSouthWest(ObjectGrammar a, ObjectGrammar b) : base(a, b)
    {

    }
}

public class MeetDuringEast : QSRRelation
{
    public MeetDuringEast(ObjectGrammar a, ObjectGrammar b) : base(a, b)
    {

    }
}

public class MeetDuringWest : QSRRelation
{
    public MeetDuringWest(ObjectGrammar a, ObjectGrammar b) : base(a, b)
    {

    }
}


/*
 Qualitative Distance Calculus - Far Relations
     */

//public class FarNorth : QSRRelation
//{
//    public FarNorth(ObjectGrammar a, ObjectGrammar b) : base(a, b)
//    {

//    }
//}

//public class FarSouth : QSRRelation
//{
//    public FarSouth(ObjectGrammar a, ObjectGrammar b) : base(a, b)
//    {

//    }
//}

//public class FarEast : QSRRelation
//{
//    public FarEast(ObjectGrammar a, ObjectGrammar b) : base(a, b)
//    {

//    }
//}

//public class FarWest : QSRRelation
//{
//    public FarWest(ObjectGrammar a, ObjectGrammar b) : base(a, b)
//    {

//    }
//}

//public class FarNorthEast : QSRRelation
//{
//    public FarNorthEast(ObjectGrammar a, ObjectGrammar b) : base(a, b)
//    {

//    }
//}

//public class FarNorthWest : QSRRelation
//{
//    public FarNorthWest(ObjectGrammar a, ObjectGrammar b) : base(a, b)
//    {

//    }
//}

//public class FarSouthEast : QSRRelation
//{
//    public FarSouthEast(ObjectGrammar a, ObjectGrammar b) : base(a, b)
//    {

//    }
//}

//public class FarSouthWest : QSRRelation
//{
//    public FarSouthWest(ObjectGrammar a, ObjectGrammar b) : base(a, b)
//    {

//    }
//}
