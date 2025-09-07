using UnityEngine;

public class SmartCameraFollowByWall : MonoBehaviour
{
    public Transform target1;
    public Transform target2;
    public float followSpeed = 10f;
    public float rayDistance = 8f;
    public float raygroundDistance = 4f;
    public float yOffset = 3f;
    public LayerMask wallLayer;
    public LayerMask groundLayer;
    public SwapController.PlayerChar playerID; // Inspector���� P1 or P2 ����
    public SwapController swap; // �ν����Ϳ��� ���� �巡�� ���� (SwapController ������Ʈ)
    public PlayerMouseMovement carry;
    public bool swapsup = true;
    public GameObject selectmark1;
    public GameObject selectmark2;
    public PlayerMouseMovement rb;

    private bool blockLeft, blockRight, blockUp;
    private Vector3 currentVelocity;
    [SerializeField] private float arrowRotationOffsetDeg = 0f;

    [SerializeField] private Color nearColor = new Color(1f, 0.78f, 0.06f, 1f); // ���� ���(Amber #FFC107)
    [SerializeField] private Color farColor = new Color(1f, 0.97f, 0.71f, 1f); // ���� ���
    [SerializeField] private float nearDistance = 3f;   // �� �����̸� ���� nearColor/nearScale
    [SerializeField] private float farDistance = 25f;  // �� �̻��̸� ���� farColor/farScale
    [SerializeField] private UnityEngine.UI.Graphic indicatorGraphic; // ȭ��ǥ UI(Image ��)

    [SerializeField] private GameObject Knight_UI;
    [SerializeField] private GameObject Princess_UI;

    // ===== Off-screen Indicator UI =====
    [SerializeField] private Camera cam;                    // ����θ� �ڵ����� Camera.main ���
    [SerializeField] private RectTransform canvasRect;      // Canvas�� RectTransform
    [SerializeField] private RectTransform offscreenIndicator; // ȭ�� �����ڸ��� ���� ������(ȭ��ǥ)
    [SerializeField] private float edgePadding = 48f;       // ȭ�� �����ڸ��κ��� ����
    [SerializeField] private bool showDistance = false;     // ���Ͻø� �Ÿ� �ؽ�Ʈ�� ǥ��
    [SerializeField] private TMPro.TextMeshProUGUI distanceText; // (����) �Ÿ� ǥ�� �ؽ�Ʈ

    // --- ��� ������(���� ���̵� ��/�ƿ�) ---
    [Header("Danger Warning")]
    [SerializeField] private LayerMask hazardMask;           // Trap | Bullet | Monster ����
    [SerializeField] private float hazardCheckRadius = 3.0f; // �÷��̾�2 �ֺ� üũ �ݰ�
    [SerializeField] private RectTransform warnIcon;         // ȭ��ǥ ���� ��ġ�� ��� ������
    [SerializeField] private Vector2 warnScreenOffset = new Vector2(0f, 36f); // ȭ��ǥ���� ���� ����
    [SerializeField] private float warnBlinkSpeed = 6f;      // ������ ��¦
    [SerializeField] private float warnAlphaMin = 0.15f;
    [SerializeField] private float warnAlphaMax = 1f;
    [SerializeField] private float warnFadeOutSpeed = 8f;    // ������ ����� �� ������ �����
    private CanvasGroup warnGroup;
    private readonly Collider2D[] _hazardHits = new Collider2D[8];

    // === �߰�: ��ȯ ���� ===
    [Header("Tab ��ȯ �̵�")]
    [SerializeField] private bool disableWallGroundWhileTransit = true; // ��ȯ �� ��/�ٴ� ���� ����
    [SerializeField] private float transitArriveEps = 0.20f;            // ī�޶� ���� ����(���� ����)
    [SerializeField] private float transitMaxDuration = 1.2f;           // ��ȯ Ÿ�Ӿƿ�(��)
    [SerializeField] private float transitBoostFollowSpeed = 16f;       // ��ȯ �� �ӽ� ���� �ӵ�
    private bool isTransit = false;
    private float transitUntil = 0f;
    private float originalFollowSpeed = 0f;


    // === �Ÿ� ��� ������ ===
    [Header("Indicator Scale by Distance")]
    [SerializeField] private float nearScale = 1.4f;   // ����� �� ȭ��ǥ ũ��
    [SerializeField] private float farScale = 0.7f;    // �� �� ȭ��ǥ ũ��
    [SerializeField, Tooltip("������ ���� �ӵ�(�ʴ�)")]
    private float scaleLerpSpeed = 12f;

