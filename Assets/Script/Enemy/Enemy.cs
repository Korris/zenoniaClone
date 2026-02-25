using System.Collections;
using UnityEngine;

public class Enemy : Character
{
    public enum EnemyState { Idle, Patrol, Chase }

    [Header("State Settings")]
    public EnemyState currentState = EnemyState.Patrol;
    public float idleTime = 1.0f;

    [Header("Chase Settings")]
    public Transform player;
    public float chaseSpeed = 3.0f;
    public float detectionRange = 5.0f;
    public float stopDistance = 0.5f;

    [Header("Patrol Settings")]
    public float patrolSpeed = 1.5f;
    public float patrolDistance = 3.0f;
    public bool useRandomPatrol = false;

    [Header("Avoidance Settings")]
    public float avoidanceRadius = 1.0f;
    public float avoidanceWeight = 1.5f;
    public LayerMask enemyLayer;

    [Header("Animation Settings")]
    public float directionThreshold = 0.5f;
    public float flipCooldown = 0.5f;

    // Spawn settings - NonSerialized để Unity luôn dùng giá trị code, không bị serialize = 0
    [System.NonSerialized] public int spawnExtra = 3;
    [System.NonSerialized] public Vector2 spawnAreaSize = new Vector2(10f, 10f);

    private Vector2 movement;
    private float lastFlipTime = 0f;
    private bool facingRight = true;
    private bool isClone = false;

    // Patrol state
    private Vector2 startPosition;
    private Vector2 currentPatrolTarget;
    private float patrolTimer = 0f;
    private bool isWaiting = false;

    protected override void Awake()
    {
        base.Awake();

        // Disable EnemyPathFinding vì Enemy.cs đã handle toàn bộ movement
        var pathFinding = GetComponent<EnemyPathFinding>();
        if (pathFinding != null) pathFinding.enabled = false;

        // Xóa CompositeCollider2D nếu có (component cho Tilemap, gây conflict trên enemy)
        var composite = GetComponent<CompositeCollider2D>();
        if (composite != null) Destroy(composite);
    }

    protected override void Start()
    {
        base.Start();
        InitPlayer();
        InitEnemyLayer();

        facingRight = !spriteRenderer.flipX;
        startPosition = transform.position;
        SetNewPatrolTarget();

        // Enemy gốc tự clone thêm N con khi game bắt đầu
        if (!isClone && spawnExtra > 0)
            SpawnClones();
    }

    private void InitPlayer()
    {
        if (player != null) return;
        Player playerScript = FindFirstObjectByType<Player>();
        if (playerScript != null) player = playerScript.transform;
    }

    private void InitEnemyLayer()
    {
        if (enemyLayer == 0)
            enemyLayer = 1 << gameObject.layer;
    }

    private void SpawnClones()
    {
        for (int i = 0; i < spawnExtra; i++)
        {
            float x = transform.position.x + Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
            float y = transform.position.y + Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);

            GameObject clone = Instantiate(gameObject, new Vector3(x, y, transform.position.z), Quaternion.identity);
            clone.name = $"Enemy_{i + 2}";

            Enemy cloneEnemy = clone.GetComponent<Enemy>();
            cloneEnemy.isClone = true;
            cloneEnemy.spawnExtra = 0;
            cloneEnemy.useRandomPatrol = true;
            cloneEnemy.currentState = EnemyState.Patrol;
            cloneEnemy.player = this.player;
        }

