using UnityEngine;
using System.Collections;

public class BallLauncher : MonoBehaviour
{
    [Header("Launch Settings")]
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Transform launchPoint;
    [SerializeField] private float maxLaunchForce = 25f; // Reduced from 50f
    [SerializeField] private float forceMultiplier = 12f; // Increased from 8f
    
    [Header("Input Settings")]
    [SerializeField] private float maxDragDistance = 3f; // Reduced from 4f
    [SerializeField] private Camera gameCamera;
    
    [Header("Trajectory Improvements")]
    [SerializeField] private float upwardForceBoost = 2f; // Add arc to shots
    [SerializeField] private float minimumUpwardForce = 3f; // Ensure ball goes up
    [SerializeField] private float forwardForceBoost = 1.2f; // Help reach hoop
    
    [Header("Audio & Feedback")]
    [SerializeField] private AudioClip launchSound;
    [SerializeField] private AudioClip dryFireSound;
    [SerializeField] private HapticFeedback hapticFeedback;
    
    [Header("Contest Integration")]
    [SerializeField] private ThreePointContest contestManager;
    [SerializeField] private bool useContestMode = true;
    
    [Header("Game Logic")]
    [SerializeField] private int maxShotsPerLevel = 5;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool showTrajectoryPreview = true;
    
    // Components
    private TrajectoryRenderer trajectoryRenderer;
    private AudioSource audioSource;
    
    // State
    private bool isAiming = false;
    private bool canShoot = true;
    private Vector3 startDragPosition;
    private Vector3 currentDragPosition;
    private GameObject currentBall;
    private int shotsUsed = 0;
    
    // Contest state
    private bool contestMode = false;
    private ShootingPosition currentPosition;
    
    // Events
    public System.Action<int> OnShotTaken; // shots remaining
    public System.Action OnLevelComplete;
    public System.Action OnLevelFailed;
    public System.Action OnBallLaunched; // Ball was actually launched
    
