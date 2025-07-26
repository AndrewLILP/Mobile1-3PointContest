using UnityEngine;

[System.Serializable]
public class ShootingPosition : MonoBehaviour
{
    [Header("Position Info")]
    [SerializeField] public string positionName = "3-Point Position";
    [SerializeField] public int positionIndex = 0;
    
    [Header("Transform References")]
    [SerializeField] public Transform launchPoint;
    [SerializeField] public Transform cameraPosition;
    [SerializeField] public Transform ballSpawnPoint;
    
    [Header("Position Settings")]
    [SerializeField] public float distanceToHoop = 7.24f; // NBA 3-point line distance
    [SerializeField] public bool isCornerPosition = false;
    [SerializeField] public Vector3 aimDirection = Vector3.forward;
    
    [Header("Visual Markers")]
    [SerializeField] public GameObject positionMarker;
    [SerializeField] public GameObject activeIndicator;
    [SerializeField] public Color positionColor = Color.white;
    
    [Header("Position-Specific Settings")]
    [SerializeField] public float recommendedForceMultiplier = 8f;
    [SerializeField] public Vector3 optimalAimOffset = Vector3.zero;
    
    // State
    private bool isActive = false;
    private bool isCompleted = false;
    private int ballsShot = 0;
    private int ballsScored = 0;
    private bool lastShotScored = false; // Track if last shot scored
    
    // Events
    public System.Action<ShootingPosition> OnPositionActivated;
    public System.Action<ShootingPosition, int, int> OnPositionCompleted; // position, balls shot, balls scored
    
    private void Awake()
    {
        // Auto-setup if transforms not assigned
        SetupDefaultTransforms();
        
        // Set initial state
        SetActiveState(false);
    }
    
    private void SetupDefaultTransforms()
    {
        // Create launch point if not assigned
        if (launchPoint == null)
        {
            GameObject launchPointObj = new GameObject("LaunchPoint");
            launchPointObj.transform.SetParent(transform);
            launchPointObj.transform.localPosition = Vector3.zero;
            launchPointObj.transform.LookAt(Vector3.zero); // Look toward center court
            launchPoint = launchPointObj.transform;
        }
        
        // Create camera position if not assigned
        if (cameraPosition == null)
        {
            GameObject cameraObj = new GameObject("CameraPosition");
            cameraObj.transform.SetParent(transform);
            // Position camera behind and above the launch point
            cameraObj.transform.localPosition = new Vector3(0, 3, -5);
            cameraObj.transform.LookAt(Vector3.zero); // Look toward hoop
            cameraPosition = cameraObj.transform;
        }
        
        // Create ball spawn point if not assigned
        if (ballSpawnPoint == null)
        {
            GameObject spawnObj = new GameObject("BallSpawnPoint");
            spawnObj.transform.SetParent(launchPoint);
            spawnObj.transform.localPosition = new Vector3(0, 1, 0); // Slightly above launch point
            ballSpawnPoint = spawnObj.transform;
        }
    }
    
    public void Initialize(int index, int totalBalls)
    {
        positionIndex = index;
        ballsShot = 0;
        ballsScored = 0;
        isCompleted = false;
        lastShotScored = false;
        
        // Calculate aim direction toward hoop (assuming hoop is at origin)
        aimDirection = (Vector3.zero - transform.position).normalized;
        
        // Update position marker
        UpdatePositionMarker();
        
        Debug.Log($"Initialized {positionName} at index {index}");
    }
    
    public void ActivatePosition()
    {
        if (isActive) return;
        
        isActive = true;
        ballsShot = 0;
        ballsScored = 0;
        lastShotScored = false;
        
        SetActiveState(true);
        OnPositionActivated?.Invoke(this);
        
        Debug.Log($"Activated position: {positionName}");
    }
    
    public void DeactivatePosition()
    {
        if (!isActive) return;
        
        isActive = false;
        SetActiveState(false);
        
        Debug.Log($"Deactivated position: {positionName}");
    }
    
    public void CompletePosition()
    {
        if (isCompleted) return;
        
        isCompleted = true;
        isActive = false;
        
        SetActiveState(false);
        UpdatePositionMarker();
        
        OnPositionCompleted?.Invoke(this, ballsShot, ballsScored);
        
        Debug.Log($"Completed {positionName}: {ballsScored}/{ballsShot} shots made");
    }
    
