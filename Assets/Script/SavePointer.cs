using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// - �� ��ũ��Ʈ�� ���� ������Ʈ�� SavePoint ���̾� ������Ʈ�� �����ϸ� �ش� ��ġ�� �����Ѵ�.
/// - �� �ε� �� ���������� ����� ��ġ�� �÷��̾�(�Ǵ� Player1 ������)�� ��ġ/��ȯ�Ѵ�.
/// - ���� �����ʹ� PlayerPrefs�� ���� ����ȴ�.
/// </summary>
[DisallowMultipleComponent]
public class SavePointer : MonoBehaviour
{
    [Header("SavePoint Layer")]
    [Tooltip("���̺� ����Ʈ�� ���� ���̾� �̸� (�⺻: SavePoint)")]
    [SerializeField] private string savePointLayerName = "SavePoint";
    private int _savePointLayer = -1;

    [Header("Spawn / Player")]
    [Tooltip("�� �ε� �� ���������� �÷��̾ �̵�/��ȯ���� ����")]
    [SerializeField] private bool spawnOnSceneLoaded = true;

    [Tooltip("�÷��̾� �±�(���� ���� �Ǵܿ�). ��������� 'Player1'�� ���")]
    [SerializeField] private string playerTag = "Player1";

    [Tooltip("�÷��̾ ���� �� ��ȯ�� Player1 ������")]
    [SerializeField] private GameObject player1Prefab;

    [Tooltip("�÷��̾ �� ã���� 'Player1' �̸����ε� ã�ƺ���")]
    [SerializeField] private bool alsoFindByName = true;

    [Header("Advanced")]
    [Tooltip("���̺� ����Ʈ �ݶ��̴��� ��ġ ���, SavePointAnchor ������Ʈ�� ������ �� ��ġ�� ����/���")]
    [SerializeField] private bool preferAnchor = true;

    // PlayerPrefs Ű
    private const string K_ACTIVE = "SAVE_ACTIVE";
    private const string K_SCENE = "SAVE_SCENE";
    private const string K_X = "SAVE_X";
    private const string K_Y = "SAVE_Y";
    private const string K_Z = "SAVE_Z";

    private void Awake()
    {
        _savePointLayer = LayerMask.NameToLayer(savePointLayerName);
        if (_savePointLayer < 0)
            Debug.LogWarning($"[SavePointer] Layer '{savePointLayerName}' �� ã�� �� �����ϴ�. �ν����Ϳ��� ���̾���� Ȯ���ϼ���.", this);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // --- Trigger/Collision�� ���̺� ����Ʈ ���� ���� ---
    private void OnTriggerEnter2D(Collider2D other) { TrySaveFromCollider(other); }
    private void OnCollisionEnter2D(Collision2D col) { TrySaveFromCollider(col.collider); }

    private void TrySaveFromCollider(Collider2D col)
    {
        if (!col) return;
        // ���̾� ��ġ Ȯ��
        if (_savePointLayer >= 0 && col.gameObject.layer != _savePointLayer) return;

        Vector3 savePos = col.transform.position;
        if (preferAnchor && col.GetComponentInParent<SavePointAnchor>(true) is SavePointAnchor anchor && anchor.spawnPoint)
            savePos = anchor.spawnPoint.position;

        SaveToPrefs(SceneManager.GetActiveScene().name, savePos);
#if UNITY_EDITOR
        Debug.Log($"[SavePointer] Saved at {savePos} (scene='{SceneManager.GetActiveScene().name}')", col);
#endif
    }

    // --- �� �ε� �� ��Ȱ ó�� ---
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!spawnOnSceneLoaded) return;

        if (!HasSave()) return; // ���� ����
        string savedScene = PlayerPrefs.GetString(K_SCENE, "");
        Vector3 savedPos = new Vector3(
            PlayerPrefs.GetFloat(K_X, transform.position.x),
            PlayerPrefs.GetFloat(K_Y, transform.position.y),
            PlayerPrefs.GetFloat(K_Z, transform.position.z)
        );

