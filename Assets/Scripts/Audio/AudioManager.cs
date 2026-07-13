using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource uiSource;
    [SerializeField] private AudioSource[] sfxSources;

    [Header("Global Gameplay SFX")]
    [SerializeField] private AudioClip deathSFX;
    [SerializeField] private AudioClip winSFX;
    [SerializeField] private AudioClip loseSFX;

    [SerializeField] private AudioClip uiClickSFX;

    [Header("Scene Music")]
    [SerializeField] private List<SceneMusicData> sceneMusicList = new();

    private readonly Dictionary<string, AudioClip> sceneMusicLookup = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        ValidateAudioSources();
        BuildSceneMusicLookup();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    private void BuildSceneMusicLookup()
    {
        sceneMusicLookup.Clear();

        foreach (SceneMusicData data in sceneMusicList)
        {
            if (data == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(data.sceneName))
            {
                continue;
            }

            sceneMusicLookup[data.sceneName] = data.musicClip;
        }
    }

    private void PlayMusicForScene(string sceneName)
    {
        if (!sceneMusicLookup.TryGetValue(sceneName, out AudioClip clip))
        {
            Debug.LogWarning($"[AudioManager] No music assigned for scene: {sceneName}");
            musicSource.Stop();
            return;
        }

        if (musicSource.clip == clip)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.Play();
    }

    private void ValidateAudioSources()
    {
        if (musicSource == null)
        {
            Debug.LogWarning("[AudioManager] MusicSource has not been assigned.");
        }

        if (uiSource == null)
        {
            Debug.LogWarning("[AudioManager] UISource has not been assigned.");
        }

        if (sfxSources == null || sfxSources.Length == 0)
        {
            Debug.LogWarning("[AudioManager] SFX Pool has not been assigned.");
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] PlaySFX received a null AudioClip.");
            return;
        }

        AudioSource availableSource = GetAvailableSFXSource();

        if (availableSource == null)
        {
            Debug.LogWarning("[AudioManager] No available SFX AudioSource.");
            return;
        }

        availableSource.PlayOneShot(clip);
    }

    public void PlayDeathSFX()
    {
        PlaySFX(deathSFX);
    }

    public void PlayWinSFX()
    {
        PlaySFX(winSFX);
    }

    public void PlayLoseSFX()
    {
        PlaySFX(loseSFX);
    }

    private AudioSource GetAvailableSFXSource()
    {
        foreach (AudioSource source in sfxSources)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        return null;
    }

    public void PlayUISFX(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] PlayUISFX received a null AudioClip.");
            return;
        }

        if (uiSource == null)
        {
            Debug.LogWarning("[AudioManager] UISource has not been assigned.");
            return;
        }

        uiSource.PlayOneShot(clip);
    }

    public void PlayUIClickSFX()
    {
        PlayUISFX(uiClickSFX);
    }
}