using System.Collections;
using UnityEngine;

public class Player : Character
{
    public LayerMask solidObjectsLayer;
    public LayerMask enemyLayer;
    public float attackRange = 0.5f;
    public bool isMoving;
    public bool isAttacking;
    private Vector2 input;
    private Vector2 lastMoveDir;
    public Collider2D[] hitEnemies;
    public Transform attackPoint;
    public Enemy enemy;

    [Header("Combo Settings")]
    public int maxComboHits = 3;           // số đòn trong 1 combo
    public float comboWindowTime = 0.4f;   // thời gian cho phép nhấn tiếp combo
    public float attackDuration = 0.35f;   // thời gian mỗi đòn chém
    public float comboResetTime = 0.8f;    // thời gian reset combo sau đòn cuối
    public int[] comboDamage = { 1, 1, 2 }; // damage mỗi đòn (đòn 3 mạnh hơn)

    [Header("Dash Settings")]
    public int dashTiles = 3;              // số tiles dash
    public float dashSpeed = 15f;          // tốc độ dash
    public float doubleTapWindow = 0.3f;   // thời gian giữa 2 lần nhấn
    public float dashCooldown = 0.8f;      // cooldown giữa các lần dash

    private int currentComboStep = 0;
    private float lastAttackTime = -10f;
    private bool comboQueued = false;
    private Coroutine comboRoutine;
    private EquipmentManager _equipmentManager;
    private float _lastTapTime;
    private Vector2 _lastTapDir;
    private float _lastDashTime = -10f;
    private bool _isDashing;

    protected override void Awake()
    {
        base.Awake();
        _equipmentManager = GetComponent<EquipmentManager>();
    }

    private void Update()
    {
        if (!isMoving && !_isDashing)
        {
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");

            if (input.x != 0) input.y = 0;

            if (input != Vector2.zero)
            {
                animator.SetFloat("moveX", input.x);
                animator.SetFloat("moveY", input.y);
                lastMoveDir = input;

                var targetPos = transform.position + new Vector3(input.x, input.y, 0);
                if (IsWalkable(targetPos))
                    StartCoroutine(Move(targetPos));
            }
        }

        animator.SetBool("isMoving", isMoving);
        HandleComboAttack();
        HandleDash();
    }

    private void HandleComboAttack()
    {
        if (!Input.GetKeyDown(KeyCode.Space)) return;

        float timeSinceLastAttack = Time.time - lastAttackTime;

        // Reset combo nếu quá lâu không đánh
        if (timeSinceLastAttack > comboResetTime)
            currentComboStep = 0;

        // Đang đánh → queue đòn tiếp theo
        if (isAttacking)
        {
            if (currentComboStep < maxComboHits && timeSinceLastAttack > attackDuration * 0.5f)
                comboQueued = true;
            return;
        }

        // Bắt đầu đòn mới
        if (currentComboStep < maxComboHits)
            ExecuteAttack();
    }

    private void ExecuteAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        comboQueued = false;

        animator.SetFloat("moveX", lastMoveDir.x);
        animator.SetFloat("moveY", lastMoveDir.y);
        animator.SetBool("isAttacking", true);
        animator.SetInteger("comboStep", currentComboStep);

        // Damage theo combo step
        int damage = currentComboStep < comboDamage.Length ? comboDamage[currentComboStep] : 1;
        HitMonster(damage);

        currentComboStep++;

        if (comboRoutine != null) StopCoroutine(comboRoutine);
        comboRoutine = StartCoroutine(ComboStepRoutine());
    }

    private IEnumerator ComboStepRoutine()
    {
        // Chờ animation đòn hiện tại
        yield return new WaitForSeconds(attackDuration);

        isAttacking = false;
        animator.SetBool("isAttacking", false);

        // Nếu có đòn queued → đánh tiếp ngay
        if (comboQueued && currentComboStep < maxComboHits)
        {
            ExecuteAttack();
            yield break;
        }

        // Chờ combo window cho người chơi nhấn tiếp
        yield return new WaitForSeconds(comboWindowTime);

        // Hết window → reset combo
        if (!isAttacking)
        {
            currentComboStep = 0;
            animator.SetInteger("comboStep", 0);
        }
    }

    private void HitMonster(int damage)
    {
        if (attackPoint == null) return;

        // Add equipment attack bonus
        int bonus = _equipmentManager != null ? _equipmentManager.GetAttackBonus() : 0;
        int totalDamage = damage + bonus;

        hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
        foreach (Collider2D col in hitEnemies)
        {
            enemy = col.GetComponent<Enemy>();
            if (enemy != null)
                enemy.TakeDamage(totalDamage, transform.position);
        }
    }

    IEnumerator Move(Vector3 targetPos)
    {
        isMoving = true;
        // Bắt đầu di chuyển → reset combo
        currentComboStep = 0;

        while ((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;
        isMoving = false;
    }

    private bool IsWalkable(Vector3 targetPos)
    {
        return Physics2D.OverlapCircle(targetPos, 0.3f, solidObjectsLayer) == null;
    }

    // --- Dash: double-tap cùng hướng để lướt nhanh qua N tiles ---
    // Luôn track tap ngay cả khi đang move, chỉ block trigger dash khi đang dash/attack
    private void HandleDash()
    {
        if (_isDashing || isAttacking) return;

        Vector2 dir = Vector2.zero;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) dir = Vector2.right;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) dir = Vector2.left;
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) dir = Vector2.up;
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) dir = Vector2.down;

        if (dir == Vector2.zero) return;

        float timeSinceLastTap = Time.time - _lastTapTime;

        // Double-tap cùng hướng → dash (cho phép ngay cả khi đang walk)
        if (dir == _lastTapDir && timeSinceLastTap <= doubleTapWindow && Time.time - _lastDashTime >= dashCooldown)
        {
            // Hủy move hiện tại nếu đang walk
            StopAllCoroutines();
            isMoving = false;
            comboQueued = false;

            StartCoroutine(Dash(dir));
            _lastTapTime = 0f;
            return;
        }

        _lastTapTime = Time.time;
        _lastTapDir = dir;
    }

    private IEnumerator Dash(Vector2 direction)
    {
        _isDashing = true;
        isMoving = true;
        _lastDashTime = Time.time;
        currentComboStep = 0;

        // Tìm tile xa nhất có thể dash tới
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos;
        for (int i = 1; i <= dashTiles; i++)
        {
            Vector3 nextTile = startPos + new Vector3(direction.x, direction.y, 0) * i;
            if (!IsWalkable(nextTile)) break;
            targetPos = nextTile;
        }

        // Lướt nhanh tới target
        while ((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, dashSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;

        isMoving = false;
        _isDashing = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
