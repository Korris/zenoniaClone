using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    // Enum để quản lý trạng thái của enemy
    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase
    }

    [Header("State Settings")]
    public EnemyState currentState = EnemyState.Patrol;
    public float idleTime = 1.0f; // Thời gian đứng im sau khi đến điểm tuần tra

    [Header("Chase Settings")]
    public Transform player;
    public float chaseSpeed = 3.0f;
    public float detectionRange = 5.0f;
    public float stopDistance = 0.5f;

    [Header("Patrol Settings")]
    public float patrolSpeed = 1.5f;
    public float patrolDistance = 3.0f; // Khoảng cách di chuyển từ vị trí ban đầu
    public bool useRandomPatrol = false; // Tuần tra ngẫu nhiên hay qua lại

    [Header("Health and Damage Settings")]
    public int maxHealth = 100;
    public float knockbackForce = 10f;
    public float knockbackDuration = 0.25f;
    public Color hitColor = Color.red;
    public float flashDuration = 0.15f;

    [Header("Avoidance Settings")]
    public float avoidanceRadius = 1.0f;
    public float avoidanceWeight = 1.5f;
    public LayerMask enemyLayer;

    [Header("Animation Settings")]
    public float directionThreshold = 0.5f;
    public float flipCooldown = 0.5f;

    private int currentHealth;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Vector2 movement;
    private Color originalColor;
    private bool isKnockedBack = false;
    public GameObject damagePopupPrefab;
    private bool isDead = false;
    private float lastFlipTime = 0f;
    private bool facingRight = true;

    // Các biến cho hành vi tuần tra
    private Vector2 startPosition;
    private Vector2 currentPatrolTarget;
    private float patrolTimer = 0f;
    private bool isWaiting = false;

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
        }

        // Ensure the rigidbody is set up correctly
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        else
        {
            Debug.LogError("Rigidbody2D component missing from enemy!");
        }

        // Đảm bảo enemy layer được thiết lập đúng
        if (enemyLayer == 0)
        {
            enemyLayer = 1 << gameObject.layer;
        }

        // Khởi tạo trạng thái facing dựa trên sprite ban đầu
        facingRight = !spriteRenderer.flipX;

        // Lưu vị trí bắt đầu cho hành vi tuần tra
        startPosition = transform.position;
        SetNewPatrolTarget();
    }

    void Update()
    {
        if (isKnockedBack || isDead) return;

        // Máy trạng thái cơ bản
        switch (currentState)
        {
            case EnemyState.Idle:
                UpdateIdleState();
                break;
            case EnemyState.Patrol:
                UpdatePatrolState();
                break;
            case EnemyState.Chase:
                UpdateChaseState();
                break;
        }

        // Luôn kiểm tra xem có thấy player không
        CheckForPlayerDetection();
    }

    // Kiểm tra xem có thể thấy player không
    private void CheckForPlayerDetection()
    {
        // Nếu không có player, không thể phát hiện
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Nếu player trong phạm vi phát hiện, chuyển sang trạng thái Chase
        if (distanceToPlayer <= detectionRange && currentState != EnemyState.Chase)
        {
            currentState = EnemyState.Chase;
        }
        // Nếu player ra khỏi phạm vi phát hiện, quay lại tuần tra
        else if (distanceToPlayer > detectionRange && currentState == EnemyState.Chase)
        {
            currentState = EnemyState.Patrol;
            SetNewPatrolTarget();
        }
    }

    // Update cho trạng thái đứng im
    private void UpdateIdleState()
    {
        movement = Vector2.zero;

        // Đếm thời gian đứng im
        patrolTimer += Time.deltaTime;
        if (patrolTimer >= idleTime)
        {
            // Hết thời gian đứng im, chuyển sang tuần tra
            patrolTimer = 0f;
            isWaiting = false;
            currentState = EnemyState.Patrol;
            SetNewPatrolTarget();
        }
    }

    // Update cho trạng thái tuần tra
    private void UpdatePatrolState()
    {
        // Nếu đang đợi, không làm gì
        if (isWaiting) return;

        // Tính toán hướng đến điểm tuần tra tiếp theo
        Vector2 directionToTarget = (currentPatrolTarget - (Vector2)transform.position).normalized;

        // Cập nhật hướng nhìn dựa trên hướng di chuyển
        UpdateFacingDirection(directionToTarget);

        // Cập nhật vector di chuyển
        movement = directionToTarget;

        // Kiểm tra xem đã đến điểm tuần tra chưa
        float distanceToTarget = Vector2.Distance(transform.position, currentPatrolTarget);
        if (distanceToTarget < 0.1f)
        {
            // Đã đến điểm tuần tra, chuyển sang trạng thái đứng im
            isWaiting = true;
            patrolTimer = 0f;
            currentState = EnemyState.Idle;
        }
    }

    // Update cho trạng thái đuổi theo
    private void UpdateChaseState()
    {
        if (player == null) return;

        // Tính khoảng cách đến player
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Tính toán hướng đến player
        Vector2 directionToPlayer = (player.position - transform.position).normalized;

        // Tính toán hướng tránh va chạm với các enemy khác
        Vector2 avoidanceDirection = CalculateAvoidanceDirection();

        // Kết hợp hướng di chuyển đến player và hướng tránh va chạm
        Vector2 finalDirection = directionToPlayer + avoidanceDirection * avoidanceWeight;
        finalDirection.Normalize();

        // Cập nhật hướng nhìn dựa trên hướng đến player
        UpdateFacingDirection(directionToPlayer);

        // Chỉ di chuyển nếu không quá gần player
        if (distanceToPlayer > stopDistance)
        {
            movement = finalDirection;
        }
        else
        {
            movement = Vector2.zero;
        }
    }

    // Thiết lập điểm tuần tra mới
    private void SetNewPatrolTarget()
    {
        if (useRandomPatrol)
        {
            // Chọn một điểm ngẫu nhiên trong phạm vi patrolDistance
            float randomX = Random.Range(-patrolDistance, patrolDistance);
            float randomY = Random.Range(-patrolDistance, patrolDistance);
            currentPatrolTarget = startPosition + new Vector2(randomX, randomY);
        }
        else
        {
            // Di chuyển qua lại: nếu đang ở vị trí ban đầu, di chuyển sang phải, ngược lại thì về vị trí ban đầu
            if (Vector2.Distance(transform.position, startPosition) < 0.1f)
            {
                // Chọn hướng di chuyển (phải)
                currentPatrolTarget = startPosition + new Vector2(patrolDistance, 0);
            }
            else
            {
                // Quay lại vị trí ban đầu
                currentPatrolTarget = startPosition;
            }
        }
    }

    // Cập nhật hướng nhìn của enemy dựa trên hướng di chuyển chính
    private void UpdateFacingDirection(Vector2 direction)
    {
        // Chỉ flip sprite khi vượt qua ngưỡng và thời gian cooldown đã hết
        if (Mathf.Abs(direction.x) > directionThreshold && Time.time - lastFlipTime > flipCooldown)
        {
            bool shouldFaceRight = direction.x > 0;

            // Chỉ flip khi cần thiết
            if (shouldFaceRight != facingRight)
            {
                facingRight = shouldFaceRight;
                spriteRenderer.flipX = !facingRight;
                lastFlipTime = Time.time;
            }
        }
    }

    // Tính toán hướng để tránh các enemy khác
    private Vector2 CalculateAvoidanceDirection()
    {
        Vector2 avoidance = Vector2.zero;

        // Phát hiện các enemy khác trong bán kính avoidanceRadius
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, avoidanceRadius, enemyLayer);

        int count = 0;
        foreach (var enemyCollider in nearbyEnemies)
        {
            // Bỏ qua chính enemy này
            if (enemyCollider.gameObject == gameObject)
                continue;

            count++;

            // Tính toán vector tránh va chạm
            Vector2 distanceVector = (Vector2)transform.position - (Vector2)enemyCollider.transform.position;

            // Trọng số dựa trên khoảng cách (gần hơn = trọng số lớn hơn)
            float distance = distanceVector.magnitude;
            if (distance < 0.1f) distance = 0.1f; // Tránh chia cho 0

            // Thêm vector tránh va chạm được chuẩn hóa và có trọng số
            avoidance += distanceVector.normalized * (avoidanceRadius / distance);
        }

        // Nếu không có enemy nào gần thì trả về vector không
        if (count == 0)
            return Vector2.zero;

        return avoidance.normalized;
    }

    // Use FixedUpdate for physics-based movement
    void FixedUpdate()
    {
        // Don't move if knocked back or dead
        if (isKnockedBack || isDead) return;

        // Move the enemy using the calculated movement vector
        if (rb != null && movement != Vector2.zero)
        {
            // Tốc độ di chuyển phụ thuộc vào trạng thái
            float currentSpeed = currentState == EnemyState.Chase ? chaseSpeed : patrolSpeed;
            rb.MovePosition(rb.position + movement * currentSpeed * Time.fixedDeltaTime);
        }
    }

    private void ShowDamagePopup(int damage)
    {
        if (damagePopupPrefab != null)
        {
            var popup = Instantiate(damagePopupPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
            popup.GetComponent<DamagePopup>().Setup(damage);
        }
    }

    public void TakeDamage(int damage, Vector2 knockbackSource)
    {
        // Reduce health
        currentHealth -= damage;

        // Visual feedback
        StartCoroutine(FlashColor());

        // Apply knockback
        StartCoroutine(ApplyKnockback(knockbackSource));

        // Show damage popup
        ShowDamagePopup(damage);

        // Khi bị tấn công, enemy ngay lập tức phát hiện player (nếu có)
        if (player != null && currentState != EnemyState.Chase)
        {
            currentState = EnemyState.Chase;
        }

        // Check if enemy is defeated
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void TakeDamage(int damage)
    {
        // If player exists, use their position as knockback source
        if (player != null)
        {
            TakeDamage(damage, player.position);
        }
        else
        {
            // Just reduce health without knockback
            currentHealth -= damage;
            StartCoroutine(FlashColor());
            ShowDamagePopup(damage);

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
        // Set dead flag to stop all behavior
        isDead = true;

        // Reset movement
        movement = Vector2.zero;

        // Stop movement and physics
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

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

    // Visualize the detection and avoidance ranges in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, avoidanceRadius);

        // Vẽ đường tuần tra
        if (Application.isPlaying && currentState == EnemyState.Patrol)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentPatrolTarget);
            Gizmos.DrawWireSphere(currentPatrolTarget, 0.2f);
        }
    }
}