        useRandomPatrol = true;
        Debug.Log($"Spawned {spawnExtra} extra enemies ({spawnExtra + 1} total)");
    }

    // --- State Machine ---

    void Update()
    {
        if (isKnockedBack || isDead) return;

        switch (currentState)
        {
            case EnemyState.Idle:   UpdateIdleState();   break;
            case EnemyState.Patrol: UpdatePatrolState();  break;
            case EnemyState.Chase:  UpdateChaseState();   break;
        }

        CheckForPlayerDetection();
    }

    private void CheckForPlayerDetection()
    {
        if (player == null) return;
        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= detectionRange && currentState != EnemyState.Chase)
            currentState = EnemyState.Chase;
        else if (dist > detectionRange && currentState == EnemyState.Chase)
        {
            currentState = EnemyState.Patrol;
            SetNewPatrolTarget();
        }
    }

    private void UpdateIdleState()
    {
        movement = Vector2.zero;
        patrolTimer += Time.deltaTime;
        if (patrolTimer >= idleTime)
        {
            patrolTimer = 0f;
            isWaiting = false;
            currentState = EnemyState.Patrol;
            SetNewPatrolTarget();
        }
    }

    private void UpdatePatrolState()
    {
        if (isWaiting) return;

        Vector2 dir = (currentPatrolTarget - (Vector2)transform.position).normalized;
        UpdateFacingDirection(dir);
        movement = dir;

        if (Vector2.Distance(transform.position, currentPatrolTarget) < 0.1f)
        {
            isWaiting = true;
            patrolTimer = 0f;
            currentState = EnemyState.Idle;
        }
    }

    private void UpdateChaseState()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        Vector2 avoidance = CalculateAvoidanceDirection();
        Vector2 finalDir = (dirToPlayer + avoidance * avoidanceWeight).normalized;

        UpdateFacingDirection(dirToPlayer);
        movement = dist > stopDistance ? finalDir : Vector2.zero;
    }

    // --- Movement & Physics ---

    void FixedUpdate()
    {
        if (isKnockedBack || isDead) return;
        if (rb != null && movement != Vector2.zero)
        {
            float speed = currentState == EnemyState.Chase ? chaseSpeed : patrolSpeed;
            rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);
        }
    }

    // --- Patrol ---

    private void SetNewPatrolTarget()
    {
        if (useRandomPatrol)
        {
            float rx = Random.Range(-patrolDistance, patrolDistance);
            float ry = Random.Range(-patrolDistance, patrolDistance);
            currentPatrolTarget = startPosition + new Vector2(rx, ry);
        }
        else
        {
            currentPatrolTarget = Vector2.Distance(transform.position, startPosition) < 0.1f
                ? startPosition + new Vector2(patrolDistance, 0)
                : startPosition;
        }
    }

    // --- Visuals ---

    private void UpdateFacingDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) <= directionThreshold) return;
        if (Time.time - lastFlipTime <= flipCooldown) return;

        bool shouldFaceRight = direction.x > 0;
        if (shouldFaceRight == facingRight) return;

        facingRight = shouldFaceRight;
        spriteRenderer.flipX = !facingRight;
        lastFlipTime = Time.time;
    }

    private Vector2 CalculateAvoidanceDirection()
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, avoidanceRadius, enemyLayer);
        Vector2 avoidance = Vector2.zero;
        int count = 0;

        foreach (var col in nearby)
        {
            if (col.gameObject == gameObject) continue;
            count++;
            Vector2 away = (Vector2)transform.position - (Vector2)col.transform.position;
            float dist = Mathf.Max(away.magnitude, 0.1f);
            avoidance += away.normalized * (avoidanceRadius / dist);
        }

        return count == 0 ? Vector2.zero : avoidance.normalized;
    }

    // --- Damage & Death (override Character base) ---

    public override void TakeDamage(int damage, Vector2 knockbackSource)
    {
        if (isDead) return;

        // Reduce health + visual feedback + knockback (from Character)
        base.TakeDamage(damage, knockbackSource);

        // Enemy-specific: switch to chase when hit
        if (player != null && currentState != EnemyState.Chase)
            currentState = EnemyState.Chase;
    }

    public override void TakeDamage(int damage)
    {
        if (player != null)
            TakeDamage(damage, player.position);
        else
        {
            // No knockback when no player reference
            if (isDead) return;
            currentHealth -= damage;
            StartCoroutine(FlashColor());
            ShowDamagePopup(damage);
            if (currentHealth <= 0) Die();
        }
    }

    protected override void Die()
    {
        base.Die();
        movement = Vector2.zero;

        // Enemy-specific: flip Y scale and auto-destroy
        Vector3 scale = transform.localScale;
        transform.localScale = new Vector3(scale.x, -scale.y, scale.z);
        Destroy(gameObject, 2f);
    }

    // --- Editor Gizmos ---

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, avoidanceRadius);

        if (Application.isPlaying && currentState == EnemyState.Patrol)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentPatrolTarget);
            Gizmos.DrawWireSphere(currentPatrolTarget, 0.2f);
        }
    }
}
