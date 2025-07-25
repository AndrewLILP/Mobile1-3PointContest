using UnityEngine;
using UnityEngine.UI;

public class ContestStarter : MonoBehaviour
{
    [Header("Contest Management")]
    [SerializeField] private ThreePointContest contestManager;
    
    [Header("UI Elements")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject gameUI;
    
    private void Start()
    {
        // Find contest manager if not assigned
        if (contestManager == null)
        {
            contestManager = FindFirstObjectByType<ThreePointContest>();
        }
        
        // Set up button listeners
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartContest);
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetContest);
        }
        
        // Subscribe to contest events
        if (contestManager != null)
        {
            contestManager.OnContestComplete += HandleContestComplete;
        }
        
        // Show menu initially
        ShowMenu();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (contestManager != null)
        {
            contestManager.OnContestComplete -= HandleContestComplete;
        }
    }
    
    public void StartContest()
    {
        if (contestManager != null)
        {
            contestManager.StartContest();
            ShowGameUI();
            Debug.Log("Contest Started!");
        }
        else
        {
            Debug.LogError("No ThreePointContest found! Make sure it's in the scene.");
        }
    }
    
    public void ResetContest()
    {
        if (contestManager != null)
        {
            contestManager.ResetContest();
            ShowMenu();
            Debug.Log("Contest Reset!");
        }
    }
    
    private void HandleContestComplete(int finalScore, System.Collections.Generic.List<int> positionScores)
    {
        Debug.Log($"Contest Complete! Final Score: {finalScore}");
        
        // Show results and return to menu after delay
        Invoke(nameof(ShowMenu), 3f);
    }
    
    private void ShowMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
        if (gameUI != null) gameUI.SetActive(false);
    }
    
    private void ShowGameUI()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (gameUI != null) gameUI.SetActive(true);
    }
    
    // Public methods for other scripts or inspector buttons
    public void QuitGame()
    {
        Application.Quit();
    }
    
    public void PauseContest()
    {
        Time.timeScale = 0f;
    }
    
    public void ResumeContest()
    {
        Time.timeScale = 1f;
    }
}