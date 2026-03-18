using UnityEngine;

public class MusicVisualizer : MonoBehaviour
{
    public AudioSource audioSource;      // 拖入你的音频源
    public ParticleSystem particleSys;   // 拖入你的粒子系统

    private float[] spectrum = new float[512]; // 频谱数据数组
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.NoiseModule noiseModule;

    void Start()
    {
        // 缓存模块引用以提高性能
        mainModule = particleSys.main;
        emissionModule = particleSys.emission;
        noiseModule = particleSys.noise;
    }

    void Update()
    {
        // 1. 获取音频的频谱数据
        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        // 2. 计算低音强度（取频谱的前几个采样）
        float bass = 0;
        for (int i = 0; i < 10; i++)
        {
            bass += spectrum[i];
        }
        bass /= 10; // 取平均值

        // 3. 将音乐数据映射到粒子属性
        // 灵敏度倍率，你可以根据歌曲动态调整这些数字
        float intensity = bass * 100f;

        // 粒子发射数量随节奏爆发
        emissionModule.rateOverTime = intensity * 200f;

        // 粒子流动混乱度随音乐增加
        noiseModule.strength = intensity * 5f;

        // 颜色随强度从浅蓝色变为亮紫色
        mainModule.startColor = Color.Lerp(Color.cyan, Color.magenta, intensity * 2f);
    }
}