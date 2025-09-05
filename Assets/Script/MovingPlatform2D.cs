using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform2D : MonoBehaviour
{
    public float moveDistance = 2f;
    public float moveSpeed = 2f;
    public bool moveUp = true;
    public bool loop = true;      // true�� �պ�
    public bool activeOnStart = false;

    public Vector2 CurrentVelocity { get; private set; }

    Vector2 _startPos;
    Vector2 _endPos;
    Rigidbody2D _rb;
    float _t;           // 0~1 ���� �պ���
    int _dir = 1;       // ���� ����
    bool _active;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        // �� �κ� ����
        // _rb.bodyType = Rigidbody2D.Kinematic;

        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        _startPos = transform.position;
        Vector2 delta = (moveUp ? Vector2.up : Vector2.down) * moveDistance;
        _endPos = _startPos + delta;
        _active = activeOnStart;
    }


    public void SetActive(bool on) => _active = on;

    void FixedUpdate()
    {
        if (!_active) { CurrentVelocity = Vector2.zero; return; }

        // �պ� ����
        float step = (moveSpeed / Mathf.Max(0.0001f, moveDistance)) * Time.fixedDeltaTime * _dir;
        float prevT = _t;
        _t = Mathf.Clamp01(_t + step);

        Vector2 from = (_dir > 0) ? _startPos : _endPos;
        Vector2 to = (_dir > 0) ? _endPos : _startPos;

        Vector2 newPos = Vector2.Lerp(from, to, _t);
        Vector2 prevPos = _rb.position;
        _rb.MovePosition(newPos);

        // �̹� Fixed �������� �÷��� �ӵ�
        CurrentVelocity = (newPos - prevPos) / Time.fixedDeltaTime;

        // ���� ���� �� ���� ��ȯ(������ ��)
        if (loop && (_t <= 0f || _t >= 1f))
        {
            _dir *= -1;
        }
    }
}
