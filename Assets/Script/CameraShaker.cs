using UnityEngine;

[DisallowMultipleComponent]
public class CameraShaker : MonoBehaviour
{
    public static CameraShaker Instance { get; private set; }
    public static bool Exists => Instance != null;

    [Header("Amplitude")]
    [Tooltip("������ ��鸲 �ִ�ġ(���� ����)")]
    [SerializeField] private float maxPositionShake = 0.6f;
    [Tooltip("Z ȸ�� ��鸲 �ִ�ġ(��)")]
    [SerializeField] private float maxRotationShake = 4f;

    [Header("Noise")]
    [Tooltip("������ ���ļ�(���� Ŭ���� ������ ��鸲)")]
    [SerializeField] private float frequency = 22f;

    [Header("Options")]
    [Tooltip("ȸ�� ��鸲�� ��������")]
    [SerializeField] private bool applyZRotation = false;
    [Tooltip("TimeScale�� ������ ��������(=�Ͻ����� �߿��� ��鸲 ����)")]
    [SerializeField] private bool useUnscaledTime = true;

    // �ܺο��� �о ī�޶� �ȷο� ����� ���� ���� ��
    public Vector2 CurrentOffset { get; private set; }
    public float CurrentAngleZ { get; private set; }

    // ���� ����(Ʈ��츶 ���)
    private float _trauma;                 // 0~1
    private float _decayPerSec;            // �ʴ� ���ҷ�
    private float _t;                      // �ð� ��
    private float _seedX, _seedY, _seedR;  // ������ �õ�

    void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _seedX = Random.value * 1000f;
        _seedY = Random.value * 2000f;
        _seedR = Random.value * 3000f;
    }

    /// <summary>
    /// ��𼭵� ȣ��: ����[0~1 ����], ���ӽð�(��)
    /// </summary>
    public static void Shake(float intensity, float seconds)
    {
        if (!Exists) return;
        intensity = Mathf.Clamp01(intensity);
        seconds = Mathf.Max(0.0001f, seconds);

        // �����ǵ��� ���ϰ�(1�� Ŭ����), ���� �� ���� �ӵ��� ����
        Instance._trauma = Mathf.Clamp01(Instance._trauma + intensity);
        Instance._decayPerSec = Mathf.Max(Instance._decayPerSec, intensity / seconds);
    }

    // �ѱ���/��Ī ȣ�⵵ ����(���ϼ̴� ��ī�޶� ����ŷ(����, ��)��) 
    public static void ī�޶���ŷ(float ����, float ��) => Shake(����, ��);

    public static void StopShake()
    {
        if (!Exists) return;
        Instance._trauma = 0f;
        Instance.CurrentOffset = Vector2.zero;
        Instance.CurrentAngleZ = 0f;
    }

    void LateUpdate()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float tt = useUnscaledTime ? Time.unscaledTime : Time.time;

        // Ʈ��츶 ����
        if (_trauma > 0f)
        {
            _trauma = Mathf.Max(0f, _trauma - _decayPerSec * dt);
        }

        // ��鸲 ��� (trauma^2�� ������ ����)
        float amp = _trauma * _trauma;

        if (amp <= 0f)
        {
            CurrentOffset = Vector2.zero;
            CurrentAngleZ = 0f;
            return;
        }

        float nx = Mathf.PerlinNoise(_seedX, tt * frequency) * 2f - 1f;
        float ny = Mathf.PerlinNoise(_seedY, tt * frequency) * 2f - 1f;
        float nr = Mathf.PerlinNoise(_seedR, tt * frequency) * 2f - 1f;

        CurrentOffset = new Vector2(nx, ny) * (maxPositionShake * amp);
        CurrentAngleZ = applyZRotation ? nr * (maxRotationShake * amp) : 0f;
    }
}
