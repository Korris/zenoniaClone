using Assets.Script;
using System.Collections;
using UnityEngine;

public class NewPlayer : Character, IAttacker
{
    [Header("Player Attack Settings")]
    public int baseDamage = 25;
    public float attackCooldown = 0.5f;
    public Transform attackPoint;
    public float attackRange = 1.5f;
    public LayerMask enemyLayers;

    // Player specific properties
    private bool canAttack = true;
    private Vector2 moveDirection;
    private Vector2 lastMoveDirection = Vector2.right; // Default facing right

    [Header("Player Specific Settings")]
    public float dashSpeed = 10f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private bool canDash = true;
    private bool isDashing = false;

    private float doubleTapTime = 0.25f;
    private float lastKeyPressedTime;

    private KeyCode lastKeyPressed;

    public int AttackDamage => baseDamage;

    protected override void Start()
    {
        base.Start();
        if (attackPoint == null)
        {
            // Create attack point if not assigned
            GameObject attackPointObj = new GameObject("AttackPoint");
            attackPointObj.transform.parent = transform;
            attackPointObj.transform.localPosition = Vector3.right * 0.7f;
            attackPoint = attackPointObj.transform;
        }
    }

    private void Update()
    {
        if (isDead || isKnockedBack || isDashing) return;

        // Handle player input
        HandleMovementInput();
        HandleAttackInput();
        HandleDashInput();
    }

    private void FixedUpdate()
    {
        if (isDead || isKnockedBack || isDashing) return;

        // Apply movement
        Move();
    }

    private void HandleMovementInput()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");





        moveDirection = new Vector2(moveX, moveY).normalized;

        // Update the last move direction for attack orientation
        if (moveDirection != Vector2.zero)
        {
            lastMoveDirection = moveDirection;
            UpdateAnimationState(EnumAnimation.Walk);
            animator.SetFloat("moveX", moveX);
            animator.SetFloat("moveY", moveY);
        }
        else
        {
            UpdateAnimationState(EnumAnimation.Idle);
        }

        // Flip sprite based on movement direction
        if (moveX != 0 && spriteRenderer != null)
        {
            spriteRenderer.flipX = (moveX < 0);
        }
    }

    private void HandleAttackInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && canAttack)
        {
            Attack();
        }
    }

    private void HandleDashInput()
    {


        // Dash bằng double arrow key (hoặc WASD)
        KeyCode key = KeyCode.None;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) key = KeyCode.RightArrow;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) key = KeyCode.LeftArrow;
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) key = KeyCode.UpArrow;
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) key = KeyCode.DownArrow;

        if (key != KeyCode.None)
        {
            //if (lastKeyPressed == key && Time.time - lastKeyPressedTime <= doubleTapTime && canDash)
            //{
            //    Vector2 dashDir = key switch
            //    {
            //        KeyCode.RightArrow or KeyCode.D => Vector2.right,
            //        KeyCode.LeftArrow or KeyCode.A => Vector2.left,
            //        KeyCode.UpArrow or KeyCode.W => Vector2.up,
            //        KeyCode.DownArrow or KeyCode.S => Vector2.down,
            //        _ => Vector2.right
            //    };

            //    StartCoroutine(Dash(dashDir));
            //}
            if (lastKeyPressed == key && Time.time - lastKeyPressedTime <= doubleTapTime && canDash)
            {
                StartCoroutine(Dash());
            }
            lastKeyPressed = key;
            lastKeyPressedTime = Time.time;
        }
    }


    private void Move()
    {
        if (rb != null)
        {
            rb.linearVelocity = moveDirection * moveSpeed;
        }
    }

    public void Attack()
    {
        if (!canAttack) return;
        UpdateAnimationState(EnumAnimation.Attack);

        //// Position the attack point in the direction of movement or last movement
        //if (attackPoint != null)
        //{
        //    attackPoint.localPosition = lastMoveDirection * 0.7f;
        //}

        // Detect enemies in range
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        Debug.Log($"dm hitEnemies {hitEnemies.Length}");

        // Apply damage to enemies
        foreach (Collider2D enemy in hitEnemies)
        {
            IDamageable damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(AttackDamage, transform.position);
            }
        }

        // Start cooldown
        StartCoroutine(AttackCooldown());
    }

    private IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;

        // Store original speed
        float originalSpeed = moveSpeed;

        // Apply dash speed
        Vector2 dashDirection = lastMoveDirection != Vector2.zero ?
                                lastMoveDirection : Vector2.right * (spriteRenderer.flipX ? -1 : 1);

        rb.linearVelocity = dashDirection * dashSpeed;

        // Make player briefly invulnerable during dash
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), true);

        yield return new WaitForSeconds(dashDuration);

        // Reset
        isDashing = false;
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), false);

        yield return new WaitForSeconds(dashCooldown - dashDuration);
        canDash = true;
    }

    // For debug visualization
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}