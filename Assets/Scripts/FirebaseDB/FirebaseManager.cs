using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class FirebaseManager : MonoBehaviour
{
    private const string FirebaseURL = "https://cloneprojectmobile-default-rtdb.asia-southeast1.firebasedatabase.app/";

    [System.Serializable]
    public class PlayerData
    {
        public string username;
        public int score;

        public PlayerData(string username, int score)
        {
            this.username = username;
            this.score = score;
        }
    }
    
    [System.Serializable]
    public class ContestData
    {
        public string username;
        public int totalScore;
        public int[] positionScores;
        public string timestamp;
        
        public ContestData(string username, int totalScore, int[] positionScores)
        {
            this.username = username;
            this.totalScore = totalScore;
            this.positionScores = positionScores;
            this.timestamp = System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
        }
    }

    // POST data to the database
    public void SavePlayerData(string username, int score)
    {
        PlayerData playerData = new PlayerData(username, score);
        string json = JsonUtility.ToJson(playerData);

        // Use UnityWebRequest to send the data to Firebase
        StartCoroutine(PostToDatabase("CloneGame/PlayerData", json));
    }
    
    // Save contest data specifically
    public void SaveContestData(string username, int totalScore, int[] positionScores)
    {
        ContestData contestData = new ContestData(username, totalScore, positionScores);
        string json = JsonUtility.ToJson(contestData);
        
        StartCoroutine(PostToDatabase("CloneGame/ContestData", json));
    }
    
    // Get leaderboard data
    public void GetLeaderboard(System.Action<string> onComplete)
    {
        StartCoroutine(GetFromDatabase("CloneGame/ContestData", onComplete));
    }

    private IEnumerator PostToDatabase(string path, string json)
    {
        string url = FirebaseURL + path + ".json";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        // ✅ FIX: Correct way to create UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Data successfully sent to Firebase: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Error sending data to Firebase: " + request.error);
        }
        
        request.Dispose();
    }
    
    private IEnumerator GetFromDatabase(string path, System.Action<string> onComplete)
    {
        string url = FirebaseURL + path + ".json";
        
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Data retrieved from Firebase: " + request.downloadHandler.text);
            onComplete?.Invoke(request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Error retrieving data from Firebase: " + request.error);
            onComplete?.Invoke(null);
        }
        
        request.Dispose();
    }
    
    // Alternative method using UnityWebRequest.Post (simpler for basic POST)
    public void SavePlayerDataSimple(string username, int score)
    {
        PlayerData playerData = new PlayerData(username, score);
        string json = JsonUtility.ToJson(playerData);
        
        StartCoroutine(PostToDatabaseSimple("CloneGame/PlayerData", json));
    }
    
    private IEnumerator PostToDatabaseSimple(string path, string json)
    {
        string url = FirebaseURL + path + ".json";
        
        // Alternative method using UnityWebRequest.Post
        using (UnityWebRequest request = UnityWebRequest.Post(url, json, "application/json"))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Data successfully sent to Firebase: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error sending data to Firebase: " + request.error);
            }
        }
    }
}