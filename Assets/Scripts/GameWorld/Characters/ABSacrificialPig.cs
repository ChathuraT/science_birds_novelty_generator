using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ABSacrificialPig : ABPig
{
    private ABPig[] pigsList;

    public override void Die(bool withEffect = true)
    {
        pigsList = FindObjectsOfType<ABPig>();

        foreach (ABPig pig in pigsList)
        {
            pig.setCurrentLife(float.PositiveInfinity);
        }
        base.Die(withEffect);
    }

}
