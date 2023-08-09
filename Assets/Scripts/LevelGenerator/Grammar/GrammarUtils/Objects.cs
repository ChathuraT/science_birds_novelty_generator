using UnityEngine;

public class Object
{
    public float positionX = -100;
    public float positionY = -100;
    public float scaleX = 1;
    public float scaleY = 1;
    public float rotation = 0;
    public string material = "wood";
    public float forceMagnitude = 2; // the magnitude of the force assoicated with the novelty (only used for novelties associated with the force)

    public int groupID = 0; //group id is used to group objects that need to stay together in the generation
    public UnityEngine.Object associatedPrefab; // prefab associated with the game object
    public float[] size; // size of the prefab WxH (note: object sizes are round off to the nearest even value to make them compatible with the constraint solver)
    public int instanceID = 0; // the ID of the object when it is instantiated in the gameworld
    public bool isNovel = false; // the flag to store whether the object is novel/nonNovel (whether the object is the novel/nonNovel solution path) - used for the simulation solver
}

public class Block : Object
{

}

public class Bird : Object
{

}

public class Pig : Object
{

}

public class Plat : Object
{

}

public class NovelForceObject : Object
{

}

public class BasicSmall : Pig
{
    public BasicSmall()
    {
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Characters\\Pigs\\BasicSmall");
        // size = new float[] { 0.47f, 0.45f };
        size = new float[] { 0.48f, 0.46f };
    }
}

public class BasicMedium : Pig
{
    public BasicMedium()
    {
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Characters\\Pigs\\BasicMedium");
        size = new float[] { 0.78f, 0.76f };

    }
}

public class BasicBig : Pig
{
    public BasicBig()
    {
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Characters\\Pigs\\BasicBig");
        //size = new float[] { 0.99f, 0.97f };
        size = new float[] { 1f, 0.98f };

    }
}
public class BirdRed : Bird
{

    public BirdRed()
    {
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Characters\\Birds\\BirdRed");
        //size = new float[] { 0.43f, 0.43f };
        size = new float[] { 0.44f, 0.44f };

    }
}

public class Circle : Block
{
    public Circle()
    {
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Blocks\\Circle");
        size = new float[] { 0.8f, 0.8f };

    }
}

public class CircleSmall : Block
{
    public CircleSmall()
    {
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Blocks\\CircleSmall");
        // size = new float[] { 0.43f, 0.43f };
        size = new float[] { 0.44f, 0.44f };

    }
}

public class RectBig : Block
{
    public RectBig()
    {
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Blocks\\RectBig");
        size = new float[] { 2.06f, 0.22f };

    }
}

public class RectFat : Block
{
    public RectFat()
    {
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Blocks\\RectFat");
        // size = new float[] { 0.85f, 0.43f };
        size = new float[] { 0.86f, 0.44f };

    }
}

public class RectMedium : Block
{
    public RectMedium()
    {
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Blocks\\RectMedium");
        size = new float[] { 1.68f, 0.22f };

    }
}

public class RectSmall : Block
{
    public RectSmall()
    {
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Blocks\\RectSmall");
        //size = new float[] { 0.85f, 0.22f };
        size = new float[] { 0.86f, 0.22f };

    }
}

public class RectTiny : Block
{
    public RectTiny()
    {
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Blocks\\RectTiny");
        // size = new float[] { 0.43f, 0.22f };
        size = new float[] { 0.44f, 0.22f };

    }
}

public class SquareHole : Block
{
    public SquareHole()
    {
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Blocks\\SquareHole");
        size = new float[] { 0.84f, 0.84f };

    }
}

public class SquareSmall : Block
{
    public SquareSmall()
    {
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Blocks\\SquareSmall");
        // size = new float[] { 0.43f, 0.43f };
        size = new float[] { 0.44f, 0.44f };

    }
}

public class SquareTiny : Block
{
    public SquareTiny()
    {
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Blocks\\SquareTiny");
        size = new float[] { 0.22f, 0.22f };

    }
}

public class Triangle : Block
{
    public Triangle()
    {
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Blocks\\Triangle");
        size = new float[] { 0.82f, 0.82f };

    }
}

public class TriangleHole : Block
{
    public TriangleHole()
    {
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Blocks\\TriangleHole");
        size = new float[] { 0.82f, 0.82f };

    }
}

public class Platform : Plat
{
    public Platform()
    {
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Platform");
        size = new float[] { 0.64f, 0.64f };

    }
}

public class RightForce : NovelForceObject
{
    public RightForce()
    {
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Novelties\\RightForce");
        size = new float[] { 0.64f, 0.64f };

    }

    public RightForce(float forceMagnitude)
    {
        this.forceMagnitude = forceMagnitude;
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Novelties\\RightForce");
        size = new float[] { 0.64f, 0.64f };

    }
}

public class LeftForce : NovelForceObject
{
    public LeftForce()
    {
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Novelties\\LeftForce");
        size = new float[] { 0.64f, 0.64f };

    }

    public LeftForce(float forceMagnitude)
    {
        this.forceMagnitude = forceMagnitude;
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Novelties\\LeftForce");
        size = new float[] { 0.64f, 0.64f };

    }
}

public class UpForce : NovelForceObject
{
    public UpForce()
    {
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Novelties\\UpForce");
        size = new float[] { 0.64f, 0.64f };

    }

    public UpForce(float forceMagnitude)
    {
        this.forceMagnitude = forceMagnitude;
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Novelties\\UpForce");
        size = new float[] { 0.64f, 0.64f };

    }
}

public class DownForce : NovelForceObject
{
    public DownForce()
    {
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Novelties\\DownForce");
        size = new float[] { 0.64f, 0.64f };

    }

    public DownForce(float forceMagnitude)
    {
        this.forceMagnitude = forceMagnitude;
        associatedPrefab = Resources.Load("Prefabs\\GameWorld\\Novelties\\DownForce");
        size = new float[] { 0.64f, 0.64f };

    }
}
