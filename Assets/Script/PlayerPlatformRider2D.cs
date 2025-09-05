using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerPlatformRider2D : MonoBehaviour
{
    [Tooltip("����.y�� �� �� �̻��̸� '������ ��Ҵ�'�� ����")]
    public float upNormalThreshold = 0.5f;
    [Tooltip("�÷������� �̼��� ƴ�� �޿�� �Ʒ��� ����(����)")]
    public float snapDownMax = 0.05f;

    Rigidbody2D _rb;
    PlatformMotionReporter _platform;  // ���� ��� �ִ� �÷���
    bool _onTop;                       // ������ ��� ������

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (_platform == null || !_onTop) return;

        // 1) �÷����� ���������� ��pos��ŭ �÷��̾ ���� MovePosition
        Vector2 carryDelta = _platform.DeltaPosition;
        if (carryDelta.sqrMagnitude > 0f)
        {
            _rb.MovePosition(_rb.position + carryDelta);
        }

        // 2) ���� �ӵ��� �÷������� �Ʒ��� �������� �ʵ��� ����
        //    (���� �� ���� ���ư� ������ ����)
        Vector2 v = _rb.linearVelocity;
        if (v.y < _platform.FixedVelocity.y)
            v.y = _platform.FixedVelocity.y;
        _rb.linearVelocity = v;

        // 3) �̼��� ƴ�� ����� ��¦ �Ʒ��� ����(���� Ʀ ����)
        //    (�÷��̾� �ٴڿ��� ���� ���� Ray�� üũ)
        Vector2 origin = _rb.position + Vector2.down * 0.01f;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, snapDownMax, ~0);
        if (hit && hit.collider.GetComponent<PlatformMotionReporter>() == _platform)
        {
            float gap = hit.distance;
            if (gap > 1e-4f)
                _rb.MovePosition(_rb.position + Vector2.down * gap);
        }
    }

    void OnCollisionEnter2D(Collision2D col) { TryAttach(col); }
    void OnCollisionStay2D(Collision2D col) { TryAttach(col); }
    void OnCollisionExit2D(Collision2D col)
    {
        if (_platform != null && col.collider.GetComponent<PlatformMotionReporter>() == _platform)
        {
            _platform = null;
            _onTop = false;
        }
    }

    void TryAttach(Collision2D col)
    {
        var rep = col.collider.GetComponent<PlatformMotionReporter>();
        if (rep == null) return;

        // '������' ���� ���˸� ����
        bool top = false;
        foreach (var c in col.contacts)
        {
            if (c.normal.y >= upNormalThreshold) { top = true; break; }
        }

        if (top)
        {
            _platform = rep;
            _onTop = true;
        }
    }
}
