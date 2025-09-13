using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class TrapTouchDamage : MonoBehaviour
{
    [Header("Who to hit")]
    [SerializeField] private LayerMask playerMask = 0;   // Player, Player1, Player2 �� ���� ���̾� ����

    [Header("Damage")]
    [SerializeField] private int damageAmount = 2;       // ������ ���� HP
    [SerializeField] private bool onEnterOnly = true;    // true: ���� ���� 1ȸ��, false: �ӹ��� ���� �ֱ���
    [SerializeField] private float rehitCooldown = 0.2f; // Enter ��� �ߺ� ���� ��ٿ�

    [Header("Stay damage (if onEnterOnly=false)")]
    [SerializeField] private float stayInterval = 0.5f;  // Stay ����� �� �ݺ� ����

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    private readonly Dictionary<int, float> _nextAllowedTime = new Dictionary<int, float>();

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (!col.isTrigger && logDebug)
            Debug.LogWarning($"[TrapTouchDamage] {name} collider is not Trigger. �浹���� �����ϳ� Trigger ����.");
    }

    private void OnEnable() => _nextAllowedTime.Clear();

    private void OnTriggerEnter2D(Collider2D other) { TryDamage(other, enterPhase: true); }
    private void OnTriggerStay2D(Collider2D other) { TryDamage(other, enterPhase: false); }
    private void OnCollisionEnter2D(Collision2D c) { TryDamage(c.collider, enterPhase: true); }
    private void OnCollisionStay2D(Collision2D c) { TryDamage(c.collider, enterPhase: false); }

    private void TryDamage(Collider2D other, bool enterPhase)
    {
        if (!other) return;

        // �÷��̾� ���̾� ���� (���� ���̾� ����)
        if ((playerMask.value & (1 << other.gameObject.layer)) == 0)
            return;

        // ��Ʈ �������� �ߺ� ����
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform.root : other.transform.root;
        if (!root) return;

        int id = root.GetInstanceID();

        if (onEnterOnly && !enterPhase) return;

        float now = Time.time;
        float neededGap = onEnterOnly ? rehitCooldown : stayInterval;

        if (_nextAllowedTime.TryGetValue(id, out float nextTime) && now < nextTime)
            return; // ���� ��ٿ�

        ApplyDamage(root, other);
        _nextAllowedTime[id] = now + neededGap;
    }

    private void ApplyDamage(Transform root, Collider2D hitCol)
    {
        // 1) ǥ�� �������̽�
        var dmgIf = root.GetComponentInChildren<global::IDamageable>();
        if (dmgIf != null)
        {
            Vector2 hitPoint = hitCol.bounds.center;
            Vector2 hitNormal = ((Vector2)root.position - (Vector2)transform.position).normalized;
            if (hitNormal.sqrMagnitude < 0.0001f) hitNormal = Vector2.up;

            dmgIf.TakeDamage(damageAmount, hitPoint, hitNormal);
            if (logDebug) Debug.Log($"[TrapTouchDamage] {root.name} IDamageable -{damageAmount}");
            return;
        }

        // 2) Player1/2 ���� ����(����)
        var p1 = root.GetComponentInChildren<Player1HP>();
        if (p1 != null) { p1.TakeDamage(damageAmount); if (logDebug) Debug.Log($"[TrapTouchDamage] {root.name} Player1HP -{damageAmount}"); return; }

        var p2 = root.GetComponentInChildren<Player2HP>();
        if (p2 != null) { p2.TakeDamage(damageAmount); if (logDebug) Debug.Log($"[TrapTouchDamage] {root.name} Player2HP -{damageAmount}"); return; }

        // 3) ���� ����
        root.SendMessage("TakeDamage", damageAmount, SendMessageOptions.DontRequireReceiver);
        root.SendMessage("OnHit", damageAmount, SendMessageOptions.DontRequireReceiver);
        if (logDebug) Debug.Log($"[TrapTouchDamage] {root.name} SendMessage -{damageAmount}");
    }
}
