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
            // 初始化时如果还没播放，显示播放图标
            //pauseBtn.image.sprite = playIcon;
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
        // 逻辑：如果当前是最后一首 (Length - 1)，+1 后取模会变成 0，实现回到第一首
        currentIndex = (currentIndex + 1) % allMusic.Length;
        UpdateSongDisplay();
        PlayCurrent();
    }

    public void PreviousSong()
    {
        currentIndex--;
        // 逻辑：如果索引小于 0，跳转到最后一首
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
            pauseBtn.image.sprite = playIcon; // 暂停后显示“播放”图标
        }
        else
        {
            // 如果当前没有 Clip（比如刚启动），先指定一下
            if (source.clip == null) PlayCurrent();
            else source.UnPause();

            isPlaying = true;
            pauseBtn.image.sprite = pauseIcon; // 播放时显示“暂停”图标
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
        pauseBtn.image.sprite = pauseIcon; // 切换歌曲并播放时，自动更新为“暂停”图标
    }
}