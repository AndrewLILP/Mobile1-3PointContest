using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class IntegratedContestUIManager : MonoBehaviour
{
    [Header("Contest Reference")]
    [SerializeField] private ThreePointContest contestManager;
    
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private GameObject playerSetupPanel;
    [SerializeField] private GameObject pausePanel;
    
    [Header("Main Menu UI")]
    [SerializeField] private Button startContestButton;
    [SerializeField] private Button viewLeaderboardButton;
    [SerializeField] private Button playerSettingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI currentPlayerNameDisplay;
    
    [Header("Gameplay UI")]
    [SerializeField] private TextMeshProUGUI totalScoreText;
    [SerializeField] private TextMeshProUGUI positionScoreText;
    [SerializeField] private TextMeshProUGUI currentPositionText;
    [SerializeField] private TextMeshProUGUI ballsRemainingText;
    [SerializeField] private TextMeshProUGUI positionNameText;
    [SerializeField] private Button pauseButton;
    
    [Header("Results UI")]
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI[] finalPositionScores = new TextMeshProUGUI[5];
    [SerializeField] private TextMeshProUGUI starRatingText;
    [SerializeField] private Image[] starImages = new Image[3];
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button viewLeaderboardFromResultsButton;
    [SerializeField] private Button mainMenuButton;
    
    [Header("Player Setup UI")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Button savePlayerNameButton;
    [SerializeField] private Button closePlayerSetupButton;
    [SerializeField] private TextMeshProUGUI playerSetupStatus;
    
    [Header("Leaderboard UI")]
    [SerializeField] private Transform leaderboardContent;
    [SerializeField] private GameObject leaderboardEntryPrefab;
    [SerializeField] private Button closeLeaderboardButton;
    [SerializeField] private Button refreshLeaderboardButton;
    [SerializeField] private TextMeshProUGUI leaderboardStatus;
    [SerializeField] private GameObject leaderboardLoadingIndicator;
    [SerializeField] private ScrollRect leaderboardScrollRect;
    
    [Header("Firebase Integration")]
    [SerializeField] private ContestFirebaseManager firebaseManager;
    [SerializeField] private bool autoSaveResults = true;
    [SerializeField] private bool autoShowLeaderboardAfterSave = false;
    
    [Header("Connection Status")]
    [SerializeField] private Image connectionStatusIcon;
    [SerializeField] private Color connectedColor = Color.green;
    [SerializeField] private Color disconnectedColor = Color.red;
    [SerializeField] private TextMeshProUGUI connectionStatusText;
    
    // State tracking
    private bool isPaused = false;
    private List<int> currentPositionScores = new List<int>();
    private List<GameObject> leaderboardEntries = new List<GameObject>();
    private bool isLoadingLeaderboard = false;
    private int lastFinalScore = 0;
    
    private void Awake()
    {
        // Find components if not assigned
        if (contestManager == null)
            contestManager = FindFirstObjectByType<ThreePointContest>();
        if (firebaseManager == null)
            firebaseManager = FindFirstObjectByType<ContestFirebaseManager>();
        
        // Initialize position scores
        currentPositionScores = new List<int>(new int[5]);
    }
    
    private void Start()
    {
        SetupButtonListeners();
        SubscribeToEvents();
        ShowMainMenu();
        UpdatePlayerNameDisplay();
        UpdateConnectionStatus();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    #region Setup and Event Connections
    
    private void SetupButtonListeners()
    {
        // Main Menu
        if (startContestButton != null)
            startContestButton.onClick.AddListener(StartContest);
        if (viewLeaderboardButton != null)
            viewLeaderboardButton.onClick.AddListener(() => LoadAndShowLeaderboard());
        if (playerSettingsButton != null)
            playerSettingsButton.onClick.AddListener(ShowPlayerSetup);
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
        
        // Gameplay
        if (pauseButton != null)
            pauseButton.onClick.AddListener(PauseGame);
        
        // Results
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(StartContest);
        if (viewLeaderboardFromResultsButton != null)
            viewLeaderboardFromResultsButton.onClick.AddListener(() => LoadAndShowLeaderboard());
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ShowMainMenu);
        
        // Player Setup
        if (savePlayerNameButton != null)
            savePlayerNameButton.onClick.AddListener(SavePlayerName);
        if (closePlayerSetupButton != null)
            closePlayerSetupButton.onClick.AddListener(ClosePlayerSetup);
        
        // Leaderboard
        if (closeLeaderboardButton != null)
            closeLeaderboardButton.onClick.AddListener(CloseLeaderboard);
        if (refreshLeaderboardButton != null)
            refreshLeaderboardButton.onClick.AddListener(() => LoadAndShowLeaderboard());
    }
    
    private void SubscribeToEvents()
    {
        // Contest events
        if (contestManager != null)
        {
            contestManager.OnScoreUpdated += UpdateScoreDisplay;
            contestManager.OnPositionChanged += UpdatePositionDisplay;
            contestManager.OnBallShot += UpdateBallsRemaining;
            contestManager.OnContestComplete += HandleContestComplete;
        }
        
        // Firebase events
        if (firebaseManager != null)
        {
            firebaseManager.OnDataSaved += HandleDataSaved;
            firebaseManager.OnLeaderboardLoaded += HandleLeaderboardLoaded;
            firebaseManager.OnError += HandleFirebaseError;
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        if (contestManager != null)
        {
            contestManager.OnScoreUpdated -= UpdateScoreDisplay;
            contestManager.OnPositionChanged -= UpdatePositionDisplay;
            contestManager.OnBallShot -= UpdateBallsRemaining;
            contestManager.OnContestComplete -= HandleContestComplete;
        }
        
        if (firebaseManager != null)
        {
            firebaseManager.OnDataSaved -= HandleDataSaved;
            firebaseManager.OnLeaderboardLoaded -= HandleLeaderboardLoaded;
            firebaseManager.OnError -= HandleFirebaseError;
        }
    }
    
    #endregion
    
    #region Panel Management
    
    public void ShowMainMenu()
    {
        SetPanelActive(mainMenuPanel, true);
        SetPanelActive(gameplayPanel, false);
        SetPanelActive(resultsPanel, false);
        SetPanelActive(leaderboardPanel, false);
        SetPanelActive(playerSetupPanel, false);
        SetPanelActive(pausePanel, false);
        
        isPaused = false;
        Time.timeScale = 1f;
        
        UpdatePlayerNameDisplay();
        UpdateConnectionStatus();
    }
    
    public void ShowGameplay()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(gameplayPanel, true);
        SetPanelActive(resultsPanel, false);
        SetPanelActive(leaderboardPanel, false);
        SetPanelActive(playerSetupPanel, false);
        SetPanelActive(pausePanel, false);
        
        // Reset position scores display
        currentPositionScores = new List<int>(new int[5]);
        UpdateAllPositionScores();
    }
    
    public void ShowResults(int finalScore, List<int> positionScores)
    {
        lastFinalScore = finalScore;
        
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(gameplayPanel, false);
        SetPanelActive(resultsPanel, true);
        SetPanelActive(leaderboardPanel, false);
        SetPanelActive(playerSetupPanel, false);
        SetPanelActive(pausePanel, false);
        
        DisplayFinalResults(finalScore, positionScores);
        
        // Auto-save if enabled
        if (autoSaveResults && firebaseManager != null && firebaseManager.IsConnected)
        {
            StartCoroutine(DelayedAutoSave(finalScore, positionScores));
        }
    }
    
    public void ShowPlayerSetup()
    {
        SetPanelActive(playerSetupPanel, true);
        
        // Populate current player name
        if (playerNameInput != null && firebaseManager != null)
        {
            playerNameInput.text = firebaseManager.GetPlayerName();
        }
    }
    
    public void ClosePlayerSetup()
    {
        SetPanelActive(playerSetupPanel, false);
    }
    
    public void LoadAndShowLeaderboard()
    {
        SetPanelActive(leaderboardPanel, true);
        LoadLeaderboard();
    }
    
    public void CloseLeaderboard()
    {
        SetPanelActive(leaderboardPanel, false);
    }
    
    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
    
    #endregion
    
    #region Contest Control and Display Updates
    
    public void StartContest()
    {
        if (contestManager != null)
        {
            contestManager.StartContest();
            ShowGameplay();
        }
    }
    
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        SetPanelActive(pausePanel, true);
    }
    
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SetPanelActive(pausePanel, false);
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }
    
    private void UpdateScoreDisplay(int totalScore, int positionScore)
    {
        if (totalScoreText != null)
            totalScoreText.text = $"Total: {totalScore}";
        
        if (positionScoreText != null)
            positionScoreText.text = $"Position: {positionScore}";
        
        // Update current position score in tracking
        if (contestManager != null)
        {
            int currentPos = contestManager.CurrentPosition - 1;
            if (currentPos >= 0 && currentPos < currentPositionScores.Count)
            {
                currentPositionScores[currentPos] = positionScore;
                UpdateAllPositionScores();
            }
        }
    }
    
    private void UpdatePositionDisplay(int positionIndex, int ballsRemaining, int positionScore)
    {
        if (currentPositionText != null)
            currentPositionText.text = $"Position {positionIndex + 1}/5";
        
        if (positionNameText != null && contestManager != null)
            positionNameText.text = contestManager.CurrentPositionName;
    }
    
    private void UpdateBallsRemaining(int ballsRemaining)
    {
        if (ballsRemainingText != null)
            ballsRemainingText.text = $"Balls: {ballsRemaining + 1}/5";
    }
    
    private void UpdateAllPositionScores()
    {
        // This can be expanded to show individual position scores during gameplay
    }
    
    #endregion
    
    #region Results Display
    
    private void DisplayFinalResults(int finalScore, List<int> positionScores)
    {
        if (finalScoreText != null)
            finalScoreText.text = $"Final Score: {finalScore}/30";
        
        // Individual position scores
        for (int i = 0; i < finalPositionScores.Length; i++)
        {
            if (finalPositionScores[i] == null) continue;
            
            if (i < positionScores.Count)
            {
                finalPositionScores[i].text = $"Pos {i + 1}: {positionScores[i]}";
            }
            else
            {
                finalPositionScores[i].text = $"Pos {i + 1}: 0";
            }
        }
        
        // Star rating
        int stars = CalculateStarRating(finalScore);
        DisplayStarRating(stars);
    }
    
    private int CalculateStarRating(int score)
    {
        if (score >= 25) return 3;
        if (score >= 18) return 2;
        if (score >= 10) return 1;
        return 0;
    }
    
    private void DisplayStarRating(int stars)
    {
        if (starRatingText != null)
        {
            string[] ratingTexts = { "Try Again", "Fair", "Good", "Excellent!" };
            starRatingText.text = ratingTexts[Mathf.Clamp(stars, 0, 3)];
        }
        
        // Update star images
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;
            starImages[i].color = i < stars ? Color.yellow : Color.gray;
        }
    }
    
    #endregion
    
    #region Firebase Integration
    
    private void HandleContestComplete(int finalScore, List<int> positionScores)
    {
        ShowResults(finalScore, positionScores);
    }
    
    private IEnumerator DelayedAutoSave(int finalScore, List<int> positionScores)
    {
        yield return new WaitForSeconds(1f); // Let the results display
        
        if (firebaseManager != null)
        {
            int starRating = CalculateStarRating(finalScore);
            firebaseManager.SaveContestResult(
                firebaseManager.GetPlayerName(), 
                finalScore, 
                positionScores, 
                starRating
            );
        }
    }
    
    private void HandleDataSaved(bool success)
    {
        if (success)
        {
            ShowStatus("Score saved successfully!", connectedColor);
            
            if (autoShowLeaderboardAfterSave)
            {
                StartCoroutine(DelayedLeaderboardShow());
            }
        }
        else
        {
            ShowStatus("Failed to save score", disconnectedColor);
        }
    }
    
    private IEnumerator DelayedLeaderboardShow()
    {
        yield return new WaitForSeconds(2f);
        LoadAndShowLeaderboard();
    }
    
    private void SavePlayerName()
    {
        if (firebaseManager == null || playerNameInput == null) return;
        
        string newName = playerNameInput.text.Trim();
        if (string.IsNullOrEmpty(newName))
        {
            ShowPlayerSetupStatus("Please enter a valid name", disconnectedColor);
            return;
        }
        
        firebaseManager.SetPlayerName(newName);
        UpdatePlayerNameDisplay();
        ShowPlayerSetupStatus("Player name saved!", connectedColor);
        
        // Auto-close after successful save
        StartCoroutine(DelayedClosePlayerSetup());
    }
    
    private IEnumerator DelayedClosePlayerSetup()
    {
        yield return new WaitForSeconds(1.5f);
        ClosePlayerSetup();
    }
    
    #endregion
    
    #region Leaderboard Management
    
    private void LoadLeaderboard()
    {
        if (firebaseManager == null)
        {
            ShowLeaderboardStatus("Firebase not available", disconnectedColor);
            return;
        }
        
        if (isLoadingLeaderboard) return;
        
        isLoadingLeaderboard = true;
        SetLeaderboardLoading(true);
        ShowLeaderboardStatus("Loading leaderboard...", Color.white);
        
        firebaseManager.LoadLeaderboard();
    }
    
    private void HandleLeaderboardLoaded(ContestFirebaseManager.LeaderboardData leaderboard)
    {
        isLoadingLeaderboard = false;
        SetLeaderboardLoading(false);
        
        DisplayLeaderboard(leaderboard);
        
        if (leaderboard.entries.Count > 0)
        {
            ShowLeaderboardStatus($"Loaded {leaderboard.entries.Count} entries", connectedColor);
        }
        else
        {
            ShowLeaderboardStatus("No scores yet - be the first!", Color.white);
        }
    }
    
    private void DisplayLeaderboard(ContestFirebaseManager.LeaderboardData leaderboard)
    {
        ClearLeaderboard();
        
        if (leaderboard.entries == null || leaderboard.entries.Count == 0)
            return;
        
        if (leaderboardEntryPrefab == null || leaderboardContent == null)
        {
            Debug.LogWarning("Leaderboard UI not properly configured!");
            return;
        }
        
        for (int i = 0; i < Mathf.Min(leaderboard.entries.Count, 50); i++)
        {
            var entry = leaderboard.entries[i];
            GameObject entryObj = Instantiate(leaderboardEntryPrefab, leaderboardContent);
            leaderboardEntries.Add(entryObj);
            
            SetupLeaderboardEntry(entryObj, i + 1, entry);
        }
        
        // Scroll to top
        if (leaderboardScrollRect != null)
        {
            leaderboardScrollRect.verticalNormalizedPosition = 1f;
        }
    }
    
    private void SetupLeaderboardEntry(GameObject entryObj, int rank, ContestFirebaseManager.LeaderboardEntry entry)
    {
        // Find UI components (adjust names based on your prefab)
        TextMeshProUGUI rankText = entryObj.transform.Find("RankText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI nameText = entryObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI scoreText = entryObj.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI accuracyText = entryObj.transform.Find("AccuracyText")?.GetComponent<TextMeshProUGUI>();
        
        // Set text values
        if (rankText != null) rankText.text = $"#{rank}";
        if (nameText != null) nameText.text = entry.playerName;
        if (scoreText != null) scoreText.text = $"{entry.totalScore}/30";
        if (accuracyText != null) accuracyText.text = $"{entry.accuracy:F0}%";
        
        // Highlight current player
        if (firebaseManager != null && entry.playerName == firebaseManager.GetPlayerName())
        {
            if (nameText != null) nameText.color = Color.cyan;
        }
        
        // Color coding for top ranks
        Color entryColor = Color.white;
        if (rank == 1) entryColor = Color.yellow;
        else if (rank == 2) entryColor = Color.cyan;
        else if (rank == 3) entryColor = new Color(1f, 0.6f, 0.2f);
        
        if (rankText != null) rankText.color = entryColor;
    }
    
    private void ClearLeaderboard()
    {
        foreach (GameObject entry in leaderboardEntries)
        {
            if (entry != null)
                Destroy(entry);
        }
        leaderboardEntries.Clear();
    }
    
    private void SetLeaderboardLoading(bool loading)
    {
        if (leaderboardLoadingIndicator != null)
            leaderboardLoadingIndicator.SetActive(loading);
        
        if (refreshLeaderboardButton != null)
            refreshLeaderboardButton.interactable = !loading;
    }
    
    #endregion
    
    #region Status and Feedback
    
    private void UpdatePlayerNameDisplay()
    {
        if (currentPlayerNameDisplay != null && firebaseManager != null)
        {
            currentPlayerNameDisplay.text = $"Player: {firebaseManager.GetPlayerName()}";
        }
    }
    
    private void UpdateConnectionStatus()
    {
        bool connected = firebaseManager != null && firebaseManager.IsConnected;
        
        if (connectionStatusIcon != null)
            connectionStatusIcon.color = connected ? connectedColor : disconnectedColor;
        
        if (connectionStatusText != null)
            connectionStatusText.text = connected ? "Online" : "Offline";
    }
    
    private void HandleFirebaseError(string error)
    {
        isLoadingLeaderboard = false;
        SetLeaderboardLoading(false);
        ShowStatus($"Error: {error}", disconnectedColor);
        UpdateConnectionStatus();
    }
    
    private void ShowStatus(string message, Color color)
    {
        Debug.Log($"[UI] {message}");
        // You can add a global status text here if needed
    }
    
    private void ShowLeaderboardStatus(string message, Color color)
    {
        if (leaderboardStatus != null)
        {
            leaderboardStatus.text = message;
            leaderboardStatus.color = color;
        }
        
        Debug.Log($"[Leaderboard] {message}");
    }
    
    private void ShowPlayerSetupStatus(string message, Color color)
    {
        if (playerSetupStatus != null)
        {
            playerSetupStatus.text = message;
            playerSetupStatus.color = color;
        }
    }
    
    #endregion
    
    #region Public API
    
    public void ManualSaveScore()
    {
        if (firebaseManager != null && lastFinalScore > 0)
        {
            // You can implement manual save logic here
            firebaseManager.ManualSave();
        }
    }
    
    public bool IsConnectedToFirebase()
    {
        return firebaseManager != null && firebaseManager.IsConnected;
    }
    
    #endregion
}