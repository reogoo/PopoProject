using UnityEngine;

public class OnlyCollideWithGround : MonoBehaviour
{
    [SerializeField] private string groundLayerName = "Ground";

    void OnEnable()
    {
        int myLayer = gameObject.layer;
        int groundLayer = LayerMask.NameToLayer(groundLayerName);

        // 0~31 ��� ���̾ ����, Ground�� �浹 ���, �������� ���� ����
        for (int i = 0; i < 32; i++)
        {
            bool allow = (i == groundLayer);
            Physics2D.IgnoreLayerCollision(myLayer, i, !allow);
        }
    }
}