        // "���� �ε�� ��"�� ������ ����� ���� ���� ���� ��ġ�� ����
        if (!string.IsNullOrEmpty(savedScene) && savedScene == scene.name)
        {
            var player = FindPlayerObject();
            if (player != null)
            {
                // �̹� �÷��̾ �ִ� �� ��ġ�� �̵�
                player.transform.position = savedPos;
            }
            else
            {
                // �÷��̾ ���� �� ���������� ��ȯ
                if (player1Prefab != null)
                {
                    var spawned = Instantiate(player1Prefab, savedPos, Quaternion.identity);
                    // �±װ� ����ְ� �����տ� �±װ� ���ٸ� �±� ����
                    TryAssignTag(spawned);
                }
                else
                {
                    Debug.LogWarning("[SavePointer] Player1 �������� �������� �ʾ� ��ȯ�� �� �����ϴ�.", this);
                }
            }
        }
        // ���� �ٸ��� �ƹ��͵� ���� �ʴ´�(�䱸����: �ش� �� �ε�� ������ ���̺꿡�� ��Ȱ)
    }

    // --- ����/�ε� ��ƿ ---
    private static bool HasSave() => PlayerPrefs.GetInt(K_ACTIVE, 0) == 1;

    private static void SaveToPrefs(string sceneName, Vector3 pos)
    {
        PlayerPrefs.SetInt(K_ACTIVE, 1);
        PlayerPrefs.SetString(K_SCENE, sceneName);
        PlayerPrefs.SetFloat(K_X, pos.x);
        PlayerPrefs.SetFloat(K_Y, pos.y);
        PlayerPrefs.SetFloat(K_Z, pos.z);
        PlayerPrefs.Save();
    }

    public static void ClearSave()
    {
        PlayerPrefs.DeleteKey(K_ACTIVE);
        PlayerPrefs.DeleteKey(K_SCENE);
        PlayerPrefs.DeleteKey(K_X);
        PlayerPrefs.DeleteKey(K_Y);
        PlayerPrefs.DeleteKey(K_Z);
        PlayerPrefs.Save();
    }

    // --- Player Ž��/��ȯ ���� ---
    private GameObject FindPlayerObject()
    {
        GameObject player = null;

        // 1) �±� �켱
        if (!string.IsNullOrEmpty(playerTag))
            player = GameObject.FindWithTag(playerTag);

        // 2) �̸� ����
        if (player == null && alsoFindByName)
        {
            var go = GameObject.Find("Player1");
            if (go) player = go;
        }

        return player;
    }

    private void TryAssignTag(GameObject go)
    {
        if (!go) return;
        if (string.IsNullOrEmpty(playerTag)) return;
        try
        {
            // ������/������Ʈ�� ���� �±װ� ���ٸ� �ο� (��ȿ�� �±׿��� ��)
            if (go.CompareTag("Untagged"))
                go.tag = playerTag;
        }
        catch { /* ��ȿ���� ���� �±׸� ���� */ }
    }

    // ����׿� ���ؽ�Ʈ �޴�
    [ContextMenu("Force Save Here (Current Scene)")]
    private void Editor_ForceSaveHere()
    {
        SaveToPrefs(SceneManager.GetActiveScene().name, transform.position);
        Debug.Log($"[SavePointer] Force saved at {transform.position}");
    }

    [ContextMenu("Clear Save")]
    private void Editor_ClearSave()
    {
        ClearSave();
        Debug.Log("[SavePointer] Save cleared");
    }
}

/// <summary>
/// (����) ���̺� ����Ʈ�� �ٿ��� ���� ��ġ�� �����ϰ� ���� �� ���.
/// SavePointer�� preferAnchor=true�� ��, �� ������Ʈ�� �켱 ����Ѵ�.
/// </summary>
public class SavePointAnchor : MonoBehaviour
{
    public Transform spawnPoint;
}
