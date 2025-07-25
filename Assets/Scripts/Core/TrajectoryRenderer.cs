using UnityEngine;

public class TrajectoryRenderer : MonoBehaviour
{
    [Header("Trajectory Settings")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int trajectoryPoints = 50;
    [SerializeField] private float timeStep = 0.1f;
    [SerializeField] private float maxTrajectoryTime = 5f;
    
    [Header("Visual Settings")]
    [SerializeField] private Material trajectoryMaterial;
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Color trajectoryColor = Color.white;
    
    private void Awake()
    {
        // Set up LineRenderer if not assigned
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }
        }
        
        ConfigureLineRenderer();
    }
    
    private void ConfigureLineRenderer()
    {
        lineRenderer.positionCount = trajectoryPoints;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        // REPLACE with this:
        if (lineRenderer.material != null)
        {
            lineRenderer.material.color = trajectoryColor;
        }


        lineRenderer.useWorldSpace = true;
        lineRenderer.sortingOrder = 10; // Render on top
        
        if (trajectoryMaterial != null)
        {
            lineRenderer.material = trajectoryMaterial;
        }
        
        // Start hidden
        lineRenderer.enabled = false;
    }
    
    public void ShowTrajectory(Vector3 startPosition, Vector3 velocity)
    {
        lineRenderer.enabled = true;
        UpdateTrajectory(startPosition, velocity);
    }
    
    public void HideTrajectory()
    {
        lineRenderer.enabled = false;
    }
    
    public void UpdateTrajectory(Vector3 startPosition, Vector3 velocity)
    {
        Vector3[] points = CalculateTrajectoryPoints(startPosition, velocity);
        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);
    }
    
    private Vector3[] CalculateTrajectoryPoints(Vector3 startPosition, Vector3 velocity)
    {
        Vector3[] points = new Vector3[trajectoryPoints];
        Vector3 currentPosition = startPosition;
        Vector3 currentVelocity = velocity;
        
        for (int i = 0; i < trajectoryPoints; i++)
        {
            points[i] = currentPosition;
            
            // Calculate next position using physics
            currentPosition += currentVelocity * timeStep;
            currentVelocity += Physics.gravity * timeStep;
            
            // Stop if trajectory goes too far down (hit ground level)
            if (currentPosition.y < -2f)
            {
                // Resize array to actual points used
                Vector3[] finalPoints = new Vector3[i + 1];
                System.Array.Copy(points, finalPoints, i + 1);
                return finalPoints;
            }
            
            // Stop if trajectory time exceeds maximum
            if (i * timeStep > maxTrajectoryTime)
            {
                Vector3[] finalPoints = new Vector3[i + 1];
                System.Array.Copy(points, finalPoints, i + 1);
                return finalPoints;
            }
        }
        
        return points;
    }
    
    public Vector3 GetTrajectoryEndPoint(Vector3 startPosition, Vector3 velocity)
    {
        Vector3[] points = CalculateTrajectoryPoints(startPosition, velocity);
        return points[points.Length - 1];
    }
}