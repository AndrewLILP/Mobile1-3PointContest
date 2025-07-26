using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;

public class ContestFirebaseManager : MonoBehaviour
{
    [Header("Firebase Configuration")]
    [SerializeField] private string firebaseURL = "https://cloneprojectmobile-default-rtdb.asia-southeast1.firebasedatabase.app/";
    [SerializeField] private string gameDataPath = "BasketballContest";
    [SerializeField] private bool enableDebugLogs = true;
    
    [Header("Game Integration")]
    [SerializeField] private ThreePointContest contestManager;
    [SerializeField] private ContestUIManager uiManager;
    
    [Header("Player Settings")]
    [SerializeField] private string defaultPlayerName = "Anonymous";
    
    // Data Models
    [System.Serializable]
    public class ContestResult
    {
        public string playerName;
        public int totalScore;
        public int maxPossibleScore;
        public float accuracy;
        public List<int> positionScores;
        public string timestamp;
        public string deviceId;
        public int starRating;
        
        public ContestResult(string playerName, int totalScore, List<int> positionScores, int starRating)
        {
            this.playerName = playerName;
            this.totalScore = totalScore;
            this.maxPossibleScore = 30; // 5 positions × 6 points max (5 regular + 1 money ball)
            this.accuracy = (float)totalScore / maxPossibleScore * 100f;
            this.positionScores = new List<int>(positionScores);
            this.timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            this.deviceId = SystemInfo.deviceUniqueIdentifier;
            this.starRating = starRating;
        }
    }
    
    [System.Serializable]
    public class LeaderboardEntry
    {
        public string playerName;
        public int totalScore;
        public float accuracy;
        public string timestamp;
        public int starRating;
        
        public LeaderboardEntry(ContestResult result)
        {
            this.playerName = result.playerName;
            this.totalScore = result.totalScore;
            this.accuracy = result.accuracy;
            this.timestamp = result.timestamp;
            this.starRating = result.starRating;
        }
    }
    
    [System.Serializable]
    public class LeaderboardData
    {
        public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
    }
    
    // Events
    public System.Action<bool> OnDataSaved; // success
    public System.Action<LeaderboardData> OnLeaderboardLoaded;
    public System.Action<string> OnError;
    
    private string currentPlayerName;
    private bool isConnected = true;
    
    private void Awake()
    {
        // Get player name from PlayerPrefs or use default
        currentPlayerName = PlayerPrefs.GetString("PlayerName", defaultPlayerName);
        
        // Find components if not assigned
        if (contestManager == null)
            contestManager = FindFirstObjectByType<ThreePointContest>();
        if (uiManager == null)
            uiManager = FindFirstObjectByType<ContestUIManager>();
    }
    
    private void Start()
    {
        ConnectToContestEvents();
        TestConnection();
    }
    
    private void ConnectToContestEvents()
    {
        if (contestManager != null)
        {
            contestManager.OnContestComplete += HandleContestComplete;
        }
    }
    
    private void HandleContestComplete(int finalScore, List<int> positionScores)
    {
        int starRating = CalculateStarRating(finalScore);
        SaveContestResult(currentPlayerName, finalScore, positionScores, starRating);
    }
    
    private int CalculateStarRating(int score)
    {
        if (score >= 25) return 3; // Excellent
        if (score >= 18) return 2; // Good
        if (score >= 10) return 1; // Fair
        return 0; // Try again
    }
    
    public void SaveContestResult(string playerName, int totalScore, List<int> positionScores, int starRating)
    {
        if (!isConnected)
        {
            OnError?.Invoke("No internet connection");
            return;
        }
        
        ContestResult result = new ContestResult(playerName, totalScore, positionScores, starRating);
        string json = JsonUtility.ToJson(result);
        
        LogDebug($"Saving contest result for {playerName}: {totalScore} points");
        StartCoroutine(SaveResultToDatabase(result, json));
    }
    
    private IEnumerator SaveResultToDatabase(ContestResult result, string json)
    {
        // Save to contest results
        string resultsPath = $"{gameDataPath}/Results";
        yield return StartCoroutine(PostToDatabase(resultsPath, json, "Contest result saved successfully"));
        
        // Update leaderboard
        yield return StartCoroutine(UpdateLeaderboard(result));
    }
    
