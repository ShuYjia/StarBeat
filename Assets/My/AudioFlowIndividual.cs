using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class AudioFlowIndividual : MonoBehaviour
{
    public AudioController audioController;

    [Header("响应速度控制 (核心变化)")]
    [Range(5f, 50f)]
    public float attackSpeed = 25f;  // 声音变大时的跟进速度（越大越敏锐）
    [Range(1f, 10f)]
    public float decaySpeed = 3f;    // 声音变小时的回落速度（越小越丝滑）

    [Header("发射量控制")]
    public float minEmission = 0f;
    public float maxEmission = 800f;

    [Header("流动控制")]
    public float baseFlowSpeed = 15f;
    public float turbulenceIntensity = 8f;
    public Vector3 flowDirection = Vector3.forward;

    [Header("颜色控制 (随时间)")]
    public Gradient timeBasedGradient;
    public float colorCycleSpeed = 0.15f;

    private ParticleSystem ps;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.NoiseModule noiseModule;
    private ParticleSystem.Particle[] particles;

    // 关键变量：用于存储平滑后的音频值
    private float smoothedAmplitude;
    private float smoothedHighFreq;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        emissionModule = ps.emission;
        noiseModule = ps.noise;
        particles = new ParticleSystem.Particle[ps.main.maxParticles];
    }

    void Update()
    {
        if (audioController == null) return;

        // 1. 提取当前原始音频数据
        float rawAmp = 0;
        float rawHigh = 0;
        for (int i = 0; i < 8; i++)
        {
            rawAmp += audioController.audioBandBuffer[i];
            if (i >= 4) rawHigh += audioController.audioBandBuffer[i];
        }
        rawAmp /= 8f;
        rawHigh /= 4f;

        // 2. 双重平滑逻辑 (多变化的缓冲)
        // 处理总音量的缓冲
        float currentSpeedAmp = (rawAmp > smoothedAmplitude) ? attackSpeed : decaySpeed;
        smoothedAmplitude = Mathf.Lerp(smoothedAmplitude, rawAmp, Time.deltaTime * currentSpeedAmp);

        // 处理高频（曲折度）的缓冲
        float currentSpeedHigh = (rawHigh > smoothedHighFreq) ? attackSpeed : decaySpeed;
        smoothedHighFreq = Mathf.Lerp(smoothedHighFreq, rawHigh, Time.deltaTime * currentSpeedHigh);

        // 3. 应用到发射数量 (会有明显的爆发和缓慢消散感)
        emissionModule.rateOverTime = Mathf.Lerp(minEmission, maxEmission, smoothedAmplitude);

        // 4. 应用到曲折流动 (随高频频率变化)
        noiseModule.strengthMultiplier = smoothedHighFreq * turbulenceIntensity;
        noiseModule.frequency = 0.3f + (smoothedHighFreq * 1.5f);

        // 5. 逐粒子颜色与速度控制
        int numParticlesAlive = ps.GetParticles(particles);
        float colorPos = (Time.time * colorCycleSpeed) % 1.0f;
        Color globalColor = timeBasedGradient.Evaluate(colorPos);

        for (int i = 0; i < numParticlesAlive; i++)
        {
            // 颜色随时间变化
            particles[i].startColor = globalColor;

            // 沿着统一方向流动，速度受缓冲后的音量影响
            particles[i].velocity += flowDirection.normalized * smoothedAmplitude * baseFlowSpeed * Time.deltaTime;
        }

        ps.SetParticles(particles, numParticlesAlive);
    }
}