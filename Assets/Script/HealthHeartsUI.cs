using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player1HP�� HpChanged �̺�Ʈ�� �޾�, ��Ʈ(GameObject)���� On/Off�� ǥ��.
/// Hearts Parent�� "���� �ڽ�"�� ��Ʈ�� ����(�¡��).
/// </summary>
public class HealthHeartsUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Player1HP playerHP;
    [SerializeField] private Transform heartsParent; // �ڽĵ��� ��Ʈ

    private readonly List<GameObject> _hearts = new();

    private void Reset()
    {
        // �����Ϳ��� �ڵ� ä��� �õ�
        if (!playerHP) playerHP = FindAnyObjectByType<Player1HP>();
        if (!heartsParent) heartsParent = transform;
    }

    private void Awake()
    {
        if (!heartsParent) heartsParent = transform;

        _hearts.Clear();
        for (int i = 0; i < heartsParent.childCount; i++)
        {
            var child = heartsParent.GetChild(i).gameObject;
            _hearts.Add(child);
        }
    }

    private void OnEnable()
    {
        if (!playerHP)
        {
            playerHP = FindAnyObjectByType<Player1HP>();
        }

        if (playerHP != null)
        {
            playerHP.HpChanged += OnHpChanged;
            playerHP.Died += OnDied;

            // �ʱ� ���� ����ȭ
            OnHpChanged(playerHP.CurrentHP, playerHP.MaxHP);
        }
        else
        {
            Debug.LogWarning("[HealthHeartsUI] Player1HP ���۷����� �����ϴ�.");
        }
    }

    private void OnDisable()
    {
        if (playerHP != null)
        {
            playerHP.HpChanged -= OnHpChanged;
            playerHP.Died -= OnDied;
        }
    }

    private void OnHpChanged(int current, int max)
    {
        int total = _hearts.Count;

        for (int i = 0; i < total; i++)
        {
            // �����ʺ��� �������� ���� �ε��� ���
            // ex) current=2 �� ������ 2�� ��, ���� 1�� ��
            bool visible = i >= total - current;
            _hearts[i].SetActive(visible);
        }
    }

    private void OnDied()
    {
        // ����: ��� �� ��� Off�� Ȯ���� ����
        for (int i = 0; i < _hearts.Count; i++)
            _hearts[i].SetActive(false);
    }
}
