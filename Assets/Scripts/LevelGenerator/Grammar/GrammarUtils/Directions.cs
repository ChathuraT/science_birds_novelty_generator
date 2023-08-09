
public class Direction
{

}

public class Left : Direction
{

}

public class Right : Direction
{

}

public class Above : Direction
{

}

public class Below : Direction
{

}

public class AllDirection : Direction
{
    Direction[] directions;
    public AllDirection(Direction[] directions)
    {
        this.directions = directions;
    }
}

public class AnyDirection : Direction
{
    Direction[] directions;
    public AnyDirection(Direction[] directions)
    {
        this.directions = directions;
    }
}


