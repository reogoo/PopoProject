using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class Player1HP : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private int maxHP = 2;
    public int CurrentHP { get; private set; }
    public bool IsDead { get; private set; }

    [Header("Layers (��� �� Ground�� ����)")]
    [SerializeField] private string groundLayerName = "Ground";

    [Header("Optional")]
    [SerializeField] private string deadBoolName = "dead"; // Animator bool �Ķ���͸�(������ ����)

    private PlayerMouseMovement move;   // ����/�Ļ� �̵� ������Ʈ ��� ���̵� OK
    private Rigidbody2D rb;
    private Animator anim;

    void Awake()
    {
        move = GetComponent<PlayerMouseMovement>(); // Player1Movement �Ǵ� ���� ���� ��ũ��Ʈ���� ����
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        CurrentHP = Mathf.Max(1, maxHP);
    }

    /// <summary>�ܺο��� ������ �� �� ���</summary>
    public void TakeDamage(int dmg = 1)
    {
        if (IsDead) return;
        int amount = Mathf.Max(1, dmg);
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        if (CurrentHP == 0) Die();
    }

    /// <summary>ȸ���� �ʿ��ϸ� ���(�ִ�ġ �ʰ� ����)</summary>
    public void Heal(int amount)
    {
        if (IsDead) return;
        if (amount <= 0) return;
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
    }

    /// <summary>P1 ��� ó��: ���ۺҰ� + Ground ���̾� + ����ȭ</summary>
    private void Die()
    {
        if (IsDead) return;
        IsDead = true;

        // 1) ĳ�� ���̸� �����ϰ� ����(������ ����)
        if (move != null && move.isCarrying && move.otherPlayer != null)
        {
            var op = move.otherPlayer;
            // ���� �θ� ������ �Ұ�(�̵� ��ũ��Ʈ ���ο� private ����)�ϴ� ���� �ֻ�����
            op.transform.SetParent(null, true);
            if (op.rb) op.rb.simulated = true;
            op.isCarried = false;
            move.isCarrying = false;
            move.carryset = false;
        }

        // 2) �̵�/�Է� ���� (������Ʈ ��ü ��Ȱ��)
        if (move) move.enabled = false;

        // 3) ���� ���� �� ���� ����ȭ
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Static;
        }

        // 4) ���̾� ����: Player -> Ground
        int groundIdx = LayerMask.NameToLayer(groundLayerName);
        if (groundIdx >= 0) gameObject.layer = groundIdx;
        else Debug.LogWarning($"[Player1HP] Ground ���̾� '{groundLayerName}'�� ã�� �� �����ϴ�.");

        // 5) ����: �ִϸ����Ϳ� dead �÷��� ����
        if (anim && !string.IsNullOrEmpty(deadBoolName))
            anim.SetBool(deadBoolName, true);

        Debug.Log("[Player1HP] ��� ó�� �Ϸ�: ���ۺҰ�, Ground ���̾�, Static ����(�� ���ε� ����)");
    }
}
