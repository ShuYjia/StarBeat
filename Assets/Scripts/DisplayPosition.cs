using UnityEngine;
using UnityEngine.UI; // 1. 注意：原生的 Text 组件必须引入这个命名空间

public class DisplayPosition : MonoBehaviour
{
    [Header("目标物体")]
    [Tooltip("你想实时获取位置的 GameObject")]
    public Transform targetGameObject;
    [Header("UI 文本组件")]

    public Text uiText; // 2. 变量类型从 TextMeshProUGUI 改为 Text

    void Update()
    {
        // 确保目标物体和UI组件都已赋值
        if (targetGameObject != null && uiText != null)
        {
            // 获取目标物体的世界坐标
            Vector3 position = targetGameObject.position;

            // 格式化输出文本，"F2" 表示保留两位小数
            uiText.text = $"位置:\nX: {position.x:F2}\nY: {position.y:F2}\nZ: {position.z:F2}";
        }
    }
}