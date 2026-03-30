using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class AudioFlowParticle : MonoBehaviour
{
    [Header("绑定我们之前的音频控制器")]
    public AudioController audioController;

    [Header("颜色渐变带")]
    public Gradient audioColorGradient;

    [Header("平滑与强度控制")]
    [Range(1f, 20f)]
    [Tooltip("值越小，变化越平滑；值越大，变化越灵敏。")]
    public float smoothSpeed = 8f;

    [Range(0f, 2f)]
    [Tooltip("整体音频响应强度。")]
    public float reactionIntensity = 1f;

    [Header("粒子大小随音量变化")]
    [Tooltip("是否启用粒子大小随音乐音量变化")]
    public bool enableSizeByAudio = true;

    [Range(0f, 10f)]
    [Tooltip("粒子大小变化的强度，越大变化越明显")]
    public float sizeReactionIntensity = 2f;

    [Tooltip("是否限制粒子的最大大小")]
    public bool limitMaxSize = true;

    [Min(0.01f)]
    [Tooltip("粒子大小的最大倍数上限")]
    public float maxSizeMultiplier = 3f;

    [Header("其他效果强度")]
    [Range(0f, 10f)]
    public float simulationSpeedIntensity = 4f;

    [Range(0f, 10f)]
    public float noiseStrengthIntensity = 3f;

    // 内部引用
    private ParticleSystem ps;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.NoiseModule noiseModule;

    // 平滑后的音量值
    private float smoothedAmplitude = 0f;

    // 记录初始值，避免每次都从 1 开始算
    private float defaultSizeMultiplier;
    private float defaultSimulationSpeed;
    private float defaultNoiseStrength;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        mainModule = ps.main;
        noiseModule = ps.noise;

        defaultSizeMultiplier = mainModule.startSizeMultiplier;
        defaultSimulationSpeed = mainModule.simulationSpeed;
        defaultNoiseStrength = noiseModule.strengthMultiplier;
    }

    void Update()
    {
        if (audioController == null) return;

        // 1. 计算当前音乐振幅
        float targetAmplitude = 0f;
        for (int i = 0; i < 8; i++)
        {
            targetAmplitude += audioController.audioBandBuffer[i];
        }

        targetAmplitude = (targetAmplitude / 8f) * reactionIntensity;

        // 2. 平滑过渡
        smoothedAmplitude = Mathf.Lerp(smoothedAmplitude, targetAmplitude, Time.deltaTime * smoothSpeed);

        // 3. 粒子大小：随音量变化
        if (enableSizeByAudio)
        {
            float sizeValue = defaultSizeMultiplier * (1f + smoothedAmplitude * sizeReactionIntensity);

            if (limitMaxSize)
            {
                sizeValue = Mathf.Min(sizeValue, maxSizeMultiplier);
            }

            mainModule.startSizeMultiplier = sizeValue;
        }
        else
        {
            mainModule.startSizeMultiplier = defaultSizeMultiplier;
        }

        // 4. 粒子速度
        mainModule.simulationSpeed = defaultSimulationSpeed * (1f + smoothedAmplitude * simulationSpeedIntensity);

        // 5. 噪声强度
        noiseModule.strengthMultiplier = defaultNoiseStrength * (1f + smoothedAmplitude * noiseStrengthIntensity);

        // 6. 颜色渐变
        if (audioColorGradient != null)
        {
            mainModule.startColor = audioColorGradient.Evaluate(Mathf.Clamp01(smoothedAmplitude));
        }
    }
}