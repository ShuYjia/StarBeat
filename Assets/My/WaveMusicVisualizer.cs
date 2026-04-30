using UnityEngine;

public class WaveMusicVisualizer : MonoBehaviour
{
    [Header("音频控制")]
    public AudioController audioController;

    [Header("波浪配置")]
    [Tooltip("波浪行的数量（Z轴）")]
    [Range(1, 50)]
    public int rows = 5;

    [Tooltip("波浪列的数量（X轴）")]
    [Range(10, 100)]
    public int columns = 32;

    [Tooltip("方块之间的间距")]
    public float spacing = 0.8f;

    [Tooltip("方块的基础大小")]
    public float cubeSize = 0.5f;

    [Header("动画效果")]
    [Tooltip("高度放大的倍数")]
    [Range(10f, 200f)]
    public float heightMultiplier = 50f;

    [Tooltip("动画平滑速度（值越大变化越快）")]
    [Range(1f, 30f)]
    public float smoothSpeed = 10f;

    [Tooltip("最小高度")]
    [Range(0.1f, 5f)]
    public float minHeight = 0.2f;

    [Header("波浪传播")]
    [Tooltip("启用波浪传播效果")]
    public bool enableWavePropagation = true;

    [Tooltip("波浪传播速度")]
    [Range(0.1f, 10f)]
    public float waveSpeed = 2f;

    [Tooltip("波浪幅度")]
    [Range(0f, 2f)]
    public float waveAmplitude = 0.5f;

    [Tooltip("波浪频率")]
    [Range(0.1f, 5f)]
    public float waveFrequency = 1f;

    [Header("颜色效果")]
    [Tooltip("启用颜色渐变")]
    public bool enableColorGradient = true;

    public Gradient colorGradient;

    [Tooltip("颜色变化速度")]
    [Range(0.1f, 5f)]
    public float colorSpeed = 1f;

    [Header("显示选项")]
    public GameObject cubePrefab;

    [Tooltip("波浪方向（水平/垂直/圆形）")]
    public enum WaveDirection { Horizontal, Vertical, Circular }
    public WaveDirection waveDirection = WaveDirection.Horizontal;

    // 内部变量
    private GameObject[,] cubes;
    private float[,] targetHeights;
    private float[,] currentHeights;
    private MeshRenderer[] renderers;
    private MaterialPropertyBlock[] propBlocks;

    private bool isInitialized = false;
    private float time = 0f;

    void Start()
    {
        InitializeWave();
    }

    void Update()
    {
        if (!isInitialized || audioController == null) return;

        time += Time.deltaTime;

        UpdateWaveVisualization();
    }

    void InitializeWave()
    {
        // 清理旧的方块
        ClearExistingCubes();

        cubes = new GameObject[rows, columns];
        targetHeights = new float[rows, columns];
        currentHeights = new float[rows, columns];

        int totalCubes = rows * columns;
        renderers = new MeshRenderer[totalCubes];
        propBlocks = new MaterialPropertyBlock[totalCubes];

        Vector3 centerOffset = new Vector3(
            (columns - 1) * spacing * 0.5f,
            0f,
            (rows - 1) * spacing * 0.5f
        );

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector3 position;

                switch (waveDirection)
                {
                    case WaveDirection.Horizontal:
                        position = new Vector3(col * spacing, 0, row * spacing) - centerOffset;
                        break;
                    case WaveDirection.Vertical:
                        position = new Vector3(row * spacing, 0, col * spacing) - centerOffset;
                        break;
                    case WaveDirection.Circular:
                        float angle = (float)col / columns * Mathf.PI * 2f;
                        float radius = (float)row * spacing;
                        position = new Vector3(
                            Mathf.Cos(angle) * radius,
                            0f,
                            Mathf.Sin(angle) * radius
                        );
                        break;
                    default:
                        position = new Vector3(col * spacing, 0, row * spacing) - centerOffset;
                        break;
                }

                GameObject cube;
                if (cubePrefab != null)
                {
                    cube = Instantiate(cubePrefab, transform);
                }
                else
                {
                    cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.SetParent(transform);
                }

                cube.transform.localPosition = position;
                cube.transform.localScale = new Vector3(cubeSize, cubeSize, cubeSize);

                cubes[row, col] = cube;

                int index = row * columns + col;
                MeshRenderer renderer = cube.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderers[index] = renderer;
                    propBlocks[index] = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(propBlocks[index]);
                }

                currentHeights[row, col] = minHeight;
            }
        }

        isInitialized = true;
    }

    void ClearExistingCubes()
    {
        // 清除所有子对象（旧的方块）
        while (transform.childCount > 0)
        {
            if (Application.isPlaying)
            {
                Destroy(transform.GetChild(0).gameObject);
            }
            else
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
        }
    }

    void UpdateWaveVisualization()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                // 获取对应的音频频率
                int freqIndex = Mathf.Clamp(col, 0, 15);
                float audioValue = audioController.audioBandBuffer[freqIndex];

                // 计算波浪传播偏移
                float waveOffset = 0f;
                if (enableWavePropagation)
                {
                    float distance = waveDirection == WaveDirection.Circular ?
                        row * spacing :
                        Vector3.Distance(Vector3.zero, new Vector3(col, 0, row));

                    waveOffset = Mathf.Sin(distance * waveFrequency - time * waveSpeed) * waveAmplitude;
                }

                // 计算目标高度
                float targetHeight = minHeight + (audioValue * heightMultiplier);
                targetHeight *= (1f + waveOffset);
                targetHeight = Mathf.Max(minHeight, targetHeight);

                targetHeights[row, col] = targetHeight;

                // 平滑过渡
                float lerpSpeed = Time.deltaTime * smoothSpeed;
                currentHeights[row, col] = Mathf.Lerp(
                    currentHeights[row, col],
                    targetHeights[row, col],
                    lerpSpeed
                );

                // 应用新的缩放
                float newY = currentHeights[row, col];
                cubes[row, col].transform.localScale = new Vector3(cubeSize, newY, cubeSize);

                // 更新颜色
                if (enableColorGradient)
                {
                    int index = row * columns + col;
                    if (renderers[index] != null)
                    {
                        // 根据高度和时间计算颜色
                        float colorT = Mathf.Clamp01(audioValue + Mathf.Sin(time * colorSpeed) * 0.2f);
                        Color targetColor = colorGradient.Evaluate(colorT);

                        propBlocks[index].SetColor("_Color", targetColor);

                        // 添加发光效果（如果材质支持）
                        float emissionIntensity = audioValue * 2f;
                        propBlocks[index].SetColor("_EmissionColor", targetColor * emissionIntensity);

                        renderers[index].SetPropertyBlock(propBlocks[index]);
                    }
                }
            }
        }
    }

    // 编辑器中的初始化按钮
    [ContextMenu("Reinitialize Wave")]
    public void ReinitializeWave()
    {
        InitializeWave();
    }

    void OnDestroy()
    {
        // 清理
        ClearExistingCubes();
    }
}
