using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShowPortal : MonoBehaviour
{
    [Header("传送门")]
    public GameObject[] portal;

    [Header("手柄输入")]
    public InputActionReference rightPrimary_A;

    [Header("生成位置")]
    public GameObject transformObject;

    [Header("UI")]
    public GameObject portalUI;

    [Header("按钮")]
    public Button showStarBtn;
    public Button rainbowPortalBtn;
    public Button hideStarBtn;
    public Button hideRainbowBtn;

    [Header("动画")]
    [Range(0.05f, 2f)]
    public float portalAnimTime = 0.35f;

    private Coroutine rainbowCoroutine;
    private Coroutine starCoroutine;

    private void Awake()
    {
        showStarBtn.onClick.AddListener(() => ShowStar(true));
        rainbowPortalBtn.onClick.AddListener(() => ShowRainbow(true));

        hideStarBtn.onClick.AddListener(() => ShowStar(false));
        hideRainbowBtn.onClick.AddListener(() => ShowRainbow(false));
    }

    private void Start()
    {
        portalUI.SetActive(false);

        foreach (GameObject p in portal)
        {
            if (p == null)
                continue;

            p.SetActive(false);
            p.transform.localScale = Vector3.zero;
        }
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

    private void OnRightAPress(InputAction.CallbackContext context)
    {
        if (SpatialAnchor.Instance.MainUI.activeSelf)
            return;

        if (!SpatialAnchor.Instance.IsCreateAnchor)
            portalUI.SetActive(true);
    }

    private void OnRightARelese(InputAction.CallbackContext context)
    {
        if (SpatialAnchor.Instance.MainUI.activeSelf)
            return;

        if (!SpatialAnchor.Instance.IsCreateAnchor)
            portalUI.SetActive(false);
    }

    private void ShowRainbow(bool show)
    {
        if (portal == null || portal.Length < 1 || portal[0] == null)
            return;

        if (show)
        {
            SetupPortal(portal[0]);

            if (portal.Length > 1 && portal[1] != null)
                HidePortal(portal[1]);

            if (rainbowCoroutine != null)
                StopCoroutine(rainbowCoroutine);

            rainbowCoroutine = StartCoroutine(ScalePortal(portal[0], true));
        }
        else
        {
            if (rainbowCoroutine != null)
                StopCoroutine(rainbowCoroutine);

            rainbowCoroutine = StartCoroutine(ScalePortal(portal[0], false));
        }
    }

    private void ShowStar(bool show)
    {
        if (portal == null || portal.Length < 2 || portal[1] == null)
            return;

        if (show)
        {
            SetupPortal(portal[1]);

            if (portal[0] != null)
                HidePortal(portal[0]);

            if (starCoroutine != null)
                StopCoroutine(starCoroutine);

            starCoroutine = StartCoroutine(ScalePortal(portal[1], true));
        }
        else
        {
            if (starCoroutine != null)
                StopCoroutine(starCoroutine);

            starCoroutine = StartCoroutine(ScalePortal(portal[1], false));
        }
    }

    private void SetupPortal(GameObject targetPortal)
    {
        targetPortal.transform.position =
            transformObject.transform.position;

        Transform cam = Camera.main.transform;

        Vector3 forward = cam.forward;
        forward.y = 0;

        if (forward.sqrMagnitude > 0.001f)
        {
            targetPortal.transform.rotation =
                Quaternion.LookRotation(forward);
        }

        targetPortal.SetActive(true);
    }

    private void HidePortal(GameObject targetPortal)
    {
        if (targetPortal == null)
            return;

        targetPortal.SetActive(false);
        targetPortal.transform.localScale = Vector3.zero;
    }

    private IEnumerator ScalePortal(GameObject targetPortal, bool show)
    {
        if (targetPortal == null)
            yield break;

        Vector3 startScale =
            targetPortal.transform.localScale;

        Vector3 targetScale =
            show ? Vector3.one : Vector3.zero;

        if (show)
            targetPortal.SetActive(true);

        float timer = 0f;

        while (timer < portalAnimTime)
        {
            timer += Time.deltaTime;

            float t = timer / portalAnimTime;

            t = Mathf.SmoothStep(0f, 1f, t);

            targetPortal.transform.localScale =
                Vector3.Lerp(startScale, targetScale, t);

            yield return null;
        }

        targetPortal.transform.localScale = targetScale;

        if (!show)
            targetPortal.SetActive(false);
    }
}