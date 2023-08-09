### List of Changes (release alpha v0.3.3)
Add batch groudntruth request

### List of Changes (release alpha v0.2.0)
28th Feb 2020

1. Added capability of reading novlety level 1-3 with 1200 sample levels
    -  Level 1: new objects with 5 novelty type samples provided (100 levels for each)
    -  Level 2: change of parameters of objects with 5 novelty type samples provided (100 levels for each)
    -  Level 3: change of representation with 2 novelty type samples provided (100 levels for each)
    - The original non-onvelty levels are also provided for comparasion
    - Note: the source code of the novelty generator is not included in the release 
2. Fixed cshoot return shoot successfully indicator before the level is stable problem. 
    - now the return value for cshoot/pshoot will be returned once the not objects in level is moving
    -  now the return value for cfastshoot/pfastshoot will be returned after the shoot procedure is finished, i.e., the drag and tap operations are executed 
3. Fixed science birds error message display bug 

### List of Changes (release alpha v0.1) 

10th Feb 2020 
1. Add function to extract groundtruth information of all game objects. 
2. Fine tune the parameters of the science birds game to make it perform more similar to the angry birds chrome version.
3. Allow the game to load levels that are added during its run time.
4. Update groundtruth.cs to handle exception from empty state when objects are not loaded
