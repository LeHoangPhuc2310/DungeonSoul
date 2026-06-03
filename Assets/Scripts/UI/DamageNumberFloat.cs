using TMPro;
using UnityEngine;

public class DamageNumberFloat : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private float floatSpeed = 1.5f;

    private float timer;
    private TextMeshPro tmp;
    private Color startColor;

    public void Initialize(float duration)
    {
        lifetime = Mathf.Max(0.1f, duration);
        timer = lifetime;
    }

    private void Start()
    {
        tmp = GetComponent<TextMeshPro>();
        if (tmp != null)
            startColor = tmp.color;
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        transform.position += Vector3.up * (floatSpeed * Time.deltaTime);

        if (tmp != null)
        {
            Color c = startColor;
            c.a = Mathf.Clamp01(timer / lifetime);
            tmp.color = c;
        }

        if (timer <= 0f)
            Destroy(gameObject);
    }
}
