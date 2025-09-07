using System; // �� �߰�
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class Player1HP : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private int maxHP = 2;
    public int CurrentHP { get; private set; }
    public int MaxHP => maxHP;                // �� UI���� ���� �� �ְ� ���� Getter
    public bool IsDead { get; private set; }

    // �� HP ����/��� �̺�Ʈ
    public event Action<int, int> HpChanged;   // (current, max)
    public event Action Died;
    public bool Dead = false;
    public SmartCameraFollowByWall swap;


    [Header("Layers (��� �� Ground�� ����)")]
    [SerializeField] private string groundLayerName = "Ground";

    [Header("Optional")]
    [SerializeField] private string deadBoolName = "dead"; // Animator bool �Ķ���͸�(������ ����)

    private PlayerMouseMovement move;
    private Rigidbody2D rb;
    private Animator anim;

    void Awake()
    {
        move = GetComponent<PlayerMouseMovement>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        CurrentHP = Mathf.Max(1, maxHP);

        // �� ���� ���� ��ε�ĳ��Ʈ(�ʱ� UI ����ȭ)
        HpChanged?.Invoke(CurrentHP, maxHP);
    }

    /// <summary>�ܺο��� ������ �� �� ���</summary>
    public void TakeDamage(int dmg = 1)
    {
        if (IsDead) return;

        int amount = Mathf.Max(1, dmg);
        int prev = CurrentHP;
        CurrentHP = Mathf.Max(0, CurrentHP - amount);

        if (CurrentHP <= 0)
        {
            CameraShaker.Shake(0.5f, 0.2f);
            Die(); // Die() ���ο��� HpChanged(0, max)�� Died ȣ��
        }
        else
        {
            CameraShaker.Shake(0.5f, 0.2f);
            HpChanged?.Invoke(CurrentHP, maxHP); // �� ���� �˸�
        }
    }

    /// <summary>ȸ���� �ʿ��ϸ� ���(�ִ�ġ �ʰ� ����)</summary>
    public void Heal(int amount)
    {
        if (IsDead) return;
        if (amount <= 0) return;

        int prev = CurrentHP;
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);

        if (CurrentHP != prev)
            HpChanged?.Invoke(CurrentHP, maxHP); // �� ȸ�� �˸�
    }

    /// <summary>P1 ��� ó��: ���ۺҰ� + Ground ���̾� + ����ȭ</summary>
    public void Die()
    {
        if (IsDead) return;
        IsDead = true;

        // ĳ�� ���̸� �����ϰ� ����(������ ����)
        if (move != null && move.isCarrying && move.otherPlayer != null)
        {
            var op = move.otherPlayer;
            op.transform.SetParent(null, true);
            if (op.rb) op.rb.simulated = true;
            op.isCarried = false;
            move.isCarrying = false;
            move.carryset = false;
        }

        // �̵�/�Է� ����
        if (move) move.enabled = false;

        // ���� ���� �� ���� ����ȭ
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Static;
        }

        // ���̾� ����
        int groundIdx = LayerMask.NameToLayer(groundLayerName);
        if (groundIdx >= 0) gameObject.layer = groundIdx;
        else Debug.LogWarning($"[Player1HP] Ground ���̾� '{groundLayerName}'�� ã�� �� �����ϴ�.");

        swap.swapsup = false;
        // �ִϸ����� dead �÷���
        if (anim && !string.IsNullOrEmpty(deadBoolName))
            anim.SetBool(deadBoolName, true);
        Dead = true;
        // �� ���������� UI�� 0 ����ȭ & ��� �˸�
        HpChanged?.Invoke(0, maxHP);
        Died?.Invoke();

        Debug.Log("[Player1HP] ��� ó�� �Ϸ�: ���ۺҰ�, Ground ���̾�, Static ����(�� ���ε� ����)");
    }
}
