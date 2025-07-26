using UnityEngine;
using System.Collections;

public class Hoop : MonoBehaviour
{
    [Header("Hoop Settings")]
    [SerializeField] private Transform hoopRim;
    [SerializeField] private Transform hoopCenter;
    [SerializeField] private float swishZoneRadius = 0.3f;
    
    [Header("Scoring")]
    [SerializeField] private int swishPoints = 300;
    [SerializeField] private int normalShotPoints = 200;
    [SerializeField] private int bankShotPoints = 150;
    
    [Header("Audio")]
    [SerializeField] private AudioClip swishSound;
    [SerializeField] private AudioClip normalShotSound;
    [SerializeField] private AudioClip rimHitSound;
    [SerializeField] private AudioClip backboardSound;
    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem swishEffect;
    [SerializeField] private ParticleSystem scoreEffect;
    [SerializeField] private GameObject scoreTextPrefab;
    
    [Header("Detection")]
    [SerializeField] private LayerMask ballLayerMask = -1;
    
    // Components
    private AudioSource audioSource;
    private BallLauncher ballLauncher;
    
    // State tracking
    private bool ballHitRim = false;
    private bool ballHitBackboard = false;
    private bool shotScored = false;
    
    // Events
    public System.Action<int, bool> OnScore; // points, isSwish
    public System.Action OnMiss;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        ballLauncher = FindFirstObjectByType<BallLauncher>();
        
        SetupColliders();
    }
    
    private void SetupColliders()
    {
        // Ensure the main hoop trigger exists
        Collider hoopCollider = GetComponent<Collider>();
        if (hoopCollider == null)
        {
            hoopCollider = gameObject.AddComponent<SphereCollider>();
        }
        hoopCollider.isTrigger = true;
        
        // Set up rim detection (for rim hits)
        if (hoopRim != null)
        {
            Collider rimCollider = hoopRim.GetComponent<Collider>();
            if (rimCollider == null)
            {
                rimCollider = hoopRim.gameObject.AddComponent<CapsuleCollider>();
            }
            
            // Add RimDetector component
            RimDetector rimDetector = hoopRim.GetComponent<RimDetector>();
            if (rimDetector == null)
            {
                rimDetector = hoopRim.gameObject.AddComponent<RimDetector>();
                rimDetector.parentHoop = this;
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if it's a basketball
        if (!IsBall(other.gameObject)) return;
        
        // Check if ball is coming from above (valid shot)
        Rigidbody ballRb = other.GetComponent<Rigidbody>();
        if (ballRb == null) return;
        
        Vector3 ballVelocity = ballRb.linearVelocity;
        
        // Ball must be moving downward to count as a score
        if (ballVelocity.y > -1f) return;
        
        // Check if ball passed through the center area (swish zone)
        float distanceFromCenter = Vector3.Distance(other.transform.position, hoopCenter.position);
        bool isSwish = distanceFromCenter <= swishZoneRadius && !ballHitRim && !ballHitBackboard;
        
        // Score the shot
        ScoreShot(isSwish);
    }
    
    private void ScoreShot(bool isSwish)
    {
        if (shotScored) return; // Prevent double scoring
        
        shotScored = true;
        
        int points = CalculatePoints(isSwish);
        
        // Audio feedback
        PlayShotSound(isSwish);
        
        // Visual effects
        PlayVisualEffects(isSwish, points);
        
        // Notify listeners
        OnScore?.Invoke(points, isSwish);
        
        // Complete level
        if (ballLauncher != null)
        {
            ballLauncher.CompleteLevel();
        }
        
        Debug.Log($"SCORE! {points} points - {(isSwish ? "SWISH!" : "Good shot!")}");
    }
    
    private int CalculatePoints(bool isSwish)
    {
        if (isSwish)
        {
            return swishPoints;
        }
        else if (ballHitBackboard && !ballHitRim)
        {
            return bankShotPoints; // Bank shot
        }
        else
        {
            return normalShotPoints;
        }
    }
    
    private void PlayShotSound(bool isSwish)
    {
        AudioClip soundToPlay = isSwish ? swishSound : normalShotSound;
        
        if (soundToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(soundToPlay);
        }
    }
    
    private void PlayVisualEffects(bool isSwish, int points)
    {
        // Particle effects
        if (isSwish && swishEffect != null)
        {
            swishEffect.Play();
        }
        else if (scoreEffect != null)
        {
            scoreEffect.Play();
        }
        
        // Score text
        if (scoreTextPrefab != null)
        {
            GameObject scoreText = Instantiate(scoreTextPrefab, hoopCenter.position + Vector3.up, Quaternion.identity);
            
            // Try to set the text (assuming it has a Text or TextMesh component)
            var textComponent = scoreText.GetComponentInChildren<UnityEngine.UI.Text>();
            if (textComponent != null)
            {
                textComponent.text = $"+{points}";
                if (isSwish) textComponent.text += "\nSWISH!";
            }
            
            // Auto-destroy after 2 seconds
            Destroy(scoreText, 2f);
        }
    }
    
    public void OnRimHit(GameObject ball)
    {
        if (!IsBall(ball)) return;
        
        ballHitRim = true;
        
        // Play rim hit sound
        if (rimHitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(rimHitSound, 0.7f);
        }
        
        Debug.Log("Ball hit rim");
    }
    
    public void OnBackboardHit(GameObject ball)
    {
        if (!IsBall(ball)) return;
        
        ballHitBackboard = true;
        
        // Play backboard sound
        if (backboardSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(backboardSound, 0.5f);
        }
        
        Debug.Log("Ball hit backboard");
    }
    
    private bool IsBall(GameObject obj)
    {
        // Check if object is on the ball layer or has "Ball" tag
        return obj.CompareTag("Ball") || (ballLayerMask.value & (1 << obj.layer)) != 0;
    }
    
    public void ResetHoop()
    {
        ballHitRim = false;
        ballHitBackboard = false;
        shotScored = false;
    }
    
    // For debugging - visualize the swish zone
    private void OnDrawGizmosSelected()
    {
        if (hoopCenter != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(hoopCenter.position, swishZoneRadius);
        }
        
        if (hoopRim != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(hoopRim.position, Vector3.one * 0.1f);
        }
    }
}

// Helper component for rim detection
public class RimDetector : MonoBehaviour
{
    [System.NonSerialized]
    public Hoop parentHoop;
    
    private void OnCollisionEnter(Collision collision)
    {
        if (parentHoop != null)
        {
            parentHoop.OnRimHit(collision.gameObject);
        }
    }
}

// Helper component for backboard detection
public class BackboardDetector : MonoBehaviour
{
    [System.NonSerialized]
    public Hoop parentHoop;
    
    private void OnCollisionEnter(Collision collision)
    {
        if (parentHoop != null)
        {
            parentHoop.OnBackboardHit(collision.gameObject);
        }
    }
}