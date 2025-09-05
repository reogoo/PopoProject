using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SpriteAnimationManager : MonoBehaviour
{
    [Serializable]
    public class SpriteAnim
    {
        public string name;
        public List<Sprite> frames = new List<Sprite>();
        public float fps = 12f;
        public bool loop = true;
    }

    [Header("Target")]
    [SerializeField] private SpriteRenderer target;

    [Header("Clips")]
    [SerializeField] private List<SpriteAnim> clips = new List<SpriteAnim>();

    [Header("Timing")]
    [SerializeField] private bool useUnscaledTime = false;

    [Header("Behavior")]
    [Tooltip("true�� PlayOnce ��� �� �ٸ� Play ȣ���� �����մϴ�.")]
    public bool respectOneShots = true;

    // runtime
    private readonly Dictionary<string, SpriteAnim> _map = new();
    private SpriteAnim _current;
    private int _frameIndex;
    private float _accum;
    private bool _isOneShot;
    private string _fallbackAfterOnce;
    private string _currentName;

    public bool IsOneShotActive => _isOneShot;
    public string Current => _currentName;

    void Awake()
    {
        if (!target) target = GetComponentInChildren<SpriteRenderer>();
        BuildMap();
        // �ʱ�ȭ: ù Ŭ���� ������ �װɷ� ����
        if (clips.Count > 0) SetClip(clips[0], forceRestart: true, markOnce: false, fallback: null);
    }

    void Reset()
    {
        target = GetComponentInChildren<SpriteRenderer>();
    }

    void BuildMap()
    {
        _map.Clear();
        foreach (var c in clips)
        {
            if (string.IsNullOrEmpty(c.name)) continue;
            if (!_map.ContainsKey(c.name)) _map.Add(c.name, c);
        }
    }

    void Update()
    {
        if (_current == null || target == null) return;
        var frames = _current.frames;
        if (frames == null || frames.Count == 0) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (_current.fps <= 0f)
        { // fps 0�̸� ���� ������ ����
            ApplyFrame();
            return;
        }

        _accum += dt;
        float frameDur = 1f / _current.fps;

        while (_accum >= frameDur)
        {
            _accum -= frameDur;
            _frameIndex++;

            if (_frameIndex >= frames.Count)
            {
                if (_current.loop && !_isOneShot)
                {
                    _frameIndex = 0;
                }
                else
                {
                    // once ���� ����
                    _frameIndex = frames.Count - 1; // ������ ������ ����
                    ApplyFrame();

                    if (_isOneShot)
                    {
                        // fallback���� ��ȯ
                        string fb = _fallbackAfterOnce;
                        _isOneShot = false;
                        _fallbackAfterOnce = null;

                        if (!string.IsNullOrEmpty(fb) && _map.TryGetValue(fb, out var fbClip))
                        {
                            SetClip(fbClip, forceRestart: true, markOnce: false, fallback: null);
                        }
                    }
                    return;
                }
            }
        }

        ApplyFrame();
    }

    private void ApplyFrame()
    {
        var frames = _current.frames;
        if (frames == null || frames.Count == 0) return;
        int idx = Mathf.Clamp(_frameIndex, 0, frames.Count - 1);
        target.sprite = frames[idx];
    }

    private void SetClip(SpriteAnim clip, bool forceRestart, bool markOnce, string fallback)
    {
        if (clip == null) return;

        bool same = (clip == _current);
        if (!forceRestart && same) return;

        _current = clip;
        _currentName = clip.name;
        _isOneShot = markOnce;
        _fallbackAfterOnce = markOnce ? fallback : null;

        _frameIndex = 0;
        _accum = 0f;
        ApplyFrame();
    }

    // ============ Public API ============

    /// <summary>
    /// ���� �ִ� �÷���. 1ȸ��� ���̸� respectOneShots=true�� �� ����.
    /// </summary>
    public void Play(string name, bool forceRestart = false, bool interruptOneShot = false)
    {
        if (string.IsNullOrEmpty(name) || !_map.TryGetValue(name, out var clip)) return;

        if (_isOneShot && respectOneShots && !interruptOneShot)
            return; // 1ȸ ��� ��ȣ

        // Play�� loop �ִϷ� ���� (clip.loop ������ �״�� ���)
        SetClip(clip, forceRestart, markOnce: false, fallback: null);
    }

    /// <summary>
    /// 1ȸ ��� �� fallback���� �Ѿ. (fallback�� null/���ڸ� ������ ������ ����)
    /// </summary>
    public void PlayOnce(string name, string fallback = null, bool forceRestart = true)
    {
        if (string.IsNullOrEmpty(name) || !_map.TryGetValue(name, out var clip)) return;

        // clip.loop ���� ������� 1ȸ�� ���
        SetClip(clip, forceRestart, markOnce: true, fallback: fallback);
    }

    /// <summary>
    /// ���� �ִϸ��̼��� name�� ������.
    /// </summary>
    public bool IsPlaying(string name) => !string.IsNullOrEmpty(name) && _currentName == name;

    /// <summary>
    /// Ŭ�� ��ȿ�� �˻� (������)
    /// </summary>
    public bool HasClip(string name) => !string.IsNullOrEmpty(name) && _map.ContainsKey(name);
}
