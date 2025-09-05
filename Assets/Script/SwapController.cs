using System;
using UnityEngine;

public class SwapController : MonoBehaviour
{
    public enum PlayerChar { P1, P2 }
    public PlayerChar charSelect = PlayerChar.P1; // �⺻�� P1 ����
    public PlayerMouseMovement carry;

    public PlayerChar Current; // ���� ������Ʈ�� �ҽ� ���� Ʈ�罺

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (carry.carryset == false)
            {
                // P1 <-> P2 ���
                charSelect = (charSelect == PlayerChar.P1) ? PlayerChar.P2 : PlayerChar.P1;
                Debug.Log($"[SwapController] ���� ���� = {charSelect}");
            }
        }
    }

}
