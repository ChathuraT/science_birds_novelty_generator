using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockFriendlyPig : ABPig
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public override void OnCollisionEnter2D(Collision2D collision)
    {
        float collisionMagnitude = collision.relativeVelocity.magnitude;

        if (collision.gameObject.tag == "Bird")
        {
            // spawn the points for colliding with the bird - give points if it only exeeds 10 and not killed
            int points = (int)System.Math.Round(collisionMagnitude * 10) * 10;
            if ((points > 10) & ((base.getCurrentLife() - collisionMagnitude) > 0f))
                ScoreHud.Instance.SpawnScorePoint(points, transform.position);

            // immediately die
            Die();
        }

        // stop calling the base OnCollisionEnter2D to avoid dying when collided with other objects
        // base.OnCollisionEnter2D(collision);

    }

}
