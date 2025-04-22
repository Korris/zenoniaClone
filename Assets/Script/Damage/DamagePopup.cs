using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    private float disappearTimer;
    private Vector3 moveVector;
    private Color textColor;

    public void Setup(int damageAmount)
    {
        textMesh.text = damageAmount.ToString();
        textColor = textMesh.color;
        disappearTimer = 1f;

        moveVector = new Vector3(Random.Range(-0.2f, 0.2f), 1f, 0f);
        transform.localScale = Vector3.one;
    }

    private void Update()
    {
        transform.position += moveVector * Time.deltaTime;
        moveVector *= 0.8f;

        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            textColor.a -= 3f * Time.deltaTime;
            textMesh.color = textColor;
            if (textColor.a <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
