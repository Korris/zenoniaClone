using Assets.Script;
using System.Collections;
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int damage);
    void TakeDamage(int damage, Vector2 knockbackSource);
    bool IsDead { get; }
}

// 2. Interface cho đối tượng có thể tấn công
public interface IAttacker
{
    void Attack();
    int AttackDamage { get; }
}


public abstract class Character : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public float knockbackForce = 10f;
    public float knockbackDuration = 0.25f;
    public Color hitColor = Color.red;
    public float flashDuration = 0.15f;
    public GameObject damagePopupPrefab;

    [Header("Movement Settings")]
    public float moveSpeed = 3.0f;

    // Properties
    protected int currentHealth;
    protected SpriteRenderer spriteRenderer;
    protected Rigidbody2D rb;
    protected Animator animator;
    protected Color originalColor;
    protected bool isKnockedBack = false;
    protected bool isDead = false;

    // Animation parameter names - có thể override nếu cần
    public bool IsDead => isDead;

    protected virtual void Awake()
    {
        // Get component references
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Setup rigidbody
        if (rb != null)
        {
            rb.gravityScale = 0f; // Disable gravity for top-down movement
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Prevent rotation
        }
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;
    }

    public virtual void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position);
    }

    public virtual void TakeDamage(int damage, Vector2 knockbackSource)
    {
        if (isDead) return;

        // Reduce health
        currentHealth -= damage;

        // Visual feedback
        StartCoroutine(FlashColor());

        // Apply knockback
        StartCoroutine(ApplyKnockback(knockbackSource));

        // Show damage popup
        ShowDamagePopup(damage);

        // Check if character is defeated
        if (currentHealth <= 0)
        {
            Die();
        }

        // Debug info
        Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");
    }

    protected virtual void ShowDamagePopup(int damage)
    {
        if (damagePopupPrefab != null)
        {
            var popup = Instantiate(damagePopupPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
            popup.GetComponent<DamagePopup>().Setup(damage);
        }
    }

    protected virtual IEnumerator FlashColor()
    {
        if (spriteRenderer != null)
        {
            // Change to hit color
            spriteRenderer.color = hitColor;

            // Wait for flash duration
            yield return new WaitForSeconds(flashDuration);

            // Return to original color
            spriteRenderer.color = originalColor;
        }
    }

    protected virtual IEnumerator ApplyKnockback(Vector2 source)
    {
        if (rb == null) yield break;

        isKnockedBack = true;

        // Calculate knockback direction (away from source)
        Vector2 knockbackDirection = ((Vector2)transform.position - source).normalized;

        // Apply force
        rb.linearVelocity = Vector2.zero; // Reset current velocity
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

        // Wait for knockback duration
        yield return new WaitForSeconds(knockbackDuration);

        // Reset velocity and end knockback state
        rb.linearVelocity = Vector2.zero;
        isKnockedBack = false;
    }

    // Update animation state - shared method
    protected virtual void UpdateAnimationState(string animState)
    {
        if (animator != null)
        {
            animator.SetBool(EnumAnimation.Idle, animState == EnumAnimation.Idle);
            animator.SetBool(EnumAnimation.Walk, animState == EnumAnimation.Walk);
            animator.SetBool(EnumAnimation.Attack, animState == EnumAnimation.Attack);
            animator.SetBool(EnumAnimation.Die, animState == EnumAnimation.Die);
        }
    }

    protected virtual void Die()
    {
        if (isDead) return;

        Debug.Log($"{gameObject.name} has died!");

        // Set dead flag to stop all behavior
        isDead = true;

        // Stop movement and physics
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Disable any colliders
        if (GetComponent<Collider2D>() != null)
        {
            GetComponent<Collider2D>().enabled = false;
        }

        // Play death animation
        UpdateAnimationState(EnumAnimation.Die);
    }
}
