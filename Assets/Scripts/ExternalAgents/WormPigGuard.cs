using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WormPigGuard : Worm
{
    private ABPig[] pigsList;

    protected override void Awake()
    {
        base.Awake();
        pigsList = FindObjectsOfType<ABPig>();
        foreach (ABPig pig in pigsList)
        {
            if (pig != this)
            {
                pig.setCurrentLife(float.PositiveInfinity);
            }
        }
    }

    public override void Die(bool withEffect = true)
    {
        foreach (ABPig pig in pigsList)
        {
            pig.setCurrentLife(pig._life);
        }
        base.Die(withEffect);
    }

}
