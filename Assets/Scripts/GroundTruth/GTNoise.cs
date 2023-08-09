/// <summary>
/// Ad-hoc uniformly distributed noise
/// This will be changed to systematic computer vision noise in the future  
/// </summary>
using UnityEngine;
public static class GTNoise{

    public static float posMin = 0, posMax = 1, posSigma = 0.5f;

    public static float colorMin, colorMax, colorSigma;

    //TODO to be completed
    public static Vector2 ApplyPositionNoise(Vector2 coord){
        float distance = Gaussian();
        float angle = UniformAngle();
        int[] deltaCoord = DeltaCoord(angle,distance);
        Vector2 noisyCoord = new Vector2();
        noisyCoord.x = coord.x + deltaCoord[0];
        noisyCoord.y = coord.y + deltaCoord[1];
        
        return noisyCoord;
    }
    public static Vector2[] ApplyPositionNoise(Vector2[] coords){
        float distance = Gaussian();
        float angle = UniformAngle();
        int[] deltaCoord = DeltaCoord(angle,distance);
        Vector2[] noisyCoords = new Vector2[coords.Length];
        for(int i = 0; i < noisyCoords.Length; i++){
            noisyCoords[i].x = coords[i].x + deltaCoord[0];
            noisyCoords[i].y = coords[i].y + deltaCoord[1];
        }
                
        return noisyCoords;
    }

    public static float Gaussian()
    {
        float var1 = Random.Range(0.0f, 1.0f);
        float var2 = Random.Range(0.0f, 1.0f);
        
        float rand = Mathf.Sqrt(-2.0f * Mathf.Log(var1)) * Mathf.Cos((2.0f * Mathf.PI) * var2);

        return Mathf.Clamp((posMin+posMax)/2 + posSigma * rand,posMin, posMax);
    }    

    //random angle in radians
    public static float UniformAngle(){
        return UnityEngine.Random.Range(-Mathf.PI,Mathf.PI);
    }

    public static int[] DeltaCoord(float angle, float distance){
        int[] deltaCoord = {0,0};
        
        deltaCoord[0] =  (int)Mathf.Round(distance * Mathf.Cos(angle));
        deltaCoord[1] =  (int)Mathf.Round(distance * Mathf.Sin(angle));
        return deltaCoord;

    }
    //TODO to be completed
    public static void ApplyColourNoise(int noiseLevel){

    }


}