// IgnoreDefaultLayer2D.cs
using UnityEngine;

[DisallowMultipleComponent]
public class IgnoreDefaultLayer2D : MonoBehaviour
{
    [SerializeField] private bool revertOnDisable = false;

    private int myLayer;
    private int defaultLayer;
    private bool prevState;

    private void Awake()
    {
        myLayer = gameObject.layer;
        defaultLayer = LayerMask.NameToLayer("Default");
        if (defaultLayer < 0)
        {
            Debug.LogWarning("[IgnoreDefaultLayer2D] 'Default' ���̾ ã�� ���߽��ϴ�.");
            enabled = false;
            return;
        }

        // ���� ���� ���� ��, Default���� �浹 ����
        prevState = Physics2D.GetIgnoreLayerCollision(myLayer, defaultLayer);
        Physics2D.IgnoreLayerCollision(myLayer, defaultLayer, true);
    }

    private void OnDisable()
    {
        if (revertOnDisable && defaultLayer >= 0)
        {
            // ���󺹱� (�ʿ��� ����)
            Physics2D.IgnoreLayerCollision(myLayer, defaultLayer, prevState);
        }
    }
}
