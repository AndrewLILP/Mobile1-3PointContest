using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LeaderboardUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Transform leaderboardContent;
    [SerializeField] private GameObject leaderboardEntryPrefab;
    [SerializeField] private Button loadLeaderboardButton;
    [SerializeField] private Button closeLeaderboardButton;
    [SerializeField] private Button saveScoreButton;
    
    [Header("Player Name Input")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Button saveNameButton;
    [SerializeField] private TextMeshProUGUI currentPlayerNameText;
    
    [Header("Status Display")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color errorColor = Color.red;
    [SerializeField] private Color normalColor = Color.white;
    
    [Header("Leaderboard Entry UI")]
    [SerializeField] private TextMeshProUGUI[] rankTexts = new TextMeshProUGUI[10];
    [SerializeField] private TextMeshProUGUI[] nameTexts = new TextMeshProUGUI[10];
    [SerializeField] private TextMeshProUGUI[] scoreTexts = new TextMeshProUGUI[10];
    [SerializeField] private TextMeshProUGUI[] accuracyTexts = new TextMeshProUGUI[10];
    [SerializeField] private Image[] starContainers = new Image[10];
    
    [Header("Firebase Integration")]
    [SerializeField] private ContestFirebaseManager firebaseManager;
    
    private List<GameObject> leaderboardEntries = new List<GameObject>();
    private bool isLoading = false;
    
    private void Awake()
    {
        // Find firebase manager if not assigned
        if (firebaseManager == null)
        {
            firebaseManager = FindFirstObjectByType<ContestFirebaseManager>();
        }
    }
    
    private void Start()
    {
        SetupUI();
        ConnectToFirebaseEvents();
        UpdatePlayerNameDisplay();
        
        // Hide leaderboard initially
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
    }
    
    private void SetupUI()
    {
        // Setup button listeners
        if (loadLeaderboardButton != null)
            loadLeaderboardButton.onClick.AddListener(LoadLeaderboard);
        
        if (closeLeaderboardButton != null)
            closeLeaderboardButton.onClick.AddListener(CloseLeaderboard);
        
        if (saveScoreButton != null)
            saveScoreButton.onClick.AddListener(SaveCurrentScore);
        
        if (saveNameButton != null)
            saveNameButton.onClick.AddListener(SavePlayerName);
        
        // Setup input field
        if (playerNameInput != null && firebaseManager != null)
        {
            playerNameInput.text = firebaseManager.GetPlayerName();
        }
        
        SetLoadingState(false);
    }
    
    private void ConnectToFirebaseEvents()
    {
        if (firebaseManager == null) return;
        
        firebaseManager.OnLeaderboardLoaded += HandleLeaderboardLoaded;
        firebaseManager.OnDataSaved += HandleDataSaved;
        firebaseManager.OnError += HandleFirebaseError;
    }
    
    private void HandleLeaderboardLoaded(ContestFirebaseManager.LeaderboardData leaderboard)
    {
        SetLoadingState(false);
        DisplayLeaderboard(leaderboard);
        ShowStatus("Leaderboard loaded successfully!", successColor);
    }
    
    private void HandleDataSaved(bool success)
    {
        if (success)
        {
            ShowStatus("Score saved successfully!", successColor);
            // Auto-refresh leaderboard after saving
            LoadLeaderboard();
        }
        else
        {
            ShowStatus("Failed to save score", errorColor);
        }
    }
    
    private void HandleFirebaseError(string error)
    {
        SetLoadingState(false);
        ShowStatus($"Error: {error}", errorColor);
    }
    
    public void LoadLeaderboard()
    {
        if (firebaseManager == null)
        {
            ShowStatus("Firebase manager not found!", errorColor);
            return;
        }
        
        if (isLoading) return;
        
        SetLoadingState(true);
        ShowStatus("Loading leaderboard...", normalColor);
        firebaseManager.LoadLeaderboard();
        
        // Show leaderboard panel
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(true);
    }
    
    public void CloseLeaderboard()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
    }
    
    public void SaveCurrentScore()
    {
        if (firebaseManager == null)
        {
            ShowStatus("Firebase manager not found!", errorColor);
            return;
        }
        
        ShowStatus("Saving score...", normalColor);
        firebaseManager.ManualSave();
    }
    
    public void SavePlayerName()
    {
        if (firebaseManager == null || playerNameInput == null) return;
        
        string newName = playerNameInput.text.Trim();
        if (string.IsNullOrEmpty(newName))
        {
            ShowStatus("Please enter a valid name", errorColor);
            return;
        }
        
        firebaseManager.SetPlayerName(newName);
        UpdatePlayerNameDisplay();
        ShowStatus("Player name saved!", successColor);
    }
    
    private void UpdatePlayerNameDisplay()
    {
        if (currentPlayerNameText != null && firebaseManager != null)
        {
            currentPlayerNameText.text = $"Player: {firebaseManager.GetPlayerName()}";
        }
    }
    
    private void DisplayLeaderboard(ContestFirebaseManager.LeaderboardData leaderboard)
    {
        // Clear existing entries
        ClearLeaderboard();
        
        if (leaderboard.entries == null || leaderboard.entries.Count == 0)
        {
            ShowStatus("No leaderboard data available", normalColor);
            return;
        }
        
        // Display entries using static UI elements or create dynamic ones
        if (rankTexts.Length > 0 && rankTexts[0] != null)
        {
            DisplayInStaticUI(leaderboard.entries);
        }
        else if (leaderboardEntryPrefab != null && leaderboardContent != null)
        {
            DisplayInDynamicUI(leaderboard.entries);
        }
        else
        {
            Debug.LogWarning("No leaderboard UI setup found!");
        }
    }
    
    private void DisplayInStaticUI(List<ContestFirebaseManager.LeaderboardEntry> entries)
    {
        int maxEntries = Mathf.Min(entries.Count, rankTexts.Length);
        
        for (int i = 0; i < maxEntries; i++)
        {
            var entry = entries[i];
            
            if (rankTexts[i] != null)
                rankTexts[i].text = $"#{i + 1}";
            
            if (nameTexts[i] != null)
                nameTexts[i].text = entry.playerName;
            
            if (scoreTexts[i] != null)
                scoreTexts[i].text = entry.totalScore.ToString();
            
            if (accuracyTexts[i] != null)
                accuracyTexts[i].text = $"{entry.accuracy:F1}%";
            
            if (starContainers[i] != null)
                SetupStarDisplay(starContainers[i], entry.starRating);
        }
        
        // Hide unused entries
        for (int i = maxEntries; i < rankTexts.Length; i++)
        {
            if (rankTexts[i] != null && rankTexts[i].transform.parent != null)
                rankTexts[i].transform.parent.gameObject.SetActive(false);
        }
    }
    
    private void DisplayInDynamicUI(List<ContestFirebaseManager.LeaderboardEntry> entries)
    {
        int maxEntries = Mathf.Min(entries.Count, 50); // Limit to 50 entries for performance
        
        for (int i = 0; i < maxEntries; i++)
        {
            var entry = entries[i];
            GameObject entryObj = Instantiate(leaderboardEntryPrefab, leaderboardContent);
            leaderboardEntries.Add(entryObj);
            
            // Setup entry UI components
            SetupLeaderboardEntry(entryObj, i + 1, entry);
        }
    }
    
    private void SetupLeaderboardEntry(GameObject entryObj, int rank, ContestFirebaseManager.LeaderboardEntry entry)
    {
        // Find UI components in the entry prefab
        TextMeshProUGUI rankText = entryObj.transform.Find("RankText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI nameText = entryObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI scoreText = entryObj.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI accuracyText = entryObj.transform.Find("AccuracyText")?.GetComponent<TextMeshProUGUI>();
        Transform starContainer = entryObj.transform.Find("StarContainer");
        
        // Set text values
        if (rankText != null) rankText.text = $"#{rank}";
        if (nameText != null) nameText.text = entry.playerName;
        if (scoreText != null) scoreText.text = entry.totalScore.ToString();
        if (accuracyText != null) accuracyText.text = $"{entry.accuracy:F1}%";
        
        // Setup stars
        if (starContainer != null)
            SetupStarDisplay(starContainer.GetComponent<Image>(), entry.starRating);
        
        // Color coding for top ranks
        Color entryColor = Color.white;
        if (rank == 1) entryColor = Color.yellow;
        else if (rank == 2) entryColor = Color.cyan;
        else if (rank == 3) entryColor = new Color(1f, 0.6f, 0.2f); // Bronze
        
        if (nameText != null) nameText.color = entryColor;
    }
    
    private void SetupStarDisplay(Image starContainer, int starRating)
    {
        if (starContainer == null) return;
        
        // Simple star display using UI Image color
        switch (starRating)
        {
            case 0:
                starContainer.color = Color.gray;
                break;
            case 1:
                starContainer.color = new Color(0.8f, 0.6f, 0.2f); // Bronze
                break;
            case 2:
                starContainer.color = new Color(0.7f, 0.7f, 0.8f); // Silver
                break;
            case 3:
                starContainer.color = Color.yellow; // Gold
                break;
        }
        
        // You can expand this to show individual star images
    }
    
    private void ClearLeaderboard()
    {
        // Clear dynamic entries
        foreach (GameObject entry in leaderboardEntries)
        {
            if (entry != null)
                Destroy(entry);
        }
        leaderboardEntries.Clear();
        
        // Clear static entries
        for (int i = 0; i < rankTexts.Length; i++)
        {
            if (rankTexts[i] != null)
            {
                rankTexts[i].text = "-";
                if (nameTexts[i] != null) nameTexts[i].text = "-";
                if (scoreTexts[i] != null) scoreTexts[i].text = "-";
                if (accuracyTexts[i] != null) accuracyTexts[i].text = "-";
            }
        }
    }
    
    private void SetLoadingState(bool loading)
    {
        isLoading = loading;
        
        if (loadingIndicator != null)
            loadingIndicator.SetActive(loading);
        
        if (loadLeaderboardButton != null)
            loadLeaderboardButton.interactable = !loading;
    }
    
    private void ShowStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
        
        Debug.Log($"[Leaderboard] {message}");
        
        // Auto-clear status after 3 seconds
        if (color != normalColor)
        {
            Invoke(nameof(ClearStatus), 3f);
        }
    }
    
    private void ClearStatus()
    {
        if (statusText != null)
        {
            statusText.text = "";
            statusText.color = normalColor;
        }
    }
    
    // Public methods for external use
    public void ShowLeaderboard()
    {
        LoadLeaderboard();
    }
    
    public void RefreshLeaderboard()
    {
        if (!isLoading)
        {
            LoadLeaderboard();
        }
    }
    
    private void OnDestroy()
    {
        if (firebaseManager != null)
        {
            firebaseManager.OnLeaderboardLoaded -= HandleLeaderboardLoaded;
            firebaseManager.OnDataSaved -= HandleDataSaved;
            firebaseManager.OnError -= HandleFirebaseError;
        }
    }
}