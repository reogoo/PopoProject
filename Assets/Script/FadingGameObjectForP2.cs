using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FadingGameObjectForP2 : MonoBehaviour
{
    [Header("Target Roots (auto-filled by tags if enabled)")]
    public Transform player2Root;
    public Transform player1Root;

    [Header("Fade Distance (world units)")]
    public float fadeStartDistance = 6f;
    public float fadeEndDistance = 1.5f;

    [Header("Mode")]
    [Tooltip("true��: �ּ��� ����, �������� ���̰�(���� ��)")]
    public bool reverse = false;

    [Header("Appear gating")]
    [Tooltip("���İ� �� �� �̻��� ������ �浹 ����(��: 0.98)")]
    [Range(0f, 1f)] public float appearAlphaThreshold = 0.98f; // ������ ����: ���� �б⿡�� �̻��

    [Header("Easing")]
    public AnimationCurve alphaCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Tooltip("�浹�� �ݶ��̴� ������Ʈ(����). ����θ� ����")]
    public GameObject coll;

    [Header("Auto-assign by Tag")]
    [Tooltip("���� �� �±׷� Player1/Player2�� �ڵ� Ž���Ͽ� �θ� Transform�� Root�� ����")]
    public bool autoAssignPlayersByTag = true;
    public string player1Tag = "Player1";
    public string player2Tag = "Player2";
    [Tooltip("�±׷� ã�� ������Ʈ�� �θ� Root�� ����ϴ�. �θ� ������ �ش� ������Ʈ�� ���")]
    public bool assignToParentOfTagged = true;

    // --- cached ---
    private Collider2D wallCollider;
    private SpriteRenderer[] spriteRenderers;  // �� ������Ʈ�� ���� SR��(�ڽ� ����)
    private Renderer[] genericRenderers;       // MeshRenderer/Skinned ��(_Color MPB ���)

    private readonly List<Collider2D> p2Cols = new();
    private readonly List<Collider2D> p1Cols = new();

    private bool collisionsIgnoredWithP2 = false;
    private bool collisionsIgnoredWithP1 = false;

    void Awake()
    {
        wallCollider = GetComponent<Collider2D>();

        // ���� ������Ʈ�� ������ �������� ����(�ڽ� ����: ���� ���� ����)
        spriteRenderers = GetComponents<SpriteRenderer>();

        var allRenderers = GetComponents<Renderer>();
        var genericList = new List<Renderer>();
        foreach (var r in allRenderers)
        {
            if (r is SpriteRenderer) continue;
            genericList.Add(r);
        }
        genericRenderers = genericList.ToArray();

        // �� �±� ��� �ڵ� �Ҵ�
        if (autoAssignPlayersByTag)
        {
            AutoAssignPlayersByTag();
        }

        CacheP2Colliders();
        CacheP1Colliders();
        ValidateThresholds();
    }

    void OnValidate() => ValidateThresholds();

    private void ValidateThresholds()
    {
        if (fadeEndDistance < 0f) fadeEndDistance = 0f;
        if (fadeStartDistance < fadeEndDistance + 0.01f)
            fadeStartDistance = fadeEndDistance + 0.01f;
        if (appearAlphaThreshold < 0f) appearAlphaThreshold = 0f;
        if (appearAlphaThreshold > 1f) appearAlphaThreshold = 1f;
    }

    private void AutoAssignPlayersByTag()
    {
        // Player1
        if (player1Root == null && !string.IsNullOrEmpty(player1Tag))
        {
            var p1 = GameObject.FindWithTag(player1Tag);
            if (p1)
            {
                var root = assignToParentOfTagged && p1.transform.parent ? p1.transform.parent : p1.transform;
                SetPlayer1Root(root);
            }
            else
            {
                Debug.LogWarning($"[FadingGameObjectForP2] '{player1Tag}' �±׸� ���� ������Ʈ�� ã�� ���߽��ϴ�.", this);
            }
        }

        // Player2
        if (player2Root == null && !string.IsNullOrEmpty(player2Tag))
        {
            var p2 = GameObject.FindWithTag(player2Tag);
            if (p2)
            {
                var root = assignToParentOfTagged && p2.transform.parent ? p2.transform.parent : p2.transform;
                SetPlayer2Root(root);
            }
            else
            {
                Debug.LogWarning($"[FadingGameObjectForP2] '{player2Tag}' �±׸� ���� ������Ʈ�� ã�� ���߽��ϴ�.", this);
            }
        }
    }

    private void CacheP2Colliders()
    {
        p2Cols.Clear();
        if (player2Root == null) return;
        player2Root.GetComponentsInChildren(true, p2Cols);
        p2Cols.RemoveAll(c => c == null);
    }

    private void CacheP1Colliders()
    {
        p1Cols.Clear();
        if (player1Root == null) return;
        player1Root.GetComponentsInChildren(true, p1Cols);
        p1Cols.RemoveAll(c => c == null);
    }

    void LateUpdate()
    {
        if (player2Root == null && player1Root == null)
        {
            SetAlpha(1f);
            ForceCollision(true); // �׻� �浹 ON
            if (coll) coll.SetActive(true);
            return;
        }

        // ���� ��ġ: P2 �켱, ������ P1
        Vector2 refPos = player2Root ? (Vector2)player2Root.position
                       : player1Root ? (Vector2)player1Root.position
                       : (Vector2)transform.position;

        // ������ ����: �� ������Ʈ�� Collider2D�� ClosestPoint
        Vector2 closest = wallCollider ? wallCollider.ClosestPoint(refPos) : (Vector2)transform.position;
        float dist = Vector2.Distance(closest, refPos);

        // far(0) -> near(1)
        float t = Mathf.InverseLerp(fadeStartDistance, fadeEndDistance, dist);

        // �⺻: �ָ� 1, ������ 0  �� Ŀ�� �� reverse�� ������
        float alphaNormal = Mathf.Clamp01(alphaCurve.Evaluate(1f - t));
        float alpha = reverse ? (1f - alphaNormal) : alphaNormal;

        SetAlpha(alpha);

        // === ���� �浹 ��Ģ ȣ�� 1: alpha�� ������ 0�� ���� ���(�߰� �� �� ȣ��) ===
        bool allowCollision = alpha > 0f;
        ForceCollision(allowCollision);

        // === ������ ������ ���� �б� ===
        if (alpha <= 0f)
        {
            ForceCollision(false);            // �浹 ����
            if (coll) coll.SetActive(false);  // �ݶ��̴� ������Ʈ ��Ȱ��ȭ
        }
        else if (alpha >= 0.5f)
        {
            ForceCollision(true);             // �浹 ����
            if (coll) coll.SetActive(true);   // �ݶ��̴� ������Ʈ Ȱ��ȭ
        }
        else
        {
            ForceCollision(false);
            if (coll) coll.SetActive(false);
        }
    }

    private void ForceCollision(bool enable)
    {
        bool ignore = !enable;

        if (collisionsIgnoredWithP2 != ignore)
        {
            SetIgnoreCollisionWithList(p2Cols, ignore);
            collisionsIgnoredWithP2 = ignore;
        }
        if (collisionsIgnoredWithP1 != ignore)
        {
            SetIgnoreCollisionWithList(p1Cols, ignore);
            collisionsIgnoredWithP1 = ignore;
        }
    }

    private void SetAlpha(float a)
    {
        // 1) SpriteRenderer��
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                var sr = spriteRenderers[i];
                if (!sr) continue;
                Color c = sr.color; c.a = a; sr.color = c;
            }
        }

        // 2) �� �� Renderer��(MeshRenderer/Skinned ��): MPB�� _Color ���ĸ� ����
        if (genericRenderers != null && genericRenderers.Length > 0)
        {
            for (int i = 0; i < genericRenderers.Length; i++)
            {
                var r = genericRenderers[i];
                if (!r) continue;

                var mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);

                Color c = Color.white;
                if (!mpb.isEmpty && mpb.HasVector("_Color")) c = mpb.GetColor("_Color");
                else if (r.sharedMaterial && r.sharedMaterial.HasProperty("_Color")) c = r.sharedMaterial.color;

                c.a = a;
                mpb.SetColor("_Color", c);
                r.SetPropertyBlock(mpb);
            }
        }
    }

    private void SetIgnoreCollisionWithList(List<Collider2D> list, bool ignore)
    {
        if (!wallCollider) return;
        for (int i = 0; i < list.Count; i++)
        {
            var col = list[i];
            if (col) Physics2D.IgnoreCollision(wallCollider, col, ignore);
        }
    }

    void OnDisable()
    {
        ForceCollision(true); // ����: �浹 ON
        SetAlpha(1f);
        if (coll) coll.SetActive(true);
    }

    public void SetPlayer2Root(Transform newRoot) { player2Root = newRoot; CacheP2Colliders(); }
    public void SetPlayer1Root(Transform newRoot) { player1Root = newRoot; CacheP1Colliders(); }

    // �����Ϳ��� ���� ���ſ�
    [ContextMenu("Auto-Assign Players From Tags")]
    public void Editor_AutoAssignNow()
    {
        AutoAssignPlayersByTag();
        CacheP2Colliders();
        CacheP1Colliders();
    }
}
