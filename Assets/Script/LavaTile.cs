using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LavaTileh : MonoBehaviour
{
    [Header("Detection")]
    public string lavaTag = "Lava";
    public bool reactToTrigger = true;     // Trigger �浹�� ó������
    public bool reactToCollision = true;   // ���� �浹�� ó������

    [Header("On Kill Action")]
    public KillAction killAction = KillAction.ReloadScene;
    public string sendMessageName = "OnKilled"; // SendMessage ��忡�� ȣ��� �޼����

    [Tooltip("RespawnAtPoint ��忡�� ����� ������ ����")]
    public Transform respawnPoint;
    [Tooltip("������ �� ���� �ð�(��). 0�̸� ��Ȱ��")]
    public float respawnInvincibleTime = 0.5f;

    private bool _isProcessing;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!reactToTrigger || _isProcessing) return;
        if (other.CompareTag(lavaTag)) HandleKill();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!reactToCollision || _isProcessing) return;
        if (collision.collider.CompareTag(lavaTag)) HandleKill();
    }

    void HandleKill()
    {
        if (_isProcessing) return;
        _isProcessing = true;

        switch (killAction)
        {
            case KillAction.ReloadScene:
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                break;

            case KillAction.DestroyPlayer:
                Destroy(gameObject);
                break;

            case KillAction.SendMessage:
                SendMessage(sendMessageName, SendMessageOptions.DontRequireReceiver);
                // SendMessage�� ���� �ܺο��� �� ����/�ִ�/���� ó��
                // �ߺ� �߻� ������ �÷��׸� ����
                break;

            case KillAction.RespawnAtPoint:
                if (respawnPoint != null)
                {
                    var rb = GetComponent<Rigidbody2D>();
                    if (rb) rb.linearVelocity = Vector2.zero;
                    transform.position = respawnPoint.position;

                    if (respawnInvincibleTime > 0f)
                        StartCoroutine(InvincibleWindow(respawnInvincibleTime));
                    else
                        _isProcessing = false; // ���� ���ٸ� ��� �Է� �簳
                }
                else
                {
                    // ������ ������ ������ �����ϰ� �� ���ε�
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
                break;
        }
    }

    IEnumerator InvincibleWindow(float seconds)
    {
        bool prevTrigger = reactToTrigger;
        bool prevCollision = reactToCollision;

        reactToTrigger = false;
        reactToCollision = false;

        yield return new WaitForSeconds(seconds);

        reactToTrigger = prevTrigger;
        reactToCollision = prevCollision;
        _isProcessing = false;
    }
}

public enum KillAction
{
    ReloadScene,
    DestroyPlayer,
    SendMessage,
    RespawnAtPoint
}
