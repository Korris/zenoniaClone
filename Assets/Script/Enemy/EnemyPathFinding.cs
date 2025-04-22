using UnityEngine;

public class EnemyPathFinding : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;

    private Rigidbody2D rb;
    private Vector2 moveDir;
    public bool isKnockbacked = false;
    public bool isWalkable = true;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (!isKnockbacked && isWalkable)
        {
            rb.MovePosition(rb.position + moveDir * (moveSpeed * Time.fixedDeltaTime));
        }
    }

    public void MoveTo(Vector2 targetPos)
    {
        moveDir = targetPos;
    }

}
