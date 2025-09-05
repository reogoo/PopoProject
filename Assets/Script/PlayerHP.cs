using UnityEngine;

public class PlayerHP : MonoBehaviour
{
    public int maxHP = 5;
    public int currentHP;

    public bool IsDead { get; private set; } = false; // �ܺο��� �б� ����, ���ο����� ����

    void Start()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int dmg = 1)
    {
        if (IsDead) return; // �̹� �׾����� ����

        currentHP -= dmg;
        Debug.Log("�÷��̾� HP: " + currentHP);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        IsDead = true;
        Debug.Log("�÷��̾� ���!");

        // TODO: ��� ���, ���� �Ұ� ó��, Rigidbody2D ���� ��
        // (Player1ó�� Ư�� ��� �ʿ��ϸ� ���� ���/Ȯ�� ����)
    }
}