    // ���� ĳ��
    private Vector3 indicatorBaseScale = Vector3.one; // �ε������� ���� ������
    private float currentScale = 1f;                  // ���� ����(1=�⺻)

    private void Awake()
    {
        if (!cam) cam = Camera.main;
        originalFollowSpeed = followSpeed;

        if (offscreenIndicator)
        {
            if (!indicatorGraphic)
                indicatorGraphic = offscreenIndicator.GetComponent<UnityEngine.UI.Graphic>();
            offscreenIndicator.pivot = new Vector2(0.5f, 0.5f);
            offscreenIndicator.anchorMin = offscreenIndicator.anchorMax = new Vector2(0.5f, 0.5f);

            indicatorBaseScale = offscreenIndicator.localScale;
            currentScale = 1f;
        }

        if (warnIcon)
        {
            warnGroup = warnIcon.GetComponent<CanvasGroup>();
            if (!warnGroup) warnGroup = warnIcon.gameObject.AddComponent<CanvasGroup>();
            warnGroup.alpha = 0f;
            warnIcon.gameObject.SetActive(false);
        }
    }


    private void Reset()
    {
        Knight_UI = gameObject;
        Princess_UI = gameObject;
    }

    void Start()
    {
        selectmark2.SetActive(false);
        selectmark1.SetActive(true);
    }

    void Update()
    {
        Vector3 targetPos1 = target1.position;
        Vector3 targetPos2 = target2.position;
        Vector3 cameraPos = transform.position;

        // === ����: ��ȯ ���̸� ��/�ٴ� ������ ���� ===
        if (isTransit && disableWallGroundWhileTransit)
        {
            blockLeft = blockRight = blockUp = false;
        }
        else
        {
            blockLeft = Physics2D.Raycast(cameraPos, Vector2.left, rayDistance, wallLayer);
            blockRight = Physics2D.Raycast(cameraPos, Vector2.right, rayDistance, wallLayer);

            RaycastHit2D hitUpRaw = Physics2D.Raycast(cameraPos, Vector2.up, raygroundDistance, groundLayer);
            blockUp = hitUpRaw.collider != null && hitUpRaw.collider.tag != "OneWay";
        }

        float targetX = cameraPos.x;
        float targetY = cameraPos.y;

        Transform focus = swapsup ? target1 : target2;

        // === �� �Է�: ��ȯ ���� �������� ��ȯ ���� On + �ӵ� �ν�Ʈ ===
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (carry.carryset == false)
            {
                if (!swapsup)
                {
                    Knight_UI.SetActive(true);
                    Princess_UI.SetActive(false);
                    selectmark2.SetActive(false);
                    selectmark1.SetActive(true);
                    swapsup = true;
                }
                else
                {
                    Knight_UI.SetActive(false);
                    Princess_UI.SetActive(true);
                    selectmark2.SetActive(true);
                    selectmark1.SetActive(false);
                    swapsup = false;
                }

                // === ��ȯ ���� ===
                isTransit = true;
                transitUntil = Time.unscaledTime + transitMaxDuration;
                originalFollowSpeed = Mathf.Approximately(originalFollowSpeed, 0f) ? followSpeed : originalFollowSpeed;
                followSpeed = Mathf.Max(followSpeed, transitBoostFollowSpeed);
            }
        }

        if (swapsup)
        {
            if (!blockLeft && target1.position.x < cameraPos.x) targetX = target1.position.x;
            else if (!blockRight && target1.position.x > cameraPos.x) targetX = target1.position.x;

            float desiredY = target1.position.y + yOffset;
            if (!blockUp && desiredY > cameraPos.y) targetY = desiredY;
            else if (desiredY < cameraPos.y) targetY = desiredY;
        }
        else
        {
            if (!blockLeft && target2.position.x < cameraPos.x) targetX = target2.position.x;
            else if (!blockRight && target2.position.x > cameraPos.x) targetX = target2.position.x;

            float desiredY = target2.position.y + yOffset;
            if (!blockUp && desiredY > cameraPos.y) targetY = desiredY;
            else if (desiredY < cameraPos.y) targetY = desiredY;
        }

        Vector3 desiredPosition = new Vector3(targetX, targetY, cameraPos.z);
        transform.position = Vector3.SmoothDamp(cameraPos, desiredPosition, ref currentVelocity, 1f / followSpeed);

