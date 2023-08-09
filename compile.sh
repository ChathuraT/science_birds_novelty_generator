#!/bin/bash

# paths of source code
project_path=`pwd`
script_path=$project_path/Assets/Scripts
gameworld_path=$script_path/GameWorld
character_path=$gameworld_path/Characters
particle_path=$script_path/ParticleSystem
camera_path=$gameworld_path/Camera
utils_path=$script_path/Utils
level_path=$script_path/Levels

# paths of libraries
unity_home=$HOME/Unity/Hub/Editor/2019.3.4f1
mono_home=$unity_home/Editor/Data/MonoBleedingEdge
lib_path1=$unity_home/Editor/Data/Managed
lib_path2=$unity_home/Editor/Data/Managed/UnityEngine
lib_path3=$project_path/Library/ScriptAssemblies

# unity libraries
unity_libs=$lib_path2/UnityEngine.dll,$lib_path2/UnityEngine.CoreModule.dll,$lib_path2/UnityEngine.Physics2DModule.dll,$lib_path2/UnityEngine.AudioModule.dll,$lib_path2/UnityEngine.AssetBundleModule.dll,$lib_path2/UnityEngine.AnimationModule.dll,$lib_path2/UnityEngine.InputLegacyModule.dll,$lib_path2/UnityEngine.JSONSerializeModule.dll,$lib_path2/UnityEngine.ImageConversionModule.dll,$lib_path3/UnityEngine.UI.dll

# path of compiled DLL
dll_path=$project_path
echo "DLL PATH: $dll_path"
echo ""

# path of Mono compiler
compiler=$mono_home/bin-linux64/mcs

# to avoid an error about magic number when using mcs compiler
export TERM=xterm   


# build AIBirds.dll
rm -f $dll_path/AIBirds.dll
rm -f $dll_path/AIBirds.dll.mdb

$compiler \
    -r:$unity_libs \
    -target:library \
    -debug \
    -out:$dll_path/AIBirds.dll \
    $script_path/EvaluationHandler.cs \
    $gameworld_path/ABBlock.cs \
    $gameworld_path/ABEgg.cs \
    $gameworld_path/ABEggTNT.cs \
    $gameworld_path/ABGameObject.cs \
    $gameworld_path/ABGameWorld.cs \
    $gameworld_path/ABSlingshot.cs \
    $gameworld_path/ABTNT.cs \
    $gameworld_path/Camera/ABGameplayCamera.cs \
    $gameworld_path/Camera/ABParallaxLayer.cs \
    $character_path/ABCharacter.cs \
    $character_path/ABPig.cs \
    $character_path/Birds/ABBBirdBlue.cs \
    $character_path/Birds/ABBird.cs \
    $character_path/Birds/ABBirdBlack.cs \
    $character_path/Birds/ABBirdDown.cs \
    $character_path/Birds/ABBirdWhite.cs \
    $character_path/Birds/ABBirdYellow.cs \
    $script_path/HUD/HUD.cs \
    $script_path/HUD/ScoreHud.cs \
    $script_path/TestHarness/LoadLevelSchema.cs \
    $script_path/TestHarness/TrialInfo.cs \
    $script_path/TestHarness/GameLevelSetInfo.cs \
    $script_path/TestHarness/TestSet.cs \
    $script_path/TestHarness/TrainingSet.cs \
    $script_path/TestHarness/ConfigGenerator.cs \
    $script_path/GroundTruth/SymbolicGameState.cs \
    $script_path/GroundTruth/ColorMap.cs \
    $script_path/GroundTruth/GTGround.cs \
    $script_path/GroundTruth/GTObject.cs \
    $script_path/GroundTruth/GTObjectBase.cs \
    $script_path/GroundTruth/GTNoise.cs \
    $script_path/GroundTruth/GTTrajectory.cs \
    $level_path/ABLevel.cs \
    $level_path/ABLevelUpdate.cs \
    $level_path/LevelList.cs \
    $level_path/LevelLoader.cs \
    $particle_path/ABParticle.cs \
    $particle_path/ABParticleSystem.cs \
    $particle_path/PhysicalBody.cs \
    $script_path/Plugins/SimpleJSON.cs \
    $script_path/RepresentationNovelty.cs \
    $utils_path/ABArrayUtils.cs \
    $utils_path/ABAudioController.cs \
    $utils_path/ABConstants.cs \
    $utils_path/ABMath.cs \
    $utils_path/ABObjectPool.cs \
    $utils_path/ABSceneManager.cs \
    $utils_path/ABSingleton.cs


# build UglyBird.dll
rm -f $dll_path/UglyBird.dll
rm -f $dll_path/UglyBird.dll.mdb

$compiler \
    -r:$unity_libs,$dll_path/AIBirds.dll \
    -target:library \
    -debug \
    -out:$dll_path/UglyBird.dll \
    $character_path/Birds/ABBirdUgly.cs


# build MovingPig.dll
rm -f $dll_path/MovingPig.dll
rm -f $dll_path/MovingPig.dll.mdb

$compiler \
    -r:$unity_libs,$dll_path/AIBirds.dll \
    -target:library \
    -debug \
    -out:$dll_path/MovingPig.dll \
    $character_path/ABMovingPig.cs

echo ""
echo "The following DLL files have been compiled:"
ls -lh $dll_path/*.dll


# update DLL files in the assetbundle_project, the basic_game_project and the game built using the basic game project
# assuming the three unity projects: source_code_project, assetbundle_project and basic_game_project are in the same directory
assetbundle_script=../assetbundle_project/Assets/Scripts
rm -f $assetbundle_script/AIBirds.dll
rm -f $assetbundle_script/AIBirds.dll.mdb
rm -f $assetbundle_script/UglyBird.dll
rm -f $assetbundle_script/UglyBird.dll.mdb
rm -f $assetbundle_script/MovingPig.dll
rm -f $assetbundle_script/MovingPig.dll.mdb
cp $dll_path/*.dll $assetbundle_script/

basicgame_script=../basic_game_project/Assets/Scripts
rm -f $basicgame_script/AIBirds.dll
rm -f $basicgame_script/AIBirds.dll.mdb
cp $dll_path/AIBirds.dll $basicgame_script/

# cp $dll_path/*.dll ../build/sbird_Data/Managed/

