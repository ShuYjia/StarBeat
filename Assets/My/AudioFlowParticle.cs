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

    [Header("尺寸限制 (新增)")]
    [Tooltip("粒子的最大缩放尺寸限制，防止音乐高潮时粒子过大遮挡视线。")]
    public float maxParticleSize = 5f;

    // 内部引用
    private ParticleSystem ps;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.NoiseModule noiseModule;

    // 内部平滑变量
    private float smoothedAmplitude = 0f;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        mainModule = ps.main;
        noiseModule = ps.noise;
    }

    void Update()
    {
        if (audioController == null) return;

        // 1. 获取并平滑音频振幅
        float targetAmplitude = 0;
        for (int i = 0; i < 8; i++)
        {
            targetAmplitude += audioController.audioBandBuffer[i];
        }
        targetAmplitude = (targetAmplitude / 8f) * reactionIntensity;
        smoothedAmplitude = Mathf.Lerp(smoothedAmplitude, targetAmplitude, Time.deltaTime * smoothSpeed);

        // 2. 更新物理属性
        // 计算目标大小
        float rawSize = 1f + (smoothedAmplitude * 2f);
        // 使用 Mathf.Clamp 限制在 [0.1, maxParticleSize] 之间，确保不会太小也不会超出上限
        mainModule.startSizeMultiplier = Mathf.Clamp(rawSize, 0.1f, maxParticleSize);

        mainModule.simulationSpeed = 1f + (smoothedAmplitude * 4f);
        noiseModule.strengthMultiplier = 1f + (smoothedAmplitude * 3f);

        // 3. 颜色处理
        Color finalColor = audioColorGradient.Evaluate(smoothedAmplitude);
        finalColor.a = 1f; // 锁定不透明
        mainModule.startColor = finalColor;
    }
}