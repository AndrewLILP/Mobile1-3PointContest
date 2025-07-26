using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ContestUIManager : MonoBehaviour
{
    [Header("Contest Reference")]
    [SerializeField] private ThreePointContest contestManager;
    
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private GameObject pausePanel;
    
    [Header("Main Menu UI")]
    [SerializeField] private Button startContestButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TextMeshProUGUI titleText;
    
    [Header("Gameplay UI")]
    [SerializeField] private TextMeshProUGUI totalScoreText;
    [SerializeField] private TextMeshProUGUI positionScoreText;
    [SerializeField] private TextMeshProUGUI currentPositionText;
    [SerializeField] private TextMeshProUGUI ballsRemainingText;
    [SerializeField] private TextMeshProUGUI positionNameText;
    
    [Header("Progress Indicators")]
    [SerializeField] private Slider positionProgressSlider;
    [SerializeField] private Slider overallProgressSlider;
    [SerializeField] private Image[] positionIndicators = new Image[5]; // For 5 positions
    
    [Header("Individual Position Scores")]
    [SerializeField] private TextMeshProUGUI[] positionScoreLabels = new TextMeshProUGUI[5];
    
    [Header("Results UI")]
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI[] finalPositionScores = new TextMeshProUGUI[5];
    [SerializeField] private TextMeshProUGUI starRatingText;
    [SerializeField] private Image[] starImages = new Image[3];
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button mainMenuButton;
    
    [Header("Pause UI")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseMenuButton;
    [SerializeField] private Button pauseQuitButton;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject scorePopupPrefab;
    [SerializeField] private Transform popupParent;
    [SerializeField] private AnimationCurve scoreAnimCurve = new AnimationCurve(
        new Keyframe(0, 0, 0, 2), 
        new Keyframe(0.5f, 1.1f, 0, 0), 
        new Keyframe(1, 1, -2, 0)
    );
    
    [Header("Colors")]
    [SerializeField] private Color primaryPurple = new Color(0.5f, 0.2f, 0.8f);
    [SerializeField] private Color accentBlue = new Color(0.2f, 0.5f, 1f);
    [SerializeField] private Color goldHighlight = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private Color darkBackground = new Color(0.1f, 0.1f, 0.1f);
    
    // State tracking
    private bool isPaused = false;
    private List<int> currentPositionScores = new List<int>();
    
    private void Awake()
    {
        // Find contest manager if not assigned
        if (contestManager == null)
        {
            contestManager = FindFirstObjectByType<ThreePointContest>();
        }
        
        // Initialize position scores
        currentPositionScores = new List<int>(new int[5]);
    }
    
    private void Start()
    {
        SetupButtonListeners();
        SubscribeToContestEvents();
        ShowMainMenu();
        ApplyColorScheme();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromContestEvents();
    }
    
    private void SetupButtonListeners()
    {
        // Main Menu
        if (startContestButton != null)
            startContestButton.onClick.AddListener(StartContest);
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
        
        // Gameplay
        if (pauseButton != null)
            pauseButton.onClick.AddListener(PauseGame);
        
        // Results
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(StartContest);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ShowMainMenu);
        
        // Pause Menu
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
        if (pauseMenuButton != null)
            pauseMenuButton.onClick.AddListener(ShowMainMenu);
        if (pauseQuitButton != null)
            pauseQuitButton.onClick.AddListener(QuitGame);
    }
    
    private void SubscribeToContestEvents()
    {
        if (contestManager == null) return;
        
        contestManager.OnScoreUpdated += UpdateScoreDisplay;
        contestManager.OnPositionChanged += UpdatePositionDisplay;
        contestManager.OnBallShot += UpdateBallsRemaining;
        contestManager.OnContestComplete += ShowResults;
    }
    
    private void UnsubscribeFromContestEvents()
    {
        if (contestManager == null) return;
        
        contestManager.OnScoreUpdated -= UpdateScoreDisplay;
        contestManager.OnPositionChanged -= UpdatePositionDisplay;
        contestManager.OnBallShot -= UpdateBallsRemaining;
        contestManager.OnContestComplete -= ShowResults;
    }
    
    #region Panel Management
    
    public void ShowMainMenu()
    {
        SetPanelActive(mainMenuPanel, true);
        SetPanelActive(gameplayPanel, false);
        SetPanelActive(resultsPanel, false);
        SetPanelActive(pausePanel, false);
        
        isPaused = false;
        Time.timeScale = 1f;
    }
    
    public void ShowGameplay()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(gameplayPanel, true);
        SetPanelActive(resultsPanel, false);
        SetPanelActive(pausePanel, false);
        
        // Reset position scores display
        currentPositionScores = new List<int>(new int[5]);
        UpdateAllPositionScores();
    }
    
    public void ShowResults(int finalScore, List<int> positionScores)
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(gameplayPanel, false);
        SetPanelActive(resultsPanel, true);
        SetPanelActive(pausePanel, false);
        
        DisplayFinalResults(finalScore, positionScores);
    }
    
    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
    
    #endregion
    
    #region Contest Control
    
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
    
    #endregion
    
    #region Score Display Updates
    
    private void UpdateScoreDisplay(int totalScore, int positionScore)
    {
        // Update main score displays
        if (totalScoreText != null)
            totalScoreText.text = $"Total: {totalScore}";
        
        if (positionScoreText != null)
            positionScoreText.text = $"Position: {positionScore}";
        
        // Update current position score in tracking
        if (contestManager != null)
        {
            int currentPos = contestManager.CurrentPosition - 1; // Convert to 0-based
            if (currentPos >= 0 && currentPos < currentPositionScores.Count)
            {
                currentPositionScores[currentPos] = positionScore;
                UpdateAllPositionScores();
            }
        }
        
        // Animate score popup
        ShowScorePopup(positionScore > 0 ? "+1" : "Miss");
    }
    
    private void UpdatePositionDisplay(int positionIndex, int ballsRemaining, int positionScore)
    {
        // Update position info
        if (currentPositionText != null)
            currentPositionText.text = $"Position {positionIndex + 1}/5";
        
        if (positionNameText != null && contestManager != null)
            positionNameText.text = contestManager.CurrentPositionName;
        
        // Update position indicators
        UpdatePositionIndicators(positionIndex);
        
        // Update overall progress
        UpdateOverallProgress();
    }
    
    private void UpdateBallsRemaining(int ballsRemaining)
    {
        if (ballsRemainingText != null)
            ballsRemainingText.text = $"Balls: {ballsRemaining + 1}/5";
        
        // Update position progress
        if (positionProgressSlider != null && contestManager != null)
        {
            float progress = 1f - (float)ballsRemaining / contestManager.BallsPerPosition;
            positionProgressSlider.value = progress;
        }
    }
    
    private void UpdatePositionIndicators(int currentIndex)
    {
        for (int i = 0; i < positionIndicators.Length; i++)
        {
            if (positionIndicators[i] == null) continue;
            
            if (i == currentIndex)
            {
                // Current position - gold
                positionIndicators[i].color = goldHighlight;
            }
            else if (i < currentIndex)
            {
                // Completed position - blue
                positionIndicators[i].color = accentBlue;
            }
            else
            {
                // Upcoming position - purple
                positionIndicators[i].color = primaryPurple;
            }
        }
    }
    
    private void UpdateOverallProgress()
    {
        if (overallProgressSlider == null || contestManager == null) return;
        
        float progress = (float)contestManager.CurrentPosition / contestManager.TotalPositions;
        overallProgressSlider.value = progress;
    }
    
    private void UpdateAllPositionScores()
    {
        for (int i = 0; i < positionScoreLabels.Length; i++)
        {
            if (positionScoreLabels[i] == null) continue;
            
            if (i < currentPositionScores.Count)
            {
                positionScoreLabels[i].text = currentPositionScores[i].ToString();
            }
            else
            {
                positionScoreLabels[i].text = "-";
            }
        }
    }
    
    #endregion
    
    #region Results Display
    
    private void DisplayFinalResults(int finalScore, List<int> positionScores)
    {
        // Final score
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
        if (score >= 25) return 3; // Excellent
        if (score >= 18) return 2; // Good  
        if (score >= 10) return 1; // Fair
        return 0; // Try again
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
            
            starImages[i].color = i < stars ? goldHighlight : Color.gray;
        }
    }
    
    #endregion
    
    #region Visual Effects
    
    private void ShowScorePopup(string text)
    {
        if (scorePopupPrefab == null || popupParent == null) return;
        
        GameObject popup = Instantiate(scorePopupPrefab, popupParent);
        TextMeshProUGUI popupText = popup.GetComponentInChildren<TextMeshProUGUI>();
        
        if (popupText != null)
        {
            popupText.text = text;
            popupText.color = text == "Miss" ? Color.red : goldHighlight;
        }
        
        StartCoroutine(AnimateScorePopup(popup));
    }
    
    private IEnumerator AnimateScorePopup(GameObject popup)
    {
        Vector3 startPos = popup.transform.localPosition;
        Vector3 endPos = startPos + Vector3.up * 100f;
        
        float duration = 1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            
            // Animate position
            popup.transform.localPosition = Vector3.Lerp(startPos, endPos, scoreAnimCurve.Evaluate(progress));
            
            // Animate alpha
            CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - progress;
            }
            
            yield return null;
        }
        
        Destroy(popup);
    }
    
    #endregion
    
    #region Color Scheme
    
    private void ApplyColorScheme()
    {
        // Apply color scheme to UI elements
        // This is a basic implementation - expand as needed
        
        // Example: Apply colors to buttons
        ApplyColorToButtons();
    }
    
    private void ApplyColorToButtons()
    {
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        
        foreach (Button button in allButtons)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = primaryPurple;
            colors.highlightedColor = accentBlue;
            colors.pressedColor = goldHighlight;
            button.colors = colors;
        }
    }
    
    #endregion
    
    #region Public Getters (for other UI scripts)
    
    public bool IsPaused => isPaused;
    public bool IsContestActive => contestManager != null && contestManager.IsContestActive;
    
    #endregion
}