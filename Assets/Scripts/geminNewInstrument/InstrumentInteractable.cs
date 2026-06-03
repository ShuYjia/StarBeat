using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider))]
public class InstrumentInteractable : MonoBehaviour
{
    [Header("声音设置")]
    public AudioClip instrumentSound;
    public float volumeScale = 1f;

    [Header("随机模型特效")]
    public GameObject[] effectModelPrefabs;

    [Header("随机颜色")]
    public Color[] effectColors;

    [Header("生成设置")]
    public Vector3 spawnOffset;

    [Tooltip("生成范围")]
    public float spawnRadius = 0.05f;

    [Tooltip("持续时间")]
    public float effectDuration = 1.5f;

    [Tooltip("上升速度")]
    public float riseSpeed = 0.4f;

    [Tooltip("初始大小")]
    public float startScale = 0.08f;

    [Header("钢琴键动画与冷却")]
    public bool isKeyboardKey = false;
    public float pressDepth = 0.015f;
    public float pressDuration = 0.1f;

    [Tooltip("仅在作为钢琴键(isKeyboardKey为true)时生效的触发冷却时间（秒）")]
    public float keyboardTriggerCooldown = 1.0f; // 可以在 Inspector 中调节
    private float nextKeyboardTriggerTime = 0f;  // 仅用于钢琴键的时间戳

    private Transform targetTransform;
    private Vector3 originalLocalPos;
    private AudioController audioController;
    private bool isEffectPlaying = false;

    private void Start()
    {
        if (isKeyboardKey)
            targetTransform = transform.parent;
        else
            targetTransform = transform;

        if (targetTransform != null)
            originalLocalPos = targetTransform.localPosition;

        audioController = FindObjectOfType<AudioController>();

        GetComponent<BoxCollider>().isTrigger = true;
    }

    public void TriggerInstrument(Vector3 hitPosition)
    {
        // === 核心改动：如果是钢琴键，单独进行冷却时间拦截 ===
        if (isKeyboardKey)
        {
            if (Time.time < nextKeyboardTriggerTime)
            {
                // 还在冷却中，直接退出，不播声音、不播动画、不生特效
                return;
            }
            // 满足触发条件，更新该钢琴键的下一次允许触发时间
            nextKeyboardTriggerTime = Time.time + keyboardTriggerCooldown;
        }

        // 其它非钢琴乐器不受上述时间拦截，直接走原本的特效防重叠逻辑
        if (isEffectPlaying)
            return;

        // 执行声音触发
        if (audioController != null && instrumentSound != null)
        {
            audioController.PlayInstrumentSound(
                instrumentSound,
                volumeScale
            );
        }

        // 执行特效生成
        StartCoroutine(PlaySpawnEffect(hitPosition));

        // 执行按键动画（此时能走到这里的钢琴键必然已经通过了上面的冷却筛选）
        if (isKeyboardKey)
        {
            StopCoroutine(nameof(AnimateKeyPress));
            StartCoroutine(AnimateKeyPress());
        }
    }

    private IEnumerator PlaySpawnEffect(Vector3 hitPosition)
    {
        isEffectPlaying = true;

        if (effectModelPrefabs == null ||
            effectModelPrefabs.Length == 0)
        {
            isEffectPlaying = false;
            yield break;
        }

        GameObject prefab =
            effectModelPrefabs[
                Random.Range(
                    0,
                    effectModelPrefabs.Length
                )
            ];

        if (prefab == null)
        {
            isEffectPlaying = false;
            yield break;
        }

        Vector3 randomOffset =
            new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                Random.Range(-0.02f, 0.02f),
                Random.Range(-spawnRadius, spawnRadius)
            );

        Vector3 spawnPos =
            hitPosition +
            spawnOffset +
            randomOffset;

        GameObject effect =
            Instantiate(
                prefab,
                spawnPos,
                Random.rotation
            );

        float randomScale =
            startScale *
            Random.Range(0.8f, 1.3f);

        effect.transform.localScale =
            Vector3.one * randomScale;

        Color selectedColor = Color.white;

        if (effectColors != null &&
            effectColors.Length > 0)
        {
            selectedColor =
                effectColors[
                    Random.Range(
                        0,
                        effectColors.Length
                    )
                ];
        }

        Renderer[] renderers =
            effect.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            Material[] mats = renderer.materials;

            foreach (Material mat in mats)
            {
                if (mat == null)
                    continue;

                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor(
                        "_BaseColor",
                        selectedColor
                    );
                }

                if (mat.HasProperty("_Color"))
                {
                    mat.SetColor(
                        "_Color",
                        selectedColor
                    );
                }

                SetupTransparentMaterial(mat);
            }
        }

        Vector3 driftDirection =
            (
                Vector3.up +
                new Vector3(
                    Random.Range(-0.15f, 0.15f),
                    0f,
                    Random.Range(-0.15f, 0.15f)
                )
            ).normalized;

        Vector3 rotationSpeed =
            new Vector3(
                Random.Range(-120f, 120f),
                Random.Range(-180f, 180f),
                Random.Range(-120f, 120f)
            );

        float timer = 0f;

        while (timer < effectDuration)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / effectDuration
                );

            effect.transform.position +=
                driftDirection *
                riseSpeed *
                Time.deltaTime;

            effect.transform.Rotate(
                rotationSpeed *
                Time.deltaTime,
                Space.Self
            );

            float scale =
                Mathf.Lerp(
                    randomScale,
                    0f,
                    t
                );

            effect.transform.localScale =
                Vector3.one * scale;

            foreach (Renderer renderer in renderers)
            {
                Material[] mats =
                    renderer.materials;

                foreach (Material mat in mats)
                {
                    if (mat == null)
                        continue;

                    Color c = selectedColor;
                    c.a = Mathf.Lerp(1f, 0f, t);

                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor(
                            "_BaseColor",
                            c
                        );
                    }

                    if (mat.HasProperty("_Color"))
                    {
                        mat.SetColor(
                            "_Color",
                            c
                        );
                    }
                }
            }

            yield return null;
        }

        Destroy(effect);

        isEffectPlaying = false;
    }

    private void SetupTransparentMaterial(Material mat)
    {
        if (mat == null)
            return;

        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1);
        }

        if (mat.HasProperty("_Blend"))
        {
            mat.SetFloat("_Blend", 0);
        }

        if (mat.HasProperty("_ZWrite"))
        {
            mat.SetFloat("_ZWrite", 0);
        }

        mat.renderQueue = 3000;
    }

    private IEnumerator AnimateKeyPress()
    {
        if (targetTransform == null)
            yield break;

        targetTransform.localPosition =
            originalLocalPos +
            Vector3.down * pressDepth;

        yield return new WaitForSeconds(
            pressDuration
        );

        targetTransform.localPosition =
            originalLocalPos;
    }

    public void TestTriggerInInspector()
    {
        TriggerInstrument(transform.position);
    }
}