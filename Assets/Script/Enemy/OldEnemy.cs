using System.Collections;
using UnityEngine;

public class OldEnemy : MonoBehaviour
{
    [Header("Chase Settings")]
    public Transform player;
    public float chaseSpeed = 3.0f;
    public float detectionRange = 5.0f;
    public float stopDistance = 0.5f;

    [Header("Health and Damage Settings")]
    public int maxHealth = 100;
    public float knockbackForce = 10f;
    public float knockbackDuration = 0.25f;
    public Color hitColor = Color.red;
    public float flashDuration = 0.15f;

    private int currentHealth;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Vector2 movement;
    private Color originalColor;
    private bool isKnockedBack = false;
    public GameObject damagePopupPrefab; // Drag prefab vào trong Unity Inspector
    private bool isDead = false;  // Add this field to track death state

    void Start()
    {
        // Get component references
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        originalColor = spriteRenderer.color;

        // Initialize health
        currentHealth = maxHealth;

        // If player wasn't assigned in inspector, try to find it in the scene
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null)
            {
                Debug.LogError("Player not found! Tag a GameObject as 'Player' or assign it in the inspector.");
            }
        }

        // Ensure the rigidbody is set up correctly
        if (rb != null)
        {
            rb.gravityScale = 0f; // Disable gravity for top-down movement
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Prevent rotation
        }
        else
        {
            Debug.LogError("Rigidbody2D component missing from enemy!");
        }
    }

    void Update()
    {
        if (player == null || isKnockedBack) return;

        // Calculate distance to player
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Check if player is within detection range
        if (distanceToPlayer <= detectionRange)
        {
            // Calculate direction to player
            Vector2 direction = (player.position - transform.position).normalized;

            // Flip sprite based on direction
            if (direction.x > 0)
            {
                spriteRenderer.flipX = false;
            }
            else if (direction.x < 0)
            {
                spriteRenderer.flipX = true;
            }

            // Only move if not too close to player
            if (distanceToPlayer > stopDistance)
            {
                // Store movement for use in FixedUpdate
                movement = direction;
            }
            else
            {
                movement = Vector2.zero;
            }
        }
        else
        {
            // Player out of range
            movement = Vector2.zero;
        }
    }

    // Use FixedUpdate for physics-based movement
    void FixedUpdate()
    {
        Debug.Log("move start");
        // Don't move if knocked back
        if (isKnockedBack || isDead) return;

        // Move the enemy using the calculated movement vector
        if (rb != null && movement != Vector2.zero)
        {
            Debug.Log("move on");

            rb.MovePosition(rb.position + movement * chaseSpeed * Time.fixedDeltaTime);
        }
    }


    private void ShowDamagePopup(int damage)
    {
        Debug.Log("show dam ne ");
        var popup = Instantiate(damagePopupPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
        popup.GetComponent<DamagePopup>().Setup(damage);
    }

    /// <summary>
    /// Apply damage to the enemy and knock it back
    /// </summary>
    /// <param name="damage">Amount of damage to deal</param>
    /// <param name="knockbackSource">Position from which the knockback originates</param>
    public void TakeDamage(int damage, Vector2 knockbackSource)
    {
        // Reduce health
        currentHealth -= damage;

        // Visual feedback
        StartCoroutine(FlashColor());

        // Apply knockback
        StartCoroutine(ApplyKnockback(knockbackSource));

        // Check if enemy is defeated
        if (currentHealth <= 0)
        {
            Die();
        }

        // Debug info
        Debug.Log($"Enemy took {damage} damage. Health: {currentHealth}/{maxHealth}");
        ShowDamagePopup(damage);
    }

    // Overload for when no knockback source is specified
    public void TakeDamage(int damage)
    {
        // If player exists, use their position as knockback source
        if (player != null)
        {
            TakeDamage(damage, player.position);
            Debug.Log("show dam");
        }
        else
        {
            Debug.Log("méo thấy người");

            // Just reduce health without knockback
            currentHealth -= damage;
            StartCoroutine(FlashColor());

            if (currentHealth <= 0)
            {
                Die();
            }
        }
    }

    private IEnumerator FlashColor()
    {
        // Change to hit color
        spriteRenderer.color = hitColor;

        // Wait for flash duration
        yield return new WaitForSeconds(flashDuration);

        // Return to original color
        spriteRenderer.color = originalColor;
    }

    private IEnumerator ApplyKnockback(Vector2 source)
    {
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

    private void Die()
    {
        Debug.Log("Enemy defeated!");

        // Set dead flag to stop all behavior
        isDead = true;

        // Reset movement
        movement = Vector2.zero;

        // Stop movement and physics
        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = true;

        // Disable any colliders
        if (GetComponent<Collider2D>() != null)
        {
            GetComponent<Collider2D>().enabled = false;
        }

        // Flip along Y axis for death animation
        Vector3 currentScale = transform.localScale;
        transform.localScale = new Vector3(currentScale.x, -currentScale.y, currentScale.z);

        // Destroy the game object after 2 seconds
        Destroy(gameObject, 2f);
    }

    // Visualize the detection range in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}