    private void Awake()
    {
        // Get components
        trajectoryRenderer = GetComponent<TrajectoryRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        // Set camera if not assigned
        if (gameCamera == null)
        {
            gameCamera = Camera.main;
        }
        
        // Create audio source if needed
        if (audioSource == null && launchSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Find contest manager if not assigned
        if (contestManager == null && useContestMode)
        {
            contestManager = FindFirstObjectByType<ThreePointContest>();
        }
    }
    
    private void Start()
    {
        // Check if we're in contest mode
        contestMode = (contestManager != null && useContestMode);
        
        if (!contestMode)
        {
            // Traditional mode - prepare ball immediately
            PrepareBall();
        }
        
        Debug.Log($"BallLauncher initialized in {(contestMode ? "Contest" : "Traditional")} mode");
        
        // Debug launch point setup
        if (enableDebugLogs)
        {
            Debug.Log($"Launch point: {(launchPoint != null ? launchPoint.position.ToString() : "NULL")}");
            Debug.Log($"Camera: {(gameCamera != null ? gameCamera.name : "NULL")}");
        }
    }
    
    private void Update()
    {
        // Check if shooting is allowed
        bool shootingAllowed = contestMode ? 
            (contestManager != null && contestManager.CanShoot) : 
            canShoot;
            
        if (!shootingAllowed) return;
        
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#elif UNITY_ANDROID || UNITY_IOS
        HandleTouchInput();
#endif
    }
    
    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartAiming(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && isAiming)
        {
            UpdateAiming(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && isAiming)
        {
            Launch();
        }
    }
    
    private void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    StartAiming(touch.position);
                    break;
                case TouchPhase.Moved:
                    if (isAiming)
                        UpdateAiming(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (isAiming)
                        Launch();
                    break;
            }
        }
    }
    
    private void StartAiming(Vector3 screenPosition)
    {
        // Check if we can shoot
        if (contestMode && (contestManager == null || !contestManager.CanShoot))
        {
            PlaySound(dryFireSound);
            return;
        }
        
        if (!contestMode && !canShoot)
        {
            PlaySound(dryFireSound);
            return;
        }
        
        isAiming = true;
        
        // Improved screen to world conversion
        Vector3 worldPos = gameCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
        startDragPosition = worldPos;
        
        if (enableDebugLogs)
        {
            Debug.Log($"Start aiming at screen: {screenPosition}, world: {worldPos}");
        }
        
        // Show trajectory
        if (trajectoryRenderer != null && showTrajectoryPreview)
        {
            trajectoryRenderer.ShowTrajectory(launchPoint.position, Vector3.zero);
        }
    }
    
    private void UpdateAiming(Vector3 screenPosition)
    {
        if (!isAiming) return;
        
        // Improved screen to world conversion
        Vector3 worldPos = gameCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
        currentDragPosition = worldPos;
        
        Vector3 launchVelocity = CalculateLaunchVelocity();
        
        // Update trajectory
        if (trajectoryRenderer != null && showTrajectoryPreview)
        {
            trajectoryRenderer.UpdateTrajectory(launchPoint.position, launchVelocity);
        }
        
        // Debug visualization
        if (enableDebugLogs)
        {
            Debug.DrawLine(launchPoint.position, currentDragPosition, Color.red, 0.1f);
            Debug.DrawRay(launchPoint.position, launchVelocity.normalized * 3f, Color.green, 0.1f);
        }
    }
    
    private Vector3 CalculateLaunchVelocity()
    {
        Vector3 dragVector = startDragPosition - currentDragPosition;
        
        // Clamp drag distance
        if (dragVector.magnitude > maxDragDistance)
        {
            dragVector = dragVector.normalized * maxDragDistance;
        }
        
        // Get force multiplier (may be adjusted by position)
        float currentForceMultiplier = GetCurrentForceMultiplier();
        
        // Calculate base velocity (opposite of drag direction)
        Vector3 velocity = dragVector * currentForceMultiplier;
        
        // ✅ IMPROVED: Add upward force for better arc
        velocity.y += upwardForceBoost;
        
        // ✅ IMPROVED: Ensure minimum upward force
        if (velocity.y < minimumUpwardForce)
        {
            velocity.y = minimumUpwardForce;
        }
        
        // ✅ IMPROVED: Add forward force boost toward hoop
        Vector3 directionToHoop = (Vector3.zero - launchPoint.position).normalized;
        velocity += directionToHoop * forwardForceBoost;
        
        // Clamp maximum force
        if (velocity.magnitude > maxLaunchForce)
        {
            velocity = velocity.normalized * maxLaunchForce;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"Drag vector: {dragVector}, Final velocity: {velocity}, Magnitude: {velocity.magnitude}");
        }
        
        return velocity;
    }
    
    private float GetCurrentForceMultiplier()
    {
        // In contest mode, use position-specific force if available
        if (contestMode && currentPosition != null)
        {
            return currentPosition.GetRecommendedForce();
        }
        
        return forceMultiplier;
    }
    
    private void Launch()
    {
        if (!isAiming) return;
        
        isAiming = false;
        
        // Hide trajectory
        if (trajectoryRenderer != null)
        {
            trajectoryRenderer.HideTrajectory();
        }
        
        // Calculate and apply launch velocity
        Vector3 launchVelocity = CalculateLaunchVelocity();
        
        // Check if we have a ball ready
        if (currentBall == null)
        {
            PrepareBall();
        }
        
        if (currentBall != null)
        {
            Rigidbody ballRb = currentBall.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.isKinematic = false; // Enable physics
                ballRb.linearVelocity = launchVelocity;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"Ball launched with velocity: {launchVelocity} from position: {currentBall.transform.position}");
                    Debug.Log($"Hoop direction: {(Vector3.zero - currentBall.transform.position).normalized}");
                    Debug.Log($"Ball mass: {ballRb.mass}, Linear damping: {ballRb.linearDamping}");
                }
                
                // Ball is now launched - clear reference
                currentBall = null;
            }
            else
            {
                Debug.LogError("Ball prefab missing Rigidbody component!");
            }
        }
        else
        {
            Debug.LogError("No ball available to launch!");
        }
        
        // Audio feedback
        PlaySound(launchSound);
        
        // Haptic feedback
        if (hapticFeedback != null)
        {
            hapticFeedback.TriggerVibration();
        }
        
        // Update shot count
        shotsUsed++;
        OnBallLaunched?.Invoke();
        
        // In contest mode, let the contest manager handle shot tracking
        if (contestMode)
        {
            OnShotTaken?.Invoke(0); // Contest manager handles remaining shots
        }
        else
        {
            // Traditional mode
            OnShotTaken?.Invoke(maxShotsPerLevel - shotsUsed);
            StartCoroutine(HandleShotResult());
        }
        
        // Prepare next ball after a short delay
        StartCoroutine(PrepareNextBall());
    }
    
    private IEnumerator PrepareNextBall()
    {
        // Wait a moment before preparing the next ball
        yield return new WaitForSeconds(0.5f);
        
        if (contestMode)
        {
            // In contest mode, only prepare if contest is still active
            if (contestManager != null && contestManager.CanShoot)
            {
                PrepareBall();
            }
        }
        else
        {
            // Traditional mode
            if (shotsUsed < maxShotsPerLevel)
            {
                PrepareBall();
                canShoot = true;
            }
        }
    }
    
    private IEnumerator HandleShotResult()
    {
        // Traditional mode shot handling
        canShoot = false;
        
        // Wait for ball to settle or score
        yield return new WaitForSeconds(3f);
        
        // Check if level should continue
        if (shotsUsed >= maxShotsPerLevel)
        {
            // No more shots - level failed (unless score was achieved)
            OnLevelFailed?.Invoke();
        }
        else
        {
            // Prepare next shot
            PrepareBall();
            canShoot = true;
        }
    }
    
    private void PrepareBall()
    {
        // Remove previous ball if it exists
        if (currentBall != null)
        {
            Destroy(currentBall);
        }
        
        // Create new ball at launch point
        if (ballPrefab != null)
        {
            Vector3 spawnPosition = launchPoint.position;
            
            // In contest mode, use position-specific spawn point if available
            if (contestMode && currentPosition != null && currentPosition.ballSpawnPoint != null)
            {
                spawnPosition = currentPosition.ballSpawnPoint.position;
            }
            
            currentBall = Instantiate(ballPrefab, spawnPosition, launchPoint.rotation);
            
            // Make ball kinematic until launch
            Rigidbody ballRb = currentBall.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.isKinematic = true;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"Ball prepared at position: {spawnPosition}");
                }
            }
            else
            {
                Debug.LogError("Ball prefab is missing Rigidbody component!");
            }
        }
        else
        {
            Debug.LogError("Ball prefab is not assigned!");
        }
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // Contest integration methods
    public void SetPosition(ShootingPosition position)
    {
        currentPosition = position;
        
        if (position != null)
        {
            // Update launch point to position's launch point
            if (position.launchPoint != null)
            {
                launchPoint = position.launchPoint;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"Launch point updated to: {launchPoint.position}");
                }
            }
            
            Debug.Log($"BallLauncher set to position: {position.positionName}");
        }
    }
    
    public void EnableContestMode(bool enable)
    {
        contestMode = enable;
        
        if (contestMode)
        {
            canShoot = false; // Contest manager controls shooting
        }
        else
        {
            canShoot = true;
            PrepareBall();
        }
    }
    
    // Traditional mode methods
    public void ResetLevel()
    {
        shotsUsed = 0;
        canShoot = !contestMode; // In contest mode, contest manager controls this
        isAiming = false;
        
        if (trajectoryRenderer != null)
        {
            trajectoryRenderer.HideTrajectory();
        }
        
        PrepareBall();
    }
    
    public void CompleteLevel()
    {
        canShoot = false;
        isAiming = false;
        
        if (trajectoryRenderer != null)
        {
            trajectoryRenderer.HideTrajectory();
        }
        
        OnLevelComplete?.Invoke();
    }
    
    // Public getters for UI
    public int ShotsRemaining => contestMode ? 0 : (maxShotsPerLevel - shotsUsed);
    public int ShotsUsed => shotsUsed;
    public int MaxShots => maxShotsPerLevel;
    public bool IsContestMode => contestMode;
    public bool CanShoot => contestMode ? 
        (contestManager != null && contestManager.CanShoot) : canShoot;
}