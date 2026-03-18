using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class AudioParticleController : MonoBehaviour
{
    [Header("核心配置")]
    public AudioController audioController;
    // 注意：这里我们删除了 listenBand，因为我们不再只听某一个频段了

    [Header("动态效果控制")]
    [Tooltip("粒子随整体音乐加速移动的倍率。因为平均值通常比单一低音的峰值小，你可能需要把这个值调大一些。")]
    public float pushForce = 80f;

    [Tooltip("平滑过渡速度")]
    public float smoothSpeed = 10f;

    [Tooltip("随整体音乐能量变化的颜色带")]
    public Gradient audioColorGradient;

    // --- 内部私有变量 ---
    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;

    private float smoothedAudioValue;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[ps.main.maxParticles];
    }

    void Update()
    {
        if (audioController == null) return;

        // 1. 【核心改动】计算整首音乐的平均能量
        float currentAmplitude = 0f;

        // 遍历前 8 个主要频段（这涵盖了绝大多数乐器和人声的核心频率）
        for (int i = 0; i < 8; i++)
        {
            currentAmplitude += audioController.audioBandBuffer[i];
        }

        // 将总和除以 8，得到平均值。这个值依然会保持在 0 到 1 之间。
        currentAmplitude = currentAmplitude / 8f;

        // 2. 平滑处理，避免画面抽搐
        smoothedAudioValue = Mathf.Lerp(smoothedAudioValue, currentAmplitude, Time.deltaTime * smoothSpeed);

        // 3. 根据平滑后的整体能量，获取当前颜色
        Color currentColor = audioColorGradient.Evaluate(smoothedAudioValue);

        // 4. 抓取所有存活的粒子
        int numParticlesAlive = ps.GetParticles(particles);

        // 5. 遍历并修改每一个粒子
        for (int i = 0; i < numParticlesAlive; i++)
        {
            // 修改为整体能量对应的颜色
            particles[i].startColor = currentColor;

            // 让粒子向外推
            Vector3 pushDirection = particles[i].position.normalized;

            // 使用整体能量 (smoothedAudioValue) 来驱动粒子速度
            particles[i].velocity += pushDirection * smoothedAudioValue * pushForce * Time.deltaTime;
        }

        // 6. 重新塞回粒子系统进行渲染
        ps.SetParticles(particles, numParticlesAlive);
    }
}