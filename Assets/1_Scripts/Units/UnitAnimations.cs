using UnityEngine;

/// <summary>
/// Handles all unit animations by interfacing with the Animator component.
/// This script should be attached to units that have an Animator component.
/// </summary>
public class UnitAnimations : MonoBehaviour
{
    private Animator animator;
    private Unit unit;
    
    // Animation state names (these should match the state names in your Animator Controller)
    private const string STATE_IDLE = "Idle";
    private const string STATE_ATTACK = "Attack";
    private const string STATE_HURT = "Hurt";
    private const string STATE_DEAD = "Dead";
    
    private bool shouldReturnToIdle = false;
    private string currentAnimationState = STATE_IDLE;
    
    void Start()
    {
        // Get Animator component
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"UnitAnimations on {gameObject.name}: No Animator component found. Animations will not work.");
        }
        
        // Get Unit component for state tracking
        unit = GetComponent<Unit>();
        
        // Initialize animation state to Idle
        if (animator != null)
        {
            PlayIdleAnimation();
        }
    }
    
    void Update()
    {
        if (animator == null)
            return;
        
        // Check if we're in Dead state and animation has finished
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(STATE_DEAD) && stateInfo.normalizedTime >= 1.0f)
        {
            // Freeze the animator on the last frame of Dead animation
            animator.speed = 0f;
            shouldReturnToIdle = false;
            return;
        }
        
        // For other animations that should return to idle
        if (!shouldReturnToIdle)
            return;
        
        // If we're playing one of the animations that should return to idle
        if (stateInfo.IsName(currentAnimationState))
        {
            // Check if animation has finished (normalizedTime >= 1 means it's done)
            if (stateInfo.normalizedTime >= 1.0f)
            {
                // Attack and Hurt always return to Idle
                PlayIdleAnimation();
            }
        }
    }
    
    /// <summary>
    /// Plays the Idle animation
    /// </summary>
    public void PlayIdleAnimation()
    {
        if (animator != null)
        {
            animator.Play(STATE_IDLE);
            currentAnimationState = STATE_IDLE;
            shouldReturnToIdle = false;
        }
    }
    
    /// <summary>
    /// Plays the attack animation
    /// </summary>
    public void PlayAttackAnimation()
    {
        if (animator != null)
        {
            animator.Play(STATE_ATTACK);
            currentAnimationState = STATE_ATTACK;
            shouldReturnToIdle = true;
        }
    }
    
    /// <summary>
    /// Plays the hurt animation (when taking damage)
    /// </summary>
    public void PlayHurtAnimation()
    {
        if (animator != null)
        {
            animator.Play(STATE_HURT);
            currentAnimationState = STATE_HURT;
            shouldReturnToIdle = true;
        }
    }
    
    /// <summary>
    /// Plays the death animation
    /// </summary>
    public void PlayDeadAnimation()
    {
        if (animator != null)
        {
            animator.Play(STATE_DEAD);
            currentAnimationState = STATE_DEAD;
            shouldReturnToIdle = false; // Dead animation never returns to Idle
        }
    }
    
    /// <summary>
    /// Gets the Animator component (for external access if needed)
    /// </summary>
    public Animator GetAnimator()
    {
        return animator;
    }
    
    /// <summary>
    /// Checks if the animator is currently playing a specific animation state
    /// </summary>
    public bool IsPlayingState(string stateName, int layerIndex = 0)
    {
        if (animator != null)
        {
            return animator.GetCurrentAnimatorStateInfo(layerIndex).IsName(stateName);
        }
        return false;
    }
    
    /// <summary>
    /// Gets the normalized time of the current animation state
    /// </summary>
    public float GetCurrentAnimationTime(int layerIndex = 0)
    {
        if (animator != null)
        {
            return animator.GetCurrentAnimatorStateInfo(layerIndex).normalizedTime;
        }
        return 0f;
    }
}
