using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ThreePointContest : MonoBehaviour
{
    [Header("Contest Settings")]
    [SerializeField] private int ballsPerPosition = 5;
    [SerializeField] private int regularBallPoints = 1;
    [SerializeField] private int moneyBallPoints = 2;
    [SerializeField] private float timeBetweenPositions = 2f;
    
    [Header("Shooting Positions")]
    [SerializeField] private ShootingPosition[] shootingPositions;
    
    [Header("Game Components")]
    [SerializeField] private BallLauncher ballLauncher;
    [SerializeField] private Hoop basketballHoop;
    [SerializeField] private Camera gameCamera;
    
    [Header("Audio")]
    [SerializeField] private AudioClip positionChangeSound;
    [SerializeField] private AudioClip contestCompleteSound;
    [SerializeField] private AudioClip moneyBallSound;
    
    // Contest State
    private int currentPositionIndex = 0;
    private int currentBallIndex = 0;
    private int totalScore = 0;
    private int positionScore = 0;
    private bool contestActive = false;
    private bool shootingAllowed = true;
    
    // Score tracking
    private List<int> positionScores = new List<int>();
    
    // Events
    public System.Action<int, int> OnScoreUpdated; // (total score, position score)
    public System.Action<int, int, int> OnPositionChanged; // (position index, balls remaining, position score)
    public System.Action<int> OnBallShot; // (balls remaining in position)
    public System.Action<int, List<int>> OnContestComplete; // (final score, position scores)
    public System.Action<bool> OnMoneyBall; // (is money ball)
    
    private AudioSource audioSource;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Find components if not assigned
        if (ballLauncher == null)
            ballLauncher = FindFirstObjectByType<BallLauncher>();
        if (basketballHoop == null)
            basketballHoop = FindFirstObjectByType<Hoop>();
        if (gameCamera == null)
            gameCamera = Camera.main;
    }
    
    private void Start()
    {
        // Subscribe to events
        if (ballLauncher != null)
        {
            ballLauncher.OnShotTaken += HandleShotTaken;
        }
        
        if (basketballHoop != null)
        {
            basketballHoop.OnScore += HandleScore;
        }
        
        // Initialize positions
        InitializePositions();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (ballLauncher != null)
        {
            ballLauncher.OnShotTaken -= HandleShotTaken;
        }
        
        if (basketballHoop != null)
        {
            basketballHoop.OnScore -= HandleScore;
        }
    }
    
    public void StartContest()
    {
        if (contestActive) return;
        
        // Reset contest state
        currentPositionIndex = 0;
        currentBallIndex = 0;
        totalScore = 0;
        positionScore = 0;
        contestActive = true;
        shootingAllowed = true;
        positionScores.Clear();
        
        // Move to first position
        MoveToPosition(0);
        
        Debug.Log("3-Point Contest Started!");
    }
    
    public void ResetContest()
    {
        contestActive = false;
        shootingAllowed = false;
        currentPositionIndex = 0;
        currentBallIndex = 0;
        totalScore = 0;
        positionScore = 0;
        positionScores.Clear();
        
        // Reset all positions
        foreach (var position in shootingPositions)
        {
            position.ResetPosition();
        }
        
        // Reset hoop
        if (basketballHoop != null)
        {
            basketballHoop.ResetHoop();
        }
        
        Debug.Log("Contest Reset");
    }
    
    private void InitializePositions()
    {
        // Validate shooting positions
        if (shootingPositions == null || shootingPositions.Length == 0)
        {
            Debug.LogError("No shooting positions assigned! Please assign 5 positions around the 3-point arc.");
            return;
        }
        
        // Initialize each position
        for (int i = 0; i < shootingPositions.Length; i++)
        {
            if (shootingPositions[i] != null)
            {
                shootingPositions[i].Initialize(i, ballsPerPosition);
            }
        }
    }
    
    private void MoveToPosition(int positionIndex)
    {
        if (positionIndex >= shootingPositions.Length)
        {
            CompleteContest();
            return;
        }
        
        // Save previous position score
        if (currentPositionIndex > 0 || positionScore > 0)
        {
            positionScores.Add(positionScore);
        }
        
        currentPositionIndex = positionIndex;
        positionScore = 0;
        currentBallIndex = 0;
        
        ShootingPosition newPosition = shootingPositions[positionIndex];
        
        // Move camera and launcher to new position
        StartCoroutine(TransitionToPosition(newPosition));
        
        // Play position change sound
        PlaySound(positionChangeSound);
        
        // Notify UI
        OnPositionChanged?.Invoke(currentPositionIndex, ballsPerPosition, positionScore);
        
        Debug.Log($"Moved to Position {positionIndex + 1}: {newPosition.positionName}");
    }
    
    private IEnumerator TransitionToPosition(ShootingPosition newPosition)
    {
        shootingAllowed = false;
        
        // Move ball launcher to new position
        if (ballLauncher != null)
        {
            ballLauncher.transform.position = newPosition.launchPoint.position;
            ballLauncher.transform.rotation = newPosition.launchPoint.rotation;
        }
        
        // Move camera to new angle
        if (gameCamera != null && newPosition.cameraPosition != null)
        {
            float transitionTime = 1f;
            Vector3 startPos = gameCamera.transform.position;
            Quaternion startRot = gameCamera.transform.rotation;
            
            for (float t = 0; t < transitionTime; t += Time.deltaTime)
            {
                float progress = t / transitionTime;
                gameCamera.transform.position = Vector3.Lerp(startPos, newPosition.cameraPosition.position, progress);
                gameCamera.transform.rotation = Quaternion.Lerp(startRot, newPosition.cameraPosition.rotation, progress);
                yield return null;
            }
            
            gameCamera.transform.position = newPosition.cameraPosition.position;
            gameCamera.transform.rotation = newPosition.cameraPosition.rotation;
        }
        
        yield return new WaitForSeconds(timeBetweenPositions);
        
        // Reset ball launcher for new position
        if (ballLauncher != null)
        {
            ballLauncher.ResetLevel();
        }
        
        shootingAllowed = true;
        
        Debug.Log($"Ready to shoot from {newPosition.positionName}");
    }
    
    private void HandleShotTaken(int shotsRemaining)
    {
        if (!contestActive || !shootingAllowed) return;
        
        currentBallIndex++;
        
        // Check if this is a money ball (last ball at each position)
        bool isMoneyBall = (currentBallIndex == ballsPerPosition);
        OnMoneyBall?.Invoke(isMoneyBall);
        
        if (isMoneyBall)
        {
            PlaySound(moneyBallSound);
        }
        
        OnBallShot?.Invoke(ballsPerPosition - currentBallIndex);
        
        Debug.Log($"Shot {currentBallIndex}/{ballsPerPosition} from position {currentPositionIndex + 1}" + 
                  (isMoneyBall ? " (MONEY BALL!)" : ""));
    }
    
    private void HandleScore(int points, bool isSwish)
    {
        if (!contestActive) return;
        
        // Check if this is a money ball
        bool isMoneyBall = (currentBallIndex == ballsPerPosition);
        int finalPoints = isMoneyBall ? moneyBallPoints : regularBallPoints;
        
        positionScore += finalPoints;
        totalScore += finalPoints;
        
        OnScoreUpdated?.Invoke(totalScore, positionScore);
        
        Debug.Log($"SCORE! +{finalPoints} points" + (isSwish ? " (SWISH!)" : "") + 
                  (isMoneyBall ? " (MONEY BALL!)" : "") + 
                  $" | Position: {positionScore} | Total: {totalScore}");
        
        // Check if position is complete
        if (currentBallIndex >= ballsPerPosition)
        {
            StartCoroutine(CompletePosition());
        }
    }
    
    private IEnumerator CompletePosition()
    {
        yield return new WaitForSeconds(1f); // Let the ball settle
        
        Debug.Log($"Position {currentPositionIndex + 1} complete! Score: {positionScore}");
        
        // Move to next position
        MoveToPosition(currentPositionIndex + 1);
    }
    
    private void CompleteContest()
    {
        // Add final position score
        positionScores.Add(positionScore);
        
        contestActive = false;
        shootingAllowed = false;
        
        // Calculate star rating
        int stars = CalculateStarRating(totalScore);
        
        PlaySound(contestCompleteSound);
        
        OnContestComplete?.Invoke(totalScore, positionScores);
        
        Debug.Log($"Contest Complete! Final Score: {totalScore}/30 | Stars: {stars}");
        Debug.Log($"Position Breakdown: {string.Join(", ", positionScores)}");
    }
    
    private int CalculateStarRating(int score)
    {
        // Star rating based on score out of 30 possible points
        if (score >= 25) return 3; // Excellent
        if (score >= 18) return 2; // Good  
        if (score >= 10) return 1; // Fair
        return 0; // Try again
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // Public getters for UI
    public int CurrentPosition => currentPositionIndex + 1;
    public int TotalPositions => shootingPositions.Length;
    public int CurrentBall => currentBallIndex + 1;
    public int BallsPerPosition => ballsPerPosition;
    public int TotalScore => totalScore;
    public int PositionScore => positionScore;
    public bool IsContestActive => contestActive;
    public bool CanShoot => contestActive && shootingAllowed;
    public string CurrentPositionName => 
        (shootingPositions != null && currentPositionIndex < shootingPositions.Length) 
        ? shootingPositions[currentPositionIndex].positionName 
        : "Unknown";
}