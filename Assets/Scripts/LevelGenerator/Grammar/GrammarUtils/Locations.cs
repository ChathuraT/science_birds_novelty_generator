
public class Location
{

}

public class OnLeft : Location
{

}

public class OnCentre : Location
{

}

public class OnRight : Location
{

}

public class UpperLeft : Location
{

}

public class CentreLeft : Location
{

}

public class LowerLeft : Location
{

}

public class UpperRight : Location
{

}

public class CentreRight : Location
{

}

public class LowerRight : Location
{

}

public class AnyLocation : Location
{
    Direction[] directions;
    AnyLocation(Direction[] directions)
    {
        this.directions = directions;
    }
}