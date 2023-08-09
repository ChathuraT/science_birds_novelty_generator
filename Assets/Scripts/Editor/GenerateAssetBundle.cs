using System.IO;
using UnityEditor;
using UnityEngine;

public class GenerateAssetBundle
{

    // ## Instructions to create your own assetbundle ##
    // step 1: include the assets you need to compile into the "Assets/Scripts/Editor/AssetsToCompile/" folder
    //         assets should be in pairs of (prefab, material) and they should be named as novel_object_x.prefab and novel_material_x.physicsMaterial2D; where x is an integer
    // step 2: in the task bar select "AssetBundles/Compile" to compile those assets into aassetbundle
    //         the compiled assetbundles will be availabe in the "Assets/Scripts/Editor/GeneratedAssetBundle/" folder

    // the path to include the assets needed to be compiled into an asset bundle
    private static string pathToSaveConfiguredAssests = "Assets/Scripts/Editor/AssetsToCompile/";

    // the path to save the compiled assetbundle
    private static string pathToSaveAssetBundle = "Assets/Scripts/Editor/GeneratedAssetBundle/";

    [MenuItem("AssetBundles/Compile")]
    public static void build()
    {
        int noveltyLevel = 3;

        string[] configuredAssests = null;
        if (noveltyLevel == 1)
        {

            // reading the configured prefabs and materials and getting the individual paths
            FileInfo[] configuredPrefabs = new DirectoryInfo(pathToSaveConfiguredAssests).GetFiles("*.prefab");
            FileInfo[] configuredMaterials = new DirectoryInfo(pathToSaveConfiguredAssests).GetFiles("*.physicsMaterial2D");


            configuredAssests = new string[configuredPrefabs.Length + configuredMaterials.Length];

            for (int i = 0; i < configuredPrefabs.Length; i++)
            {
                configuredAssests[i] = pathToSaveConfiguredAssests + "/" + configuredPrefabs[i].Name;
            }
            for (int i = configuredPrefabs.Length; i < configuredPrefabs.Length + configuredMaterials.Length; i++)
            {
                configuredAssests[i] = pathToSaveConfiguredAssests + "/" + configuredMaterials[i - configuredPrefabs.Length].Name;
            }

        }
        else if (noveltyLevel == 3)
        {
            // for level 3 novelties include a text file named level_3_config.txt with the necessary novelty index (novelty_type: x)
            configuredAssests = new string[] { pathToSaveConfiguredAssests + "/" + "level_3_config.txt" };

        }
        
        // creating the asset bundle using the configured prefabs and building the asset bundle
        AssetBundleBuild[] buildMap = new AssetBundleBuild[1];
        buildMap[0].assetBundleName = "AssetBundle";
        buildMap[0].assetNames = configuredAssests;

        // build the asset bundle for all three OSs
        buildMap[0].assetBundleName = "AssetBundle";
        BuildPipeline.BuildAssetBundles(pathToSaveAssetBundle + "linux", buildMap, BuildAssetBundleOptions.None, BuildTarget.StandaloneLinux64);
        buildMap[0].assetBundleName = "AssetBundle";
        BuildPipeline.BuildAssetBundles(pathToSaveAssetBundle + "OSX", buildMap, BuildAssetBundleOptions.None, BuildTarget.StandaloneOSX);
        buildMap[0].assetBundleName = "AssetBundle";
        BuildPipeline.BuildAssetBundles(pathToSaveAssetBundle + "windows", buildMap, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);

        Debug.Log("successfully compiled the assetbundle");
    }
}
