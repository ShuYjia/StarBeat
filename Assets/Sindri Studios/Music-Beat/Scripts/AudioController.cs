using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

[RequireComponent(typeof(AudioSource))]
public class AudioController : MonoBehaviour
{

    //Microphone management
    public AudioClip audioClip;                  
    public bool useMicrophone;  
    public string selectedDevice;                
    public AudioMixerGroup audioGroupMaster;     
    public AudioMixerGroup audioGroupMicrophone; 


    AudioSource audioSource;                      //Object that stores the audio

    float[] samples    = new float[512];   //unity原始采样的数据

    float[] freqBand   = new float[16]; //将unity给出的512个数据分为16个部分，每个32

    public float[] bandBuffer = new float[16];//存缓冲的数据

    public float[] audioBandBuffer = new float[16]; //归一化的数据

    float[] freqBandMax = new float[16]; //记录每个阶段的最大值

    public float[] bufferDecrease = new float[16];//缓冲的下降速度

    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (useMicrophone)
        {
            if (Microphone.devices.Length > 0)
            {
                Debug.Log(Microphone.devices.Length);
                selectedDevice = Microphone.devices[0];
                audioSource.outputAudioMixerGroup = audioGroupMicrophone;                                        //Sets the group for the audio source
                audioSource.clip = Microphone.Start(selectedDevice, true, 1, AudioSettings.outputSampleRate);
            }
            else
                useMicrophone = false;
        }
        if (!useMicrophone)
        {
            audioSource.outputAudioMixerGroup = audioGroupMaster;
            audioSource.clip = audioClip;
        }

        audioSource.Play();

        for (int i = 0; i < 16; i++) //清空
            freqBandMax[i] = 0;
    }

    // Update is called once per frame
    void Update()
    {
        GetSpectrumAudioSource();
        MakeFrequencyBand();
        BandBuffer();
        NomralizeBufferBand();
    }

    void NomralizeBufferBand()
    {//Function that normalizes de band buffer values
        for (int i = 0; i < 16; i++)
        {
            if (bandBuffer[i] > freqBandMax[i])
            {
                freqBandMax[i] = freqBand[i];
            }
            freqBand[i] = freqBand[i] / freqBandMax[i];
            audioBandBuffer[i] = bandBuffer[i] / freqBandMax[i];
        }
    }

    void GetSpectrumAudioSource()
    {//将声音拆解成512个频率强度值，塞进 samples 数组里。FFTWindow.Blackman是unity的一种是一种过滤算法，能让抓取到的数据更干净、杂音更少。
        audioSource.GetSpectrumData(samples, 0, FFTWindow.Blackman);
    }

    void BandBuffer() //缓冲用
    {
        for(int i = 0; i < 16; ++i)
        {
            if (freqBand[i] > bandBuffer[i])
            {
                bandBuffer[i] = freqBand[i];
                bufferDecrease[i] = 0.005f;
            }
            if (freqBand[i]< bandBuffer[i])
            {
                bandBuffer[i] -= bufferDecrease[i];
                //1.2
                bufferDecrease[i] *= 1.2f;
            }
        }
    }

    void MakeFrequencyBand()
    {
        int count = 0;//当前是512的第几个
        //1) 20 - 60 Hz-> 2 
        //2) 60 - 250 Hz-> 4 
        //3) 250 - 500 Hz-> 8 
        //4) 500 - 2000 Hz-> 16 
        //5) 2000 - 4000 Hz-> 32 
        //6) 4000 - 6000 Hz-> 64 
        //7) 6000 - 20,000 Hz-> 128
        for (int i = 0; i < 8; i++)
        {
            float average = 0;

            int sampleCount = (int)Mathf.Pow(2, i); //人更能识别低音而非高音，512正好是2的9次方，故利用pow来分段成
            for (int k = 0; k < 2; k++)//8*2=16
            {
                for (int j = 0; j < sampleCount; j++)
                {
                    average += samples[count] * (count + 1);//Gets the average
                    count++;
                }

                average = average / count;

                freqBand[i*2+k] = average * 10;
            }
        }
    }
}