    private IEnumerator UpdateLeaderboard(ContestResult result)
    {
        // First, get current leaderboard
        string leaderboardPath = $"{gameDataPath}/Leaderboard";
        
        bool loadSuccess = false;
        LeaderboardData currentLeaderboard = new LeaderboardData();
        
        yield return StartCoroutine(GetFromDatabase(leaderboardPath, (success, data) => {
            loadSuccess = success;
            if (success && !string.IsNullOrEmpty(data) && data != "null")
            {
                try
                {
                    currentLeaderboard = JsonUtility.FromJson<LeaderboardData>(data);
                    if (currentLeaderboard.entries == null)
                        currentLeaderboard.entries = new List<LeaderboardEntry>();
                }
                catch (System.Exception e)
                {
                    LogDebug($"Error parsing leaderboard data: {e.Message}");
                    currentLeaderboard = new LeaderboardData();
                }
            }
        }));
        
        // Add new entry
        LeaderboardEntry newEntry = new LeaderboardEntry(result);
        currentLeaderboard.entries.Add(newEntry);
        
        // Sort by score (descending) and keep top 100
        currentLeaderboard.entries.Sort((a, b) => b.totalScore.CompareTo(a.totalScore));
        if (currentLeaderboard.entries.Count > 100)
        {
            currentLeaderboard.entries.RemoveRange(100, currentLeaderboard.entries.Count - 100);
        }
        
        // Save updated leaderboard
        string leaderboardJson = JsonUtility.ToJson(currentLeaderboard);
        yield return StartCoroutine(PutToDatabase(leaderboardPath, leaderboardJson, "Leaderboard updated successfully"));
    }
    
    public void LoadLeaderboard()
    {
        if (!isConnected)
        {
            OnError?.Invoke("No internet connection");
            return;
        }
        
        string leaderboardPath = $"{gameDataPath}/Leaderboard";
        StartCoroutine(GetFromDatabase(leaderboardPath, (success, data) => {
            if (success && !string.IsNullOrEmpty(data) && data != "null")
            {
                try
                {
                    LeaderboardData leaderboard = JsonUtility.FromJson<LeaderboardData>(data);
                    OnLeaderboardLoaded?.Invoke(leaderboard);
                    LogDebug($"Leaderboard loaded with {leaderboard.entries.Count} entries");
                }
                catch (System.Exception e)
                {
                    OnError?.Invoke($"Error parsing leaderboard: {e.Message}");
                }
            }
            else
            {
                // Return empty leaderboard
                OnLeaderboardLoaded?.Invoke(new LeaderboardData());
                LogDebug("Empty leaderboard loaded");
            }
        }));
    }
    
    private IEnumerator PostToDatabase(string path, string json, string successMessage)
    {
        string url = $"{firebaseURL}{path}.json";
        
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                LogDebug(successMessage);
                OnDataSaved?.Invoke(true);
            }
            else
            {
                string error = $"POST Error: {request.error}";
                LogDebug(error);
                OnDataSaved?.Invoke(false);
                OnError?.Invoke(error);
            }
        }
    }
    
    private IEnumerator PutToDatabase(string path, string json, string successMessage)
    {
        string url = $"{firebaseURL}{path}.json";
        
        using (UnityWebRequest request = UnityWebRequest.Put(url, json))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                LogDebug(successMessage);
            }
            else
            {
                string error = $"PUT Error: {request.error}";
                LogDebug(error);
                OnError?.Invoke(error);
            }
        }
    }
    
    private IEnumerator GetFromDatabase(string path, System.Action<bool, string> onComplete)
    {
        string url = $"{firebaseURL}{path}.json";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                LogDebug("Data retrieved successfully");
                onComplete?.Invoke(true, request.downloadHandler.text);
            }
            else
            {
                string error = $"GET Error: {request.error}";
                LogDebug(error);
                onComplete?.Invoke(false, null);
                OnError?.Invoke(error);
            }
        }
    }
    
    private void TestConnection()
    {
        StartCoroutine(TestConnectionCoroutine());
    }
    
    private IEnumerator TestConnectionCoroutine()
    {
        string testUrl = $"{firebaseURL}.json";
        
        using (UnityWebRequest request = UnityWebRequest.Get(testUrl))
        {
            yield return request.SendWebRequest();
            
            isConnected = (request.result == UnityWebRequest.Result.Success);
            
            if (isConnected)
            {
                LogDebug("Firebase connection successful");
            }
            else
            {
                LogDebug($"Firebase connection failed: {request.error}");
                OnError?.Invoke("Failed to connect to Firebase");
            }
        }
    }
    
    // Public methods for UI integration
    public void SetPlayerName(string playerName)
    {
        currentPlayerName = playerName;
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.Save();
    }
    
    public string GetPlayerName()
    {
        return currentPlayerName;
    }
    
    public void ManualSave()
    {
        if (contestManager != null && contestManager.IsContestActive)
        {
            Debug.LogWarning("Cannot manually save while contest is active");
            return;
        }
        
        // For testing - save a dummy result
        List<int> dummyScores = new List<int> {3, 4, 2, 5, 3};
        SaveContestResult(currentPlayerName, 17, dummyScores, 2);
    }
    
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[Firebase] {message}");
        }
    }
    
    // Getters
    public bool IsConnected => isConnected;
    public string CurrentPlayerName => currentPlayerName;
    
    private void OnDestroy()
    {
        if (contestManager != null)
        {
            contestManager.OnContestComplete -= HandleContestComplete;
        }
    }
}