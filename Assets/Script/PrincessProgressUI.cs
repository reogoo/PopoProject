using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ����~�� �������� '����/�÷��̾�1' ���൵�� ��/�̴ϸʿ� ǥ��.
/// ���� �ֺ��� Monster/Trap ���̾� ���� �� ���� ��Ŀ(�׸��� ���������� �̴ϸ� ��) ���̵� ��/�ƿ�.
/// </summary>
public class PrincessProgressUI : MonoBehaviour
{
    [Header("World References")]
    [SerializeField] private Transform princess;
    [SerializeField] private Transform player1;
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;

    [Header("Progress Bar UI (1D)")]
    [SerializeField] private RectTransform barRect;           // ���� �� ����
    [SerializeField] private RectTransform markerPrincess;    // ���� ��Ŀ
    [SerializeField] private RectTransform markerPlayer1;     // P1 ��Ŀ
    [SerializeField] private Image fillImage;                 // (����) ä��� �̹���(Filled-Horizontal ����)

    [Header("MiniMap UI (2D, Optional)")]
    [SerializeField] private RectTransform miniMapRect;       // �̴ϸ� �ڽ�
    [SerializeField] private RectTransform dotPrincess;       // ���� ��
    [SerializeField] private RectTransform dotPlayer1;        // P1 ��
    [Tooltip("����/�� AABB�� ���� �е�(���� ����)")]
    [SerializeField] private float worldPadding = 1f;

    [Header("Smoothing")]
    [Tooltip("UI ���� �ð�(��). 0�̸� ��� �ݿ�")]
    [SerializeField] private float smoothTime = 0.08f;
    private float _tPrincessSmoothed; // 0~1
    private float _tP1Smoothed;       // 0~1

    // ================== Danger Blink ==================
    [Header("Danger Blink (Princess)")]
    [Tooltip("���� ���� ���̾�(��: Monster, Trap)")]
    [SerializeField] private LayerMask dangerMask;
    [Tooltip("���� �ֺ� ���� ���� �ݰ�(���� ����)")]
    [SerializeField] private float dangerRadius = 2.0f;
    [Tooltip("���̵� ������ �ӵ�(�ֱ�/��)")]
    [SerializeField] private float blinkSpeed = 4.0f;
    [Tooltip("������ ���� �ּ�/�ִ�")]
    [SerializeField] private float blinkMinAlpha = 0.35f;
    [SerializeField] private float blinkMaxAlpha = 1.0f;
    [Tooltip("�̴ϸ� ���� ���� ��������")]
    [SerializeField] private bool blinkMiniMapDotAlso = true;
    [Tooltip("UnscaledTime ���(�Ͻ����� �߿��� ������ ����)")]
    [SerializeField] private bool useUnscaledTime = true;

    // ĳ��/����
    private CanvasGroup _cgMarkerPrincess;
    private Graphic[] _gfxMarkerPrincess;

    private CanvasGroup _cgDotPrincess;
    private Graphic[] _gfxDotPrincess;

    private float _blinkPhase;         // ���� ����
    private bool _dangerNow;           // �̹� ������ ���� ����
    private bool _dangerPrev;          // ���� ������ ���� ����

