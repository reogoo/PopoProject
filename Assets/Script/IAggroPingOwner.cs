using UnityEngine;

namespace Game.AI
{
    public interface IAggroPingOwner
    {
        // ���� �÷��̾ ����� �� ���ο��� �˷���
        void OnAggroPingHit(Transform hitPlayer);
    }
}

