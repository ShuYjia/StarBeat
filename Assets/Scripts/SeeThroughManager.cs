using UnityEngine;
using Unity.XR.PXR;

public class SeeThroughManager : MonoBehaviour
{
    public static SeeThroughManager Instance { get; private set; }

    public static bool IsSeeThroughEnabled
    {
        get
        {
            return Instance != null ? Instance.isSeeThroughActive : PXR_Manager.EnableVideoSeeThrough;
        }
    }

    [SerializeField] private bool activateOnAwake = true;

    public bool isSeeThroughActive { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (activateOnAwake)
        {
            ActivateSeeThrough();
        }
        else
        {
            RefreshState();
        }
    }

    private void OnEnable()
    {
        RefreshState();
    }

    private void Update()
    {
        RefreshState();
    }

    public void ActivateSeeThrough()
    {
        PXR_Manager.EnableVideoSeeThrough = true;
        RefreshState();
    }

    public void DeactivateSeeThrough()
    {
        PXR_Manager.EnableVideoSeeThrough = false;
        RefreshState();
    }

    public void ToggleSeeThrough()
    {
        if (IsSeeThroughEnabled)
        {
            DeactivateSeeThrough();
        }
        else
        {
            ActivateSeeThrough();
        }
    }

    private void RefreshState()
    {
        isSeeThroughActive = PXR_Manager.EnableVideoSeeThrough;
    }

    private void OnApplicationPause(bool pause)
    {
        if (!pause && activateOnAwake)
        {
            ActivateSeeThrough();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
