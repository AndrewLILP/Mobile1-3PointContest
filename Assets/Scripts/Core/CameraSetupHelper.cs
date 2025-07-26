using UnityEngine;

[System.Serializable]
public class CameraSetupData
{
    public string positionName;
    public Vector3 cameraPosition;
    public Vector3 cameraRotation;
}

public class CameraSetupHelper : MonoBehaviour
{
    [Header("Hoop Settings")]
    [SerializeField] private Vector3 hoopPosition = new Vector3(0, 3.05f, 0);
    
    [Header("Camera Setup Data")]
    [SerializeField] private CameraSetupData[] cameraSetups = new CameraSetupData[]
    {
        new CameraSetupData 
        { 
            positionName = "Left Corner", 
            cameraPosition = new Vector3(-8f, 4f, -6f), 
            cameraRotation = new Vector3(15f, 25f, 0f) 
        },
        new CameraSetupData 
        { 
            positionName = "Left Wing", 
            cameraPosition = new Vector3(-6f, 4f, -8f), 
            cameraRotation = new Vector3(15f, 15f, 0f) 
        },
        new CameraSetupData 
        { 
            positionName = "Top Key", 
            cameraPosition = new Vector3(0f, 4f, -10f), 
            cameraRotation = new Vector3(15f, 0f, 0f) 
        },
        new CameraSetupData 
        { 
            positionName = "Right Wing", 
            cameraPosition = new Vector3(6f, 4f, -8f), 
            cameraRotation = new Vector3(15f, -15f, 0f) 
        },
        new CameraSetupData 
        { 
            positionName = "Right Corner", 
            cameraPosition = new Vector3(8f, 4f, -6f), 
            cameraRotation = new Vector3(15f, -25f, 0f) 
        }
    };
    
    [Header("Auto Setup")]
    [SerializeField] private ShootingPosition[] shootingPositions;
    [SerializeField] private bool autoSetupOnStart = false;
    
    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupAllCameras();
        }
    }
    
    [ContextMenu("Setup All Cameras")]
    public void SetupAllCameras()
    {
        if (shootingPositions == null || shootingPositions.Length == 0)
        {
            // Try to find shooting positions
            shootingPositions = FindObjectsByType<ShootingPosition>(FindObjectsSortMode.None);
        }
        
        for (int i = 0; i < shootingPositions.Length && i < cameraSetups.Length; i++)
        {
            SetupCameraForPosition(shootingPositions[i], cameraSetups[i]);
        }
        
        Debug.Log($"Set up cameras for {shootingPositions.Length} positions");
    }
    
    private void SetupCameraForPosition(ShootingPosition position, CameraSetupData setupData)
    {
        if (position == null || position.cameraPosition == null) return;
        
        // Set camera position and rotation
        position.cameraPosition.position = setupData.cameraPosition;
        position.cameraPosition.rotation = Quaternion.Euler(setupData.cameraRotation);
        
        // Make sure camera looks toward hoop
        Vector3 directionToHoop = (hoopPosition - position.cameraPosition.position).normalized;
        position.cameraPosition.LookAt(hoopPosition);
        
        // Apply slight downward angle for better shooting view
        position.cameraPosition.Rotate(15f, 0f, 0f);
        
        Debug.Log($"Set up camera for {position.positionName} at {setupData.cameraPosition}");
    }
    
    [ContextMenu("Auto-Calculate Camera Positions")]
    public void AutoCalculateCameraPositions()
    {
        if (shootingPositions == null || shootingPositions.Length == 0)
        {
            shootingPositions = FindObjectsByType<ShootingPosition>(FindObjectsSortMode.None);
        }
        
        for (int i = 0; i < shootingPositions.Length; i++)
        {
            AutoSetupCameraForPosition(shootingPositions[i]);
        }
        
        Debug.Log("Auto-calculated all camera positions");
    }
    
    private void AutoSetupCameraForPosition(ShootingPosition position)
    {
        if (position == null || position.cameraPosition == null) return;
        
        Vector3 launchPos = position.transform.position;
        
        // Calculate camera position: behind and above the launch point
        Vector3 directionFromHoop = (launchPos - hoopPosition).normalized;
        Vector3 cameraPos = launchPos + (directionFromHoop * 3f); // 3 units back
        cameraPos.y = 4f; // 4 units high
        
        // Set camera position
        position.cameraPosition.position = cameraPos;
        
        // Make camera look at a point slightly above the hoop
        Vector3 lookTarget = hoopPosition + Vector3.up * 0.5f;
        position.cameraPosition.LookAt(lookTarget);
        
        Debug.Log($"Auto-calculated camera for {position.positionName}");
    }
    
    [ContextMenu("Test Camera Views")]
    public void TestCameraViews()
    {
        if (shootingPositions == null) return;
        
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;
        
        StartCoroutine(CycleThroughCameras(mainCamera));
    }
    
    private System.Collections.IEnumerator CycleThroughCameras(Camera camera)
    {
        foreach (var position in shootingPositions)
        {
            if (position == null || position.cameraPosition == null) continue;
            
            Debug.Log($"Testing camera view for {position.positionName}");
            
            // Move camera to position
            camera.transform.position = position.cameraPosition.position;
            camera.transform.rotation = position.cameraPosition.rotation;
            
            // Wait for 2 seconds
            yield return new WaitForSeconds(2f);
        }
        
        Debug.Log("Camera test complete");
    }
    
    private void OnDrawGizmos()
    {
        // Draw hoop position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(hoopPosition, 0.5f);
        
        // Draw camera positions and view lines
        if (shootingPositions != null)
        {
            foreach (var position in shootingPositions)
            {
                if (position == null || position.cameraPosition == null) continue;
                
                // Draw camera position
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(position.cameraPosition.position, Vector3.one * 0.5f);
                
                // Draw view line to hoop
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(position.cameraPosition.position, hoopPosition);
            }
        }
    }
}