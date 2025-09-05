using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class MonsterKillerTrigger2D : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string monsterLayerName = "Monster"; // ���� ��� ���̾�
    [SerializeField] private float killDelay = 3.0f;              // ���� �� ���� ������(��)
    [SerializeField] private bool targetRootWithRigidbody = true; // Rigidbody ��Ʈ�� ��������
    [Header("Animation")]
    public SpriteAnimationManager anim; // Idle / Run / AttackStart / Attack / Hit / Death
    private int monsterLayer = -1;
    private Rigidbody2D rb;
    private Collider2D col;

    void Awake()
    {
        monsterLayer = LayerMask.NameToLayer(monsterLayerName);

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Ʈ���� ����(����)
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        col.isTrigger = true;

        if (monsterLayer < 0)
            Debug.LogWarning($"[MonsterKillerTrigger2D] Layer '{monsterLayerName}' not found.");
    }
    private void PlayAnim(string key, bool forceRestart = false)
    {
        if (anim == null || string.IsNullOrEmpty(key)) return;
        if (anim.IsOneShotActive) return;         // �� 1ȸ ��� ��ȣ
        anim.Play(key, forceRestart);
    }
    private void PlayOnce(string key, string fallback = null, bool forceRestart = true)
    {
        if (anim == null || string.IsNullOrEmpty(key)) return;
        anim.PlayOnce(key, fallback, forceRestart);
    }
    void OnEnable()
    {
        rb?.WakeUp();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (monsterLayer < 0) return;

        // Rigidbody ��Ʈ�� ������� ���� ����
        GameObject hit = (targetRootWithRigidbody && other.attachedRigidbody)
            ? other.attachedRigidbody.gameObject
            : other.gameObject;

        if (hit.layer != monsterLayer) return;
        PlayOnce("Hit", "Death");
        // �ߺ� ������ ������: DelayedDestroy�� ������ ���� ���� ����
        var dd = hit.GetComponent<DelayedDestroy>();
        if (!dd) dd = hit.gameObject.AddComponent<DelayedDestroy>();
        dd.Schedule(killDelay);
    }
}
