// BloodLayerCollisionDisabler2D.cs
using UnityEngine;

/// <summary>
/// �� ���� �� Physics2D ���̾� �浹 ��Ʈ��������
/// Blood ���̾���� �浹�� ���� �����ݴϴ�(�ɼ����� ���� ����).
/// - �⺻: Blood <-> ��� ���̾� �浹 ���� (Blood������ ����)
/// - Ư�� ���̾ ������ ������ ���� ����
/// </summary>
[DisallowMultipleComponent]
public class BloodLayerCollisionDisabler2D : MonoBehaviour
{
    [Header("Layer Names")]
    [SerializeField] private string bloodLayerName = "Blood";
    [SerializeField] private string[] allowedLayerNames = { "Default", "Ground" };

    [Header("Mode")]
    [Tooltip("true�� Blood�� ��� ���̾��� �浹�� ���� �����ϴ�.")]
    [SerializeField] private bool ignoreWithAllLayers = false;

    [Tooltip("ignoreWithAllLayers=false�� ���� ���: �� �迭�� ���̾��� Blood �浹�� �����ϴ�.")]
    [SerializeField] private string[] onlyTheseLayerNames = new string[0];



    [Tooltip("Blood ���̾���� �浹�� ������ ����")]
    [SerializeField] private bool alsoIgnoreBloodWithBlood = true;

    [Header("Lifecycle")]
    [Tooltip("Disable/Destroy �� ���� �浹 �������� �ǵ����ϴ�.")]
    [SerializeField] private bool revertOnDisable = false;

    private int bloodLayer = -1;
    private bool[] prevStates = new bool[32]; // ���� Ignore ���� �����
    private bool capturedPrev = false;

    private void Awake()
    {
        bloodLayer = LayerMask.NameToLayer(bloodLayerName);
        if (bloodLayer < 0)
        {
            Debug.LogWarning($"[BloodLayerCollisionDisabler2D] Layer '{bloodLayerName}' not found.");
            enabled = false;
            return;
        }

        ApplyIgnores();
    }

    private void Start()
    {
        int bloodLayer = LayerMask.NameToLayer("Blood");
        int playerLayer = LayerMask.NameToLayer("Player");

        if (bloodLayer >= 0 && playerLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(bloodLayer, playerLayer, true);
        }
    }

    private void OnEnable()
    {
        // ��Ȱ��ȭ �ÿ��� �ѹ� �� ����(�� ���ε�/��ũ��Ʈ ��� ���)
        if (bloodLayer >= 0) ApplyIgnores();
    }

    private void OnDisable()
    {
        if (revertOnDisable && bloodLayer >= 0)
            RevertIgnores();
    }

    private void OnDestroy()
    {
        if (revertOnDisable && bloodLayer >= 0)
            RevertIgnores();
    }

    private void ApplyIgnores()
    {
        // ���� ���� ������(�� ���� ����)
        if (!capturedPrev)
        {
            for (int l = 0; l < 32; l++)
                prevStates[l] = Physics2D.GetIgnoreLayerCollision(bloodLayer, l);
            capturedPrev = true;
        }

        if (ignoreWithAllLayers)
        {
            for (int l = 0; l < 32; l++)
            {
                if (l == bloodLayer && !alsoIgnoreBloodWithBlood) continue;
                Physics2D.IgnoreLayerCollision(bloodLayer, l, true);
            }
        }
        else
        {
            // ������ ���̾����� ����
            foreach (var name in onlyTheseLayerNames)
            {
                if (string.IsNullOrEmpty(name)) continue;
                int l = LayerMask.NameToLayer(name);
                if (l >= 0) Physics2D.IgnoreLayerCollision(bloodLayer, l, true);
                else Debug.LogWarning($"[BloodLayerCollisionDisabler2D] Layer '{name}' not found.");
            }
            // �ɼ�: Blood������ ����
            if (alsoIgnoreBloodWithBlood)
                Physics2D.IgnoreLayerCollision(bloodLayer, bloodLayer, true);
        }
    }
    private void RevertIgnores()
    {
        if (!capturedPrev) return;

        for (int l = 0; l < 32; l++)
            Physics2D.IgnoreLayerCollision(bloodLayer, l, prevStates[l]);

#if UNITY_EDITOR
        Debug.Log($"[BloodLayerCollisionDisabler2D] Reverted ignore matrix for '{bloodLayerName}'.", this);
#endif
    }
}
