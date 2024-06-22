// using UnityEngine;

// public class SaveManager : MonoBehaviour
// {
//     public Population population; // Assume this holds your NEAT population

//     // Called when the game quits
//     void OnApplicationQuit()
//     {
//         SavePopulation();
//     }

//     // Function to handle the saving of your NEAT population
//     void SavePopulation()
//     {
//         if (Application.isEditor) // Check if running in the Unity editor
//         {
//             Debug.Log("Saving population data...");
//             // Path where the population data will be saved
//             string path = System.IO.Path.Combine(Application.persistentDataPath, "population.json");
//             SavePopulation(population, path);
//         }
//     }

//     // Serialize and save the population to a file
//     public void SavePopulation(Population population, string path)
//     {
//         string jsonData = JsonUtility.ToJson(population.population);
//         System.IO.File.WriteAllText(path, jsonData);
//         Debug.Log("Population saved to " + path);
//     }

//     // Deserialize and load the population from a file
//     public Population LoadPopulation(string path)
//     {
//         if (System.IO.File.Exists(path))
//         {
//             string jsonData = System.IO.File.ReadAllText(path);
//             return JsonUtility.FromJson<Population>(jsonData);
//         }
//         else
//         {
//             Debug.LogError("Failed to load population data. File does not exist: " + path);
//             return null; // Return null or a new Population if no data is found.
//         }
//     }
// }
