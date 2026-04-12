using UnityEngine;

public class AudioWaveVisualizer : MonoBehaviour
{
    [Header("音频与基础设置")]
    public AudioSource audioSource;
    public GameObject particlePrefab; // 可以是一个Cube或Sphere
    public int particleCount = 64;    // 生成的粒子/方块数量
    public float spacing = 1.2f;      // 粒子之间的间距

    [Header("波浪效果参数")]
    public float heightMultiplier = 100f; // 音乐对高度的放大倍数
    public float smoothSpeed = 10f;       // 运动平滑度（避免闪烁过快）
    public float minHeight = 0.5f;        // 粒子的最小高度

    [Header("外观与颜色")]
    public Gradient colorGradient;        // 根据音乐强度改变颜色的渐变带
    public float colorMultiplier = 50f;   // 颜色变化的敏感度

    private GameObject[] particles;
    private float[] spectrumData = new float[512]; // 采样数组，通常是2的n次幂
    private MaterialPropertyBlock propBlock;

    void Start()
    {
        particles = new GameObject[particleCount];
        propBlock = new MaterialPropertyBlock();

        // 将粒子排列成一条直线（波浪状）
        // 如果想做成圆形，可以使用正弦和余弦函数来计算位置
        for (int i = 0; i < particleCount; i++)
        {
            Vector3 spawnPos = transform.position + new Vector3(i * spacing, 0, 0);
            particles[i] = Instantiate(particlePrefab, spawnPos, Quaternion.identity);
            particles[i].transform.parent = this.transform;
        }
    }

    void Update()
    {
        // 获取音频频谱数据 (使用BlackmanHarris窗函数能让数据更平滑)
        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

        for (int i = 0; i < particleCount; i++)
        {
            if (particles[i] != null)
            {
                // 1. 获取当前频段的强度
                // 乘以一个系数是因为高频的数据通常非常小
                float intensity = spectrumData[i] * (i + 1);

                // 2. 计算目标高度并进行平滑插值 (Lerp)
                float targetHeight = minHeight + (intensity * heightMultiplier);
                Vector3 currentScale = particles[i].transform.localScale;
                Vector3 newScale = new Vector3(currentScale.x, targetHeight, currentScale.z);

                particles[i].transform.localScale = Vector3.Lerp(currentScale, newScale, Time.deltaTime * smoothSpeed);

                // 3. 改变颜色
                Renderer rend = particles[i].GetComponent<Renderer>();
                if (rend != null)
                {
                    // 计算颜色在渐变带上的位置 (0 到 1)
                    float colorEvaluation = Mathf.Clamp01(intensity * colorMultiplier);
                    Color targetColor = colorGradient.Evaluate(colorEvaluation);

                    // 使用 MaterialPropertyBlock 提升性能
                    rend.GetPropertyBlock(propBlock);
                    propBlock.SetColor("_Color", targetColor); // 如果使用URP/HDRP，这里可能是 "_BaseColor"
                    rend.SetPropertyBlock(propBlock);
                }
            }
        }
    }
}