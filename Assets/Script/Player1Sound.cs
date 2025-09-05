using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Player1Sound : MonoBehaviour
{
    public AudioClip walkSound1;
    public AudioClip walkSound2;

    private AudioSource audioSource;
    private bool isGrounded;
    private int currentClipIndex = 0;
    private Coroutine walkCoroutine;

    public float stepDelay = 0.4f; // �߰��� ���� ������ ������

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;
    }

    void Update()
    {
        CheckGround();

        bool isMoving = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D);

        if (isMoving && isGrounded)
        {
            if (walkCoroutine == null)
            {
                walkCoroutine = StartCoroutine(PlayWalkSounds());
            }
        }
        else
        {
            // �̵� ���� �ƴϰų� ������ �������� ������ �ڷ�ƾ ����
            StopWalkingSound();
        }
    }

    private void StopWalkingSound()
    {
        if (walkCoroutine != null)
        {
            StopCoroutine(walkCoroutine);
            walkCoroutine = null;
            audioSource.Stop(); // Ȥ�� ��� ���� �Ҹ��� �ִٸ� ����
            Debug.Log("��Ҹ� ����");
        }
    }

    private IEnumerator PlayWalkSounds()
    {
        // ���� ������ �߼Ҹ� ���
        while (true)
        {
            // Ŭ���� �����ư��� ����
            audioSource.clip = (currentClipIndex == 0) ? walkSound1 : walkSound2;
            audioSource.Play();
            currentClipIndex = 1 - currentClipIndex;

            // ������ �����̸�ŭ ���
            yield return new WaitForSeconds(stepDelay);
        }
    }

    private void CheckGround()
    {
        // ĳ���� �ǹ����� �Ʒ��� Raycast�� ��� ���� ����
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 3f, LayerMask.GetMask("Ground"));
        isGrounded = hit.collider != null;
    }
}