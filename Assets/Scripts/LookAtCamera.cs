using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour


{
    private Transform camerTransform;
    private void Awake()
    {
        camerTransform = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (camerTransform == null)
        {
            camerTransform = Camera.main.transform;
        }
        transform.rotation = Quaternion.LookRotation(camerTransform.transform.forward);

    }
}
