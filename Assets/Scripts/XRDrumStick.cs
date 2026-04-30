using UnityEngine;
using UnityEngine.InputSystem;

public class XRDrumStick : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference activationAction;

    [Header("State")]
    [Range(0f, 1f)]
    public float pressThreshold = 0.1f;

    public bool IsActivated { get; private set; }

    private void OnEnable()
    {
        if (activationAction != null && activationAction.action != null)
        {
            activationAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (activationAction != null && activationAction.action != null)
        {
            activationAction.action.Disable();
        }
    }

    private void Update()
    {
        if (activationAction == null || activationAction.action == null)
        {
            IsActivated = false;
            return;
        }

        float value = 0f;
        try
        {
            value = activationAction.action.ReadValue<float>();
        }
        catch
        {
            IsActivated = activationAction.action.phase == InputActionPhase.Performed;
        }

        IsActivated = value >= pressThreshold;
    }
}