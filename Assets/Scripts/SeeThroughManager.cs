using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.PXR;

public class SeeThroughManager : MonoBehaviour
{
    public void ActivateSeeThrough()
    {
        PXR_Manager.EnableVideoSeeThrough = true;
    }
    public void DeactivateSeeThrough()
    {
        PXR_Manager.EnableVideoSeeThrough = false;
    }
    private void Awake()
    {
        ActivateSeeThrough();
    }
    private void OnApplicationPause(bool pause)
    {
        if (!pause)
            ActivateSeeThrough();
    }
}
