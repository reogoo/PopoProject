using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SwapPad : MonoBehaviour
{
    [Header("Players (���� Tag�� �ڵ� Ž��)")]
    public Transform player1;     // Tag: "Player1"
    public Transform player2;     // Tag: "Player2"

    [Header("Swap Settings")]
    public float delaySeconds = 2f;
    public bool keepVelocity = false;   // true�� ������ �ӵ� ��ȯ, false�� 0���� ����
    public bool once = false;           // �� ���� ����
    public float cooldown = 2.5f;       // ��Ʈ���� ��Ÿ��

    [Header("Cancel Option")]
    public bool cancelIfExit = true;    // �� �е忡�� ����� ī��Ʈ�ٿ� ���

    [Header("Trigger Filter (����)")]
    public LayerMask triggerLayers;     // ����θ� ��� ���̾� ���

    private bool busy = false;
    private float lastSwapTime = -999f;
    private int occupants = 0;          // ���� �е� ���� �ִ� �÷��̾� ��
    private Coroutine pending;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Awake()
    {
        AutoAssignPlayersIfNeeded();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        // ���̾� ����
        if (triggerLayers.value != 0 && (triggerLayers.value & (1 << other.gameObject.layer)) == 0)
            return;

        occupants = Mathf.Max(0, occupants + 1);

        if (busy) return;
        if (Time.time - lastSwapTime < cooldown) return;

        if (player1 == null || player2 == null)
        {
            AutoAssignPlayersIfNeeded();
            if (player1 == null || player2 == null) return;
        }

        pending = StartCoroutine(SwapRoutine());
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        occupants = Mathf.Max(0, occupants - 1);

        // ������ ��� �ɼ�
        if (cancelIfExit && occupants <= 0 && pending != null)
        {
            StopCoroutine(pending);
            pending = null;
            busy = false;
        }
    }

    IEnumerator SwapRoutine()
    {
        busy = true;

        // delaySeconds ���� ��� �ö�� �־�� ����
        float t = 0f;
        while (t < delaySeconds)
        {
            if (cancelIfExit && occupants <= 0)
            {
                busy = false;
                yield break; // ���
            }
            t += Time.deltaTime;
            yield return null;
        }

        // ���� ����
        if (player1 == null || player2 == null)
        {
            busy = false;
            yield break;
        }

        Vector3 p1 = player1.position;
        Vector3 p2 = player2.position;

        Rigidbody2D rb1 = player1.GetComponent<Rigidbody2D>();
        Rigidbody2D rb2 = player2.GetComponent<Rigidbody2D>();

        Vector2 v1 = Vector2.zero, v2 = Vector2.zero;
        if (keepVelocity)
        {
            if (rb1) v1 = rb1.linearVelocity;
            if (rb2) v2 = rb2.linearVelocity;
        }

        if (rb1) rb1.position = p2; else player1.position = p2;
        if (rb2) rb2.position = p1; else player2.position = p1;

        if (keepVelocity)
        {
            if (rb1) rb1.linearVelocity = v2;
            if (rb2) rb2.linearVelocity = v1;
        }
        else
        {
            if (rb1) rb1.linearVelocity = Vector2.zero;
            if (rb2) rb2.linearVelocity = Vector2.zero;
        }

        lastSwapTime = Time.time;
        busy = false;
        pending = null;

        if (once) gameObject.SetActive(false);
    }

    bool IsPlayer(Collider2D other)
    {
        return other.CompareTag("Player")
               || other.CompareTag("Player1")
               || other.CompareTag("Player2")
               || other.GetComponent<PlayerMouseMovement>() != null;
    }

    void AutoAssignPlayersIfNeeded()
    {
        if (!player1)
        {
            var p1Obj = GameObject.FindGameObjectWithTag("Player1");
            if (p1Obj) player1 = p1Obj.transform;
        }
        if (!player2)
        {
            var p2Obj = GameObject.FindGameObjectWithTag("Player2");
            if (p2Obj) player2 = p2Obj.transform;
        }
    }

#if UNITY_EDITOR
    void OnValidate() { Reset(); }
    void OnDrawGizmos()
    {
        var col = GetComponent<Collider2D>();
        if (col is BoxCollider2D b)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.2f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(b.offset, b.size);
        }
    }
#endif
}
