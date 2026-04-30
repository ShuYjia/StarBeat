// InstrumentInteractable.cs
using UnityEngine;
using System.Collections; // 用于协程动画

[RequireComponent(typeof(BoxCollider))] // 确保物体有 Collider
public class InstrumentInteractable : MonoBehaviour
{
    [Header("声音设置")]
    public AudioClip instrumentSound; // 在 Inspector 中拖入该位置对应的音效
   // [Range(0f, 1f)] // 在 Inspector 中创建一个音量滑块
    public float volumeScale = 1.0f; // 默认音量为 1 (最大)

    [Header("特效设置")]
    public GameObject noteParticlePrefab; // 拖入做好的粒子特效预制体
    public  Vector3 noteOffset = Vector3.zero;
    [Header("键盘特殊设置")]
    public bool isKeyboardKey = false; // 如果是键盘按键，勾选此项
    public float pressDepth = 0.015f;  // 按下去的深度 (Y轴偏移量)
    public float pressDuration = 0.1f; // 按下去后恢复的时间

    private Transform targetTransform; // 需要移动的目标物体
    private Vector3 originalLocalPos;
    private AudioController audioController;

    void Start()
    {
        // 从你的截图来看，脚本挂在一个空物体（GameObject）上，这个空物体是琴键模型的子物体。
        // 如果是琴键，我们获取脚本所在物体的父物体（即琴键模型）。
        if (isKeyboardKey)
        {
            targetTransform = transform.parent;
        }
        else
        {
            targetTransform = transform;
        }

        if (targetTransform != null)
        {
            // 记录目标物体的初始相对位置 (使用 localPosition 防止父物体移动时出错)
            originalLocalPos = targetTransform.localPosition;
        }

        // 自动在场景中寻找 AudioController
        audioController = FindObjectOfType<AudioController>();

        // 【关键修复】：自动确保 collider 是 trigger，防止忘记勾选
        GetComponent<BoxCollider>().isTrigger = true;
    }

    // 当手柄按下扳机并在此物体内部时，由手柄脚本调用此方法
    public void TriggerInstrument(Vector3 hitPosition)
    {
        // 1. 播放声音 (传递音量缩放)
        if (audioController != null && instrumentSound != null)
        {
            audioController.PlayInstrumentSound(instrumentSound, volumeScale);
        }

        // 2. 生成粒子特效
        if (noteParticlePrefab != null)
        {
            // 在手柄位置生成粒子
            //Vector3 spawnPos = hitPosition + Vector3.up * 0.1f; // 向上抬高 0.1 米
            //Instantiate(noteParticlePrefab, spawnPos, Quaternion.identity);
           Instantiate(noteParticlePrefab, hitPosition+noteOffset, Quaternion.identity);
        }

        // 3. 执行键盘按压效果
        if (isKeyboardKey && targetTransform != null)
        {
            StopAllCoroutines(); // 如果快速连按，先停止上一次的动画
            StartCoroutine(AnimateKeyPress());
        }
    }

    // 协程：控制目标物体 (琴键模型) 按下再弹起的过程
    private IEnumerator AnimateKeyPress()
    {
        // 向下移动 (假设按键的上方是Y轴正方向，如果是Z轴请改成 Vector3.forward)
        targetTransform.localPosition = originalLocalPos + Vector3.down * pressDepth;

        // 等待设定的时间
        yield return new WaitForSeconds(pressDuration);

        // 恢复原位
        targetTransform.localPosition = originalLocalPos;
    }
    /// <summary>
    /// Inspector 测试用：直接触发一次
    /// </summary>
    public void TestTriggerInInspector()
    {
        TriggerInstrument(transform.position);
    }
}