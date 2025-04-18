using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed;
    public LayerMask solidObjectsLayer;
    public bool isMoving;
    public bool isAttacking;
    private Vector2 input;
    private Animator animator;
    private Vector2 lastMoveDir;

    private Enemy enemy;
    private void Awake()
    {
        animator = GetComponent<Animator>();

    }

    private void HandleAttack()
    {
        if (!isAttacking && Input.GetKeyDown(KeyCode.Space))
        {
            isAttacking = true;

            // Đảm bảo animator biết hướng tấn công hiện tại
            animator.SetFloat("moveX", lastMoveDir.x);
            animator.SetFloat("moveY", lastMoveDir.y);

            animator.SetBool("isAttacking", true);
            StartCoroutine(StopAttack());
        }
        //if (!isAttacking && Input.GetKeyDown(KeyCode.Space))
        //{
        //    animator.SetBool("isAttacking", true);
        //    StartCoroutine(StopAttack());
        //}
    }

    IEnumerator StopAttack()
    {
        yield return new WaitForSeconds(0.2f);
        animator.SetBool("isAttacking", false);
        animator.SetBool("isMoving", false);
        isAttacking = false;
        Debug.Log("Stop");
    }

    private void Update()
    {
        if (!isMoving)
        {
            input.x = Input.GetAxis("Horizontal");
            input.y = Input.GetAxis("Vertical");

            if (input.x != 0) input.y = 0;

            if (input != Vector2.zero)
            {
                animator.SetFloat("moveX", input.x);
                animator.SetFloat("moveY", input.y);

                // Cập nhật hướng cuối
                lastMoveDir = input;

                var targetPos = transform.position;
                targetPos.x += input.x;
                targetPos.y += input.y;
                if (IsWalkable(targetPos))
                    StartCoroutine(Move(targetPos));
            }
        }

        animator.SetBool("isMoving", isMoving);
        HandleAttack();
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
        if (Physics2D.OverlapCircle(targetPos, 0.3f, solidObjectsLayer) != null)
        {
            return false;
        }

        return true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            Debug.Log("chem");
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(10); // hoặc sát thương theo logic
            }
        }
    }
}