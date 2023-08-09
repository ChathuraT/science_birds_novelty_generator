using System.Collections.Generic;

public class ScenarioInfo
{
    // populated at the instantiation of the scenario
    public string nonNovelScenarioID;
    public string novelScenarioID;
    public string scenarioName;
    public ScenarioObjects scenarioObjects;
    public long generationStartTimeTicks;
    public long generationEndTimeTicks;
    public NonNovelSolution nonNovelSolution;
    public NovelSolution novelSolution;
    public bool nonNovelSolvability;
    public bool novelSolvability;
}

public class NonNovelSolution
{
    public ScenarioObject targetObject;
    public Coordinate targetPoint;
    public string trajectory; // high/low
    public float releaseAngle;// might not be needed
    public int shotIndex; // might not be needed
}

public class NovelSolution
{
    public ScenarioObject targetObject;
    public Coordinate targetPoint;
    public string trajectory; // high/low
    public float releaseAngle;// might not be needed
    public int shotIndex; // might not be needed
}

public class ScenarioObjects
{
    public List<ScenarioObject> layoutObjects;
    public List<ScenarioObject> distractionObjects;
}

public class ScenarioObject
{
    public string scenarioObjectName;
    public Coordinate position;
}

public class Coordinate
{
    public float x;
    public float y;
}

