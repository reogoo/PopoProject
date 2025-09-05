using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ZiplineHandle : MonoBehaviour
{
    public ZiplinePath path;
    [Tooltip("�ʴ� ���� �ӵ�(��θ� ���� m/s ����)")]
    public float travelSpeed = 6f;

    [Tooltip("�� ������ �ݻ�(�պ�)����, t�� 0 �Ǵ� 1�� ����(�ܹ���)����")]
    public bool pingPong = true;

    [Tooltip("���� t(0=������, 1=����)")]
    [Range(0f, 1f)] public float startT = 0f;

    [Tooltip("�ʱ� ���� ����(+1 ����, -1 ����)")]
    public int direction = +1;

    [Header("�����")]
    [SerializeField] private float t; // ���� �Ķ����
    [SerializeField] private Vector2 estLinearVelocity; // ���� �����ӵ�

    private Vector3 prevPos;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true; // �÷��̾ '���' �뵵�� Ʈ���� ��õ
    }

    void Start()
    {
        t = Mathf.Clamp01(startT);
        if (path != null && path.IsValid)
        {
            transform.position = path.GetPoint(t);
            prevPos = transform.position;
        }
    }

    void Update()
    {
        if (path == null || !path.IsValid) return;

        float dt = Mathf.Max(Time.deltaTime, 1e-6f);
        float delta = (travelSpeed / Mathf.Max(path.totalLength, 1e-6f)) * direction * dt;
        t += delta;

        if (pingPong)
        {
            if (t > 1f) { t = 1f; direction = -1; }
            else if (t < 0f) { t = 0f; direction = +1; }
        }
        else
        {
            t = Mathf.Clamp01(t);
            // �ܹ����̸� ������ ����
        }

        Vector3 newPos = path.GetPoint(t);
        estLinearVelocity = (newPos - prevPos) / dt;
        transform.position = newPos;
        prevPos = newPos;
    }

    // ���� ���� �ӵ� ����(����)
    public Vector2 CurrentLinearVelocity => estLinearVelocity;

    // ���� ���� ����(����)
    public Vector2 CurrentTangent()
    {
        if (path == null) return Vector2.right;
        return path.GetTangent(t);
    }

    public float CurrentT => t;
}
