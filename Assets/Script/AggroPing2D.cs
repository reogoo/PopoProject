using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class AggroPing2D : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D col;

    private MonsterABPatrolFSM owner;
    private Transform target;
    private float speed = 100f;
    private float life;
    private System.Action onDespawn;

    // ����ũ
    public LayerMask groundMask;
    public LayerMask playerMask;
    public LayerMask obstacleMask;

    private Vector2 dir; // �߻� �� ����(�ʿ��ϸ� ���� ������Ʈ�� �ٲ� �� ����)
    private float spawnTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.gravityScale = 0f;
        col.isTrigger = true; // Ʈ���� �浹�� ó�� ����
    }

    public void Init(
        MonsterABPatrolFSM owner,
        Transform target,
        float speed,
        float lifetime,
        LayerMask groundMask,
        LayerMask playerMask,
        LayerMask obstacleMask,
        System.Action onDespawn)
    {
        this.owner = owner;
        this.target = target;
        this.speed = speed;
        this.life = lifetime;
        this.groundMask = groundMask;
        this.playerMask = playerMask;
        this.obstacleMask = obstacleMask;
        this.onDespawn = onDespawn;

        spawnTime = Time.time;

        Vector2 origin = transform.position;
        Vector2 aim = target ? (Vector2)(target.TryGetComponent<Collider2D>(out var c)
                   ? c.bounds.center : target.position) : origin + Vector2.right;
        dir = (aim - origin).normalized;

        // ��� �ӵ� �ο�
        var v = rb.linearVelocity; v = dir * speed; rb.linearVelocity = v;
    }

    private void FixedUpdate()
    {
        // ��� ����
        rb.linearVelocity = dir * speed;

        // ���� ����
        if (Time.time - spawnTime >= life)
        {
            Despawn();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        int otherLayer = other.gameObject.layer;

        // 1) �׶���/��ֹ��� ������ �����(=������ ���� ������ ����)
        if (((1 << otherLayer) & (groundMask.value | obstacleMask.value)) != 0)
        {
            Despawn();
            return;
        }

        // 2) �÷��̾ ������ �ش� �÷��̾�� ��׷�
        if (((1 << otherLayer) & playerMask.value) != 0)
        {
            Transform hitT = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
            owner?.OnAggroPingHit(hitT);
            Despawn();
            return;
        }

        // �� �ܴ� ����(�ʿ�� ���̾� ��Ʈ������ �浹 ���� ����)
    }

    private void Despawn()
    {
        onDespawn?.Invoke();
        Destroy(gameObject);
    }
}