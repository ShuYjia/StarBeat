using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class AudioVortexParticles : MonoBehaviour
{
    public AudioController audioController;

    [Header("漩涡动力学")]
    [Tooltip("基础旋转速度")]
    public float baseRotationSpeed = 20f;
    [Tooltip("音乐带动的额外旋转强度")]
    public float audioRotationIntensity = 50f;
    [Tooltip("音乐带动的向外喷发力")]
    public float explosionForce = 30f;
    [Tooltip("粒子绕哪个轴旋转？(0,1,0) 是绕Y轴")]
    public Vector3 rotationAxis = Vector3.up;

    [Header("外观控制")]
    public Gradient audioColorGradient;
    [Range(1f, 20f)]
    public float smoothSpeed = 8f;
    public float damping = 0.98f; // 阻力，让旋转更丝滑

    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;
    private float smoothedAmplitude;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[ps.main.maxParticles];
    }

    void Update()
    {
        if (audioController == null) return;

        // 1. 获取整体音乐能量
        float totalAmp = 0;
        for (int i = 0; i < 8; i++) totalAmp += audioController.audioBandBuffer[i];
        float currentAmp = totalAmp / 8f;
        smoothedAmplitude = Mathf.Lerp(smoothedAmplitude, currentAmp, Time.deltaTime * smoothSpeed);

        // 2. 获取当前粒子
        int numParticlesAlive = ps.GetParticles(particles);
        Color currentColor = audioColorGradient.Evaluate(smoothedAmplitude);

        // 计算当前帧的总旋转量
        float currentRotationSpeed = baseRotationSpeed + (smoothedAmplitude * audioRotationIntensity);

        for (int i = 0; i < numParticlesAlive; i++)
        {
            // 设置颜色
            particles[i].startColor = currentColor;

            // --- 核心数学：计算漩涡力 ---

            // 获取粒子相对于中心的方向向量 (忽略Y轴高度，只算平面旋转更像银河)
            Vector3 directionToParticle = particles[i].position;

            // A. 计算切线方向 (数学魔法：叉乘)
            // 叉乘“旋转轴”和“指向粒子的向量”，会得到一个垂直于两者的向量，即切线方向
            Vector3 tangentDirection = Vector3.Cross(directionToParticle, rotationAxis).normalized;

            // B. 计算径向方向 (向外喷发)
            Vector3 outwardDirection = directionToParticle.normalized;

            // C. 施加力
            // 旋转力：切线方向 * 旋转速度
            Vector3 rotationVelocity = tangentDirection * currentRotationSpeed;
            // 喷发力：向外方向 * 音乐强度 * 力量
            Vector3 explosionVelocity = outwardDirection * smoothedAmplitude * explosionForce;

            // D. 应用速度
            // 粒子速度 = 原有速度的衰减 + (旋转速度 + 喷发速度) * 时间
            particles[i].velocity = (particles[i].velocity * damping) + (rotationVelocity + explosionVelocity) * Time.deltaTime;
        }

        // 3. 应用回粒子系统
        ps.SetParticles(particles, numParticlesAlive);
    }
}