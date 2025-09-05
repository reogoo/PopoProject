using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class IntroSequence : MonoBehaviour
{
    [Header("UI Targets")]
    public CanvasGroup textGroup;     // ó�� �ؽ�Ʈ
    public CanvasGroup image1Group;   // �̹��� 1
    public CanvasGroup image2Group;   // �̹��� 2

    [Header("Video")]
    public VideoPlayer videoPlayer;   // ������
    public CanvasGroup videoCanvas;   // ���� ��¿� ĵ����
    public string nextSceneName = "SampleScene 1";

    [Header("Durations")]
    public float fadeTime = 1.5f;
    public float stayTime = 1.5f;

    private bool skipRequested = false;

    void Start()
    {
        StartCoroutine(RunSequence());
        
    }

    IEnumerator RunSequence()
    {
        // Step 1: �ؽ�Ʈ
        yield return StartCoroutine(FadeInOut(textGroup));

        // Step 2: �̹���1
        yield return StartCoroutine(FadeInOut(image1Group));

        // Step 3: �̹���2
        yield return StartCoroutine(FadeInOut(image2Group));

        // Step 5: ���� ��
        SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator FadeInOut(CanvasGroup group)
    {
        // �ʱ� ���� 0
        group.alpha = 0f;
        group.gameObject.SetActive(true);

        // Fade In
        yield return StartCoroutine(Fade(group, 0f, 1f, fadeTime));
        yield return new WaitForSeconds(stayTime);

        // Fade Out
        yield return StartCoroutine(Fade(group, 1f, 0f, fadeTime));
        group.gameObject.SetActive(false);
    }

    IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }

    IEnumerator PlayVideo()
    {
        skipRequested = false;
        videoCanvas.alpha = 1f;
        videoCanvas.gameObject.SetActive(true);
        videoPlayer.time = 9f;
        videoPlayer.Play();

        // �̺�Ʈ ���
        videoPlayer.loopPointReached += OnVideoEnd;

        // ���� �Է� üũ
        while (!skipRequested)
        {
            if (Input.GetMouseButtonDown(0)) // ���콺 Ŭ�� �� ��ŵ
            {
                videoPlayer.Stop();
                break;
            }
            yield return null;
        }

        // ����
        videoCanvas.alpha = 0f;
        videoCanvas.gameObject.SetActive(false);
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        skipRequested = true;
    }
}
