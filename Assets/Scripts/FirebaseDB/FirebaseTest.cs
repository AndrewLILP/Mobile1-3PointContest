using UnityEngine;

public class FirebaseTest : MonoBehaviour
{
    public FirebaseManager firebaseManager;

    public void Start()
    {
        // ✅ FIX: Use correct variable name 'firebaseManager' instead of 'firebase'
        if (firebaseManager != null)
        {
            firebaseManager.SavePlayerData("Player1", 3000);
            firebaseManager.SavePlayerData("Player2", 2000);
            
            // Test contest data saving
            int[] testScores = {5, 3, 4, 2, 5}; // Example position scores
            firebaseManager.SaveContestData("TestPlayer", 19, testScores);
        }
        else
        {
            Debug.LogError("FirebaseManager reference is null! Please assign it in the inspector.");
        }
    }
    
    // Method to test retrieving leaderboard
    public void TestGetLeaderboard()
    {
        if (firebaseManager != null)
        {
            firebaseManager.GetLeaderboard(OnLeaderboardReceived);
        }
    }
    
    private void OnLeaderboardReceived(string jsonData)
    {
        if (!string.IsNullOrEmpty(jsonData))
        {
            Debug.Log("Leaderboard data: " + jsonData);
            // You can parse the JSON data here to display in UI
        }
        else
        {
            Debug.Log("No leaderboard data received or error occurred");
        }
    }
}