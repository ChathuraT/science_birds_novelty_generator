using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Rolling : MonoBehaviour
{

    private int numberOfObjects;
    private ABLevel gameLevel;
    private Utilities utilities;


    void Start()
    {

        // avoid destroying this object even after moving to another scene
        DontDestroyOnLoad(this.gameObject);
         
        this.utilities = new Utilities();
        this.gameLevel = utilities.InstantiateGameLevel();
        CreateSkeleton();
        utilities.SimulateLevel(this.gameLevel);
        StartCoroutine(ModifySkeleton());
        Debug.Log("666");

    }

    public void CreateSkeleton()
    {

        // assign the level width, camera, and sligshot
        gameLevel.width = 2;
        gameLevel.camera = new CameraData(25, 35, 0, -1);
        gameLevel.slingshot = new SlingData(-12.0f, -2.5f);

        // a rolling level has a rollable object, a surface and a pig
        gameLevel.birds.Add(new BirdData("BirdRed"));
        gameLevel.pigs.Add(new OBjData("BasicSmall", 0, -3.53f, -0.5f));
        gameLevel.platforms.Add(new PlatData("Platform", 0, -3.5459f, -1.0676f, 1, 1));
        gameLevel.blocks.Add(new BlockData("Circle", 0, -2.62f, -3.24f, "wood"));

        Debug.Log("skeleton created");
        //utilities.printLevel(gameLevel);

    }


    public IEnumerator ModifySkeleton()
    {

        // wait until the gameworld is finished loading
        Debug.Log(SceneManager.GetActiveScene().buildIndex);
        while (SceneManager.GetActiveScene().buildIndex != 4)
        {
            Debug.Log("gameworld is not loaded");
            yield return new WaitForSeconds(0.01f);
        }

        
        if (SceneManager.GetActiveScene().buildIndex == 4)
        {
            Debug.Log("gameworld loading completed");
            
            for (float i = 0; i < 0.02; i+=0.01f)
            {
                Debug.Log("1");
                Debug.Log(ABGameWorld._blocks[0]);
                Debug.Log(ABGameWorld._platforms[0]);
                Debug.Log("2");
                ABGameWorld._blocks[0].transform.Translate(0.01f, 0f, 0f);
                Debug.Log(ABGameWorld._blocks[0].getRigidBody().velocity);
                yield return new WaitForFixedUpdate();
                //yield return new WaitForSeconds(0.01f);

            }

            // add a new game object
            // utilities.addNewObject();

            // check the reacheability
            Vector3 initialPosition;
            Vector3 initialVelocity;
            Vector3 gravity;

            initialPosition = new Vector3(-12.8f, -2.4f, 1.0f);
            initialVelocity = new Vector2(6.6f, 7.5f);
            gravity = new Vector3(0.0f, -4.7f, 0.0f);

            this.utilities.CheckTrajectory();
        }

    }



}
