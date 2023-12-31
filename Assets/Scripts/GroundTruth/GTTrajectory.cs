/// <summary>
/// This class represents a science birds object in the ground truth symbolic game state  
/// </summary>

/**
* @author Peng Zhang
*
* @date - 04/08/2020 
*/
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GTTrajectory : GTObjectBase{
    
    //The position of all trajectory points
    public Vector2[] location {get; private set;}

    public GTTrajectory(string id, string objectClass, Vector2[] location):base(id,objectClass){
        this.location = location;
    }

    //remove all trajectory points with coordinate [0,0]
    public void removeZeroTrajPts() {
        List<Vector2> locationList = this.location.ToList();
        foreach(Vector2 coord in locationList) { 
            if(coord.x <= 0.0 && coord.y <= 0.0)
            {
                locationList.Remove(coord);
            }
        }
        this.location = locationList.ToArray();
    }

    //implement the funtion that generates the json string for the object
    public override string ToJsonString(bool dev){
        string json = "{\"type\": \"Feature\",\"geometry\":{";        
        //geometric component
        //trajectory points
        json = json +"\"type\":\"MultiPoint\",\"coordinates\":[";
        foreach(Vector2 point in this.location){
    
            json+="[";
            json += Mathf.Round(point.x);
            json+= ",";
            json += Mathf.Round(point.y);
            json+="],";
        }
        //remove last ,
        json = json.Remove(json.Length-1,1);
        json+="]";

        json += "},";

        //end geometric component

        
        //porperty compoenet
        json += "\"properties\":{";
        //id
        json= json + "\"id\":\""+this.id+"\",";

        json = json +"\"label\":" +"\"Trajectory\",";

        //color map
        json += "\"colormap\":";
        json += "[]";

        json += "}";//end property component
        
        json += "}";//end object
        return json;
    }
}