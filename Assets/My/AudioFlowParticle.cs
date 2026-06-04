using UnityEngine;

/// <summary>
/// 音乐驱动粒子可视化
///
/// 功能：
/// 1. 音乐控制粒子大小
/// 2. 音乐控制粒子颜色
/// 3. 音乐控制粒子流动速度
/// 4. 音乐控制粒子旋转
/// 5. 整体波浪式流动
/// 6. 轻微涡旋运动
/// 7. 平滑音频响应
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class AudioFlowParticle : MonoBehaviour
{
    #region 音频引用

    [Header("音频分析器")]

    /// <summary>
    /// 音频控制器引用
    /// 用于获取频谱分析结果
    /// </summary>
    public AudioController audioController;

    #endregion

    #region 颜色系统

    [Header("颜色系统")]

    /// <summary>
    /// 音量与颜色映射关系
    /// 左侧=安静
    /// 右侧=高潮
    /// </summary>
    public Gradient audioColorGradient;

    #endregion

    #region 音频响应

    [Header("音频响应")]

    /// <summary>
    /// 平滑速度
    /// 小=柔和
    /// 大=灵敏
    /// </summary>
    [Range(1f, 20f)]
    public float smoothSpeed = 8f;

    /// <summary>
    /// 整体反应强度
    /// </summary>
    [Range(0f, 3f)]
    public float reactionIntensity = 1f;

    #endregion

    #region 粒子尺寸

    [Header("粒子尺寸")]

    /// <summary>
    /// 粒子最大尺寸
    /// 防止高潮时过大
    /// </summary>
    public float maxParticleSize = 5f;

    #endregion

    #region 流动系统

    [Header("音乐流动方向")]

    /// <summary>
    /// 粒子整体运动方向
    /// </summary>
    public Vector3 flowDirection = Vector3.up;

    /// <summary>
    /// 基础流动速度
    /// </summary>
    public float baseFlowSpeed = 0.5f;

    /// <summary>
    /// 音乐增强时增加的流速
    /// </summary>
    public float audioFlowBoost = 2f;

    #endregion

    #region 波浪系统

    [Header("波浪系统")]

    /// <summary>
    /// 波浪空间密度
    /// 越大波浪越密
    /// </summary>
    public float waveSpatialFrequency = 1.2f;

    /// <summary>
    /// 波浪移动速度
    /// </summary>
    public float waveTravelSpeed = 2f;

    /// <summary>
    /// 波浪幅度
    /// </summary>
    public float waveAmplitude = 0.5f;

    #endregion

    #region 漩涡系统

    [Header("漩涡系统")]

    /// <summary>
    /// 围绕中心轻微旋转
    /// </summary>
    public float orbitStrength = 0.25f;

    /// <summary>
    /// 呼吸起伏强度
    /// </summary>
    public float liftStrength = 0.15f;

    #endregion

    #region 跟随平滑

    [Header("运动平滑")]

    /// <summary>
    /// 速度跟随速度
    /// </summary>
    public float velocityResponse = 6f;

    /// <summary>
    /// 粒子随机相位差
    /// 防止完全同步
    /// </summary>
    [Range(0f, 2f)]
    public float phaseScatter = 0.3f;

    #endregion

    #region 自旋转

    [Header("粒子旋转")]

    /// <summary>
    /// 是否开启旋转
    /// </summary>
    public bool enableMusicRotation = true;

    /// <summary>
    /// 基础旋转速度
    /// </summary>
    public float baseRotationSpeed = 25f;

    /// <summary>
    /// 音乐增加的旋转速度
    /// </summary>
    public float rotationAudioBoost = 180f;

    /// <summary>
    /// 呼吸频率
    /// </summary>
    public float rotationPulseSpeed = 2f;

    /// <summary>
    /// 呼吸幅度
    /// </summary>
    [Range(0f, 1f)]
    public float rotationPulseAmount = 0.2f;

    #endregion

    #region 性能

    [Header("性能优化")]

    /// <summary>
    /// 最大粒子数量
    /// Pico推荐300~600
    /// </summary>
    [Range(1, 3000)]
    public int maxParticles = 512;

    #endregion

    #region 内部变量

    private ParticleSystem ps;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.NoiseModule noiseModule;

    private ParticleSystem.Particle[] particles;

    /// <summary>
    /// 平滑后的音量
    /// </summary>
    private float smoothedAmplitude;

    private int appliedMaxParticles = -1;

    /// <summary>
    /// 归一化方向
    /// </summary>
    private Vector3 flowDirNormalized;

    /// <summary>
    /// 横向轴
    /// </summary>
    private Vector3 sideAxis;

    /// <summary>
    /// 垂直轴
    /// </summary>
    private Vector3 upAxis;

    #endregion

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();

        mainModule = ps.main;
        noiseModule = ps.noise;

        mainModule.startRotation3D = true;

        RefreshFlowAxes();

        ApplyMaxParticleSettings();
    }

    void Update()
    {
        RefreshFlowAxes();

        UpdateAudioAmplitude();

        UpdateVisualProperties();

        UpdateParticleFlow();
    }

    /// <summary>
    /// 更新音量
    /// </summary>
    void UpdateAudioAmplitude()
    {
        float targetAmplitude = 0f;

        if (audioController != null)
        {
            targetAmplitude =
                GetAverageBand(0, 8)
                * reactionIntensity;
        }

        smoothedAmplitude =
            Mathf.Lerp(
                smoothedAmplitude,
                targetAmplitude,
                Time.deltaTime * smoothSpeed
            );
    }

    /// <summary>
    /// 更新颜色大小等
    /// </summary>
    void UpdateVisualProperties()
    {
        float t = Mathf.Clamp01(smoothedAmplitude);

        t = Mathf.SmoothStep(0f, 1f, t);

        mainModule.startSizeMultiplier =
            Mathf.Clamp(
                1f + t * 2f,
                0.1f,
                maxParticleSize
            );

        mainModule.simulationSpeed =
            1f + t * 3f;

        noiseModule.strengthMultiplier =
            1f + t * 2f;

        Color c =
            audioColorGradient.Evaluate(t);

        c.a = 1f;

        mainModule.startColor = c;
    }

    /// <summary>
    /// 更新整体流动
    /// </summary>
    void UpdateParticleFlow()
    {
        ApplyMaxParticleSettings();

        int count = ps.GetParticles(particles);

        if (count <= 0)
            return;

        float musicStrength =
            Mathf.Clamp01(smoothedAmplitude);

        Vector3 center = Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            center += particles[i].position;
        }

        center /= count;

        float globalPhase =
            Time.time * waveTravelSpeed;

        for (int i = 0; i < count; i++)
        {
            ParticleSystem.Particle p =
                particles[i];

            float seed =
                Hash01(p.randomSeed);

            float phaseOffset =
                (seed - 0.5f)
                * phaseScatter
                * Mathf.PI * 2f;

            float alongFlow =
                Vector3.Dot(
                    p.position,
                    flowDirNormalized
                );

            float phase =
                globalPhase
                + alongFlow * waveSpatialFrequency
                + phaseOffset;

            float wave =
                Mathf.Sin(phase);

            float sideWave =
                Mathf.Cos(phase * 1.3f);

            Vector3 velocity =
                flowDirNormalized *
                (
                    baseFlowSpeed +
                    musicStrength * audioFlowBoost
                );

            velocity +=
                sideAxis *
                sideWave *
                orbitStrength;

            velocity +=
                upAxis *
                wave *
                liftStrength;

            velocity +=
                flowDirNormalized *
                wave *
                waveAmplitude;

            p.velocity =
                Vector3.Lerp(
                    p.velocity,
                    velocity,
                    Time.deltaTime * velocityResponse
                );

            if (enableMusicRotation)
            {
                float pulse =
                    1f +
                    Mathf.Sin(
                        Time.time *
                        rotationPulseSpeed +
                        phaseOffset
                    )
                    * rotationPulseAmount;

                float rotationSpeed =
                    (
                        baseRotationSpeed +
                        musicStrength *
                        rotationAudioBoost
                    )
                    * pulse;

                Vector3 rot =
                    p.rotation3D;

                rot.y +=
                    rotationSpeed *
                    Time.deltaTime;

                p.rotation3D = rot;
            }

            particles[i] = p;
        }

        ps.SetParticles(
            particles,
            count
        );
    }

    /// <summary>
    /// 获取频段平均值
    /// </summary>
    float GetAverageBand(
        int startInclusive,
        int endExclusive
    )
    {
        if (audioController == null)
            return 0f;

        if (audioController.audioBandBuffer == null)
            return 0f;

        int start =
            Mathf.Clamp(
                startInclusive,
                0,
                audioController.audioBandBuffer.Length
            );

        int end =
            Mathf.Clamp(
                endExclusive,
                start + 1,
                audioController.audioBandBuffer.Length
            );

        float total = 0f;
        int count = 0;

        for (int i = start; i < end; i++)
        {
            total +=
                audioController.audioBandBuffer[i];

            count++;
        }

        return count > 0
            ? total / count
            : 0f;
    }

    /// <summary>
    /// 更新方向轴
    /// </summary>
    void RefreshFlowAxes()
    {
        flowDirNormalized =
            flowDirection.normalized;

        if (flowDirNormalized == Vector3.zero)
            flowDirNormalized = Vector3.up;

        sideAxis =
            Vector3.Cross(
                flowDirNormalized,
                Vector3.up
            );

        if (sideAxis.sqrMagnitude < 0.001f)
        {
            sideAxis =
                Vector3.Cross(
                    flowDirNormalized,
                    Vector3.right
                );
        }

        sideAxis.Normalize();

        upAxis =
            Vector3.Cross(
                sideAxis,
                flowDirNormalized
            );

        upAxis.Normalize();
    }

    /// <summary>
    /// 应用粒子数量限制
    /// </summary>
    void ApplyMaxParticleSettings()
    {
        int target =
            Mathf.Max(
                1,
                maxParticles
            );

        if (
            appliedMaxParticles == target
            && particles != null
            && particles.Length == target
        )
        {
            return;
        }

        appliedMaxParticles = target;

        mainModule.maxParticles =
            appliedMaxParticles;

        particles =
            new ParticleSystem.Particle[target];
    }

    /// <summary>
    /// 随机数生成
    /// </summary>
    float Hash01(uint seed)
    {
        seed ^= 2747636419u;
        seed *= 2654435769u;
        seed ^= seed >> 16;
        seed *= 2654435769u;
        seed ^= seed >> 16;

        return
            (seed & 0x00FFFFFFu)
            / 16777215f;
    }
}