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
    [Tooltip("整体效果的缩放倍数。")]
    public float reactionIntensity = 1f;

    [Header("尺寸限制")]
    [Tooltip("粒子的最大缩放尺寸限制，防止音乐高潮时粒子过大遮挡视线。")]
    public float maxParticleSize = 5f;

    [Header("粒子自旋转")]
    public bool enableMusicRotation = true;

    [Tooltip("无音乐时的基础自转速度")]
    public float baseRotationSpeed = 30f;

    [Tooltip("音乐增强时额外增加的自转速度")]
    public float rotationAudioBoost = 360f;

    [Tooltip("每个粒子随机转速差异")]
    [Range(0f, 1f)]
    public float randomRotationFactor = 0.35f;

    [Tooltip("自转呼吸感频率")]
    public float rotationPulseSpeed = 4f;

    [Tooltip("自转呼吸感强度")]
    [Range(0f, 1f)]
    public float rotationPulseAmount = 0.25f;

    [Header("可选优化")]
    [Range(1, 3000)]
    public int maxParticles = 512;

    // 内部引用
    private ParticleSystem ps;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.NoiseModule noiseModule;
    private ParticleSystem.Particle[] particles;

    // 内部平滑变量
    private float smoothedAmplitude = 0f;
    private int appliedMaxParticles = -1;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        mainModule = ps.main;
        noiseModule = ps.noise;

        // 启用3D旋转，便于修改每个粒子的 rotation3D
        mainModule.startRotation3D = true;

        ApplyMaxParticleSettings();
    }

    void Update()
    {
        if (audioController == null)
        {
            smoothedAmplitude = Mathf.Lerp(smoothedAmplitude, 0f, Time.deltaTime * smoothSpeed);
            ApplyAudioResponse();
            UpdateParticleRotation();
            return;
        }

        // 1. 获取并平滑音频振幅
        float targetAmplitude = GetAverageBand(0, 8) * reactionIntensity;
        smoothedAmplitude = Mathf.Lerp(smoothedAmplitude, targetAmplitude, Time.deltaTime * smoothSpeed);

        // 2. 更新原有物理属性
        ApplyAudioResponse();

        // 3. 更新每个粒子的自旋转
        UpdateParticleRotation();
    }

    void ApplyAudioResponse()
    {
        float t = Mathf.Clamp01(smoothedAmplitude);

        // 粒子大小：保留原来的逻辑，并限制上限
        float rawSize = 1f + (t * 2f);
        mainModule.startSizeMultiplier = Mathf.Clamp(rawSize, 0.1f, maxParticleSize);

        // 模拟速度：保留原来的响应
        mainModule.simulationSpeed = 1f + (t * 4f);

        // 噪声：保留原来的响应
        noiseModule.strengthMultiplier = 1f + (t * 3f);

        // 颜色：保留原来的响应
        Color finalColor = audioColorGradient.Evaluate(t);
        finalColor.a = 1f;
        mainModule.startColor = finalColor;
    }

    void UpdateParticleRotation()
    {
        if (!enableMusicRotation)
        {
            return;
        }

        ApplyMaxParticleSettings();

        int particleCount = ps.GetParticles(particles);
        if (particleCount <= 0)
        {
            return;
        }

        float t = Mathf.Clamp01(smoothedAmplitude);

        for (int i = 0; i < particleCount; i++)
        {
            // 给每个粒子一个稳定的随机差异，避免同步旋转过于机械
            float randomSeed = Mathf.Abs(Mathf.Sin(particles[i].randomSeed * 0.001f));

            float rotationMultiplier = Mathf.Lerp(
                1f - randomRotationFactor,
                1f + randomRotationFactor,
                randomSeed
            );

            // 带一点呼吸感的旋转节奏
            float pulse = 1f + Mathf.Sin(Time.time * rotationPulseSpeed + randomSeed * 10f) * rotationPulseAmount;

            float rotationSpeed = (baseRotationSpeed + t * rotationAudioBoost) * rotationMultiplier * pulse;

            Vector3 rot = particles[i].rotation3D;
            rot.y += rotationSpeed * Time.deltaTime;
            particles[i].rotation3D = rot;
        }

        ps.SetParticles(particles, particleCount);
    }

    float GetAverageBand(int startInclusive, int endExclusive)
    {
        if (audioController == null || audioController.audioBandBuffer == null || audioController.audioBandBuffer.Length == 0)
        {
            return 0f;
        }

        int start = Mathf.Clamp(startInclusive, 0, audioController.audioBandBuffer.Length);
        int end = Mathf.Clamp(endExclusive, start + 1, audioController.audioBandBuffer.Length);

        float total = 0f;
        int count = 0;

        for (int i = start; i < end; i++)
        {
            total += audioController.audioBandBuffer[i];
            count++;
        }

        return count > 0 ? total / count : 0f;
    }

    void ApplyMaxParticleSettings()
    {
        int clampedMaxParticles = Mathf.Max(1, maxParticles);

        if (appliedMaxParticles == clampedMaxParticles && particles != null && particles.Length == clampedMaxParticles)
        {
            return;
        }

        appliedMaxParticles = clampedMaxParticles;
        mainModule.maxParticles = appliedMaxParticles;
        particles = new ParticleSystem.Particle[appliedMaxParticles];
    }
}