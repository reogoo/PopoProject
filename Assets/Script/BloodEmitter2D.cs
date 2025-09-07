using System.Collections;
using UnityEngine;

/// <summary>
/// �ֱ������� Blood_0/Blood_1 �������� �����ϰ� �վ��.
/// ��� ����(��/�Ϲݽ�)�� �������� �����ؼ� ���.
/// </summary>
public class BloodEmitter2D : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject blood0;
    public GameObject blood1;

    [Header("Timing")]
    public float intervalMin = 0.06f;
    public float intervalMax = 0.20f;

    [Header("Spawn Pos")]
    public Vector2 localOffset = Vector2.zero;
    public float jitter = 0.06f;

    [Header("Lifetime")]
    public float autoDestroySeconds = 3f; // ���� ���� ������

    private Coroutine co;

    void OnEnable()
    {
        co = StartCoroutine(EmitLoop());
    }
    void OnDisable()
    {
        if (co != null) StopCoroutine(co);
    }

    private IEnumerator EmitLoop()
    {
        var wait = new WaitForSeconds(Random.Range(intervalMin, intervalMax));
        while (true)
        {
            SpawnOne();
            wait = new WaitForSeconds(Random.Range(intervalMin, intervalMax));
            yield return wait;
        }
    }

    private void SpawnOne()
    {
        var prefab = (Random.value < 0.5f) ? blood0 : blood1;
        if (!prefab) return;

        Vector3 pos = transform.TransformPoint((Vector3)localOffset + (Vector3)(Random.insideUnitCircle * jitter));
        var go = Instantiate(prefab, pos, Quaternion.identity);

        if (go.TryGetComponent<Rigidbody2D>(out var r2d))
        {
            // ��¦ Ƣ���
            Vector2 dir = Random.insideUnitCircle.normalized;
            float spd = Random.Range(0.3f, 1.2f);
            r2d.AddForce(dir * spd, ForceMode2D.Impulse);
            r2d.AddTorque(Random.Range(-2f, 2f), ForceMode2D.Impulse);
        }

        Destroy(go, autoDestroySeconds);
    }
}
