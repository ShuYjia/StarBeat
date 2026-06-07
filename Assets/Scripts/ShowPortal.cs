using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShowPortal : MonoBehaviour
{
    public GameObject[] portal;
    public InputActionReference rightPrimary_A;
    public GameObject transformObject;
    public GameObject portalUI;
    public Button showStarBtn;
    public Button rainbowPortalBtn;
    public Button hideStarBtn;
    public Button hideRainbowBtn;
        private void Awake()
    {
        showStarBtn.onClick.AddListener(()=>ShowStar(true));
        rainbowPortalBtn.onClick.AddListener(() => ShowRainbow(true));
        hideStarBtn.onClick.AddListener(() => ShowStar(false));
        hideRainbowBtn.onClick.AddListener(() => ShowRainbow(false));
    }
    private void ShowRainbow(bool v)
    {
        if (v) { 
            portal[0].transform.position = transformObject.transform.position;

        }
        else
            portal[0].transform.position = new Vector3(999, 999, 999);
    }

    private void ShowStar(bool v)
    {
        if (v)
        {
            portal[1].transform.position = transformObject.transform.position;
            Debug.Log(transform.position = transformObject.transform.position);
        }
        else
            portal[1].transform.position = new Vector3(999, 999, 999);

    }
    private void OnEnable()
    {
        rightPrimary_A.action.started += OnRightAPress;
        rightPrimary_A.action.canceled += OnRightARelese;
    }
    private void OnDisable()
    {
        rightPrimary_A.action.started -= OnRightAPress;
        rightPrimary_A.action.canceled -= OnRightARelese;
    }

    private void OnRightARelese(InputAction.CallbackContext context)
    {
        if (SpatialAnchor.Instance.MainUI.activeSelf)
            return;
        if (SpatialAnchor.Instance.IsCreateAnchor == false)
            portalUI.SetActive(false);
    }

    private void OnRightAPress(InputAction.CallbackContext context)
    {
        if (SpatialAnchor.Instance.MainUI.activeSelf)
            return;
        if (SpatialAnchor.Instance.IsCreateAnchor == false)
            portalUI.SetActive(true);
    }

    // Start is called before the first frame update
    void Start()
    {
        portalUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
