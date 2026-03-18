using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class AudioFlowParticle : MonoBehaviour
{
    [Header("绑定我们之前的音频控制器")]
    public AudioController audioController;

    [Header("颜色渐变带")]
    public Gradient audioColorGradient;

    [Header("平滑与强度控制 (核心新增)")]
    [Range(1f, 20f)]
    [Tooltip("值越小，变化越平滑、过渡越柔和；值越大，变化越灵敏、剧烈。")]
    public float smoothSpeed = 8f; 

    [Range(0f, 2f)]
    [Tooltip("整体效果的缩放倍数。如果觉得整体跳动得太夸张，可以调小这个值（如0.5）。")]
    public float reactionIntensity = 1f;

    // 内部引用
    private ParticleSystem ps;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.NoiseModule noiseModule;

    // 新增：用于记录上一帧的“平滑结果”，让数值不会瞬间跳跃
    private float smoothedAmplitude = 0f;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        mainModule = ps.main;
        noiseModule = ps.noise; // 获取流场模块
    }

    void Update()
    {
        if (audioController == null) return;

        // 1. 获取当前帧的“真实目标振幅”
        float targetAmplitude = 0;
        for (int i = 0; i < 8; i++)
        {
            targetAmplitude += audioController.audioBandBuffer[i];
        }
        
        // 计算出平均值，并乘以我们在面板里设置的反应强度
        targetAmplitude = (targetAmplitude / 8f) * reactionIntensity; 

        // 2. 核心平滑算法：Mathf.Lerp
        // 它的作用是：让 smoothedAmplitude 平滑地向 targetAmplitude 靠近，而不是瞬间等于它。
        smoothedAmplitude = Mathf.Lerp(smoothedAmplitude, targetAmplitude, Time.deltaTime * smoothSpeed);

        // 3. 将原本直接使用目标值的代码，全部替换为使用“平滑后的值 (smoothedAmplitude)”
        mainModule.startSizeMultiplier = 1f + (smoothedAmplitude * 2f);
        mainModule.simulationSpeed = 1f + (smoothedAmplitude * 4f);
        noiseModule.strengthMultiplier = 1f + (smoothedAmplitude * 3f);
        
        // 颜色也使用平滑后的值来获取
        mainModule.startColor = audioColorGradient.Evaluate(smoothedAmplitude);
    }
}