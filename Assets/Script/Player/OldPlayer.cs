using System.Collections;
using UnityEngine;

public class OldPlayer : MonoBehaviour
{
    public float moveSpeed;
    public LayerMask solidObjectsLayer;
    public LayerMask enemyLayer; // 👈 thêm để chọn layer Enemy
    public float attackRange = 100f; // 👈 phạm vi chém
    public bool isMoving;
    public bool isAttacking;
    private Vector2 input;
    private Animator animator;
    private Vector2 lastMoveDir;
    public Collider2D[] hitEnemies;
    public Transform attackPoint; // 👈 điểm gốc để quét kẻ địch (empty GameObject trước mặt Player)
    public Enemy enemy;
    Coroutine attackRoutine;
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!isMoving)
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
        HandleAttack();
    }

    private void HandleAttack()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // Kiểm tra nếu không phải đang ở trạng thái attack hoặc đã gần kết thúc thì cho phép chém
        if (!isAttacking && Input.GetKeyDown(KeyCode.Space) && !stateInfo.IsTag("Attack"))
        {
            isAttacking = true;

            animator.SetFloat("moveX", lastMoveDir.x);
            animator.SetFloat("moveY", lastMoveDir.y);
            animator.SetBool("isAttacking", true);

            HitMonster();
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            attackRoutine = StartCoroutine(StopAttack());
        }
    }

    private void HitMonster()
    {
        Debug.Log("chém");
        if (attackPoint == null) return;

        // Tìm quái trong vùng chém
        hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
        Debug.Log($"hitEnemies+ {hitEnemies.Length.ToString()}");
        foreach (Collider2D col in hitEnemies)
        {
            Debug.Log("enemy");

            enemy = col.GetComponent<Enemy>();
            if (enemy != null)
            {
                Debug.Log("có");

                enemy.TakeDamage(1, transform.position); // Knockback từ vị trí player
            }
            else
            {
                Debug.Log("không");

            }
        }
    }

    IEnumerator StopAttack()
    {
        yield return new WaitForSeconds(0.7f);
        animator.SetBool("isAttacking", false);
        animator.SetBool("isMoving", false);
        isAttacking = false;
    }

    IEnumerator Move(Vector3 targetPos)
    {
        isMoving = true;
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

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
