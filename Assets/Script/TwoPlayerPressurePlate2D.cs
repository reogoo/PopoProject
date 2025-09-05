using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class TwoPlayerPressurePlate2D : MonoBehaviour
{
    [Header("Players (Auto-assign by Tag)")]
    public bool autoAssignPlayersByTag = true;
    public string player1Tag = "Player1";
    public string player2Tag = "Player2";
    [Tooltip("�±׷� ã�� ������Ʈ�� �θ� ��Ʈ�� �������")]
    public bool useParentAsRoot = false;
    public Transform player1Root;
    public Transform player2Root;

    [Header("Detection Area")]
    [Tooltip("���� ������ �Ǵ� ���� �ݶ��̴�(������ ��, �� ������Ʈ�� ù Collider2D�� ���). 'Ʈ����'�� �ʿ�� �����ϴ�.")]
    public Collider2D areaCollider;
    [Tooltip("OverlapBox ���� ��, ������ ������ ���� ũ��(����, ����). ����/�ȱ� ���̸� ����� Y�� ���� �ּ���.")]
    public Vector2 zoneExtraSize = new Vector2(0.10f, 1.00f);
    [Tooltip("OverlapBox�� ����� ���̾�(�÷��̾� �ݶ��̴��� ���ԵǾ�� ��)")]
    public LayerMask playerLayers = ~0;

    [Header("Alternative Trigger Mode (optional)")]
    [Tooltip("Ʈ���� �ݹ����θ� �����ϰ� �ʹٸ� üũ. �Ʒ� triggerZone�� �����ϼ���.")]
    public bool useTriggerMode = false;
    [Tooltip("useTriggerMode=true �� �� ����� Ʈ����(�ݵ�� isTrigger=true)")]
    public Collider2D triggerZone;

    [Header("Spawn")]
    public GameObject spawnPrefab;
    public Transform spawnPoint;
    [Tooltip("�� �� ����(������������) �� �� ���� ��ȯ")]
    public bool spawnOnce = true;

    [Header("Visuals - Color")]
    [Tooltip("0���� ���� �⺻ ��. ����θ� ù �����ӿ� �ڵ� ä��")]
    public Color baseColor = Color.white;
    [Tooltip("1���� �� ���������� ���� ����(0~1)")]
    [Range(0f, 1f)] public float onePlayerTint = 0.45f;
    [Tooltip("2���� �� ���������� ���� ����(0~1)")]
    [Range(0f, 1f)] public float twoPlayerTint = 1.00f;
    [Tooltip("�� ��ȭ ���� �ӵ�")]
    public float colorLerpSpeed = 10f;

    [Header("Visuals - Depression")]
    [Tooltip("�� ��� �������� �Ÿ�(���� ����, +���̸� �Ʒ���)")]
    public float depressPerPlayer = 0.06f;
    [Tooltip("��ġ ���� �ӵ�")]
    public float moveLerpSpeed = 10f;

    [Header("Heuristic for stack/carry when only one is detected in zone")]
    [Tooltip("���� �� �� �� ������ ��, �ٸ� �� ���� ������(����/�ȱ�) ������ 2������ ����")]
    public bool augmentWithProximityHeuristic = true;
    [Tooltip("����/�ȱ� ���� ���� �Ÿ�(�� �� ������ ���)")]
    public float nearXDistance = 0.7f;
    [Tooltip("����/�ȱ� ���� ���� �ִ� ����(�� �� ������ ���, ���� ��� ����)")]
    public float nearYHeight = 1.2f;

    // Cached renderers for color tinting
    private Tilemap _tilemap;
    private TilemapRenderer _tilemapRenderer;
    private SpriteRenderer[] _spriteRenderers;
    private Renderer[] _genericRenderers;
    private bool _baseColorCaptured = false;
    private Color _currentTint;

    // State
    private Vector3 _basePos;
    private bool _activatedOnce = false;
    private readonly HashSet<Transform> _triggerInside = new(); // for trigger mode

    void Awake()
    {
        // Players auto-assign
        if (autoAssignPlayersByTag)
        {
            if (!player1Root && !string.IsNullOrEmpty(player1Tag))
            {
                var p1 = GameObject.FindWithTag(player1Tag);
                if (p1) player1Root = useParentAsRoot && p1.transform.parent ? p1.transform.parent : p1.transform;
            }
            if (!player2Root && !string.IsNullOrEmpty(player2Tag))
            {
                var p2 = GameObject.FindWithTag(player2Tag);
                if (p2) player2Root = useParentAsRoot && p2.transform.parent ? p2.transform.parent : p2.transform;
            }
        }

        // Detection area collider fallback
        if (!areaCollider)
        {
            areaCollider = GetComponent<Collider2D>();
        }

        // Cache renderers
        _tilemap = GetComponent<Tilemap>();
        _tilemapRenderer = GetComponent<TilemapRenderer>();
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        var allRenderers = GetComponentsInChildren<Renderer>(true);
        var genericList = new List<Renderer>();
        foreach (var r in allRenderers)
        {
            if (r is SpriteRenderer) continue;
            if (r is TilemapRenderer) continue;
            genericList.Add(r);
        }
        _genericRenderers = genericList.ToArray();

        _basePos = transform.position;
        _currentTint = baseColor;
    }

    void Start()
    {
        // Capture base color if not set by user
        if (!_baseColorCaptured)
        {
            Color captured;
            if (_tilemap) captured = _tilemap.color;
            else if (_tilemapRenderer)
            {
                var mpb = new MaterialPropertyBlock();
                _tilemapRenderer.GetPropertyBlock(mpb);
                captured = mpb.isEmpty ? Color.white : mpb.GetColor("_Color");
            }
            else if (_spriteRenderers != null && _spriteRenderers.Length > 0)
                captured = _spriteRenderers[0].color;
            else if (_genericRenderers != null && _genericRenderers.Length > 0)
            {
                var r = _genericRenderers[0];
                var mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);
                if (!mpb.isEmpty && mpb.HasVector("_Color")) captured = mpb.GetColor("_Color");
                else if (r.sharedMaterial && r.sharedMaterial.HasProperty("_Color")) captured = r.sharedMaterial.color;
                else captured = Color.white;
            }
            else captured = baseColor;

            baseColor = captured;
            _currentTint = captured;
            _baseColorCaptured = true;
        }
    }

    void Update()
    {
        int present = CountPlayersPresent();

        // Visual target states
        float tintStrength = present >= 2 ? twoPlayerTint : (present == 1 ? onePlayerTint : 0f);
        Color targetColor = Color.Lerp(baseColor, Color.red, Mathf.Clamp01(tintStrength));

        float targetDepress = Mathf.Max(0, present) * depressPerPlayer;
        Vector3 targetPos = _basePos + Vector3.down * targetDepress;

        // Apply visuals (smooth)
        _currentTint = Color.Lerp(_currentTint, targetColor, Time.deltaTime * colorLerpSpeed);
        ApplyTintToRenderers(_currentTint);

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * moveLerpSpeed);

        // Spawn on rising edge to 2 players
        if (present >= 2)
        {
            if (!spawnOnce || !_activatedOnce)
            {
                SpawnOnce();
                _activatedOnce = true;
            }
        }
    }

    private int CountPlayersPresent()
    {
        bool p1Inside = IsInsideZone(player1Root);
        bool p2Inside = IsInsideZone(player2Root);

        int count = 0;
        if (p1Inside) count++;
        if (p2Inside) count++;

        if (augmentWithProximityHeuristic && count == 1 && player1Root && player2Root)
        {
            // Identify bottom (inside) and other
            Transform inside = p1Inside ? player1Root : player2Root;
            Transform other = p1Inside ? player2Root : player1Root;

            if (inside && other)
            {
                Vector2 ib = inside.position;
                Vector2 ot = other.position;

                float dx = Mathf.Abs(ot.x - ib.x);
                float dy = ot.y - ib.y; // other above is positive

                // Heuristic: other is close horizontally AND not higher than allowed carry/stack height
                if (dx <= nearXDistance && dy >= -0.2f && dy <= nearYHeight)
                {
                    count = 2;
                }
            }
        }

        return count;
    }

    private bool IsInsideZone(Transform playerRoot)
    {
        if (!playerRoot) return false;

        if (useTriggerMode)
        {
            // Trigger mode: rely on OnTriggerEnter/Exit bookkeeping
            return _triggerInside.Contains(playerRoot) || AnyChildInSet(playerRoot, _triggerInside);
        }

        // Physics Overlap mode (recommended default)
        if (!areaCollider) return false;

        Bounds b = areaCollider.bounds;
        Vector2 center = b.center;
        Vector2 size = b.size + (Vector3)zoneExtraSize;

        // Overlap against playerLayers
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, playerLayers);
        if (hits == null || hits.Length == 0) return false;

        for (int i = 0; i < hits.Length; i++)
        {
            var t = hits[i].transform;
            if (!t) continue;
            if (t == playerRoot || t.IsChildOf(playerRoot)) return true;
        }
        return false;
    }

    private static bool AnyChildInSet(Transform root, HashSet<Transform> set)
    {
        if (set.Contains(root)) return true;
        foreach (Transform child in root)
        {
            if (set.Contains(child)) return true;
        }
        return false;
    }

    private void SpawnOnce()
    {
        CameraShaker.Shake(0.6f, 5f);
        if (!spawnPrefab) return;
        Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;
        Instantiate(spawnPrefab, pos, Quaternion.identity);
    }

    private void ApplyTintToRenderers(Color tint)
    {
        if (_tilemap)
        {
            var c = _tilemap.color; c.r = tint.r; c.g = tint.g; c.b = tint.b; c.a = tint.a;
            _tilemap.color = c;
        }
        else if (_tilemapRenderer)
        {
            var mpb = new MaterialPropertyBlock();
            _tilemapRenderer.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", tint);
            _tilemapRenderer.SetPropertyBlock(mpb);
        }

        if (_spriteRenderers != null)
        {
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                var sr = _spriteRenderers[i];
                if (!sr) continue;
                sr.color = tint;
            }
        }

        if (_genericRenderers != null)
        {
            for (int i = 0; i < _genericRenderers.Length; i++)
            {
                var r = _genericRenderers[i];
                if (!r) continue;
                var mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_Color", tint);
                r.SetPropertyBlock(mpb);
            }
        }
    }

    // Trigger bookkeeping (optional mode)
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTriggerMode) return;
        if (!triggerZone || other == triggerZone) return;

        // Map to p1/p2 roots
        if (player1Root && (other.transform == player1Root || other.transform.IsChildOf(player1Root)))
            _triggerInside.Add(player1Root);
        else if (player2Root && (other.transform == player2Root || other.transform.IsChildOf(player2Root)))
            _triggerInside.Add(player2Root);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!useTriggerMode) return;
        if (!triggerZone || other == triggerZone) return;

        if (player1Root && (other.transform == player1Root || other.transform.IsChildOf(player1Root)))
            _triggerInside.Remove(player1Root);
        else if (player2Root && (other.transform == player2Root || other.transform.IsChildOf(player2Root)))
            _triggerInside.Remove(player2Root);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!areaCollider) areaCollider = GetComponent<Collider2D>();
        if (!areaCollider) return;

        Bounds b = areaCollider.bounds;
        Vector2 size = b.size + (Vector3)zoneExtraSize;

        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawCube(b.center, size);
        Gizmos.color = new Color(1f, 0f, 0f, 1f);
        Gizmos.DrawWireCube(b.center, size);
    }
#endif
}
