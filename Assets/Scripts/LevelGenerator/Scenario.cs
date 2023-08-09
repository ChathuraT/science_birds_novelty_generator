using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA;

public class Scenario
{
    // scenario name
    public string scenarioName;
    public int scenarioIndex;

    // user input verbs of the scenario and the associated constraints
    public List<VerbGrammar> verbs;
    public List<ConstraintGrammar> constraints;
    public List<NoveltyGrammar> noveltyConstraints;

    // the target obejcts of the novel and non-novel solutions
    public Object novelTarget;
    public Object nonNovelTarget;

    // the force magnitude - used for the NovelForce novelty
    public float forceMagnitude;

    // layouts and gameobjects are inferred from the verbs and contraints - populated when LayoutInferrer.InferLayout is called
    public List<LayoutGrammar> layouts;
    public List<ObjectGrammar> objects;

    // a scenario info object to save the information about to scenario to write into the scenarioinfo.txt file at the end
    public ScenarioInfo scenarioInfo;

    public Scenario(List<VerbGrammar> verbs, List<ConstraintGrammar> constraints, List<NoveltyGrammar> noveltyConstraints, Object nonNovelTarget, Object novelTarget, float forceMagnitude, string scenarioName, int scenarioIndex) // for novel task generation
    {
        this.verbs = verbs;
        this.constraints = constraints;
        this.noveltyConstraints = noveltyConstraints;
        this.nonNovelTarget = nonNovelTarget;
        this.novelTarget = novelTarget;
        this.forceMagnitude = forceMagnitude;
        this.scenarioName = scenarioName;
        this.scenarioIndex = scenarioIndex;

        InstantiateScenarioInfoObject(scenarioName, scenarioIndex);
    }

    public Scenario(List<VerbGrammar> verbs, List<ConstraintGrammar> constraints, string scenarioName) // for original task generation
    {
        this.verbs = verbs;
        this.constraints = constraints;
        this.scenarioName = scenarioName;

        InstantiateScenarioInfoObject(scenarioName, 0);
    }

    public Scenario(List<VerbGrammar> verbs, string scenarioName)
    {
        this.verbs = verbs;
        this.scenarioName = scenarioName;

        InstantiateScenarioInfoObject(scenarioName, 0);
    }

    public void InstantiateScenarioInfoObject(string scenarioName, int scenarioIndex)
    {
        // generate a random id of length 9 considering the current time
        string iDString = DateTime.UtcNow.ToString("Hmmss") + UnityEngine.Random.Range(0, 10000).ToString().PadLeft(4, '0');

        this.scenarioInfo = new ScenarioInfo();
        this.scenarioInfo.scenarioName = scenarioName;
        this.scenarioInfo.nonNovelScenarioID = scenarioName + "_" + iDString + "_0_" + scenarioIndex;
        this.scenarioInfo.novelScenarioID = scenarioName + "_" + iDString + "_1_" + scenarioIndex;
        this.scenarioInfo.generationStartTimeTicks = System.DateTime.UtcNow.Ticks; // recording the generation start time in ticks
    }
}

public class PredefinedScenarios
{

    // =================================== SINGLE FORCE =================================== //
    public Scenario SingleForce()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();

