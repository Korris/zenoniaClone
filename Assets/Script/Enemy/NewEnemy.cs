
using Assets.Script;
using System.Collections;
using UnityEngine;

public class NewEnemy : Character, IAttacker
{
    [Header("Enemy Attack Settings")]
    public int attackDamage = 15;
    public float attackCooldown = 1.5f;
    public float detectionRange = 7f;
    public float attackRange = 1.2f;
    public LayerMask playerLayer;

    // Enemy specific properties
    private Transform player;
    private bool canAttack = true;
    private Vector2 moveDirection;
    private EnemyState currentState = EnemyState.Idle;

    // Enemy States
    private enum EnemyState
    {
        Idle,
        Chasing,
        Attacking,
        Stunned
    }

    public int AttackDamage => attackDamage;

    protected override void Start()
    {
        base.Start();

        // Find the player
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Start behavior routine
        StartCoroutine(EnemyBehavior());
    }

    private void FixedUpdate()
    {
        if (isDead || isKnockedBack || currentState == EnemyState.Stunned || player == null) return;

        UpdateFacing();

        // Only move in chase state
        if (currentState == EnemyState.Chasing)
        {
            Move();
        }
    }

    private IEnumerator EnemyBehavior()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(0.1f); // Small delay to avoid performance hit

            if (isKnockedBack || currentState == EnemyState.Stunned || player == null)
            {
                continue;
            }

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            // State transitions
            if (distanceToPlayer <= attackRange && canAttack)
            {
                currentState = EnemyState.Attacking;
                Attack();
            }
            else if (distanceToPlayer <= detectionRange)
            {
                currentState = EnemyState.Chasing;
                UpdateAnimationState(EnumAnimation.Walk);
            }
            else
            {
                currentState = EnemyState.Idle;
                if (rb != null) rb.linearVelocity = Vector2.zero;
                UpdateAnimationState(EnumAnimation.Idle);
            }
        }
    }

    private void UpdateFacing()
    {
        if (player != null && spriteRenderer != null)
        {
            // Face toward player
            spriteRenderer.flipX = transform.position.x > player.position.x;
        }
    }

    private void Move()
    {
        if (rb != null && player != null)
        {
            // Move toward player
            moveDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = moveDirection * moveSpeed;
        }
    }

    public void Attack()
    {
        if (!canAttack || player == null) return;

        UpdateAnimationState(EnumAnimation.Attack);

        // Check if player is still in range (might have moved)
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, attackRange, playerLayer);

        if (playerCollider != null)
        {
            IDamageable damageable = playerCollider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Attack with knockback from enemy position
                damageable.TakeDamage(AttackDamage, transform.position);
            }
        }

        // Start cooldown
        StartCoroutine(AttackCooldown());
    }

    public void Stun(float duration)
    {
        StartCoroutine(ApplyStun(duration));
    }

    private IEnumerator ApplyStun(float duration)
    {
        EnemyState previousState = currentState;
        currentState = EnemyState.Stunned;

        // Stop movement
        if (rb != null) rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(duration);

        // Return to previous state
        currentState = previousState;
    }

    private IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    protected override void Die()
    {
        base.Die();

        // Additional enemy-specific death behavior
        StopAllCoroutines();

        // Maybe drop item here

        // Destroy after animation finishes
        Destroy(gameObject, 2f);
    }

    // For debug visualization
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    // Take additional damage from specific damage types
    public void TakeDamageWithType(int damage, DamageType damageType, Vector2 knockbackSource)
    {
        // Apply damage modifiers based on type
        float modifier = 1f;
        switch (damageType)
        {
            case DamageType.Fire:
                modifier = 1.5f;
                StartCoroutine(ApplyBurningEffect());
                break;
            case DamageType.Ice:
                modifier = 1.2f;
                Stun(1f); // Ice damage also stuns
                break;
            case DamageType.Lightning:
                modifier = 1.3f;
                break;
            default:
                modifier = 1f;
                break;
        }

        int modifiedDamage = Mathf.RoundToInt(damage * modifier);
        TakeDamage(modifiedDamage, knockbackSource);
    }

    private IEnumerator ApplyBurningEffect()
    {
        int burnTicks = 3;
        int burnDamagePerTick = 5;

        for (int i = 0; i < burnTicks; i++)
        {
            yield return new WaitForSeconds(0.5f);
            if (!isDead)
            {
                TakeDamage(burnDamagePerTick);
            }
        }
    }
}

// Optional: Damage type enum for elemental attacks
public enum DamageType
{
    Normal,
    Fire,
    Ice,
    Lightning
}