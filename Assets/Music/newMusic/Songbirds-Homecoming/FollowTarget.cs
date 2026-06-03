using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    [Header("要跟随的手柄物体")]
    public Transform targetHand;

    void LateUpdate()
    {
        if (targetHand != null)
        {
            // 实时把自己的位置和旋转同步给手柄
            transform.position = targetHand.position;
            transform.rotation = targetHand.rotation;
        }
    }
}