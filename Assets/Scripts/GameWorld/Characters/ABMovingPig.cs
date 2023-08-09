using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ABBigPig : ABPig
{

    float max_move_range = 3;
    float moved_distance = 0;
    float moving_step = 0.01f;
    float moving_direction = 1;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        move();
    }

    private void move()
    {
        _rigidBody.transform.position = new Vector3(_rigidBody.transform.position.x + moving_direction * moving_step, _rigidBody.transform.position.y, _rigidBody.transform.position.z);
        moved_distance += moving_step;

        if (moved_distance > max_move_range)
        {
            moving_direction *= -1;
            moved_distance = 0;
        }


    }
}
