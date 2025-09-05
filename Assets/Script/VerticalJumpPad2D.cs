using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class VerticalJumpPad : MonoBehaviour
{
    public PlayerMouseMovement carryjump;

    [Header("Jump Settings")]
    [Tooltip("������ �ο��ϴ� ���� �ʱ� �ӵ�(y).")]
    public float jumpUpSpeed = 14f;
    [Tooltip("����̸� �Ȱ������� ���� �ʱ� �ӵ�(y).")]
    public float carryjumpupspeed = 9f;

    [Header("Slow Fall Settings")]
    [Tooltip("���� ���� ���� �ð�(��).")]
    public float slowFallDuration = 0.8f;
    [Tooltip("����̸� �Ȱ������� ���� ���� �ð�(��).")]
    public float carryslowFallDuration = 1.2f;
    [Tooltip("���� ���� �� �ִ� �ϰ� �ӵ�(����, m/s). ���� �������� õõ�� �������ϴ�.")]
    public float maxFallSpeed = 6f;
    [Tooltip("����̸� �Ȱ������� ���� ���� �� �ִ� �ϰ� �ӵ�(����, m/s). ���� �������� õõ�� �������ϴ�.")]
    public float carrymaxFallSpeed = 8f;

    [Header("Filter")]
    [Tooltip("�÷��̾� ���̾ �����Ϸ��� �����ϼ���. ����� �����մϴ�.")]
    public LayerMask playerMask;

    [Header("Misc")]
    [Tooltip("���� ������/ª�� �ð��� �ߺ� Ʈ���� ����(��).")]
    public float rehitLockTime = 0.1f;
    [Tooltip("������ ������ ���� �۵��ϵ��� �������� ����.")]
    public bool requireLandingFromAbove = true;

    private float lastFireTime = -999f;

    void Reset()
    {
        // �⺻�� Ʈ���� ����(�ݸ����� ����)
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if(carryjump.isCarried == true)
        {
            CarryTryFire(other.attachedRigidbody);
        }
        else
        {
            TryFire(other.attachedRigidbody);
        }
        
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (carryjump.isCarried == true)
        {
            CarryTryFire(col.rigidbody, col);
        }
        else
        {
            TryFire(col.rigidbody, col);
        }
            
    }

    private void TryFire(Rigidbody2D rb, Collision2D col = null)
    {
        if (rb == null) return;

        if (playerMask.value != 0)
        {
            if (((1 << rb.gameObject.layer) & playerMask.value) == 0) return;
        }

        if (Time.time < lastFireTime + rehitLockTime) return;

        if (requireLandingFromAbove)
        {
            // �̹� ���� ���� ���̸� �н�
            if (rb.linearVelocity.y > 0.05f) return;

            // �ݸ����� ���: ��밡 ���Ǻ��� ������ ������ ��Ȳ�� ���(�뷫 ����)
            if (col != null && rb.worldCenterOfMass.y + 0.01f < transform.position.y) return;
        }

        var boost = rb.GetComponent<PlayerPadBoost>();
        if (boost == null) boost = rb.gameObject.AddComponent<PlayerPadBoost>();

        boost.ApplyPadBoost(
            jumpUpSpeed: jumpUpSpeed,
            slowFallDuration: slowFallDuration,
            maxFallSpeed: maxFallSpeed
        );

        lastFireTime = Time.time;
    }

    private void CarryTryFire(Rigidbody2D rb, Collision2D col = null)
    {
        if (rb == null) return;

        if (playerMask.value != 0)
        {
            if (((1 << rb.gameObject.layer) & playerMask.value) == 0) return;
        }

        if (Time.time < lastFireTime + rehitLockTime) return;

        if (requireLandingFromAbove)
        {
            // �̹� ���� ���� ���̸� �н�
            if (rb.linearVelocity.y > 0.05f) return;

            // �ݸ����� ���: ��밡 ���Ǻ��� ������ ������ ��Ȳ�� ���(�뷫 ����)
            if (col != null && rb.worldCenterOfMass.y + 0.01f < transform.position.y) return;
        }

        var boost = rb.GetComponent<PlayerPadBoost>();
        if (boost == null) boost = rb.gameObject.AddComponent<PlayerPadBoost>();

        boost.ApplyPadBoost(
            jumpUpSpeed: carryjumpupspeed,
            slowFallDuration: carryslowFallDuration,
            maxFallSpeed: carrymaxFallSpeed
        );

        lastFireTime = Time.time;
    }
}
