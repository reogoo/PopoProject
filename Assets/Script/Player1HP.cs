using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[DisallowMultipleComponent]
public class Player1HP : MonoBehaviour, global::IDamageable
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
    public Animator rb2;

    [Header("Layers (��� �� Ground�� ����)")]
    [SerializeField] private string groundLayerName = "Ground";

    [Header("Optional")]
    [SerializeField] private string deadBoolName = "dead"; // Animator bool �Ķ���͸�(������ ����)

    [Header("Timing")]
    [SerializeField] private float swapDisableDelay = 1.5f;
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
    // ChargerSentinelAI, Monster ��� �� �ñ״�ó�� ȣ���մϴ�.
    public void TakeDamage(int amount, Vector2 hitPoint, Vector2 hitNormal)
    {
        TakeDamage(amount); // ���� ���� ���� ���� ����
    }

    // ================== �� �߰�: SendMessage ���� ���� ==================
    public void OnHit(int damage)
    {
        TakeDamage(damage);
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
            move.SetOtherPlayerVisible(true);
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
            rb2.SetBool("death", true);
        }

        // ���̾� ����
        int groundIdx = LayerMask.NameToLayer(groundLayerName);
        if (groundIdx >= 0) gameObject.layer = groundIdx;
        else Debug.LogWarning($"[Player1HP] Ground ���̾� '{groundLayerName}'�� ã�� �� �����ϴ�.");

        // �ִϸ����� dead �÷���
        if (anim && !string.IsNullOrEmpty(deadBoolName))
            anim.SetBool(deadBoolName, true);
        

        if (swap != null)
        {
            if (swapDisableDelay <= 0f) swap.swapsup = false;
            else StartCoroutine(DisableSwapAfterDelay());
        }

        HpChanged?.Invoke(0, maxHP);
        Died?.Invoke();

        Debug.Log("[Player1HP] ��� ó�� �Ϸ�: ���ۺҰ�, Ground ���̾�, Static ����(�� ���ε� ����)");
    }
    private IEnumerator DisableSwapAfterDelay()
    {
        yield return new WaitForSecondsRealtime(swapDisableDelay);
        if (swap != null) swap.swapsup = false; Dead = true;
    }
}