        // === ��ȯ ���� ����: ����� ��������ų� Ÿ�Ӿƿ� ===
        if (isTransit)
        {
            float remain = Vector2.Distance(new Vector2(transform.position.x, transform.position.y),
                                            new Vector2(desiredPosition.x, desiredPosition.y));

            bool arrived = remain <= transitArriveEps || currentVelocity.sqrMagnitude <= 0.0001f;
            bool timedOut = Time.unscaledTime >= transitUntil;

            if (arrived || timedOut)
            {
                isTransit = false;
                followSpeed = originalFollowSpeed; // �ӵ� ����
                                                   // ���� �����Ӻ��� �ٽ� ��/�ٴ� ���� ���̰� ����
            }
        }

        // === ����ũ ���� ===
        if (CameraShaker.Exists)
        {
            var s = CameraShaker.Instance;
            transform.position += (Vector3)s.CurrentOffset;
            if (Mathf.Abs(s.CurrentAngleZ) > 0.0001f)
                transform.rotation = Quaternion.Euler(0f, 0f, s.CurrentAngleZ);
            else
                transform.rotation = Quaternion.identity;
        }
        else
        {
            transform.rotation = Quaternion.identity;
        }

        Transform self = swapsup ? target1 : target2;
        Transform other = swapsup ? target2 : target1;
        UpdateOffscreenIndicator(other, self);
    }


    void OnDrawGizmos()
    {
        Vector3 cameraPos = transform.position;

        RaycastHit2D hitLeft = Physics2D.Raycast(cameraPos, Vector2.left, rayDistance, wallLayer);
        Gizmos.color = hitLeft.collider ? Color.blue : Color.red;
        Gizmos.DrawLine(cameraPos, cameraPos + Vector3.left * rayDistance);

        RaycastHit2D hitRight = Physics2D.Raycast(cameraPos, Vector2.right, rayDistance, wallLayer);
        Gizmos.color = hitRight.collider ? Color.blue : Color.red;
        Gizmos.DrawLine(cameraPos, cameraPos + Vector3.right * rayDistance);

        RaycastHit2D hitUp = Physics2D.Raycast(cameraPos, Vector2.up, raygroundDistance, groundLayer);
        Gizmos.color = hitUp.collider && hitUp.collider.tag != "OneWay" ? Color.blue : Color.red;
        Gizmos.DrawLine(cameraPos, cameraPos + Vector3.up * raygroundDistance);
    }

    private void UpdateOffscreenIndicator(Transform otherTarget, Transform selfTarget)
    {
        if (!offscreenIndicator || !canvasRect || !otherTarget) return;
        if (!cam) cam = Camera.main;
        if (!cam) return;

        // 1) ����� ����Ʈ ��ǥ
        Vector3 vp = cam.WorldToViewportPoint(otherTarget.position);

        // 2) ȭ�� ���̸� ȭ��ǥ/��� ����
        bool inFront = vp.z > 0f;
        bool onScreen = inFront && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
        if (onScreen)
        {
            offscreenIndicator.gameObject.SetActive(false);
            if (warnIcon) warnIcon.gameObject.SetActive(false);
            // �ʿ� �� ������ ����:
            // offscreenIndicator.localScale = indicatorBaseScale;
            return;
        }
        offscreenIndicator.gameObject.SetActive(true);

        // 3) ī�޶� �� �� �ݻ�
        Vector2 v2 = new Vector2(vp.x, vp.y);
        Vector2 center = new Vector2(0.5f, 0.5f);
        if (!inFront) v2 = center - (v2 - center);

        // 4) ����
        Vector2 dirFromCenter = (v2 - center).normalized;
        if (dirFromCenter.sqrMagnitude < 1e-6f) dirFromCenter = Vector2.right;

        // 5) ���� ���� (�е� �ݿ�)
        float padX = edgePadding / Screen.width;
        float padY = edgePadding / Screen.height;
        float minX = padX, maxX = 1f - padX;
        float minY = padY, maxY = 1f - padY;

        float t = float.PositiveInfinity;
        if (Mathf.Abs(dirFromCenter.x) > 1e-6f)
        {
            float tx1 = (minX - center.x) / dirFromCenter.x;
            float tx2 = (maxX - center.x) / dirFromCenter.x;
            if (tx1 > 0) t = Mathf.Min(t, tx1);
            if (tx2 > 0) t = Mathf.Min(t, tx2);
        }
        if (Mathf.Abs(dirFromCenter.y) > 1e-6f)
        {
            float ty1 = (minY - center.y) / dirFromCenter.y;
            float ty2 = (maxY - center.y) / dirFromCenter.y;
            if (ty1 > 0) t = Mathf.Min(t, ty1);
            if (ty2 > 0) t = Mathf.Min(t, ty2);
        }
        if (!float.IsFinite(t) || t <= 0) t = 0.001f;

        Vector2 edgeVP = center + dirFromCenter * t;

        // ���� (��ġ ���� ����)
        float dxMin = Mathf.Abs(edgeVP.x - minX);
        float dxMax = Mathf.Abs(edgeVP.x - maxX);
        float dyMin = Mathf.Abs(edgeVP.y - minY);
        float dyMax = Mathf.Abs(edgeVP.y - maxY);
        float best = Mathf.Min(Mathf.Min(dxMin, dxMax), Mathf.Min(dyMin, dyMax));
        if (best == dxMin) edgeVP.x = minX;
        else if (best == dxMax) edgeVP.x = maxX;
        else if (best == dyMin) edgeVP.y = minY;
        else edgeVP.y = maxY;

        // ���� (ȭ��ǥ ���� Ÿ�� ����)
        Vector2 dirFromEdgeToTarget = (v2 - edgeVP).normalized;
        if (dirFromEdgeToTarget.sqrMagnitude < 1e-6f) dirFromEdgeToTarget = dirFromCenter;
        float angle = Mathf.Atan2(dirFromEdgeToTarget.y, dirFromEdgeToTarget.x) * Mathf.Rad2Deg + arrowRotationOffsetDeg;

        // ����Ʈ�潺ũ����ĵ���� ��ǥ
        Vector2 screenPos = new Vector2(edgeVP.x * Screen.width, edgeVP.y * Screen.height);
        Canvas canvas = canvasRect.GetComponentInParent<Canvas>();
        Camera uiCam = (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? (canvas.worldCamera ? canvas.worldCamera : cam) : null;

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCam, out local);
        offscreenIndicator.anchoredPosition = local;
        offscreenIndicator.rotation = Quaternion.Euler(0f, 0f, angle);

        // --- �Ÿ� ��� �� & ������ ���� ---
        if (otherTarget && selfTarget)
        {
            float dist = Vector2.Distance(selfTarget.position, otherTarget.position);
            float closeness = 1f - Mathf.InverseLerp(nearDistance, farDistance, dist); // 0(�ִ�)~1(������)

            // �� ����
            if (indicatorGraphic)
                indicatorGraphic.color = Color.Lerp(farColor, nearColor, closeness);

            // ������ ����: �������� nearScale, �ּ��� farScale
            float targetScale = Mathf.Lerp(farScale, nearScale, closeness);
            currentScale = Mathf.Lerp(currentScale, targetScale, Time.unscaledDeltaTime * scaleLerpSpeed);
            offscreenIndicator.localScale = indicatorBaseScale * currentScale;

            // �Ÿ� �ؽ�Ʈ(����)
            if (showDistance && distanceText)
                distanceText.text = Mathf.RoundToInt(dist).ToString();
        }

        // --- ���� ���� & ��� ������ ���̵� ---
        if (warnIcon)
        {
            bool danger = IsDangerNear(otherTarget.position);
            // ȭ��ǥ ��ġ �������� ���ʿ� ����
            warnIcon.anchoredPosition = offscreenIndicator.anchoredPosition + warnScreenOffset;

            if (danger)
            {
                if (!warnIcon.gameObject.activeSelf) warnIcon.gameObject.SetActive(true);
                float tBlink = Mathf.PingPong(Time.unscaledTime * warnBlinkSpeed, 1f); // ������ ��¦
                warnGroup.alpha = Mathf.Lerp(warnAlphaMin, warnAlphaMax, tBlink);
            }
            else
            {
                // ���� ������ ������ ������� ��Ȱ��
                warnGroup.alpha = Mathf.MoveTowards(warnGroup.alpha, 0f, Time.unscaledDeltaTime * warnFadeOutSpeed);
                if (warnGroup.alpha <= 0.01f && warnIcon.gameObject.activeSelf)
                    warnIcon.gameObject.SetActive(false);
            }
        }
    }

    private bool IsDangerNear(Vector2 center)
    {
        // Ʈ��/�Ҹ�/���� ���̾ ���� �ݶ��̴��� �ϳ��� �ݰ� ���� ������ true
        return Physics2D.OverlapCircle(center, hazardCheckRadius, hazardMask) != null;
    }
}
