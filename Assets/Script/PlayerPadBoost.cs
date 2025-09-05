using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerPadBoost : MonoBehaviour
{
    [Header("Ground Check")]
    [Tooltip("���� ������ ���� ���̾�(������Ʈ�� Ground ���̾� ����).")]
    public LayerMask groundLayer;
    [Tooltip("���� ���� ������(�÷��̾� �����߽� ���� �Ʒ��� ����).")]
    public Vector2 groundCheckOffset = new Vector2(0f, -0.5f);
    [Tooltip("���� ���� ����.")]
    public float groundCheckDistance = 0.15f;

    private Rigidbody2D rb;

    // ���� ����
    private bool padActive = false;
    private float padEndTime = 0f;
    private float padMaxFallSpeed = 6f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// ���� �е� ���� ����: �ʱ� ���� + ���� ����
    /// </summary>
    public void ApplyPadBoost(float jumpUpSpeed, float slowFallDuration, float maxFallSpeed)
    {
        // 1) ��� ���� �ʱ� �ӵ� ����(������ ������ �����ϰ� ����� ���� ���� ����)
        Vector2 v = rb.linearVelocity;
        v.y = jumpUpSpeed;
        rb.linearVelocity = v;

        // 2) ���� �ӵ� Ŭ������ ���� ���°� ����
        padActive = true;
        padEndTime = Time.time + Mathf.Max(0f, slowFallDuration);
        padMaxFallSpeed = Mathf.Max(0.1f, maxFallSpeed);
    }

    void FixedUpdate()
    {
        if (!padActive) return;

        // �ð��� �����ų� �����Ǹ� ���� ����
        if (Time.time >= padEndTime || IsGrounded())
        {
            padActive = false;
            return;
        }

        // ���� ����: �ϰ� ���̸� �ִ� ���ϼӵ� ����
        Vector2 v = rb.linearVelocity;
        if (v.y < -padMaxFallSpeed)
        {
            v.y = -padMaxFallSpeed;
            rb.linearVelocity = v;
        }
    }

    private bool IsGrounded()
    {
        Vector2 origin = (Vector2)transform.position + groundCheckOffset;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        Debug.DrawRay(origin, Vector2.down * groundCheckDistance, hit.collider ? Color.green : Color.red);
        return hit.collider != null;
    }
}
