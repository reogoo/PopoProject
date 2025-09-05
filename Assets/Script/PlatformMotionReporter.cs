using UnityEngine;

public class PlatformMotionReporter : MonoBehaviour
{
    public Vector2 DeltaPosition { get; private set; }
    public Vector2 FixedVelocity { get; private set; }

    Vector3 _lastFixedPos;

    void Start()
    {
        _lastFixedPos = transform.position;
    }

    // �÷����� Update/LateUpdate���� Transform���� ��������,
    // Fixed �� '�� ���� �� �̵���'�� ���մϴ�.
    void FixedUpdate()
    {
        Vector3 now = transform.position;
        Vector2 delta = (Vector2)(now - _lastFixedPos);
        DeltaPosition = delta;
        FixedVelocity = delta / Mathf.Max(Time.fixedDeltaTime, 1e-6f);
        _lastFixedPos = now;
    }
}
