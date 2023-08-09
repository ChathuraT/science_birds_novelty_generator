using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectGrammar : Grammar
{

    public Object gameObject;
}

public class NormalBird : ObjectGrammar
{
    public NormalBird()
    {
        gameObject = new BirdRed();
    }
}

public class NormalPig : ObjectGrammar
{
    public NormalPig()
    {
        // randomly select a pig with 50% probability
        int randomIndex = Random.Range(1, 3);

        switch (randomIndex)
        {
            case 1:
                gameObject = new BasicSmall();
                break;
            case 2:
                gameObject = new BasicMedium();
                break;
        }
    }

    public NormalPig(Pig pig)
    {
        // instantiate with the given pig
        gameObject = pig;
    }
}

public class StrongPig : ObjectGrammar
{
    public StrongPig()
    {
        gameObject = new BasicBig();
    }
}


public class RollableObject : ObjectGrammar
{

    public RollableObject()
    {
        // randomly select a rollable object

        int randomIndex = Random.Range(2, 3);

        switch (randomIndex)
        {
            //case 1: // removed as too fragile
            //    gameObject = new CircleSmall();
            //    break;
            case 2:
                gameObject = new Circle();
                break;
        }

        // select random material
        if (Random.Range(1, 3) == 1)
        {
            gameObject.material = "wood";
        }
        else
        {
            gameObject.material = "stone";
        }
    }

    public RollableObject(Block block)
    {
        // instantiate with the given block
        gameObject = block;

        // select random material
        if (Random.Range(1, 3) == 1)
        {
            gameObject.material = "wood";
        }
        else
        {
            gameObject.material = "stone";
        }
    }
}

public class FallableObject : ObjectGrammar
{

    public FallableObject()
    {
        // randomly select a fallable object

        int randomIndex = Random.Range(2, 5);

        switch (randomIndex)
        {
            //case 1: // removed as too fragile
            //    gameObject = new CircleSmall();
            //    break;
            case 2:
                gameObject = new Circle();
                break;
            case 3:
                gameObject = new SquareHole();
                break;
            case 4:
                gameObject = new TriangleHole();
                break;
        }
        // select random material
        if (Random.Range(1, 3) == 1)
        {
            gameObject.material = "wood";
        }
        else
        {
            gameObject.material = "stone";
        }
    }

    public FallableObject(Block block)
    {
        // instantiate with the given block
        gameObject = block;

        // select random material
        if (Random.Range(1, 3) == 1)
        {
            gameObject.material = "wood";
        }
        else
        {
            gameObject.material = "stone";
        }
    }
}

public class SlidableObject : ObjectGrammar
{

    public SlidableObject()
    {
        // randomly select a slidable object

        int randomIndex = Random.Range(1, 3);

        switch (randomIndex)
        {
            case 1:
                gameObject = new SquareHole();
                break;
            case 2:
                gameObject = new RectFat();
                break;
        }
        // select random material
        if (Random.Range(1, 3) == 1)
        {
            gameObject.material = "wood";
        }
        else
        {
            gameObject.material = "stone";
        }
    }

    public SlidableObject(Block block)
    {
        // instantiate with the given objet
        gameObject = block;
        // select random material
        if (Random.Range(1, 3) == 1)
        {
            gameObject.material = "wood";
        }
        else
        {
            gameObject.material = "stone";
        }

    }
}


public class FlatSurface : ObjectGrammar
{
    public FlatSurface()
    {
        gameObject = new Platform();
    }

}

public class FlatLongSurface : ObjectGrammar // for sliding and novelrolling
{
    public FlatLongSurface()
    {
        gameObject = new Platform();
        // assign a random length to the surface TODO: determine this range
        gameObject.scaleX = Random.Range(3, 5);
        gameObject.scaleY = Random.Range(0.3f, 0.6f);
    }

    public FlatLongSurface(int minXScale, int maxXScale)
    {
        gameObject = new Platform();
        // assign a random length to the surface
        gameObject.scaleX = Random.Range(minXScale, maxXScale);
        gameObject.scaleY = Random.Range(0.3f, 0.6f);
    }
}

public class SlopeLongSurface : ObjectGrammar // for novelsliding
{
    public SlopeLongSurface()
    {
        gameObject = new Platform();
        // assign a random length to the surface TODO: determine this range
        gameObject.scaleX = Random.Range(3, 5);
        gameObject.scaleY = Random.Range(0.3f, 0.6f);
        gameObject.rotation = -Random.Range(10, 30);
    }

