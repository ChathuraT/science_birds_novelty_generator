using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoveltyGrammar
{
    public VerbGrammar a; public VerbGrammar b; // the causal interaction a -> b
}

//public class CausalInteraction
//{
//    public VerbGrammar a; public VerbGrammar b; // the causal interaction a -> b

//    public CausalInteraction(VerbGrammar a, VerbGrammar b)
//    {
//        this.a = a;
//        this.b = b;
//    }
//}

public class NotOnRightForce : NoveltyGrammar
{
    public NotOnRightForce(VerbGrammar a, VerbGrammar b)
    {
        this.a = a;
        this.b = b;
    }
}

public class OnlyOnRightForce : NoveltyGrammar
{
    public OnlyOnRightForce(VerbGrammar a, VerbGrammar b)
    {
        this.a = a;
        this.b = b;
    }
}

public class NotOnLeftForce : NoveltyGrammar
{
    public NotOnLeftForce(VerbGrammar a, VerbGrammar b)
    {
        this.a = a;
        this.b = b;
    }
}

public class OnlyOnLeftForce : NoveltyGrammar
{
    public OnlyOnLeftForce(VerbGrammar a, VerbGrammar b)
    {
        this.a = a;
        this.b = b;
    }
}

public class NotOnUpForce : NoveltyGrammar
{
    public NotOnUpForce(VerbGrammar a, VerbGrammar b)
    {
        this.a = a;
        this.b = b;
    }
}

public class OnlyOnUpForce : NoveltyGrammar
{
    public OnlyOnUpForce(VerbGrammar a, VerbGrammar b)
    {
        this.a = a;
        this.b = b;
    }
}

public class NotOnDownForce : NoveltyGrammar
{
    public NotOnDownForce(VerbGrammar a, VerbGrammar b)
    {
        this.a = a;
        this.b = b;
    }
}

public class OnlyOnDownForce : NoveltyGrammar
{
    public OnlyOnDownForce(VerbGrammar a, VerbGrammar b)
    {
        this.a = a;
        this.b = b;
    }
}