        NormalBird normalBird = new NormalBird();
        NormalPig normalPig = new NormalPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });

        verbGrammar.Add(new HitDestroy(normalBird, normalPig, direction));

        Scenario scenario = new Scenario(verbGrammar, "SingleForce");
        return scenario;
    }
    public Scenario SingleForceTopBlocked()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();

        NormalBird normalBird = new NormalBird();
        NormalPig normalPig = new NormalPig();
        Direction hittingDirection = new Left();
        Direction blockedDirection = new Above();

        verbGrammar.Add(new HitDestroy(normalBird, normalPig, hittingDirection));

        constraintGrammar.Add(new CannotReachDirectly(normalBird, normalPig, blockedDirection));

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "SingleForceTopBlocked");
        return scenario;
    }
    public Scenario SingleForceLeftBlocked()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();

        NormalBird normalBird = new NormalBird();
        NormalPig normalPig = new NormalPig();
        Direction hittingDirection = new Above();
        Direction blockedDirection = new Left();

        verbGrammar.Add(new HitDestroy(normalBird, normalPig, hittingDirection));

        constraintGrammar.Add(new CannotReachDirectly(normalBird, normalPig, blockedDirection));

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "SingleForceLeftBlocked");
        return scenario;
    }

    // =================================== MULTIPLE FORCES =================================== //
    public Scenario MultipleForces()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();

        NormalBird normalBird1 = new NormalBird();
        NormalBird normalBird2 = new NormalBird();
        StrongPig strongPig = new StrongPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });

        verbGrammar.Add(new Hit(normalBird1, strongPig, direction));
        verbGrammar.Add(new Hit(normalBird2, strongPig, direction));
        verbGrammar.Add(new HitDestroy(normalBird2, strongPig, direction));

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "MultipleForces");
        return scenario;
    }
    public Scenario MultipleForcesTopBlocked()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();

        NormalBird normalBird1 = new NormalBird();
        NormalBird normalBird2 = new NormalBird();
        StrongPig strongPig = new StrongPig();
        Direction hittingDirection = new Left();
        Direction blockedDirection = new Above();

        verbGrammar.Add(new Hit(normalBird1, strongPig, hittingDirection));
        verbGrammar.Add(new Hit(normalBird2, strongPig, hittingDirection));
        verbGrammar.Add(new HitDestroy(normalBird2, strongPig, blockedDirection));

        constraintGrammar.Add(new CannotReachDirectly(normalBird1, strongPig, blockedDirection));

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "MultipleForces");
        return scenario;
    }
    public Scenario MultipleForcesLeftBlocked()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();

        NormalBird normalBird1 = new NormalBird();
        NormalBird normalBird2 = new NormalBird();
        StrongPig strongPig = new StrongPig();
        Direction hittingDirection = new Above();
        Direction blockedDirection = new Left();

        verbGrammar.Add(new Hit(normalBird1, strongPig, hittingDirection));
        verbGrammar.Add(new Hit(normalBird2, strongPig, hittingDirection));
        verbGrammar.Add(new HitDestroy(normalBird2, strongPig, blockedDirection));

        constraintGrammar.Add(new CannotReachDirectly(normalBird1, strongPig, blockedDirection));

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "MultipleForces");
        return scenario;
    }

    // =================================== ROLLING =================================== //
    public Scenario RollingObject()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        NormalBird normalBird = new NormalBird();
        RollableObject rollableObject = new RollableObject();
        Surface surface = new Surface();
        NormalPig normalPig = new NormalPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });
        Direction right = new Right();

        verbGrammar.Add(new Hit(normalBird, rollableObject, direction));
        verbGrammar.Add(new Roll(rollableObject, surface, right));
        verbGrammar.Add(new HitDestroy(rollableObject, normalPig, direction));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(rollableObject));

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "RollingObject");
        return scenario;
    }

    public Scenario RollingObjectNovel()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        NormalBird normalBird = new NormalBird();
        RollableObject rollableObject = new RollableObject();
        LongSurface surface = new LongSurface(12);
        NormalPig normalPig = new NormalPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });
        Direction right = new Right();
        Direction left = new Left();
        Direction above = new Above();

        // NN
        RollableObject rollableObject2 = new RollableObject();
        Surface surface2 = new Surface();
        // FallableObject fallableObject = new FallableObject();

        verbGrammar.Add(new Hit(normalBird, rollableObject2, direction));
        verbGrammar.Add(new Roll(rollableObject2, surface2, right));
        verbGrammar.Add(new Fall(rollableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(rollableObject2, normalPig, left));

        // N
        verbGrammar.Add(new Hit(normalBird, rollableObject, direction));
        verbGrammar.Add(new Roll(rollableObject, surface, right));
        verbGrammar.Add(new HitDestroy(rollableObject, normalPig, left));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(rollableObject));

        constraintGrammar.Add(new TouchUpperLeft(rollableObject2, surface2));
        constraintGrammar.Add(new InDirectionBelow(normalPig, surface2));

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "RollingObjectNovel");
        return scenario;
    }

    public Scenario RollingBird() // [not impelmented]
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        NormalBird normalBird = new NormalBird();
        Surface surface = new Surface();
        NormalPig normalPig = new NormalPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });
        Direction right = new Right();
        Direction above = new Above();

        verbGrammar.Add(new Hit(normalBird, surface, above));
        verbGrammar.Add(new Roll(normalBird, surface, right));
        verbGrammar.Add(new HitDestroy(normalBird, normalPig, direction));

        constraintGrammar.Add(new CannotReachDirectly(normalBird, normalPig, direction));
        constraintGrammar.Add(new CannotFallMoving(normalBird));

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "RollingBird");
        return scenario;
    }

    // =================================== FALLING =================================== //
    public Scenario FallingObject()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        NormalBird normalBird = new NormalBird();
        FallableObject fallableObject = new FallableObject();
        NormalPig normalPig = new NormalPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });

        verbGrammar.Add(new Hit(normalBird, fallableObject, direction));
        verbGrammar.Add(new Fall(fallableObject, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject, normalPig, direction));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "FallingObject");
        return scenario;
    }

    public Scenario FallingObjectNovelty()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        NormalBird normalBird = new NormalBird();
        FallableObject fallableObject = new FallableObject();
        NormalPig normalPig = new NormalPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });

        // N 
        FallableObject fallableObject2 = new FallableObject();

        verbGrammar.Add(new Hit(normalBird, fallableObject2, direction));
        verbGrammar.Add(new Fall(fallableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject2, normalPig, direction));

        // NN
        verbGrammar.Add(new Hit(normalBird, fallableObject, direction));
        verbGrammar.Add(new Fall(fallableObject, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject, normalPig, direction));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));

        constraintGrammar.Add(new InDirectionBelow(fallableObject, fallableObject2));

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "FallingObjectNovelty");
        return scenario;
    }

    // =================================== SLIDING =================================== //
    public Scenario SlidingObject()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        NormalBird normalBird = new NormalBird();
        SlidableObject slidableObject = new SlidableObject();
        NormalPig normalPig = new NormalPig();
        FlatLongSurface flatLongSurface = new FlatLongSurface();
        Direction left = new Left();
        Direction right = new Right();

        verbGrammar.Add(new Hit(normalBird, slidableObject, left));
        verbGrammar.Add(new Slide(slidableObject, flatLongSurface, right));
        verbGrammar.Add(new HitDestroy(slidableObject, normalPig, left));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(slidableObject));


        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "SlidingObject");
        return scenario;
    }

    public Scenario SlidingObjectNovel()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        NormalBird normalBird = new NormalBird();
        SlidableObject slidableObject = new SlidableObject();
        NormalPig normalPig = new NormalPig();
        FlatLongSurface flatLongSurface = new FlatLongSurface();
        Direction left = new Left();
        Direction right = new Right();

        // NN
        SlidableObject slidableObject2 = new SlidableObject();
        SlopeLongSurface slopeLongSurface = new SlopeLongSurface();
        // FallableObject fallableObject = new FallableObject();

        verbGrammar.Add(new Hit(normalBird, slidableObject2, left));
        verbGrammar.Add(new Slide(slidableObject2, slopeLongSurface, right));
        verbGrammar.Add(new Fall(slidableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(slidableObject2, normalPig, left));

        // N
        verbGrammar.Add(new Hit(normalBird, slidableObject, left));
        verbGrammar.Add(new Slide(slidableObject, flatLongSurface, right));
        verbGrammar.Add(new HitDestroy(slidableObject, normalPig, left));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(slidableObject));

        constraintGrammar.Add(new TouchUpperLeft(slidableObject2, slopeLongSurface));
        constraintGrammar.Add(new InDirectionBelow(normalPig, slopeLongSurface));

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "SlidingObjectNovel");
        return scenario;
    }

    // =================================== BOUNCING =================================== //
    public Scenario BouncingBird()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        NormalBird normalBird = new NormalBird();
        NormalPig normalPig = new NormalPig();
        Slope slope = new Slope(4f, 1f); // set (1f, 4f) for LT shots, set (4f, 1f) for HT shots
        Direction direction1 = new AnyDirection(new Direction[] { new Left(), new Above(), new Below() });
        Direction direction2 = new Below();
        Direction direction3 = new Above();
        //Direction direction4 = new AnyDirection(new Direction[] { new Left(), new Above() });

        verbGrammar.Add(new Hit(normalBird, slope, direction1));
        verbGrammar.Add(new Bounce(normalBird, slope, direction2));
        verbGrammar.Add(new HitDestroy(normalBird, normalPig, direction3));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new LiesAtEndOfPath(normalPig, normalBird)); // the pig should be placed on the path of the bird after bird bounces off the surface - scenario specific contraint - think about excluding them later

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "BouncingBird");
        return scenario;
    }
    // =================================== COMBINED SCENARIOS =================================== //

    /*
    Limitations:
        - currently only last liesOnPath terms is satisfied (objects are adjusted only considering the last liesOnPath term), so in the combined scenarios when there are middle liesOnpath terms 
        (eg: in bouncingFalling scenario falling object has to lie on the path of the bird and the pig has to lie on the path of the falling object) objects are not adjusted to satisfy them, hence they 
        have to be satisfied by chance in order to generate a feasible level.
    */
    public Scenario RollingFallingObject()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();

        NormalBird normalBird = new NormalBird();
        RollableObject rollableObject = new RollableObject();
        FallableObject fallableObject = new FallableObject();
        Surface surface = new Surface();
        NormalPig normalPig = new NormalPig();
        Direction direction1 = new AnyDirection(new Direction[] { new Left(), new Above() });
        Direction left = new Left();
        Direction right = new Right();

        verbGrammar.Add(new Hit(normalBird, rollableObject, direction1));
        verbGrammar.Add(new Roll(rollableObject, surface, right));
        verbGrammar.Add(new Hit(rollableObject, fallableObject, left));
        verbGrammar.Add(new Fall(fallableObject, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject, normalPig, direction1));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(rollableObject));

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "RollingFallingObject");
        return scenario;
    }

    public Scenario RollingFallingObjectNovel()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();

        NormalBird normalBird = new NormalBird();
        RollableObject rollableObject = new RollableObject();
        FallableObject fallableObject = new FallableObject();
        Surface surface = new Surface();
        NormalPig normalPig = new NormalPig();
        Direction direction1 = new AnyDirection(new Direction[] { new Left(), new Above() });
        Direction left = new Left();
        Direction right = new Right();

        // N
        RollableObject rollableObject2 = new RollableObject();
        FallableObject fallableObject2 = new FallableObject();
        Surface surface2 = new Surface();
        verbGrammar.Add(new Hit(normalBird, rollableObject2, direction1));
        verbGrammar.Add(new Roll(rollableObject2, surface2, right));
        verbGrammar.Add(new Hit(rollableObject2, fallableObject2, left));
        verbGrammar.Add(new Fall(fallableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject2, normalPig, direction1));

        // NN
        verbGrammar.Add(new Hit(normalBird, rollableObject, direction1));
        verbGrammar.Add(new Roll(rollableObject, surface, right));
        verbGrammar.Add(new Hit(rollableObject, fallableObject, left));
        verbGrammar.Add(new Fall(fallableObject, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject, normalPig, direction1));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(rollableObject));

        constraintGrammar.Add(new CannotFallMoving(rollableObject2));
        constraintGrammar.Add(new InDirectionBelow(fallableObject2, fallableObject));
        // constraintGrammar.Add(new InDirectionBelow(normalPig, surface2));

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "RollingFallingObjectNovel");
        return scenario;
    }

    public Scenario RollingSlidingObject()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();

        NormalBird normalBird = new NormalBird();
        RollableObject rollableObject = new RollableObject();
        SlidableObject slidableObject = new SlidableObject();
        Surface surface = new Surface();
        FlatLongSurface flatLongSurface = new FlatLongSurface();
        NormalPig normalPig = new NormalPig();
        Direction direction1 = new AnyDirection(new Direction[] { new Left(), new Above() });
        Direction left = new Left();
        Direction right = new Right();


        verbGrammar.Add(new Hit(normalBird, rollableObject, direction1));
        verbGrammar.Add(new Roll(rollableObject, surface, right));
        verbGrammar.Add(new Hit(rollableObject, slidableObject, left));
        verbGrammar.Add(new Slide(slidableObject, flatLongSurface, right));
        verbGrammar.Add(new HitDestroy(slidableObject, normalPig, left));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(rollableObject));
        constraintGrammar.Add(new CannotFallMoving(slidableObject));

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "RollingSlidingObject");
        return scenario;
    }

    public Scenario FallingRollingObject()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        NormalBird normalBird = new NormalBird();
        FallableObject fallableObject = new FallableObject();
        RollableObject rollableObject = new RollableObject();
        Surface surface = new Surface();
        NormalPig normalPig = new NormalPig();
        Direction right = new Right();
        Direction above = new Above();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });

        verbGrammar.Add(new Hit(normalBird, fallableObject, direction));
        verbGrammar.Add(new Fall(fallableObject, rollableObject));
        verbGrammar.Add(new Hit(fallableObject, rollableObject, above)); // hitting from AnyDirection does not add a inDirection term, so fallableObject and rollableObject are not placed appropriately (as only liesOnPath term is added in between those 2 obejcts and that liesOnPath term is not tried to satisfy in the current implementation)
        verbGrammar.Add(new Roll(rollableObject, surface, right));
        verbGrammar.Add(new HitDestroy(rollableObject, normalPig, direction));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(rollableObject));

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "FallingRollingObject");
        return scenario;
    }

    public Scenario SlidingRollingObject()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();

        NormalBird normalBird = new NormalBird();
        RollableObject rollableObject = new RollableObject();
        SlidableObject slidableObject = new SlidableObject();
        Surface surface = new Surface();
        FlatLongSurface flatLongSurface = new FlatLongSurface();
        NormalPig normalPig = new NormalPig();
        Direction direction1 = new AnyDirection(new Direction[] { new Left(), new Above() });
        Direction left = new Left();
        Direction right = new Right();


        verbGrammar.Add(new Hit(normalBird, slidableObject, direction1));
        verbGrammar.Add(new Slide(slidableObject, flatLongSurface, right));
        verbGrammar.Add(new Hit(slidableObject, rollableObject, left));
        verbGrammar.Add(new Roll(rollableObject, surface, right));
        verbGrammar.Add(new HitDestroy(rollableObject, normalPig, direction1));


        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(slidableObject));
        constraintGrammar.Add(new CannotFallMoving(rollableObject));
        constraintGrammar.Add(new InDirectionBelow(normalPig, flatLongSurface)); // the sliding platform should be above the pig - scenario specific contraint - think about excluding them later

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "SlidingRollingObject");
        return scenario;
    }

    public Scenario BouncingBirdFallingObject()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        NormalBird normalBird = new NormalBird();
        NormalPig normalPig = new NormalPig();
        Slope slope = new Slope(3f, 0.6f);
        //Slope slope = new Slope(1f, 4f); // set (1f, 4f) for LT shots, set (4f, 1f) for HT shots
        Direction direction1 = new AnyDirection(new Direction[] { new Left(), new Above(), new Below() });
        Direction direction2 = new Below();
        Direction direction3 = new Above();
        Direction direction4 = new AnyDirection(new Direction[] { new Right(), new Above() });
        FallableObject fallableObject = new FallableObject(new CircleSmall()); // select CircleSmall for lower trajectories and select the circle for higher trajectories

        verbGrammar.Add(new Hit(normalBird, slope, direction1));
        verbGrammar.Add(new Bounce(normalBird, slope, direction2));
        verbGrammar.Add(new Hit(normalBird, fallableObject, direction3));
        verbGrammar.Add(new Fall(fallableObject, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject, normalPig, direction4));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new LiesAtEndOfPath(fallableObject, normalBird)); // the fallableObject should be placed on the path of the bird after bird bounces off the surface - scenario specific contraint - think about excluding them later

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "BouncingBirdFallingObject");
        return scenario;
    }

    public Scenario BouncingBirdRollingObject() // no feasible levels
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        NormalBird normalBird = new NormalBird();
        NormalPig normalPig = new NormalPig();
        Slope slope = new Slope(3f, 0.6f);
        Surface surface = new Surface();
        Direction direction1 = new AnyDirection(new Direction[] { new Left(), new Above(), new Below() });
        Direction direction2 = new Below();
        Direction direction3 = new Above();
        Direction right = new Right();

        Direction direction4 = new AnyDirection(new Direction[] { new Left(), new Above() });
        RollableObject rollableObject = new RollableObject();        // select CircleSmall for lower trajectories and select the circle for higher trajectories

        verbGrammar.Add(new Hit(normalBird, slope, direction1));
        verbGrammar.Add(new Bounce(normalBird, slope, direction2));
        verbGrammar.Add(new Hit(normalBird, rollableObject, direction3));
        verbGrammar.Add(new Roll(rollableObject, surface, right));
        verbGrammar.Add(new HitDestroy(rollableObject, normalPig, direction4));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(rollableObject));
        constraintGrammar.Add(new LiesAtEndOfPath(rollableObject, normalBird)); // the rollableObject should be placed on the path of the bird after bird bounces off the surface - scenario specific contraint - think about excluding them later

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "BouncingBirdRollingObject");
        return scenario;
    }


    public Scenario SlidingRollingFallingObject()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();

        NormalBird normalBird = new NormalBird();
        RollableObject rollableObject = new RollableObject();
        SlidableObject slidableObject = new SlidableObject();
        FallableObject fallableObject = new FallableObject();
        Surface surface = new Surface();
        FlatLongSurface flatLongSurface = new FlatLongSurface();
        NormalPig normalPig = new NormalPig();
        Direction direction1 = new AnyDirection(new Direction[] { new Left(), new Above() });
        Direction left = new Left();
        Direction right = new Right();

        verbGrammar.Add(new Hit(normalBird, slidableObject, direction1));
        verbGrammar.Add(new Slide(slidableObject, flatLongSurface, right));
        verbGrammar.Add(new Hit(slidableObject, rollableObject, left));
        verbGrammar.Add(new Roll(rollableObject, surface, right));
        verbGrammar.Add(new Hit(rollableObject, fallableObject, direction1));
        verbGrammar.Add(new Fall(fallableObject, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject, normalPig, direction1));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(slidableObject));
        constraintGrammar.Add(new CannotFallMoving(rollableObject));
        constraintGrammar.Add(new InDirectionBelow(fallableObject, flatLongSurface)); // adding these scenario specific contraints to make it work - o.w. ydimension graph can't determine which is the lowest object
        constraintGrammar.Add(new InDirectionBelow(normalPig, surface)); // adding these scenario specific contraints to make it work - o.w. ydimension graph can't determine which is the lowest object
        constraintGrammar.Add(new InDirectionBelow(normalPig, fallableObject)); // adding these scenario specific contraints to make it work - o.w. ydimension graph can't determine which is the lowest object
        constraintGrammar.Add(new InDirectionBelow(normalPig, slidableObject)); // adding these scenario specific contraints to make it work - o.w. ydimension graph can't determine which is the lowest object
        constraintGrammar.Add(new InDirectionBelow(normalPig, rollableObject)); // adding these scenario specific contraints to make it work - o.w. ydimension graph can't determine which is the lowest object

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "SlidingRollingFallingObject");
        return scenario;

    }

    public Scenario SlidingFallingRollingObject()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        NormalBird normalBird = new NormalBird();
        SlidableObject slidableObject = new SlidableObject();
        FallableObject fallableObject = new FallableObject();
        RollableObject rollableObject = new RollableObject();
        FlatLongSurface flatLongSurface = new FlatLongSurface();
        Surface surface = new Surface();
        NormalPig normalPig = new NormalPig();
        Direction right = new Right();
        Direction above = new Above();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });

        verbGrammar.Add(new Hit(normalBird, slidableObject, direction));
        verbGrammar.Add(new Slide(slidableObject, flatLongSurface, right));
        verbGrammar.Add(new Hit(slidableObject, fallableObject, direction));
        verbGrammar.Add(new Fall(fallableObject, rollableObject));
        verbGrammar.Add(new Hit(fallableObject, rollableObject, above)); // hitting from AnyDirection does not add a inDirection term, so fallableObject and rollableObject are not placed appropriately (as only liesOnPath term is added in between those 2 obejcts and that liesOnPath term is not tried to satisfy in the current implementation)
        verbGrammar.Add(new Roll(rollableObject, surface, right));
        verbGrammar.Add(new HitDestroy(rollableObject, normalPig, direction));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(rollableObject));
        constraintGrammar.Add(new CannotFallMoving(slidableObject));
        constraintGrammar.Add(new InDirectionBelow(rollableObject, flatLongSurface)); // adding these scenario specific contraints to make it work - o.w. ydimension graph can't determine which is the lowest object
        constraintGrammar.Add(new InDirectionBelow(rollableObject, slidableObject)); // adding these scenario specific contraints to make it work - o.w. ydimension graph can't determine which is the lowest object

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "SlidingFallingRollingObject");
        return scenario;
    }

    public Scenario RollingRollingRollingObject()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        NormalBird normalBird = new NormalBird();
        RollableObject rollableObject1 = new RollableObject();
        RollableObject rollableObject2 = new RollableObject();
        RollableObject rollableObject3 = new RollableObject();
        Surface surface1 = new Surface(8); // pass athe max length to the surface to make it shorter
        Surface surface2 = new Surface(8);
        Surface surface3 = new Surface(8);
        NormalPig normalPig = new NormalPig();
        Direction right = new Right();
        Direction above = new Above();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });

        verbGrammar.Add(new Hit(normalBird, rollableObject1, direction));
        verbGrammar.Add(new Roll(rollableObject1, surface1, right));
        verbGrammar.Add(new Hit(rollableObject1, rollableObject2, direction));
        verbGrammar.Add(new Roll(rollableObject2, surface2, right));
        verbGrammar.Add(new Hit(rollableObject2, rollableObject3, direction));
        verbGrammar.Add(new Roll(rollableObject3, surface3, right));
        verbGrammar.Add(new HitDestroy(rollableObject3, normalPig, direction));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(rollableObject1));
        constraintGrammar.Add(new CannotFallMoving(rollableObject2));
        constraintGrammar.Add(new CannotFallMoving(rollableObject3));
        constraintGrammar.Add(new InDirectionBelow(normalPig, surface2)); // adding these scenario specific contraints to make it work - o.w. ydimension graph can't determine which is the lowest object
        constraintGrammar.Add(new InDirectionBelow(rollableObject3, rollableObject2)); // adding these scenario specific contraints to make it work - o.w. ydimension graph can't determine which is the lowest object
        constraintGrammar.Add(new InDirectionBelow(rollableObject2, rollableObject1)); // adding these scenario specific contraints to make it work - o.w. ydimension graph can't determine which is the lowest object

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "RollingRollingRollingObject");
        return scenario;
    }


    public Scenario RollingRollingFallingObject()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        NormalBird normalBird = new NormalBird();
        RollableObject rollableObject1 = new RollableObject();
        RollableObject rollableObject2 = new RollableObject();
        FallableObject fallableObject = new FallableObject();
        Surface surface1 = new Surface(8); // pass athe max length to the surface to make it shorter
        Surface surface2 = new Surface(8);
        NormalPig normalPig = new NormalPig();
        Direction right = new Right();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });

        verbGrammar.Add(new Hit(normalBird, rollableObject1, direction));
        verbGrammar.Add(new Roll(rollableObject1, surface1, right));
        verbGrammar.Add(new Hit(rollableObject1, rollableObject2, direction));
        verbGrammar.Add(new Roll(rollableObject2, surface2, right));
        verbGrammar.Add(new Hit(rollableObject2, fallableObject, direction));
        verbGrammar.Add(new Fall(fallableObject, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject, normalPig, direction));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(rollableObject1));
        constraintGrammar.Add(new CannotFallMoving(rollableObject2));

        constraintGrammar.Add(new InDirectionBelow(rollableObject2, rollableObject1)); // adding these scenario specific contraints to make it work - o.w. ydimension graph can't determine which is the lowest object
        constraintGrammar.Add(new InDirectionBelow(fallableObject, rollableObject2)); // adding these scenario specific contraints to make it work - o.w. ydimension graph can't determine which is the lowest object
        constraintGrammar.Add(new InDirectionBelow(normalPig, surface1)); // adding these scenario specific contraints to make it work - o.w. ydimension graph can't determine which is the lowest object
        constraintGrammar.Add(new InDirectionBelow(normalPig, surface2)); // adding these scenario specific contraints to make it work - o.w. ydimension graph can't determine which is the lowest object
        constraintGrammar.Add(new InDirectionBelow(normalPig, fallableObject)); // adding these scenario specific contraints to make it work - o.w. ydimension graph can't determine which is the lowest object
        constraintGrammar.Add(new InDirectionBelow(normalPig, rollableObject1)); // adding these scenario specific contraints to make it work - o.w. ydimension graph can't determine which is the lowest object
        constraintGrammar.Add(new InDirectionBelow(normalPig, rollableObject2)); // adding these scenario specific contraints to make it work - o.w. ydimension graph can't determine which is the lowest object

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, "RollingRollingFallingObject");
        return scenario;
    }



    /////////////////////////////////////////// Novel Scenarios ///////////////////////////////////////////

    public Scenario RollingObjectNovel1() // N surface is flat long (located below) and NN surface is short inclined, novelty: rightForce
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        List<NoveltyGrammar> noveltyGrammar = new List<NoveltyGrammar>();

        NormalBird normalBird = new NormalBird();
        RollableObject rollableObject = new RollableObject();
        FlatLongSurface surface = new FlatLongSurface(12, 15);
        NormalPig normalPig = new NormalPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });
        Direction right = new Right();
        Direction left = new Left();
        Direction above = new Above();

        // NN - above rolling surface
        RollableObject rollableObject2 = new RollableObject();
        Surface surface2 = new Surface();

        verbGrammar.Add(new Hit(normalBird, rollableObject2, direction));
        verbGrammar.Add(new Roll(rollableObject2, surface2, right));
        verbGrammar.Add(new Fall(rollableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(rollableObject2, normalPig, left));

        // N - below flat rolling surface
        verbGrammar.Add(new Hit(normalBird, rollableObject, direction));
        verbGrammar.Add(new Roll(rollableObject, surface, right));
        verbGrammar.Add(new HitDestroy(rollableObject, normalPig, left));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(rollableObject));

        constraintGrammar.Add(new TouchUpperLeft(rollableObject2, surface2));
        constraintGrammar.Add(new InDirectionBelow(normalPig, surface2));

        noveltyGrammar.Add(new NotOnRightForce(new Roll(rollableObject2, surface2, right), new Fall(rollableObject2, normalPig))); // NN
        noveltyGrammar.Add(new OnlyOnRightForce(new Roll(rollableObject, surface, right), new HitDestroy(rollableObject, normalPig, left))); // N

        // setting the novel object flag to ture for the novel objects
        rollableObject.gameObject.isNovel = true;

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, noveltyGrammar, rollableObject2.gameObject, rollableObject.gameObject, 2, "RollingObjectNovel", 1);
        return scenario;
    }

    public Scenario RollingObjectNovel2() // N surface is flat long (located below) and NN surface is short inclined, novelty: rightForce
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        List<NoveltyGrammar> noveltyGrammar = new List<NoveltyGrammar>();

        NormalBird normalBird = new NormalBird();
        RollableObject rollableObject = new RollableObject();
        FlatLongSurface surface = new FlatLongSurface(8, 12);
        NormalPig normalPig = new NormalPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });
        Direction right = new Right();
        Direction left = new Left();
        Direction above = new Above();
        RollableObject rollableObject2 = new RollableObject();
        Surface surface2 = new Surface(6, 10);

        // N - above rolling surface
        verbGrammar.Add(new Hit(normalBird, rollableObject2, direction));
        verbGrammar.Add(new Roll(rollableObject2, surface2, right));
        verbGrammar.Add(new Fall(rollableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(rollableObject2, normalPig, left));

        // NN - below flat rolling surface
        verbGrammar.Add(new Hit(normalBird, rollableObject, direction));
        verbGrammar.Add(new Roll(rollableObject, surface, right));
        verbGrammar.Add(new HitDestroy(rollableObject, normalPig, left));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(rollableObject));

        constraintGrammar.Add(new TouchUpperLeft(rollableObject2, surface2));
        constraintGrammar.Add(new InDirectionBelow(normalPig, surface2));

        noveltyGrammar.Add(new NotOnDownForce(new Roll(rollableObject, surface, right), new HitDestroy(rollableObject, normalPig, left))); // NN
        noveltyGrammar.Add(new OnlyOnDownForce(new Roll(rollableObject2, surface2, right), new Fall(rollableObject2, normalPig))); // N

        // setting the novel object flag to ture for the novel objects
        rollableObject2.gameObject.isNovel = true;

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, noveltyGrammar, rollableObject.gameObject, rollableObject2.gameObject, -2, "RollingObjectNovel", 2);
        return scenario;
    }

    public Scenario FallingObjectNovel1() // N block is located above and NN object is located below novelty: rightForce
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        List<NoveltyGrammar> noveltyGrammar = new List<NoveltyGrammar>();

        NormalBird normalBird = new NormalBird();
        FallableObject fallableObject = new FallableObject();
        NormalPig normalPig = new NormalPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });

        // N
        FallableObject fallableObject2 = new FallableObject();
        verbGrammar.Add(new Hit(normalBird, fallableObject2, direction));
        verbGrammar.Add(new Fall(fallableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject2, normalPig, direction));

        // NN
        verbGrammar.Add(new Hit(normalBird, fallableObject, direction));
        verbGrammar.Add(new Fall(fallableObject, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject, normalPig, direction));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));

        constraintGrammar.Add(new InDirectionBelow(fallableObject, fallableObject2));

        noveltyGrammar.Add(new OnlyOnRightForce(new Fall(fallableObject2, normalPig), new HitDestroy(fallableObject2, normalPig, direction))); // N
        noveltyGrammar.Add(new NotOnRightForce(new Fall(fallableObject, normalPig), new HitDestroy(fallableObject, normalPig, direction))); // NN

        // setting the novel object flag to ture for the novel objects
        fallableObject2.gameObject.isNovel = true;

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, noveltyGrammar, fallableObject.gameObject, fallableObject2.gameObject, 0.1f, "FallingObjectNovel", 1);
        return scenario;
    }

    public Scenario FallingObjectNovel2() // NN block is located above and N object is located below novelty: rightForce
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        List<NoveltyGrammar> noveltyGrammar = new List<NoveltyGrammar>();

        NormalBird normalBird = new NormalBird();
        FallableObject fallableObject = new FallableObject();
        NormalPig normalPig = new NormalPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });

        // NN
        FallableObject fallableObject2 = new FallableObject();
        verbGrammar.Add(new Hit(normalBird, fallableObject2, direction));
        verbGrammar.Add(new Fall(fallableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject2, normalPig, direction));

        // N
        verbGrammar.Add(new Hit(normalBird, fallableObject, direction));
        verbGrammar.Add(new Fall(fallableObject, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject, normalPig, direction));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));

        constraintGrammar.Add(new InDirectionBelow(fallableObject, fallableObject2));

        noveltyGrammar.Add(new NotOnRightForce(new Fall(fallableObject2, normalPig), new HitDestroy(fallableObject2, normalPig, direction))); // NN
        noveltyGrammar.Add(new OnlyOnRightForce(new Fall(fallableObject, normalPig), new HitDestroy(fallableObject, normalPig, direction))); // N

        // setting the novel object flag to ture for the novel objects
        fallableObject.gameObject.isNovel = true;

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, noveltyGrammar, fallableObject2.gameObject, fallableObject.gameObject, 0.1f, "FallingObjectNovel", 2);
        return scenario;
    }

    public Scenario FallingObjectNove13() // N block is located above and NN object is located below novelty: rightForce
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        List<NoveltyGrammar> noveltyGrammar = new List<NoveltyGrammar>();

        NormalBird normalBird = new NormalBird();
        FallableObject fallableObject = new FallableObject();
        NormalPig normalPig = new NormalPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });

        // N
        FallableObject fallableObject2 = new FallableObject();
        verbGrammar.Add(new Hit(normalBird, fallableObject2, direction));
        verbGrammar.Add(new Fall(fallableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject2, normalPig, direction));

        // NN
        verbGrammar.Add(new Hit(normalBird, fallableObject, direction));
        verbGrammar.Add(new Fall(fallableObject, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject, normalPig, direction));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));

        constraintGrammar.Add(new InDirectionBelow(fallableObject, fallableObject2));

        noveltyGrammar.Add(new OnlyOnDownForce(new Fall(fallableObject2, normalPig), new HitDestroy(fallableObject2, normalPig, direction))); // N
        noveltyGrammar.Add(new NotOnDownForce(new Fall(fallableObject, normalPig), new HitDestroy(fallableObject, normalPig, direction))); // NN

        // setting the novel object flag to ture for the novel objects
        fallableObject2.gameObject.isNovel = true;

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, noveltyGrammar, fallableObject.gameObject, fallableObject2.gameObject, -1f, "FallingObjectNovel", 3);
        return scenario;
    }

    public Scenario FallingObjectNovel4() // NN block is located above and N object is located below novelty: rightForce
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        List<NoveltyGrammar> noveltyGrammar = new List<NoveltyGrammar>();

        NormalBird normalBird = new NormalBird();
        FallableObject fallableObject = new FallableObject();
        NormalPig normalPig = new NormalPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });

        // NN
        FallableObject fallableObject2 = new FallableObject();
        verbGrammar.Add(new Hit(normalBird, fallableObject2, direction));
        verbGrammar.Add(new Fall(fallableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject2, normalPig, direction));

        // N
        verbGrammar.Add(new Hit(normalBird, fallableObject, direction));
        verbGrammar.Add(new Fall(fallableObject, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject, normalPig, direction));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));

        constraintGrammar.Add(new InDirectionBelow(fallableObject, fallableObject2));

        noveltyGrammar.Add(new OnlyOnDownForce(new Fall(fallableObject2, normalPig), new HitDestroy(fallableObject2, normalPig, direction))); // NN
        noveltyGrammar.Add(new NotOnDownForce(new Fall(fallableObject, normalPig), new HitDestroy(fallableObject, normalPig, direction))); // N

        // setting the novel object flag to ture for the novel objects
        fallableObject.gameObject.isNovel = true;

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, noveltyGrammar, fallableObject2.gameObject, fallableObject.gameObject, -1f, "FallingObjectNovel", 4);
        return scenario;
    }

    public Scenario FallingObjectNovel5() // N block is located above and NN object is located below novelty: rightForce
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        List<NoveltyGrammar> noveltyGrammar = new List<NoveltyGrammar>();

        NormalBird normalBird = new NormalBird();
        FallableObject fallableObject = new FallableObject();
        NormalPig normalPig = new NormalPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });

        // N
        FallableObject fallableObject2 = new FallableObject();
        verbGrammar.Add(new Hit(normalBird, fallableObject2, direction));
        verbGrammar.Add(new Fall(fallableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject2, normalPig, direction));

        // NN
        verbGrammar.Add(new Hit(normalBird, fallableObject, direction));
        verbGrammar.Add(new Fall(fallableObject, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject, normalPig, direction));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));

        constraintGrammar.Add(new InDirectionBelow(fallableObject, fallableObject2));

        noveltyGrammar.Add(new OnlyOnUpForce(new Fall(fallableObject2, normalPig), new HitDestroy(fallableObject2, normalPig, direction))); // N
        noveltyGrammar.Add(new NotOnUpForce(new Fall(fallableObject, normalPig), new HitDestroy(fallableObject, normalPig, direction))); // NN

        // setting the novel object flag to ture for the novel objects
        fallableObject2.gameObject.isNovel = true;

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, noveltyGrammar, fallableObject.gameObject, fallableObject2.gameObject, 0.05f, "FallingObjectNovel", 5);
        return scenario;
    }

    public Scenario FallingObjectNovel6() // NN block is located above and N object is located below novelty: rightForce
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        List<NoveltyGrammar> noveltyGrammar = new List<NoveltyGrammar>();

        NormalBird normalBird = new NormalBird();
        FallableObject fallableObject = new FallableObject();
        NormalPig normalPig = new NormalPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });

        // NN
        FallableObject fallableObject2 = new FallableObject();
        verbGrammar.Add(new Hit(normalBird, fallableObject2, direction));
        verbGrammar.Add(new Fall(fallableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject2, normalPig, direction));

        // N
        verbGrammar.Add(new Hit(normalBird, fallableObject, direction));
        verbGrammar.Add(new Fall(fallableObject, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject, normalPig, direction));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));

        constraintGrammar.Add(new InDirectionBelow(fallableObject, fallableObject2));

        noveltyGrammar.Add(new OnlyOnUpForce(new Fall(fallableObject2, normalPig), new HitDestroy(fallableObject2, normalPig, direction))); // NN
        noveltyGrammar.Add(new NotOnUpForce(new Fall(fallableObject, normalPig), new HitDestroy(fallableObject, normalPig, direction))); // N

        // setting the novel object flag to ture for the novel objects
        fallableObject.gameObject.isNovel = true;

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, noveltyGrammar, fallableObject2.gameObject, fallableObject.gameObject, 0.05f, "FallingObjectNovel", 6);
        return scenario;
    }

    public Scenario FallingObjectNovel7() // N block is located above and NN object is located below novelty: rightForce
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        List<NoveltyGrammar> noveltyGrammar = new List<NoveltyGrammar>();

        NormalBird normalBird = new NormalBird();
        FallableObject fallableObject = new FallableObject();
        NormalPig normalPig = new NormalPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });

        // N
        FallableObject fallableObject2 = new FallableObject();
        verbGrammar.Add(new Hit(normalBird, fallableObject2, direction));
        verbGrammar.Add(new Fall(fallableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject2, normalPig, direction));

        // NN
        verbGrammar.Add(new Hit(normalBird, fallableObject, direction));
        verbGrammar.Add(new Fall(fallableObject, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject, normalPig, direction));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));

        constraintGrammar.Add(new InDirectionBelow(fallableObject, fallableObject2));

        noveltyGrammar.Add(new OnlyOnLeftForce(new Fall(fallableObject2, normalPig), new HitDestroy(fallableObject2, normalPig, direction))); // N
        noveltyGrammar.Add(new NotOnLeftForce(new Fall(fallableObject, normalPig), new HitDestroy(fallableObject, normalPig, direction))); // NN

        // setting the novel object flag to ture for the novel objects
        fallableObject2.gameObject.isNovel = true;

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, noveltyGrammar, fallableObject.gameObject, fallableObject2.gameObject, -0.025f, "FallingObjectNovel", 7);
        return scenario;
    }

    public Scenario FallingObjectNovel8() // NN block is located above and N object is located below novelty: rightForce
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        List<NoveltyGrammar> noveltyGrammar = new List<NoveltyGrammar>();

        NormalBird normalBird = new NormalBird();
        FallableObject fallableObject = new FallableObject();
        NormalPig normalPig = new NormalPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });

        // NN
        FallableObject fallableObject2 = new FallableObject();
        verbGrammar.Add(new Hit(normalBird, fallableObject2, direction));
        verbGrammar.Add(new Fall(fallableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject2, normalPig, direction));

        // N
        verbGrammar.Add(new Hit(normalBird, fallableObject, direction));
        verbGrammar.Add(new Fall(fallableObject, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject, normalPig, direction));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));

        constraintGrammar.Add(new InDirectionBelow(fallableObject, fallableObject2));

        noveltyGrammar.Add(new OnlyOnLeftForce(new Fall(fallableObject2, normalPig), new HitDestroy(fallableObject2, normalPig, direction))); // N
        noveltyGrammar.Add(new NotOnLeftForce(new Fall(fallableObject, normalPig), new HitDestroy(fallableObject, normalPig, direction))); // NN

        // setting the novel object flag to ture for the novel objects
        fallableObject.gameObject.isNovel = true;

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, noveltyGrammar, fallableObject2.gameObject, fallableObject.gameObject, -0.025f, "FallingObjectNovel", 8);
        return scenario;
    }

    public Scenario SlidingObjectNovel1() // NN block is located above and N object is located below novelty: rightForce
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        List<NoveltyGrammar> noveltyGrammar = new List<NoveltyGrammar>();

        NormalBird normalBird = new NormalBird();
        SlidableObject slidableObject = new SlidableObject();
        NormalPig normalPig = new NormalPig();
        FlatLongSurface flatLongSurface = new FlatLongSurface(10, 12);
        Direction left = new Left();
        Direction right = new Right();

        // NN
        SlidableObject slidableObject2 = new SlidableObject();
        SlopeLongSurface slopeLongSurface = new SlopeLongSurface(5, 8);

        verbGrammar.Add(new Hit(normalBird, slidableObject2, left));
        verbGrammar.Add(new Slide(slidableObject2, slopeLongSurface, right));
        verbGrammar.Add(new Fall(slidableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(slidableObject2, normalPig, left));

        // N
        verbGrammar.Add(new Hit(normalBird, slidableObject, left));
        verbGrammar.Add(new Slide(slidableObject, flatLongSurface, right));
        verbGrammar.Add(new HitDestroy(slidableObject, normalPig, left));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(slidableObject));

        constraintGrammar.Add(new TouchUpperLeft(slidableObject2, slopeLongSurface));
        constraintGrammar.Add(new InDirectionBelow(normalPig, slopeLongSurface));

        noveltyGrammar.Add(new OnlyOnRightForce(new Slide(slidableObject, normalPig, right), new HitDestroy(slidableObject, normalPig, left))); // N
        noveltyGrammar.Add(new NotOnRightForce(new Slide(slidableObject2, slopeLongSurface, right), new Fall(slidableObject2, normalPig))); // NN

        // setting the novel object flag to true for the novel objects
        slidableObject.gameObject.isNovel = true;

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, noveltyGrammar, slidableObject2.gameObject, slidableObject.gameObject, 0.1f, "SlidingObjectNovel", 1);
        return scenario;
    }

    public Scenario SlidingObjectNovel2() // NN block is located below and N object is located above the novelty: downForce
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        List<NoveltyGrammar> noveltyGrammar = new List<NoveltyGrammar>();

        NormalBird normalBird = new NormalBird();
        SlidableObject slidableObject = new SlidableObject();
        NormalPig normalPig = new NormalPig();
        FlatLongSurface flatLongSurface = new FlatLongSurface(5, 8);
        Direction left = new Left();
        Direction right = new Right();

        // N
        SlidableObject slidableObject2 = new SlidableObject();
        SlopeLongSurface slopeLongSurface = new SlopeLongSurface(5, 8);

        verbGrammar.Add(new Hit(normalBird, slidableObject2, left));
        verbGrammar.Add(new Slide(slidableObject2, slopeLongSurface, right));
        verbGrammar.Add(new Fall(slidableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(slidableObject2, normalPig, left));

        // NN
        verbGrammar.Add(new Hit(normalBird, slidableObject, left));
        verbGrammar.Add(new Slide(slidableObject, flatLongSurface, right));
        verbGrammar.Add(new HitDestroy(slidableObject, normalPig, left));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(slidableObject));

        constraintGrammar.Add(new TouchUpperLeft(slidableObject2, slopeLongSurface));
        constraintGrammar.Add(new InDirectionBelow(normalPig, slopeLongSurface));

        noveltyGrammar.Add(new OnlyOnDownForce(new Slide(slidableObject2, normalPig, right), new Fall(slidableObject2, normalPig))); // N
        noveltyGrammar.Add(new NotOnDownForce(new Slide(slidableObject, slopeLongSurface, right), new HitDestroy(slidableObject, normalPig, left))); // NN

        // setting the novel object flag to true for the novel objects
        slidableObject2.gameObject.isNovel = true;

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, noveltyGrammar, slidableObject.gameObject, slidableObject2.gameObject, -1f, "SlidingObjectNovel", 2);
        return scenario;
    }

    public Scenario RollingFallingObjectNovel1() // NN block is located above and N object is located below novelty: rightForce
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        List<NoveltyGrammar> noveltyGrammar = new List<NoveltyGrammar>();

        NormalBird normalBird = new NormalBird();
        RollableObject rollableObject = new RollableObject();
        FallableObject fallableObject = new FallableObject();
        // FlatLongSurface surface = new FlatLongSurface();
        //Surface surface = new Surface();
        SlopeLongSurface surface = new SlopeLongSurface(8, 15, 5, 15);

        NormalPig normalPig = new NormalPig();
        Direction direction1 = new AnyDirection(new Direction[] { new Left(), new Above() });
        Direction left = new Left();
        Direction right = new Right();
        RollableObject rollableObject2 = new RollableObject();
        FallableObject fallableObject2 = new FallableObject();
        Surface surface2 = new Surface(10);
        // SlopeLongSurface surface2 = new SlopeLongSurface(8, 15, 5, 15);

        // N
        verbGrammar.Add(new Hit(normalBird, rollableObject, direction1));
        verbGrammar.Add(new Roll(rollableObject, surface, right));
        verbGrammar.Add(new Hit(rollableObject, fallableObject, left));
        verbGrammar.Add(new Fall(fallableObject, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject, normalPig, direction1));

        // NN
        verbGrammar.Add(new Hit(normalBird, rollableObject2, direction1));
        verbGrammar.Add(new Roll(rollableObject2, surface2, right));
        verbGrammar.Add(new Hit(rollableObject2, fallableObject2, left));
        verbGrammar.Add(new Fall(fallableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject2, normalPig, direction1));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(rollableObject));
        constraintGrammar.Add(new CannotFallMoving(rollableObject2));

        constraintGrammar.Add(new InDirectionBelow(fallableObject2, fallableObject));
        constraintGrammar.Add(new InDirectionBelow(rollableObject2, rollableObject));
        // constraintGrammar.Add(new InDirectionBelow(normalPig, surface2));

        noveltyGrammar.Add(new NotOnRightForce(new Roll(rollableObject2, surface2, right), new Hit(rollableObject2, fallableObject2, left))); // NN
        noveltyGrammar.Add(new OnlyOnRightForce(new Roll(rollableObject, surface, right), new Hit(rollableObject, fallableObject, left))); // N

        // setting the novel object flag to true for the novel objects
        rollableObject.gameObject.isNovel = true;
        fallableObject.gameObject.isNovel = true;

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, noveltyGrammar, rollableObject2.gameObject, rollableObject.gameObject, 0.1f, "RollingFallingObjectNovel", 1);
        return scenario;
    }

    public Scenario RollingFallingObjectNovel2() 
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        List<NoveltyGrammar> noveltyGrammar = new List<NoveltyGrammar>();

        NormalBird normalBird = new NormalBird();
        RollableObject rollableObject = new RollableObject();
        FlatLongSurface surface = new FlatLongSurface(8, 12);
        NormalPig normalPig = new NormalPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });
        Direction right = new Right();
        Direction left = new Left();
        Direction above = new Above();
        RollableObject rollableObject2 = new RollableObject();
        Surface surface2 = new Surface(6, 10);
        FallableObject fallableObject = new FallableObject();
        FallableObject fallableObject2 = new FallableObject();

        // N - above rolling surface
        verbGrammar.Add(new Hit(normalBird, rollableObject2, direction));
        verbGrammar.Add(new Roll(rollableObject2, surface2, right));
        verbGrammar.Add(new Hit(rollableObject2, fallableObject2, left));
        verbGrammar.Add(new Fall(fallableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject2, normalPig, left));

        // NN - below flat rolling surface
        verbGrammar.Add(new Hit(normalBird, rollableObject, direction));
        verbGrammar.Add(new Roll(rollableObject, surface, right));
        verbGrammar.Add(new Hit(rollableObject, fallableObject, left));
        //verbGrammar.Add(new Fall(fallableObject, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject, normalPig, above));
        constraintGrammar.Add(new InDirectionBelow(normalPig, fallableObject));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(rollableObject));
        constraintGrammar.Add(new CannotFallMoving(rollableObject2));

        // constraintGrammar.Add(new TouchUpperLeft(rollableObject2, surface2));
        // constraintGrammar.Add(new TouchLowerLeft(surface2, fallableObject2));
        // constraintGrammar.Add(new InDirectionBelow(normalPig, surface2));
        constraintGrammar.Add(new InDirectionBelow(rollableObject, rollableObject2));

        noveltyGrammar.Add(new NotOnDownForce(new Roll(rollableObject, surface, right), new Hit(rollableObject, fallableObject, left))); // NN
        noveltyGrammar.Add(new OnlyOnDownForce(new Roll(rollableObject2, surface2, right), new Hit(rollableObject2, fallableObject2, left))); // N

        // setting the novel object flag to ture for the novel objects
        rollableObject2.gameObject.isNovel = true;
        fallableObject2.gameObject.isNovel = true;

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, noveltyGrammar, rollableObject.gameObject, rollableObject2.gameObject, -1f, "RollingFallingObjectNovel", 2);
        return scenario;
    }

    public Scenario RollingFallingObjectNovel3()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        List<NoveltyGrammar> noveltyGrammar = new List<NoveltyGrammar>();

        NormalBird normalBird = new NormalBird();
        RollableObject rollableObject = new RollableObject();
        FlatLongSurface surface = new FlatLongSurface(8, 12);
        NormalPig normalPig = new NormalPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });
        Direction right = new Right();
        Direction left = new Left();
        Direction above = new Above();
        RollableObject rollableObject2 = new RollableObject();
        Surface surface2 = new Surface(6, 10);
        FallableObject fallableObject = new FallableObject();
        FallableObject fallableObject2 = new FallableObject();

        // NN - above rolling surface
        verbGrammar.Add(new Hit(normalBird, rollableObject2, direction));
        verbGrammar.Add(new Roll(rollableObject2, surface2, right));
        verbGrammar.Add(new Hit(rollableObject2, fallableObject2, left));
        verbGrammar.Add(new Fall(fallableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject2, normalPig, left));

        // N - below flat rolling surface
        verbGrammar.Add(new Hit(normalBird, rollableObject, direction));
        verbGrammar.Add(new Roll(rollableObject, surface, right));
        verbGrammar.Add(new Hit(rollableObject, fallableObject, left));
        //verbGrammar.Add(new Fall(fallableObject, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject, normalPig, above));
        constraintGrammar.Add(new InDirectionBelow(normalPig, fallableObject));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(rollableObject));
        constraintGrammar.Add(new CannotFallMoving(rollableObject2));

        // constraintGrammar.Add(new TouchUpperLeft(rollableObject2, surface2));
        // constraintGrammar.Add(new TouchLowerLeft(surface2, fallableObject2));
        // constraintGrammar.Add(new InDirectionBelow(normalPig, surface2));
        constraintGrammar.Add(new InDirectionBelow(rollableObject, rollableObject2));

        noveltyGrammar.Add(new OnlyOnDownForce(new Roll(rollableObject, surface, right), new Hit(rollableObject, fallableObject, left))); // N
        noveltyGrammar.Add(new NotOnDownForce(new Roll(rollableObject2, surface2, right), new Hit(rollableObject2, fallableObject2, left))); // NN

        // setting the novel object flag to ture for the novel objects
        rollableObject.gameObject.isNovel = true;
        fallableObject.gameObject.isNovel = true;

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, noveltyGrammar, rollableObject2.gameObject, rollableObject.gameObject, -1f, "RollingFallingObjectNovel", 3);
        return scenario;
    }

    public Scenario RollingFallingObjectNovel4()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        List<NoveltyGrammar> noveltyGrammar = new List<NoveltyGrammar>();

        NormalBird normalBird = new NormalBird();
        RollableObject rollableObject = new RollableObject();
        FlatLongSurface surface = new FlatLongSurface(8, 12);
        NormalPig normalPig = new NormalPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });
        Direction right = new Right();
        Direction left = new Left();
        Direction above = new Above();
        RollableObject rollableObject2 = new RollableObject();
        Surface surface2 = new Surface(6, 10);
        FallableObject fallableObject = new FallableObject();
        FallableObject fallableObject2 = new FallableObject();

        // NN - above rolling surface
        verbGrammar.Add(new Hit(normalBird, rollableObject2, direction));
        verbGrammar.Add(new Roll(rollableObject2, surface2, right));
        verbGrammar.Add(new Hit(rollableObject2, fallableObject2, left));
        verbGrammar.Add(new Fall(fallableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject2, normalPig, left));

        // N - below flat rolling surface
        verbGrammar.Add(new Hit(normalBird, rollableObject, direction));
        verbGrammar.Add(new Roll(rollableObject, surface, right));
        verbGrammar.Add(new Hit(rollableObject, fallableObject, left));
        //verbGrammar.Add(new Fall(fallableObject, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject, normalPig, above));
        constraintGrammar.Add(new InDirectionBelow(normalPig, fallableObject));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(rollableObject));
        constraintGrammar.Add(new CannotFallMoving(rollableObject2));

        // constraintGrammar.Add(new TouchUpperLeft(rollableObject2, surface2));
        // constraintGrammar.Add(new TouchLowerLeft(surface2, fallableObject2));
        // constraintGrammar.Add(new InDirectionBelow(normalPig, surface2));
        constraintGrammar.Add(new InDirectionBelow(rollableObject, rollableObject2));

        noveltyGrammar.Add(new OnlyOnLeftForce(new Fall(fallableObject, normalPig), new HitDestroy(fallableObject, normalPig, above))); // N
        noveltyGrammar.Add(new NotOnLeftForce(new Fall(fallableObject2, normalPig), new HitDestroy(fallableObject2, normalPig, left))); // NN

        // setting the novel object flag to ture for the novel objects
        rollableObject.gameObject.isNovel = true;
        fallableObject.gameObject.isNovel = true;

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, noveltyGrammar, rollableObject2.gameObject, rollableObject.gameObject, -0.01f, "RollingFallingObjectNovel", 4);
        return scenario;
    }

    public Scenario RollingFallingObjectNovel5()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        List<NoveltyGrammar> noveltyGrammar = new List<NoveltyGrammar>();

        NormalBird normalBird = new NormalBird();
        RollableObject rollableObject = new RollableObject();
        FlatLongSurface surface = new FlatLongSurface(8, 12);
        NormalPig normalPig = new NormalPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });
        Direction right = new Right();
        Direction left = new Left();
        Direction above = new Above();
        RollableObject rollableObject2 = new RollableObject();
        Surface surface2 = new Surface(6, 10);
        FallableObject fallableObject = new FallableObject();
        FallableObject fallableObject2 = new FallableObject();

        // NN - above rolling surface
        verbGrammar.Add(new Hit(normalBird, rollableObject2, direction));
        verbGrammar.Add(new Roll(rollableObject2, surface2, right));
        verbGrammar.Add(new Hit(rollableObject2, fallableObject2, left));
        verbGrammar.Add(new Fall(fallableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject2, normalPig, left));

        // N - below flat rolling surface
        verbGrammar.Add(new Hit(normalBird, rollableObject, direction));
        verbGrammar.Add(new Roll(rollableObject, surface, right));
        verbGrammar.Add(new Hit(rollableObject, fallableObject, left));
        //verbGrammar.Add(new Fall(fallableObject, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject, normalPig, above));
        constraintGrammar.Add(new InDirectionBelow(normalPig, fallableObject));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(rollableObject));
        constraintGrammar.Add(new CannotFallMoving(rollableObject2));

        // constraintGrammar.Add(new TouchUpperLeft(rollableObject2, surface2));
        // constraintGrammar.Add(new TouchLowerLeft(surface2, fallableObject2));
        // constraintGrammar.Add(new InDirectionBelow(normalPig, surface2));
        constraintGrammar.Add(new InDirectionBelow(rollableObject, rollableObject2));

        noveltyGrammar.Add(new OnlyOnUpForce(new Fall(fallableObject, normalPig), new HitDestroy(fallableObject, normalPig, above))); // N
        noveltyGrammar.Add(new NotOnUpForce(new Fall(fallableObject2, normalPig), new HitDestroy(fallableObject2, normalPig, left))); // NN

        // setting the novel object flag to ture for the novel objects
        rollableObject.gameObject.isNovel = true;
        fallableObject.gameObject.isNovel = true;

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, noveltyGrammar, rollableObject2.gameObject, rollableObject.gameObject, -0.1f, "RollingFallingObjectNovel", 5);
        return scenario;
    }

    public Scenario RollingFallingObjectNovel6()
    {
        List<VerbGrammar> verbGrammar = new List<VerbGrammar>();
        List<ConstraintGrammar> constraintGrammar = new List<ConstraintGrammar>();
        List<NoveltyGrammar> noveltyGrammar = new List<NoveltyGrammar>();

        NormalBird normalBird = new NormalBird();
        RollableObject rollableObject = new RollableObject();
        FlatLongSurface surface = new FlatLongSurface(8, 12);
        NormalPig normalPig = new NormalPig();
        Direction direction = new AnyDirection(new Direction[] { new Left(), new Above() });
        Direction right = new Right();
        Direction left = new Left();
        Direction above = new Above();
        RollableObject rollableObject2 = new RollableObject();
        Surface surface2 = new Surface(6, 10);
        FallableObject fallableObject = new FallableObject();
        FallableObject fallableObject2 = new FallableObject();

        // N - above rolling surface
        verbGrammar.Add(new Hit(normalBird, rollableObject2, direction));
        verbGrammar.Add(new Roll(rollableObject2, surface2, right));
        verbGrammar.Add(new Hit(rollableObject2, fallableObject2, left));
        verbGrammar.Add(new Fall(fallableObject2, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject2, normalPig, left));

        // NN - below flat rolling surface
        verbGrammar.Add(new Hit(normalBird, rollableObject, direction));
        verbGrammar.Add(new Roll(rollableObject, surface, right));
        verbGrammar.Add(new Hit(rollableObject, fallableObject, left));
        //verbGrammar.Add(new Fall(fallableObject, normalPig));
        verbGrammar.Add(new HitDestroy(fallableObject, normalPig, above));
        constraintGrammar.Add(new InDirectionBelow(normalPig, fallableObject));

        constraintGrammar.Add(new CannotReach(normalBird, normalPig));
        constraintGrammar.Add(new CannotFallMoving(rollableObject));
        constraintGrammar.Add(new CannotFallMoving(rollableObject2));

        // constraintGrammar.Add(new TouchUpperLeft(rollableObject2, surface2));
        // constraintGrammar.Add(new TouchLowerLeft(surface2, fallableObject2));
        // constraintGrammar.Add(new InDirectionBelow(normalPig, surface2));
        constraintGrammar.Add(new InDirectionBelow(rollableObject, rollableObject2));

        noveltyGrammar.Add(new NotOnUpForce(new Fall(fallableObject, normalPig), new HitDestroy(fallableObject, normalPig, above))); // NN
        noveltyGrammar.Add(new OnlyOnUpForce(new Fall(fallableObject2, normalPig), new HitDestroy(fallableObject2, normalPig, left))); // N

        // setting the novel object flag to ture for the novel objects
        rollableObject2.gameObject.isNovel = true;
        fallableObject2.gameObject.isNovel = true;

        Scenario scenario = new Scenario(verbGrammar, constraintGrammar, noveltyGrammar, rollableObject.gameObject, rollableObject2.gameObject, -0.1f, "RollingFallingObjectNovel", 6);
        return scenario;
    }

}