    public SlopeLongSurface(float min, float max)
    {
        gameObject = new Platform();
        gameObject.scaleX = Random.Range(min, max);
        gameObject.scaleY = Random.Range(0.3f, 0.6f);
        gameObject.rotation = -Random.Range(27, 35); // original values: 10, 20
    }

    public SlopeLongSurface(int minXScale, int maxXScale, int minRotation, int maxRotation)
    {
        gameObject = new Platform();
        // assign a random length to the surface
        gameObject.scaleX = Random.Range(minXScale, maxXScale);
        gameObject.scaleY = Random.Range(0.3f, 0.6f);
        gameObject.rotation = -Random.Range(minRotation, maxRotation);
    }
}

public class Slope : ObjectGrammar
{
    public Slope()
    {
        gameObject = new Platform();

        // set a random rotation between 10 and 45
        gameObject.rotation = Random.Range(10, 45);
    }

    public Slope(float scaleXMax, float scaleYMax) // scaleY adjusted slope (for bouncing)
    {
        gameObject = new Platform();

        // assign a random length to the surface
        gameObject.scaleX = Random.Range(0.5f, scaleXMax);
        gameObject.scaleY = Random.Range(0.5f, scaleYMax);

        // set a random rotation between 10 and 45
        gameObject.rotation = -Random.Range(10, 45);
    }
}

public class Surface : ObjectGrammar
{
    // surface can be a FlatSurface or a Slope
    public Surface()
    {
        gameObject = new Platform();

        // assign a random length to the surface TODO: determine this range
        gameObject.scaleX = Random.Range(8, 15);
        gameObject.scaleY = Random.Range(0.3f, 0.6f);

        // randomly select a FlatSurface or a Slope
        if (Random.Range(1, 2) == 1)
        {
            // set a random rotation between 10 and 45 TODO: determine this angle
            gameObject.rotation = -Random.Range(10, 30);
        }
    }

    public Surface(int maxLength)
    {
        gameObject = new Platform();

        // assign a random length to the surface TODO: determine this range
        gameObject.scaleX = Random.Range(8, maxLength);
        gameObject.scaleY = Random.Range(0.3f, 0.6f);

        // randomly select a FlatSurface or a Slope
        if (Random.Range(1, 2) == 1)
        {
            // set a random rotation between 10 and 45 TODO: determine this angle
            gameObject.rotation = -Random.Range(10, 30);
        }
    }

    public Surface(float minRotation, float maxRotation)
    {
        gameObject = new Platform();

        // assign a random length to the surface TODO: determine this range
        gameObject.scaleX = Random.Range(8, 15);
        gameObject.scaleY = Random.Range(0.3f, 0.6f);

        // set a random rotation between 10 and 45 TODO: determine this angle
        gameObject.rotation = -Random.Range(minRotation, maxRotation);
        
    }
}

public class LongSurface : ObjectGrammar
{
    // FlatSurface or a Slope
    public LongSurface()
    {
        gameObject = new Platform();

        // assign a random length to the surface TODO: determine this range
        gameObject.scaleX = Random.Range(8, 15);
        gameObject.scaleY = Random.Range(0.3f, 0.6f);

        // randomly select a FlatSurface or a Slope
        if (Random.Range(1, 3) == 1)
        {
            // set a random rotation between 10 and 45 TODO: determine this angle
            gameObject.rotation = -Random.Range(10, 30);
        }
    }

    public LongSurface(int maxLength)
    {
        gameObject = new Platform();

        // assign a random length to the surface TODO: determine this range
        gameObject.scaleX = Random.Range(8, maxLength);
        gameObject.scaleY = Random.Range(0.3f, 0.6f);

        // randomly select a FlatSurface or a Slope
        if (Random.Range(1, 3) == 1)
        {
            // set a random rotation between 10 and 45 TODO: determine this angle
            gameObject.rotation = -Random.Range(10, 30);
        }
    }
}

public class DistractionObject : ObjectGrammar
{
    public DistractionObject(Block block)
    {
        gameObject = block;

        if (Random.Range(1, 3) == 1)
        {
            gameObject.material = "wood";
        }
        else
        {
            gameObject.material = "stone";
        }
    }

}

public class NovelObject : ObjectGrammar
{
    public NovelObject(NovelForceObject novelty)
    {
        gameObject = novelty;
    }
}

