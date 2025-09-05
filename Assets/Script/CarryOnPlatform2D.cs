using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CarryOnPlatform2D : MonoBehaviour
{
    Rigidbody2D _rb;
    MovingPlatform2D _currentPlatform;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (_currentPlatform != null)
        {
            // �÷��̾� ��ü �̵� ���� ��� ���� �÷��� �ӵ��� ���� ���� �̵�
            Vector2 v = _rb.linearVelocity;
            v += _currentPlatform.CurrentVelocity;
            _rb.linearVelocity = v;
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        var mp = col.collider.GetComponent<MovingPlatform2D>();
        if (mp != null)
        {
            // ������ ���� ��쿡�� ž������ ����(������ ����)
            foreach (var c in col.contacts)
            {
                if (c.normal.y > 0.5f) { _currentPlatform = mp; break; }
            }
        }
    }

    void OnCollisionExit2D(Collision2D col)
    {
        if (_currentPlatform != null && col.collider.GetComponent<MovingPlatform2D>() == _currentPlatform)
        {
            _currentPlatform = null;
        }
    }
}
