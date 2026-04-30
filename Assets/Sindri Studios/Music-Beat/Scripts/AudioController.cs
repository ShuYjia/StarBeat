using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class AudioController : MonoBehaviour
{

    //==========================
    // 麦克风 / 音乐管理
    //==========================

    [Header("Microphone / Music")]
    public AudioClip audioClip;
    public bool useMicrophone;
    public string selectedDevice;

    public AudioMixerGroup audioGroupMaster;
    public AudioMixerGroup audioGroupMicrophone;

    AudioSource audioSource; // 频谱分析用


    //==========================
    // 鼓声音系统（新增）
    //==========================

    [Header("Drum Sounds")]
    public AudioClip kickClip;
    public AudioClip snareClip;
    public AudioClip tomClip;
    public AudioClip hihatClip;
    public AudioClip crashClip;

    [Header("SFX Audio Source")]
    public AudioSource sfxSource;


    //==========================
    // 频谱分析系统（你的原逻辑）
    //==========================

    float[] samples = new float[512];

    float[] freqBand = new float[16];

    public float[] bandBuffer = new float[16];

    public float[] audioBandBuffer = new float[16];

    float[] freqBandMax = new float[16];

    public float[] bufferDecrease = new float[16];


    //==========================
    // 鼓类型枚举
    //==========================

    public enum DrumType
    {
        Kick,
        Snare,
        Tom,
        HiHat,
        Crash
    }


    //==========================
    // Start
    //==========================
    private AudioSource globalAudioSource;

    void Awake()
    {
        // 获取当前的 AudioSource，如果没有则自动添加一个
        globalAudioSource = GetComponent<AudioSource>();
        if (globalAudioSource == null)
        {
            globalAudioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    // 供乐器调用的播放方法
    public void PlayInstrumentSound(AudioClip clip, float volumeScale = 1.0f)
    {
        if (clip != null)
        {
            // PlayOneShot 允许同时播放多个声音，且可以指定音量大小
            globalAudioSource.PlayOneShot(clip, volumeScale);
        }
    }
    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        //----------------------------------
        // 创建SFX音源（打鼓用）
        //----------------------------------

        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFX_AudioSource");
            sfxObj.transform.parent = transform;

            sfxSource = sfxObj.AddComponent<AudioSource>();

            sfxSource.spatialBlend = 1f; // 3D声音
            sfxSource.playOnAwake = false;
        }


        //----------------------------------
        // 麦克风 / 音乐
        //----------------------------------

        if (useMicrophone)
        {
            if (Microphone.devices.Length > 0)
            {
                selectedDevice = Microphone.devices[0];

                audioSource.outputAudioMixerGroup =
                    audioGroupMicrophone;

                audioSource.clip =
                    Microphone.Start(
                        selectedDevice,
                        true,
                        1,
                        AudioSettings.outputSampleRate);
            }
            else
            {
                useMicrophone = false;
            }
        }

        if (!useMicrophone)
        {
            audioSource.outputAudioMixerGroup =
                audioGroupMaster;

            audioSource.clip = audioClip;
        }

        audioSource.Play();


        //----------------------------------
        // 初始化频谱
        //----------------------------------

        for (int i = 0; i < 16; i++)
            freqBandMax[i] = 0;
    }



    //==========================
    // Update
    //==========================

    void Update()
    {
        GetSpectrumAudioSource();

        MakeFrequencyBand();

        BandBuffer();

        NomralizeBufferBand();
    }



    //==========================
    // 播放鼓声音（核心）
    //==========================

    public void PlayDrum(DrumType type, float velocity = 1f)
    {
        AudioClip clip = null;

        switch (type)
        {
            case DrumType.Kick:
                clip = kickClip;
                break;

            case DrumType.Snare:
                clip = snareClip;
                break;

            case DrumType.Tom:
                clip = tomClip;
                break;

            case DrumType.HiHat:
                clip = hihatClip;
                break;

            case DrumType.Crash:
                clip = crashClip;
                break;
        }

        if (clip == null) return;

        float volume = Mathf.Clamp01(velocity);

        sfxSource.PlayOneShot(clip, volume);
    }



    //==========================
    // 频谱计算（原逻辑）
    //==========================

    void GetSpectrumAudioSource()
    {
        audioSource.GetSpectrumData(
            samples,
            0,
            FFTWindow.Blackman);
    }



    void BandBuffer()
    {
        for (int i = 0; i < 16; ++i)
        {
            if (freqBand[i] > bandBuffer[i])
            {
                bandBuffer[i] = freqBand[i];
                bufferDecrease[i] = 0.005f;
            }

            if (freqBand[i] < bandBuffer[i])
            {
                bandBuffer[i] -= bufferDecrease[i];

                bufferDecrease[i] *= 1.2f;
            }
        }
    }



    void NomralizeBufferBand()
    {
        for (int i = 0; i < 16; i++)
        {
            if (bandBuffer[i] > freqBandMax[i])
            {
                freqBandMax[i] = freqBand[i];
            }

            freqBand[i] =
                freqBand[i] / freqBandMax[i];

            audioBandBuffer[i] =
                bandBuffer[i] / freqBandMax[i];
        }
    }



    void MakeFrequencyBand()
    {
        int count = 0;

        for (int i = 0; i < 8; i++)
        {
            float average = 0;

            int sampleCount =
                (int)Mathf.Pow(2, i);

            for (int k = 0; k < 2; k++)
            {
                for (int j = 0; j < sampleCount; j++)
                {
                    average +=
                        samples[count] * (count + 1);

                    count++;
                }

                average = average / count;

                freqBand[i * 2 + k] =
                    average * 10;
            }
        }
    }
}