using UnityEngine;
using UnityEngine.SceneManagement; // �� �߰�

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PrincessAutoWalker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D body;
    [SerializeField] public Animator ani;

    [Header("Move Settings")]
    [SerializeField] private float moveSpeed = 4.0f;
    [SerializeField] private int startDirection = +1;      // +1=������, -1=����
    [SerializeField] private bool startMovingOnEnable = true;

    [Header("Step / Wall Check")]
    [Tooltip("�� ĭ ����(Ÿ��=1�̸� 1.0)")]
    [SerializeField] private float stepHeight = 1.0f;
    [SerializeField] private float obstacleCheckDistance = 0.25f;
    [SerializeField] private float skin = 0.05f;
    [SerializeField] private LayerMask obstacleMask;        // Ground/Wall ��

    [Header("Ground Check")]
    [SerializeField] private float groundCheckRadius = 0.08f;
    [SerializeField] private float groundCheckOffsetY = 0.02f;

    [Header("Step Hop (������ ����)")]
    [SerializeField] private float stepJumpYSpeed = 5.0f;   // Y�� '����'
    [SerializeField] private float stepHopXDrift = 2.0f;    // X�� '��¦'
    [SerializeField] private float stepHopMaxDuration = 0.30f; // �� �ð� ���� ���� �� ���� ����
    [SerializeField] private float stepJumpCooldown = 0.10f;
    [SerializeField] private bool requireGroundedForStepJump = true;

    [Header("Speed Modifiers (Tag)")]
    [SerializeField] private float slowMoveSpeed = 1.0f;    // SlowRun �±� �� �ӵ�
    [SerializeField] private float fastMoveSpeed = 6.0f;    // FastRun �±� �� �ӵ�

    [Header("Knight Ignore")]
    [SerializeField] private string knightTag = "Player";   // ���(�÷��̾�)�� �浹 ����

    [Header("Trap Reload")] // �� �߰�
    [SerializeField] private string trapLayerName = "Trap"; // ���̾� �̸� ���
    private int trapLayerIndex = -1;
    private bool isReloading = false;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    // ���� ����
    private int dirSign;                 // +1 or -1
    private bool isMoving = true;        // ������ ���� ��� false
    private bool pausedByFall;           // ���� �� �Ͻ� ����
    private float jumpLockTimer;
    private bool stepHopActive;
    private float stepHopTimer;

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        body = GetComponent<Collider2D>();
        ani = GetComponent<Animator>();
        obstacleMask = LayerMask.GetMask("Ground");
    }

    void OnValidate()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!body) body = GetComponent<Collider2D>();

        stepHeight = Mathf.Max(0.1f, stepHeight);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        obstacleCheckDistance = Mathf.Max(0.05f, obstacleCheckDistance);
        groundCheckRadius = Mathf.Max(0.01f, groundCheckRadius);
        stepJumpCooldown = Mathf.Max(0f, stepJumpCooldown);
        stepHopMaxDuration = Mathf.Max(0.05f, stepHopMaxDuration);
        startDirection = Mathf.Clamp(startDirection, -1, +1);
        if (startDirection == 0) startDirection = +1;

        ResolveTrapLayerIndex(); // �� �߰�
    }

    void Awake()
    {
        dirSign = Mathf.Sign(startDirection) >= 0 ? +1 : -1;
        SetupIgnoreCollisionWithKnights();
        ResolveTrapLayerIndex(); // �� �߰�
    }

    void OnEnable()
    {
        isMoving = startMovingOnEnable;
        pausedByFall = false;
        jumpLockTimer = 0f;
        stepHopActive = false;
        stepHopTimer = 0f;
        isReloading = false; // �� �߰�

        ApplyRootFlip(dirSign); // �ʱ� ���� �ݿ�
    }

    void FixedUpdate()
    {
        jumpLockTimer = Mathf.Max(0f, jumpLockTimer - Time.fixedDeltaTime);

        bool grounded = IsGrounded();
        bool falling = !grounded && rb.linearVelocity.y < -0.01f;

        // ���� �±� �˻�� ����/�ӵ� ������ '���� ��'������
        float currentSpeed = moveSpeed;
        if (grounded)
        {
            EvaluateFloorTags(ref currentSpeed, out bool foundDirTile, out bool directionChanged);
            ani.SetBool("jump", false);
            ani.SetBool("run", true);
            // ���� ���� ���絵 ���� Ÿ�� ������ �����
            if (!isMoving && foundDirTile) isMoving = true;

            if (directionChanged) ApplyRootFlip(dirSign); // ������� �ٲ�� ��Ʈ ����
        }

        // ���� �߿��� X ����(��Ģ 7)
        if (falling)
        {
            ani.SetBool("run", false);
            ani.SetBool("jump", true);
            pausedByFall = true;
            stepHopActive = false; // ���� ���� �� ȩ ����
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (grounded && pausedByFall)
        {
            pausedByFall = false;
            isMoving = true;
        }

        // ���� ���� ���˻�
        GetFrontBlockInfo(out bool feetBlocked, out bool topBlocked);

        // ���� ȩ Ȱ�� ó��
        if (stepHopActive)
        {
            stepHopTimer -= Time.fixedDeltaTime;

            if (rb.linearVelocity.y > 0.001f)
            {
                rb.linearVelocity = new Vector2(stepHopXDrift * dirSign, rb.linearVelocity.y);
            }

            if (grounded || rb.linearVelocity.y <= 0f || stepHopTimer <= 0f)
            {
                stepHopActive = false;
            }

            if (stepHopActive) return;
        }

        // ���� ���� �ڵ� �簳
        if (!isMoving)
        {
            ani.SetBool("run", false);
            if (!feetBlocked)
            {
                isMoving = true;
            }
            else if (!topBlocked && (jumpLockTimer <= 0f) && (!requireGroundedForStepJump || grounded))
            {
                rb.linearVelocity = new Vector2(stepHopXDrift * dirSign, stepJumpYSpeed);
                stepHopActive = true;
                stepHopTimer = stepHopMaxDuration;
                jumpLockTimer = stepJumpCooldown;
                isMoving = true;
                return;
            }
            else
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }
        }

        // ��� ���� �� �� ĭ �¿�
        if (feetBlocked)
        {
            bool canStepJump = !topBlocked
                               && (jumpLockTimer <= 0f)
                               && (!requireGroundedForStepJump || grounded);

            if (canStepJump)
            {
                ani.SetBool("jump", true);
                rb.linearVelocity = new Vector2(stepHopXDrift * dirSign, stepJumpYSpeed);
                stepHopActive = true;
                stepHopTimer = stepHopMaxDuration;
                jumpLockTimer = stepJumpCooldown;
                return;
            }
            else
            {
                ani.SetBool("run", false);
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                if (topBlocked) isMoving = false; // ���� �� �� ����
                return;
            }
        }

        // ����
        rb.linearVelocity = new Vector2(currentSpeed * dirSign, rb.linearVelocity.y);
    }

    /// <summary>�ܺο��� ���� ������ �� �����</summary>
    public void SetDirectionAndGo(int sign)
    {
        int newSign = (sign >= 0) ? +1 : -1;
        if (newSign != dirSign)
        {
            dirSign = newSign;
            ApplyRootFlip(dirSign); // ��Ʈ ����
        }
        isMoving = true;
        pausedByFall = false;
    }

    private bool IsGrounded()
    {
        Bounds b = body.bounds;
        Vector2 p = new Vector2(b.center.x, b.min.y + groundCheckOffsetY);
        return Physics2D.OverlapCircle(p, groundCheckRadius, obstacleMask) != null;
    }

    private void GetFrontBlockInfo(out bool feetBlocked, out bool topBlocked)
    {
        Bounds b = body.bounds;
        float s = dirSign;
        Vector2 feet = new Vector2(b.center.x, b.min.y + skin);
        Vector2 top = feet + Vector2.up * (stepHeight - skin);

        feetBlocked = Physics2D.Raycast(feet, Vector2.right * s, obstacleCheckDistance + skin, obstacleMask);
        topBlocked = Physics2D.Raycast(top, Vector2.right * s, obstacleCheckDistance + skin, obstacleMask);
    }

    /// <summary>
    /// �ٴ� �±�(LeftGo/RightGo/SlowRun/FastRun)�� �о�
    /// - ����(dirSign) �� �ӵ�(currentSpeed) ����
    /// - directionTileFound: ���� Ÿ�� ����
    /// - directionChanged: �̹� �����ӿ� ���� ���� ����
    /// </summary>
    private void EvaluateFloorTags(ref float currentSpeed, out bool directionTileFound, out bool directionChanged)
    {
        directionTileFound = false;
        directionChanged = false;

        Bounds b = body.bounds;
        Vector2 p = new Vector2(b.center.x, b.min.y + groundCheckOffsetY);
        var hits = Physics2D.OverlapCircleAll(p, groundCheckRadius * 1.1f);

        bool sawLeft = false, sawRight = false;
        bool sawSlow = false, sawFast = false;

        foreach (var h in hits)
        {
            if (!h) continue;
            var go = h.gameObject;

            if (go.CompareTag("LeftGo")) sawLeft = true;
            if (go.CompareTag("RightGo")) sawRight = true;

            if (go.CompareTag("SlowRun")) sawSlow = true;
            if (go.CompareTag("FastRun")) sawFast = true;
        }

        int oldSign = dirSign;

        if (sawLeft ^ sawRight)
        {
            dirSign = sawRight ? +1 : -1;
            directionTileFound = true;
        }

        if (sawSlow) currentSpeed = slowMoveSpeed;
        else if (sawFast) currentSpeed = fastMoveSpeed;
        else currentSpeed = moveSpeed;

        directionChanged = (dirSign != oldSign);
    }

    private void SetupIgnoreCollisionWithKnights()
    {
        var knights = GameObject.FindGameObjectsWithTag(knightTag);
        if (knights == null || knights.Length == 0) return;

        var myCols = GetComponentsInChildren<Collider2D>(true);
        foreach (var k in knights)
        {
            foreach (var kc in k.GetComponentsInChildren<Collider2D>(true))
            {
                foreach (var mc in myCols)
                {
                    if (kc && mc) Physics2D.IgnoreCollision(mc, kc, true);
                }
            }
        }
    }

    /// <summary>��Ʈ ������Ʈ ��ü�� ��/�� ����</summary>
    private void ApplyRootFlip(int sign)
    {
        bool faceRight = sign >= 0;
        var t = transform;
        Vector3 s = t.localScale;
        float absx = Mathf.Abs(s.x);
        s.x = faceRight ? absx : -absx;
        t.localScale = s;
    }

    // ==================== Trap �浹 �� �� ���ε� (�߰�) ====================

    private void ResolveTrapLayerIndex()
    {
        trapLayerIndex = LayerMask.NameToLayer(trapLayerName);
        if (trapLayerIndex < 0)
            Debug.LogWarning($"[Princess] Trap layer '{trapLayerName}' not found. ���̾� �̸��� Ȯ���ϼ���.");
    }

    private bool IsTrap(GameObject go) => trapLayerIndex >= 0 && go.layer == trapLayerIndex;

    private void ReloadSceneOnce()
    {
        if (isReloading) return;
        isReloading = true;
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (IsTrap(c.collider.gameObject)) ReloadSceneOnce();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsTrap(other.gameObject)) ReloadSceneOnce();
    }

    // (����) ��ģ ���¿��� �������� ���� �����ϰ� ó���ϰ� �ʹٸ� �ּ� ����:
    // void OnCollisionStay2D(Collision2D c) { if (IsTrap(c.collider.gameObject)) ReloadSceneOnce(); }
    // void OnTriggerStay2D(Collider2D other){ if (IsTrap(other.gameObject)) ReloadSceneOnce(); }

    // ====================================================================

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        var c = GetComponent<Collider2D>();
        if (!c) return;

        var b = c.bounds;
        int s = Mathf.Sign(startDirection) >= 0 ? +1 : -1;
        Vector2 feet = new Vector2(b.center.x, b.min.y + skin);
        Vector2 top = feet + Vector2.up * (Mathf.Max(0.1f, stepHeight - skin));

        Gizmos.color = Color.red;
        Gizmos.DrawLine(feet, feet + Vector2.right * s * (obstacleCheckDistance + skin));
        Gizmos.color = Color.green;
        Gizmos.DrawLine(top, top + Vector2.right * s * (obstacleCheckDistance + skin));

        Gizmos.color = Color.yellow;
        Vector2 gp = new Vector2(b.center.x, b.min.y + groundCheckOffsetY);
        Gizmos.DrawWireSphere(gp, groundCheckRadius);
    }
}
