using UnityEngine;
using UnityEngine.InputSystem; // 引入新版输入系统

public class VRHandTrigger : MonoBehaviour
{
    [Header("输入绑定")]
    [Tooltip("请拖入对应的 Action，例如 LeftTrigger 或 RightTrigger")]
    public InputActionReference triggerAction;

    private void OnEnable()
    {
        if (triggerAction != null) triggerAction.action.Enable();
    }

    private void OnDisable()
    {
        if (triggerAction != null) triggerAction.action.Disable();
    }

    // 当手柄的碰撞体停留在其他触发器 (Trigger) 内部时每帧调用
    private void OnTriggerStay(Collider other)
    {
        // 检查绑定的扳机键是否在这一帧刚刚被按下
        if (triggerAction != null && triggerAction.action.WasPressedThisFrame())
        {
            // 尝试获取碰到的物体身上的乐器脚本
            InstrumentInteractable instrument = other.GetComponent<InstrumentInteractable>();

            if (instrument != null)
            {
                // 触发乐器，并把手柄当前的位置传过去，用于生成粒子
                instrument.TriggerInstrument(transform.position);
            }
        }
    }
}