using UnityEngine;
using System.Collections;

public class TrapMover : MonoBehaviour
{
    public enum Direction { Up, Down, Left, Right }
    public Direction moveDirection = Direction.Up;

    public float moveDistance = 2f;
    public float moveSpeed = 2f;

    public bool loop = false; // �߰�: �ݺ� �̵����� ����

    private Vector3 startPos;
    private Vector3 endPos;
    private Coroutine moveCoroutine;
    private bool isActivated = false;

    void Start()
    {
        startPos = transform.position;
        endPos = startPos + GetDirectionVector(moveDirection) * moveDistance;
    }

    public void Activate()
    {
        if (loop)
        {
            if (moveCoroutine == null)
                moveCoroutine = StartCoroutine(LoopMove());
        }
        else
        {
            if (!isActivated)
            {
                isActivated = true;
                moveCoroutine = StartCoroutine(MoveToPosition(endPos));
            }
        }
    }

    public void Deactivate()
    {
        if (loop)
        {
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }

            // ����ġ�� ����
            StartCoroutine(MoveToPosition(startPos));
        }
    }

    private IEnumerator MoveToPosition(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator LoopMove()
    {
        Vector3 from = startPos;
        Vector3 to = endPos;

        while (true)
        {
            yield return MoveToPosition(to);

            // ���� ����
            (from, to) = (to, from);
        }
    }

    private Vector3 GetDirectionVector(Direction dir)
    {
        return dir switch
        {
            Direction.Up => Vector3.up,
            Direction.Down => Vector3.down,
            Direction.Left => Vector3.left,
            Direction.Right => Vector3.right,
            _ => Vector3.zero
        };
    }
}