    public void RecordShot(bool scored)
    {
        // Only increment shot count if this is a new shot (not updating a previous shot)
        if (!lastShotScored || !scored)
        {
            ballsShot++;
            lastShotScored = scored;
        }
        
        if (scored && !lastShotScored)
        {
            ballsScored++;
            lastShotScored = true;
        }
        
        Debug.Log($"{positionName}: Shot {ballsShot} - " + (scored ? "SCORED!" : "Missed"));
    }
    
    public void RecordShotTaken()
    {
        // Called when shot is taken (before we know if it scored)
        ballsShot++;
        lastShotScored = false;
        
        Debug.Log($"{positionName}: Shot {ballsShot} taken");
    }
    
    public void UpdateLastShotScored()
    {
        // Called when the last shot scores
        if (!lastShotScored)
        {
            ballsScored++;
            lastShotScored = true;
            Debug.Log($"{positionName}: Last shot SCORED! Total: {ballsScored}/{ballsShot}");
        }
    }
    
    public void ResetPosition()
    {
        isActive = false;
        isCompleted = false;
        ballsShot = 0;
        ballsScored = 0;
        lastShotScored = false;
        
        SetActiveState(false);
        UpdatePositionMarker();
        
        Debug.Log($"Reset position: {positionName}");
    }
    
    private void SetActiveState(bool active)
    {
        // Show/hide active indicator
        if (activeIndicator != null)
        {
            activeIndicator.SetActive(active);
        }
        
        // Update position marker color/state
        if (positionMarker != null)
        {
            Renderer markerRenderer = positionMarker.GetComponent<Renderer>();
            if (markerRenderer != null)
            {
                if (active)
                {
                    markerRenderer.material.color = Color.green;
                }
                else if (isCompleted)
                {
                    markerRenderer.material.color = Color.blue;
                }
                else
                {
                    markerRenderer.material.color = positionColor;
                }
            }
        }
    }
    
    private void UpdatePositionMarker()
    {
        if (positionMarker != null)
        {
            Renderer markerRenderer = positionMarker.GetComponent<Renderer>();
            if (markerRenderer != null)
            {
                if (isCompleted)
                {
                    markerRenderer.material.color = Color.blue; // Completed
                }
                else if (isActive)
                {
                    markerRenderer.material.color = Color.green; // Active
                }
                else
                {
                    markerRenderer.material.color = positionColor; // Waiting
                }
            }
        }
    }
    
    public Vector3 GetOptimalAimPoint()
    {
        // Calculate optimal aim point toward the hoop
        Vector3 hoopPosition = Vector3.zero; // Assuming hoop is at origin
        return hoopPosition + optimalAimOffset;
    }
    
    public float GetRecommendedForce()
    {
        return recommendedForceMultiplier;
    }
    
    public string GetPositionInfo()
    {
        return $"{positionName} ({ballsScored}/{ballsShot})";
    }
    
    // Public getters
    public bool IsActive => isActive;
    public bool IsCompleted => isCompleted;
    public int BallsShot => ballsShot;
    public int BallsScored => ballsScored;
    public float ShootingPercentage => ballsShot > 0 ? (float)ballsScored / ballsShot * 100f : 0f;
    
    // Gizmos for editor visualization
    private void OnDrawGizmos()
    {
        // Draw position marker
        Gizmos.color = positionColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // Draw launch point
        if (launchPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(launchPoint.position, Vector3.one * 0.3f);
            
            // Draw aim direction
            Gizmos.color = Color.red;
            Gizmos.DrawRay(launchPoint.position, aimDirection * 2f);
        }
        
        // Draw camera position
        if (cameraPosition != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(cameraPosition.position, Vector3.one * 0.5f);
            
            // Draw camera view direction
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(cameraPosition.position, cameraPosition.forward * 3f);
        }
        
        // Draw distance to hoop
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(transform.position, Vector3.zero);
        
        // Label in scene view
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up, positionName);
        #endif
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw detailed info when selected
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, distanceToHoop);
        
        // Draw shooting arc visualization
        if (launchPoint != null)
        {
            Vector3 start = launchPoint.position;
            Vector3 target = Vector3.zero; // Hoop position
            
            // Simple arc visualization
            for (int i = 0; i <= 10; i++)
            {
                float t = i / 10f;
                Vector3 point = Vector3.Lerp(start, target, t);
                point.y += Mathf.Sin(t * Mathf.PI) * 2f; // Add arc
                
                if (i > 0)
                {
                    Vector3 prevPoint = Vector3.Lerp(start, target, (i - 1) / 10f);
                    prevPoint.y += Mathf.Sin(((i - 1) / 10f) * Mathf.PI) * 2f;
                    
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(prevPoint, point);
                }
            }
        }
    }
}