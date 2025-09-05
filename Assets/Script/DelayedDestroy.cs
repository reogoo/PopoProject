using UnityEngine;

public class DelayedDestroy : MonoBehaviour
{
    private float destroyAt = -1f;

    // �̹� ������ �ִٸ� �� ���� �ð����θ� ����
    public void Schedule(float delay)
    {
        float t = Time.time + Mathf.Max(0f, delay);
        if (destroyAt < 0f || t < destroyAt) destroyAt = t;
    }

    void Update()
    {
        if (destroyAt >= 0f && Time.time >= destroyAt)
        {
            Destroy(gameObject);
        }
    }
}
