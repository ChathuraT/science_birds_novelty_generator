using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class LevelGenerator
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [MenuItem("Level Generator/Start")]

    static void StartLevelGenerator() {


        EditorApplication.OpenScene("Assets/Scenes/LevelGenerator/LevelGenerator.unity");
        EditorApplication.isPlaying = true;


        /*
        EditorApplication.OpenScene("Assets/Scenes/LevelGenerator/Rolling.unity");
        EditorApplication.isPlaying = true;

        Rolling rollingInstance = new Rolling(10);
        rollingInstance.printNumberOfObjects();
        rollingInstance.changeNumberOfObjects(12);
        rollingInstance.printNumberOfObjects();
        */

    }
}
