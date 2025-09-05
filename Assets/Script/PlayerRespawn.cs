using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerRespawn : MonoBehaviour
{
    [Header("üũ����Ʈ ����")]
    public Transform firstCheckpoint;

    [Header("�÷��̾� �� �� ����")]
    public GameObject player1;
    public GameObject player2;

    [Header("���� Ű ����")]
    public KeyCode firstCheckpointKey = KeyCode.R; // ù ��° üũ����Ʈ
    public KeyCode lastSavedKey = KeyCode.Q;       // ������ ����� üũ����Ʈ

    void Update()
    {
        // R Ű �� ù ��° üũ����Ʈ
        if (Input.GetKeyDown(firstCheckpointKey) && firstCheckpoint != null)
        {
            SaveCheckpoint(firstCheckpoint.position);
            ReloadScene();
            Debug.Log("R Ű �� �� ���ε� �� ù ��° üũ����Ʈ�� �̵�!");
        }

        // Q Ű �� ������ ����� üũ����Ʈ
        if (Input.GetKeyDown(lastSavedKey) && PlayerPrefs.HasKey("SavedX"))
        {
            Vector3 savedPos = new Vector3(
                PlayerPrefs.GetFloat("SavedX"),
                PlayerPrefs.GetFloat("SavedY"),
                PlayerPrefs.GetFloat("SavedZ")
            );
            SaveCheckpoint(savedPos); // ��ǥ ����
            ReloadScene();
            Debug.Log("Q Ű �� �� ���ε� �� ������ ����� üũ����Ʈ�� �̵�!");
        }
    }

    private void SaveCheckpoint(Vector3 pos)
    {
        PlayerPrefs.SetFloat("SavedX", pos.x);
        PlayerPrefs.SetFloat("SavedY", pos.y);
        PlayerPrefs.SetFloat("SavedZ", pos.z);
        PlayerPrefs.Save();
    }

    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void Start()
    {
        // �� ���� �� ����� ��ġ �ҷ�����
        if (PlayerPrefs.HasKey("SavedX"))
        {
            Vector3 savedPos = new Vector3(
                PlayerPrefs.GetFloat("SavedX"),
                PlayerPrefs.GetFloat("SavedY"),
                PlayerPrefs.GetFloat("SavedZ")
            );

            if (player1 != null) player1.transform.position = savedPos;
            if (player2 != null) player2.transform.position = savedPos;
        }
        else if (firstCheckpoint != null)
        {
            MovePlayersToFirstCheckpoint();
        }
    }

    private void MovePlayersToFirstCheckpoint()
    {
        if (player1 != null) player1.transform.position = firstCheckpoint.position;
        if (player2 != null) player2.transform.position = firstCheckpoint.position;
    }
}