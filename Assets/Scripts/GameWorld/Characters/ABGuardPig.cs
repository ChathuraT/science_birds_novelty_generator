using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ABGuardPig : ABPig
{
    private ABPig[] pigsList;

    // Start is called before the first frame update
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