    // NonAlloc ĳ��
    private readonly Collider2D[] _dangerHits = new Collider2D[8];

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.6f, 0.2f, 1f);

    void Reset()
    {
        if (!princess) princess = GameObject.Find("Princess")?.transform;
        if (!player1) player1 = GameObject.Find("Player1")?.transform;
    }

    void Awake()
    {
        // ���� ��Ŀ �׷��� ĳ��
        if (markerPrincess)
        {
            markerPrincess.TryGetComponent(out _cgMarkerPrincess);
            _gfxMarkerPrincess = markerPrincess.GetComponentsInChildren<Graphic>(true);
        }
        // �̴ϸ� �� �׷��� ĳ��
        if (dotPrincess)
        {
            dotPrincess.TryGetComponent(out _cgDotPrincess);
            _gfxDotPrincess = dotPrincess.GetComponentsInChildren<Graphic>(true);
        }
    }

    void LateUpdate()
    {
        // ===== ���൵ ��� =====
        if (!startPoint || !endPoint) return;

        Vector2 s = startPoint.position;
        Vector2 e = endPoint.position;
        Vector2 v = e - s;
        float len = v.magnitude;
        if (len <= 1e-5f)
        {
            UpdateBarUI(0f, 0f);
            UpdateMiniMapUI(Vector2.zero, Vector2.zero, s, e);
            return;
        }

        Vector2 dir = v / len;

        float tPrincess = 0f, tP1 = 0f;
        Vector2 pPos = s, p1Pos = s;

        if (princess)
        {
            pPos = princess.position;
            float projP = Mathf.Clamp(Vector2.Dot(pPos - s, dir), 0f, len);
            tPrincess = projP / len;
        }
        if (player1)
        {
            p1Pos = player1.position;
            float proj1 = Mathf.Clamp(Vector2.Dot(p1Pos - s, dir), 0f, len);
            tP1 = proj1 / len;
        }

        // ������
        if (smoothTime > 0f)
        {
            float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(1e-4f, smoothTime));
            _tPrincessSmoothed = Mathf.Lerp(_tPrincessSmoothed, tPrincess, k);
            _tP1Smoothed = Mathf.Lerp(_tP1Smoothed, tP1, k);
        }
        else
        {
            _tPrincessSmoothed = tPrincess;
            _tP1Smoothed = tP1;
        }

        // UI �ݿ�
        UpdateBarUI(_tPrincessSmoothed, _tP1Smoothed);
        UpdateMiniMapUI(pPos, p1Pos, s, e);

        // ===== ���� ���� & ������ =====
        UpdateDangerState();
        UpdateBlinkVisuals();
    }

    // ---------------- Progress Bar (�ǹ� ����: �׻� ���ʡ������) ----------------
    private void UpdateBarUI(float tPrincess, float tP1)
    {
        tPrincess = Mathf.Clamp01(tPrincess);
        tP1 = Mathf.Clamp01(tP1);

        if (barRect)
        {
            float w = barRect.rect.width;
            float leftX = -w * barRect.pivot.x;
            float rightX = w * (1f - barRect.pivot.x);

            if (markerPrincess)
            {
                var pos = markerPrincess.anchoredPosition;
                pos.x = Mathf.Lerp(leftX, rightX, tPrincess);
                markerPrincess.anchoredPosition = pos;
            }
            if (markerPlayer1)
            {
                var pos = markerPlayer1.anchoredPosition;
                pos.x = Mathf.Lerp(leftX, rightX, tP1);
                markerPlayer1.anchoredPosition = pos;
            }
        }

        if (fillImage)
        {
            if (fillImage.type != Image.Type.Filled) fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = tPrincess; // ���� ���൵�� ä��(�ʿ�� �ٲټ���)
        }
    }

    // ---------------- MiniMap (�ǹ� ����: �׻� ����-�Ʒ� ����) ----------------
    private void UpdateMiniMapUI(Vector2 princessWorld, Vector2 p1World, Vector2 s, Vector2 e)
    {
        if (!miniMapRect) return;

        Vector2 min = new Vector2(Mathf.Min(s.x, e.x), Mathf.Min(s.y, e.y)) - Vector2.one * worldPadding;
        Vector2 max = new Vector2(Mathf.Max(s.x, e.x), Mathf.Max(s.y, e.y)) + Vector2.one * worldPadding;

        float w = miniMapRect.rect.width;
        float h = miniMapRect.rect.height;
        float leftUI = -w * miniMapRect.pivot.x;
        float bottomUI = -h * miniMapRect.pivot.y;

        if (dotPrincess)
        {
            float nx = Mathf.InverseLerp(min.x, max.x, princessWorld.x);
            float ny = Mathf.InverseLerp(min.y, max.y, princessWorld.y);
            var pos = dotPrincess.anchoredPosition;
            pos.x = leftUI + nx * w;
            pos.y = bottomUI + ny * h;
            dotPrincess.anchoredPosition = pos;
        }

        if (dotPlayer1)
        {
            float nx = Mathf.InverseLerp(min.x, max.x, p1World.x);
            float ny = Mathf.InverseLerp(min.y, max.y, p1World.y);
            var pos = dotPlayer1.anchoredPosition;
            pos.x = leftUI + nx * w;
            pos.y = bottomUI + ny * h;
            dotPlayer1.anchoredPosition = pos;
        }
    }

    // ---------------- Danger detection + Blink ----------------
    private void UpdateDangerState()
    {
        _dangerPrev = _dangerNow;

        if (!princess)
        {
            _dangerNow = false;
            return;
        }

        int count = Physics2D.OverlapCircleNonAlloc(princess.position, dangerRadius, _dangerHits, dangerMask);
        _dangerNow = (count > 0);
        if (!_dangerPrev && _dangerNow)
        {
            // �� ���� ���� �� ���� �ʱ�ȭ�� ��� ���� ���
            _blinkPhase = 0f;
        }
    }

    private void UpdateBlinkVisuals()
    {
        if (_dangerNow)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _blinkPhase += Mathf.Max(0f, blinkSpeed) * dt * Mathf.PI * 2f; // rad/s
            float s = 0.5f * (1f + Mathf.Sin(_blinkPhase)); // 0~1
            float alpha = Mathf.Lerp(blinkMinAlpha, blinkMaxAlpha, s);

            // ���� �� ��Ŀ
            ApplyAlphaToTarget(markerPrincess, _cgMarkerPrincess, _gfxMarkerPrincess, alpha);

            // (�ɼ�) �̴ϸ� ��
            if (blinkMiniMapDotAlso)
                ApplyAlphaToTarget(dotPrincess, _cgDotPrincess, _gfxDotPrincess, alpha);
        }
        else
        {
            // ���� ����: ���� ����
            ApplyAlphaToTarget(markerPrincess, _cgMarkerPrincess, _gfxMarkerPrincess, 1f);
            if (blinkMiniMapDotAlso)
                ApplyAlphaToTarget(dotPrincess, _cgDotPrincess, _gfxDotPrincess, 1f);
        }
    }

    private static void ApplyAlphaToTarget(RectTransform target, CanvasGroup cg, Graphic[] gfxList, float a)
    {
        if (!target) return;

        if (cg)
        {
            cg.alpha = a;
            return;
        }

        if (gfxList != null && gfxList.Length > 0)
        {
            for (int i = 0; i < gfxList.Length; i++)
            {
                var g = gfxList[i];
                if (!g) continue;
                var c = g.color;
                c.a = a;
                g.color = c;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (drawGizmos && princess)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(princess.position, dangerRadius);
        }

        if (!drawGizmos || !startPoint || !endPoint) return;
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(startPoint.position, 0.15f);
        Gizmos.DrawSphere(endPoint.position, 0.15f);
        Gizmos.DrawLine(startPoint.position, endPoint.position);
    }
}
