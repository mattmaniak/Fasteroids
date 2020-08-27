using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fasteroids.DataLayer
{
    public class SpaceshipRepository : IRepository
    {
        public SpaceshipRepository()
        {
            // Leave empty for the sake of C#...
        }

        public SpaceShipConfig LoadData()
        {
            try
            {
                string serializedData = System.IO.File.ReadAllText(@"Assets/spaceships.json");
                var deserializedSpaceShip = JsonListHandler.Deserialize<SpaceShipConfig>(serializedData);
                
                // Don't need to check array size. Exception will be catched.
                if (deserializedSpaceShip[0].Name == null)
                {
                    return null;
                }
                return deserializedSpaceShip[0];
            }
            catch(Exception e)
            {
                Debug.Log(e.Message);
                return null;
            }
        }
    }


    // Intended not to use any third-party plugins at all.
    internal class JsonListHandler
    {
        // Deserialize JSON content to defined type.
        public static T[] Deserialize<T>(string serializedData)
        {
            try
            {
                return JsonUtility.FromJson<JsonContent<T>>(serializedData).SpaceShipData;
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                return null;
            }
        }
    }

    // Wrapper for handling array in the SpaceShip config file.
    [System.Serializable]
    internal class JsonContent<T>
    {
        public T[] SpaceShipData;
    }
}

// Example SpaceShip configuration representation.
[System.Serializable]
public class SpaceShipConfig
{
    public string Name;
}
