using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ABMovingPlatform : MonoBehaviour
{
    public float amplitude = 1.0f;
    public float frequency = 1.0f;
    private Vector2 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }
    
    // Update is called once per frame
    void Update()
    {
        transform.position = startPosition + (Vector2.up * amplitude * (float)Math.Sin(Time.timeSinceLevelLoad * frequency)); 
    }
}
