using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int maxHealth = 1;
    private int currentHealth;
    public float knockbackForce = 10f;
    private Rigidbody2D rb;
    private Animator animator;
    public GameObject damagePopupPrefab; // Drag prefab vào trong Unity Inspector

    private enum State
    {
        Roaming
    }

    private State state;
    private EnemyPathFinding enemyPathFinding;

    private void Awake()
    {
        enemyPathFinding = GetComponent<EnemyPathFinding>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        state = State.Roaming;
        currentHealth = maxHealth;
    }
    private void Start()
    {
        StartCoroutine(RoamingRoutine());
    }

    private IEnumerator RoamingRoutine()
    {
        while (state == State.Roaming)
        {
            Vector2 roamPos = GetRoamingPosition();
            enemyPathFinding.MoveTo(roamPos);
            yield return new WaitForSeconds(2f);
        }
    }


    private Vector2 GetRoamingPosition()
    {
        return new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
    }

    public void TakeDamage(Vector2 attackerPos)
    {
        Debug.Log("nhận dam");
        Vector2 knockbackDir = (transform.position - (Vector3)attackerPos).normalized;
        Debug.Log("knockbackDir");
        rb.linearVelocity = Vector2.zero;
        Debug.Log("addfroce");
        rb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
        CalculateDamage(1);
        StartCoroutine(StopPathfindingTemporarily());
    }

    private void ShowDamagePopup(int damage)
    {
        var popup = Instantiate(damagePopupPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
        popup.GetComponent<DamagePopup>().Setup(damage);
    }

    private IEnumerator StopPathfindingTemporarily()
    {
        enemyPathFinding.isKnockbacked = true;
        yield return new WaitForSeconds(0.3f); // thời gian knockback
        enemyPathFinding.isKnockbacked = false;
    }

    public void CalculateDamage(int damage)
    {

        currentHealth -= damage;
        Debug.Log($"{gameObject.name} nhận {damage} damage! Máu còn lại: {currentHealth}");
        ShowDamagePopup(damage);

        if (currentHealth <= 0)
        {
            StartCoroutine(EnemyDie());
        }
    }

    private IEnumerator EnemyDie()
    {

        animator.SetBool("isDead", true);
        enemyPathFinding.isWalkable = false;
        rb.linearVelocity = Vector2.zero; // STOP movement
        rb.bodyType = RigidbodyType2D.Static;       // Ngừng chịu ảnh hưởng vật lý
        yield return new WaitForSeconds(2f); // thời gian knockback
        Destroy(gameObject);
    }
}
