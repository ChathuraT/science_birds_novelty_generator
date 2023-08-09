# A Simple Approach to Load Novel Game Objects at Runtime in ScienceBirds

This document describes a simple approach to load novel game objects at runtime in the ScienceBirds game, which may simplify the deployment of novel game objects (i.e., it is now possible for the external teams to use a newly developed novel game object without requiring us to first rebuild the whole game and then send it to those teams.).

## The Basic Idea

Unity supports [managed plug-ins](https://docs.unity3d.com/Manual/UsingDLL.html) (i.e., programming with C# classes compiled in DLL files), which makes it possible to load novel game objects dynamically at runtime with the following process:

1. Assuming a novel game object consists of two major components: 
	- a **C# class** specifying the behaviour, and
	- an **assetbundle** which includes all the assets (e.g., animation, prefab, material, sprite etc.) the novel game object uses.
2. The **C# class** in Step 1 can be provided by compiling the source code into a DLL file, then loaded in the game or referred in an assetbundle (details are presented below).
3. The **assetbundle** in Step 1 can be built in the Unity editor, and the assets in the bundle can refer to the C# classes from the DLL files (e.g., referring to a class from the DLL in a `Script component` of a prefab).
4. Finally, to use a novel game object in the game, we can load the DLL and assetbundle in the game at runtime.


## Three Unity Projects

Three Unity projects are involved in this approach:

1. **Source code project**: the project for developing all the C# code. It can optionally include the assets for debugging purpose. The C# code in this project will be compiled into one or more DLL files.

2. **Assetbundle project**: the project for building assetbundles of novel game objects. If an asset (e.g., a prefab) refers to C# classes in the **source code project**, we need to load the DLL files built from the **source code project** and choose the corresponding classes from the DLL files (similar to how one specifies C# classes in .cs files for a prefab in Unity editor).
3. **Basic game project**: the project for building the basic ScienceBirds game. This project is the same as the **assetbundle project** except that no (secret) novel game objects -- including its C# classes and assets -- are used in this project. The ScienceBirds game built using this project is supposed to be used by external teams.

Noting that all the C# classes for the basic game have been compiled into a single DLL file which is used in the **assetbundle project** and the **basic game project** because most novel game object will depend on these classes (see `/compile.sh` for details).


## Initialisation in Awake()

It seems when building the ScienceBirds game using DLL files rather than C# source code, many initialisation activities ocurr in the `Awake` function, so some initialisation code has been ported from the `Start` function to the `Awake` function of the same class. The majority of these changes are in the `LoadLevelSchema.cs` and `ABGameWorld.cs`, see the `Awake` functions in the two files for details. 

## Recommended Process to Develop and Deploy Novel Game Objects

A suggested process for developing and deploying a novel game object consists of 6 steps:

1. Create the C# classes for the novel game object in the **source code project**
2. Compile the C# classes created in Step 1 into a DLL file (see `/compile.sh` for an example).
3. Open the **assetbundle project** in Unity editor, and load the DLL file created in Step 2 by simply dragging it to, for example, the`Script folder` in Unity editor.
4. Create assets for the novel game object in the **assetbundle project**, and specify the `Script component` (if applicable) using classes from the DLL file loaded in Step 3.
5. [Build an assetbundle](https://docs.unity3d.com/Manual/AssetBundlesIntro.html) for the novel game object.
6. Specify the novel game object in a level XML file, and copy the DLL and assetbundle to the game built using the **basic game project** as below:
 - Copy the DLL file created in Step 2 to `BUILTNAME/BUILTNAME_Data/Managed/` where `BUILTNAME` denotes the folder name specified when building the **basic game project** 
 - Copy the assetbundle created in Step 5 to the path specified by `ABLevel.assetBundleFilePath`

In short, the key for this approach to work is making sure that we load the DLL files from which certain C# classes are referred by the assets (in assetbundles) before the instantiation of the assets.

