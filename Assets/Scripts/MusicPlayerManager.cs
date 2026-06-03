using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

[Serializable]
public struct Song
{
    public string songName;
    public Sprite coverImage;
    public AudioClip audioClip;

    [Header("主题颜色渐变")]
    public Gradient themeGradient; // 在 Inspector 中可以自定义每首歌的颜色主题
}

public class MusicPlayerManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject MainUI;
    public Image coverDisplay;
    public Text nameDisplay;

    [Header("Button Icons")]
    public Sprite playIcon;     // 在 Inspector 中拖入播放图标
    public Sprite pauseIcon;    // 在 Inspector 中拖入暂停图标

    [Header("Controls")]
    public Button nextBtn;
    public Button pauseBtn;     // 这个按钮的 Image 组件将用来切换图标
    public Button perviousBtn;

    [Header("Data & Audio")]
    public Song[] allMusic;
    public AudioController audioController;
    public InputActionReference leftSecond_X;

    private int currentIndex = 0;
    private bool isPlaying = false; // 初始状态建议设为 false

    private void Awake()
    {
        MainUI.SetActive(false);

        nextBtn.onClick.AddListener(NextSong);
        pauseBtn.onClick.AddListener(TogglePause);
        perviousBtn.onClick.AddListener(PreviousSong);
    }

    private void Start()
    {
        if (allMusic.Length > 0)
        {
            UpdateSongDisplay();
            // 游戏开始时同步一下第一首歌的颜色主题
            UpdateParticleSystemGradients();
        }
    }

    private void OnEnable()
    {
        leftSecond_X.action.started += ctx => MainUI.SetActive(true);
        leftSecond_X.action.canceled += ctx => MainUI.SetActive(false);
    }

    private void OnDisable()
    {
        leftSecond_X.action.started -= ctx => MainUI.SetActive(true);
        leftSecond_X.action.canceled -= ctx => MainUI.SetActive(false);
    }

    public void NextSong()
    {
        currentIndex = (currentIndex + 1) % allMusic.Length;
        UpdateSongDisplay();
        PlayCurrent();
    }

    public void PreviousSong()
    {
        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = allMusic.Length - 1;
        }
        UpdateSongDisplay();
        PlayCurrent();
    }

    public void TogglePause()
    {
        AudioSource source = audioController.GetComponent<AudioSource>();

        if (source.isPlaying)
        {
            source.Pause();
            isPlaying = false;
            pauseBtn.image.sprite = playIcon;
        }
        else
        {
            if (source.clip == null) PlayCurrent();
            else source.UnPause();

            isPlaying = true;
            pauseBtn.image.sprite = pauseIcon;
        }
    }

    private void UpdateSongDisplay()
    {
        if (allMusic.Length == 0) return;
        coverDisplay.sprite = allMusic[currentIndex].coverImage;
        nameDisplay.text = allMusic[currentIndex].songName;
    }

    private void PlayCurrent()
    {
        audioController.audioClip = allMusic[currentIndex].audioClip;
        AudioSource source = audioController.GetComponent<AudioSource>();
        source.clip = allMusic[currentIndex].audioClip;
        source.Play();

        isPlaying = true;
        pauseBtn.image.sprite = pauseIcon;

        // 【关键点】切换歌曲时，更新粒子效果的颜色主题
        UpdateParticleSystemGradients();
    }

    /// <summary>
    /// 遍历场景中所有的 AudioFlowParticle 脚本，并同步当前歌曲的渐变色
    /// </summary>
    private void UpdateParticleSystemGradients()
    {
        if (allMusic.Length == 0) return;

        // 获取当前切到的歌曲的主题颜色
        Gradient currentGradient = allMusic[currentIndex].themeGradient;

        // 寻找场景中所有激活的 AudioFlowParticle 脚本组件
        // 注：FindObjectsByType 在较新版 Unity 中性能更好。如果你使用的是 2021 或更老版本，可以改成 FindObjectsOfType<AudioFlowParticle>()
        AudioFlowParticle[] particlesInScene = FindObjectsByType<AudioFlowParticle>(FindObjectsSortMode.None);

        foreach (AudioFlowParticle particleScript in particlesInScene)
        {
            if (particleScript != null)
            {
                // 将当前歌曲的颜色赋值给粒子脚本对应的变量
                particleScript.audioColorGradient = currentGradient;
            }
        }
    }
}