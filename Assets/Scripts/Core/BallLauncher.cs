using UnityEngine;
using System.Collections;

public class BallLauncher : MonoBehaviour
{
    [Header("Launch Settings")]
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Transform launchPoint;
    [SerializeField] private float maxLaunchForce = 20f;
    [SerializeField] private float forceMultiplier = 1.5f;
    
    [Header("Input Settings")]
    [SerializeField] private float maxDragDistance = 3f;
    [SerializeField] private Camera gameCamera;
    
    [Header("Audio & Feedback")]
    [SerializeField] private AudioClip launchSound;
    [SerializeField] private HapticFeedback hapticFeedback;
    
    [Header("Game Logic")]
    [SerializeField] private int maxShotsPerLevel = 3;
    
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
    
    // Events
    public System.Action<int> OnShotTaken; // shots remaining
    public System.Action OnLevelComplete;
    public System.Action OnLevelFailed;
    
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
    }
    
    private void Start()
    {
        PrepareBall();
    }
    
    private void Update()
    {
        if (!canShoot) return;
        
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
        if (!canShoot) return;
        
        isAiming = true;
        startDragPosition = gameCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, gameCamera.nearClipPlane + 1f));
        startDragPosition.z = launchPoint.position.z; // Keep Z consistent
        
        // Show trajectory
        if (trajectoryRenderer != null)
        {
            trajectoryRenderer.ShowTrajectory(launchPoint.position, Vector3.zero);
        }
    }

    private void UpdateAiming(Vector3 screenPosition)
    {
        if (!isAiming) return;

        currentDragPosition = gameCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, gameCamera.nearClipPlane + 1f));
        currentDragPosition.z = launchPoint.position.z; // Keep Z consistent

        Vector3 launchVelocity = CalculateLaunchVelocity();

        // Update trajectory
        if (trajectoryRenderer != null)
        {
            trajectoryRenderer.UpdateTrajectory(launchPoint.position, launchVelocity);
        }

        // Optional: Visual feedback for drag distance
        Debug.DrawLine(launchPoint.position, currentDragPosition, Color.red);
        Debug.Log("Start drag: " + startDragPosition);
        Debug.Log("Current drag: " + currentDragPosition);  
        Debug.Log("Launch velocity: " + launchVelocity);
    }
    
    private Vector3 CalculateLaunchVelocity()
    {
        Vector3 dragVector = startDragPosition - currentDragPosition;
        
        // Clamp drag distance
        if (dragVector.magnitude > maxDragDistance)
        {
            dragVector = dragVector.normalized * maxDragDistance;
        }
        
        // Calculate velocity (opposite of drag direction)
        Vector3 velocity = dragVector * forceMultiplier;
        
        // Clamp maximum force
        if (velocity.magnitude > maxLaunchForce)
        {
            velocity = velocity.normalized * maxLaunchForce;
        }
        
        return velocity;
    }
    
    private void Launch()
    {
        if (!isAiming) return;
        
        isAiming = false;
        canShoot = false;
        
        // Hide trajectory
        if (trajectoryRenderer != null)
        {
            trajectoryRenderer.HideTrajectory();
        }
        
        // Calculate and apply launch velocity
        Vector3 launchVelocity = CalculateLaunchVelocity();
        
        if (currentBall != null)
        {
            Rigidbody ballRb = currentBall.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.isKinematic = false; // Enable physics
                ballRb.linearVelocity = launchVelocity;
               
            }
        }
        
        // Audio feedback
        if (audioSource != null && launchSound != null)
        {
            audioSource.PlayOneShot(launchSound);
        }
        
        // Haptic feedback
        if (hapticFeedback != null)
        {
            hapticFeedback.TriggerVibration();
        }
        
        // Update shot count
        shotsUsed++;
        OnShotTaken?.Invoke(maxShotsPerLevel - shotsUsed);
        
        // Start coroutine to handle next shot or level end
        StartCoroutine(HandleShotResult());
    }
    
    private IEnumerator HandleShotResult()
    {
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
            currentBall = Instantiate(ballPrefab, launchPoint.position, launchPoint.rotation);
            
            // Make ball kinematic until launch
            Rigidbody ballRb = currentBall.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.isKinematic = true;
            }
        }
    }
    
    public void ResetLevel()
    {
        shotsUsed = 0;
        canShoot = true;
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
    public int ShotsRemaining => maxShotsPerLevel - shotsUsed;
    public int ShotsUsed => shotsUsed;
    public int MaxShots => maxShotsPerLevel;
}