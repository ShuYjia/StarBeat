using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.PXR;
using System;
using System.Threading.Tasks;

public class SpatialAnchor : MonoBehaviour
{
    private static SpatialAnchor instance;
    public static SpatialAnchor Instance
    {

        get
        {
            if (null == instance)
                instance = FindObjectOfType(typeof(SpatialAnchor)) as SpatialAnchor;
            return instance;
        }
    }
    public GameObject[] anchorPreviewArr;
    public InputActionReference RightPushLeft;
    public InputActionReference RightPushRight;
    private int currentModelIndex = 0;
    public int CurrentModelIndex
    {
        get => currentModelIndex;set
        {
            currentModelIndex = value;
            SetAnchorPreviewArrShow(currentModelIndex);
        }
    }

    private void SetAnchorPreviewArrShow(int index)
    {
        for (int i = 0; i < anchorPreviewArr.Length; i++)
            anchorPreviewArr[i].gameObject.SetActive(i==index);
    }

    private bool IsCreateAnchor = false;
    public Button CreateAnchorBtn;
    public Button LoadAnchorBtn;
    public Button ClearAnchorBtn;
    public GameObject MainUI;

    //按键
    public InputActionReference rightGrab;
    public InputActionReference leftGrab;
    public InputActionReference rprimaryButton_A;
    public GameObject anchorPreview;
    public AnchorCTR anchorPrefab;
    //
    public XRRayInteractor interactor;//手臂涉嫌
    public XRBaseInteractable hoverInteractable;//手柄射线对准的GB
    public AnchorCTR SelectAnchor;//选中操作的锚点
    public Dictionary<ulong, AnchorCTR> AnchorUIEventsList = new Dictionary<ulong, AnchorCTR>();
    //时间校正
    public float maxDirftdelay = 0.5f;//多久更新自己的坐标
    public float currentDriftDelay = 0.0f;
    private void Awake()
    {
        MainUI.gameObject.SetActive(false);
        anchorPreview.gameObject.SetActive(false);
        CreateAnchorBtn.onClick.AddListener(OnClickCreateAnchorBtn);
        LoadAnchorBtn.onClick.AddListener(OnClickLoadAnchorBtn);
        ClearAnchorBtn.onClick.AddListener(OnClickCLearAnchorBtn);
    }

    private async void OnClickCLearAnchorBtn()
    {
        foreach (var anchor in AnchorUIEventsList)
        {
            var result = await PXR_MixedReality.UnPersistSpatialAnchorAsync(anchor.Key);//序列化移除
            if (result == PxrResult.SUCCESS)
            {
                if (PXR_MixedReality.DestroyAnchor(anchor.Key) == PxrResult.SUCCESS)//场景中移除
                    Destroy(anchor.Value.gameObject);
            }
        }
        AnchorUIEventsList.Clear();
        PlayerPrefs.DeleteAll();
    }

    private void OnClickLoadAnchorBtn()
    {
        PressedLoadAllAnchors();
    }

    private async void PressedLoadAllAnchors()
    {
        var result = await PXR_MixedReality.QuerySpatialAnchorAsync();//查询所有空间锚点,此处是拿到所有的handle
        if (result.result == PxrResult.SUCCESS)//查询成功
        {
            foreach (var key in result.anchorHandleList)
            {
                //锚点是否存在，不在再加
                if (!AnchorUIEventsList.ContainsKey(key))
                    if (PXR_MixedReality.GetAnchorUuid(key, out Guid uuid) == PxrResult.SUCCESS)
                    {
                        //以下似乎有问题，可能应该直接实例化anchorUIEvent
                        AnchorCTR anchorObject = Instantiate(anchorPrefab);

                        //   AnchorUIEvent anchorObject =Obj.GetComponent<AnchorUIEvent>();
                        int SaveModelIndex =PlayerPrefs.GetInt(uuid.ToString(), 0);//?

                        anchorObject.OnInit(key, uuid,SaveModelIndex);
                        anchorObject.ShowMainUI(false);
                        anchorObject.ShowSavedUI(true);
                        PXR_MixedReality.LocateAnchor(key, out var position, out var orientation);
                        anchorObject.transform.position = position;
                        anchorObject.transform.rotation = orientation;
                        AnchorUIEventsList.Add(key, anchorObject);
                    }
            }
        }
    }

    private void OnClickCreateAnchorBtn()
    {
        //debug

        Debug.Log("Create Anchor Button Clicked!");
        if (!IsCreateAnchor)
        {
            CreateAnchorBtn.GetComponentInChildren<Text>().text = "取消创建";
            anchorPreview.gameObject.SetActive(true);
        }
        else
        {
            CreateAnchorBtn.GetComponentInChildren<Text>().text = "创建锚点";
            anchorPreview.gameObject.SetActive(false);
        }
        IsCreateAnchor = !IsCreateAnchor;
    }


    // Start is called before the first frame update
    void Start()
    {
        StartSpatialAnchorProvider();
    }

    private async void StartSpatialAnchorProvider()
    {
        var result = await PXR_MixedReality.StartSenseDataProvider(PxrSenseDataProviderType.SpatialAnchor);//启动锚点数据感知数据提供者
        if (result == PxrResult.SUCCESS)
            PressedLoadAllAnchors();
        Debug.Log("StartSenseDataProvider" + result.ToString());
    }

    private void OnEnable()
    {
        rightGrab.action.started += OnRightGrabPressed;
        rightGrab.action.canceled += OnRightGrabReleased;
        //A
        rprimaryButton_A.action.started += OnRprimaryButton_APressed;
        rprimaryButton_A.action.canceled += OnRprimaryButton_ACanceled;
        //射线进入与退出物体
        interactor.hoverEntered.AddListener(HoverEntered);
        interactor.hoverExited.AddListener(hoverExit);
        //锚点更新了注册事件
        PXR_Manager.SpatialAnchorDataUpdated += SpatialAnchorDataUpdated;
        //右手左划右划
        RightPushLeft.action.started += OnRightPushLeft;
        RightPushRight.action.started += OnRightPushRight;

    }

    private void OnRightPushLeft(InputAction.CallbackContext context)
    {
        CurrentModelIndex--;
        if (CurrentModelIndex < 0)
            CurrentModelIndex = anchorPreviewArr.Length - 1;
    }

    private void OnRightPushRight(InputAction.CallbackContext context)
    {
        CurrentModelIndex++;
        if(CurrentModelIndex >=anchorPreviewArr.Length) 
            CurrentModelIndex = 0;
    }

    private void OnRprimaryButton_ACanceled(InputAction.CallbackContext context)
    {
        //关闭菜单
        if (SelectAnchor != null)
        {
            SelectAnchor.ShowMainUI(false);
            SelectAnchor = null;
        }
    }

    private void hoverExit(HoverExitEventArgs arg0)
    {
        hoverInteractable = null;
    }

    private void HoverEntered(HoverEnterEventArgs arg0)
    {
        hoverInteractable = (XRBaseInteractable)arg0.interactableObject;

    }
    private void OnRprimaryButton_APressed(InputAction.CallbackContext context)
    {
        //debugText
       // CreateAnchorBtn.GetComponentInChildren<Text>().text = "A键已按下";
        //A要判断锚点预览物体是否打开，是否创建锚点
        if (IsCreateAnchor && anchorPreview.gameObject.activeSelf)
        {
            CraeteAnchor();
        }
        else
            // 调试2：看看是不是条件没满足
          //  CreateAnchorBtn.GetComponentInChildren<Text>().text = "条件未满足";
        if (hoverInteractable != null)
        {
            SelectAnchor = hoverInteractable.GetComponent<AnchorCTR>();
            if (SelectAnchor != null)
                SelectAnchor.ShowMainUI(true);
        }
    }

    private void CraeteAnchor()
    {
        CreateSpatialAnchor(anchorPreview.transform);
    }

    private async void CreateSpatialAnchor(Transform transform)
    {
       // CreateAnchorBtn.GetComponentInChildren<Text>().text = "SDK调用中...";
        var result = await PXR_MixedReality.CreateSpatialAnchorAsync(transform.position, transform.rotation);
        if (result.result == PxrResult.SUCCESS)
        {
  //          CreateAnchorBtn.GetComponentInChildren<Text>().text = "创建成功!";

            // 实例化预制体 (确保不作为手柄的子物体)
            AnchorCTR anchorObj = Instantiate(anchorPrefab, transform.position, transform.rotation, null);


            anchorObj.OnInit(result.anchorHandle, result.uuid,CurrentModelIndex);

            AnchorUIEventsList.Add(result.anchorHandle, anchorObj);
        }
    }
    private void FixedUpdate()
    {
        HandleSpatialDrift();
    }

    private void HandleSpatialDrift()
    {
        if (AnchorUIEventsList.Count == 0) return;
        currentDriftDelay += Time.deltaTime;
        if (currentDriftDelay >= maxDirftdelay)
        {
            currentDriftDelay = 0;
            foreach (var drift in AnchorUIEventsList)
            {
                var handle = drift.Key;
                var anchorObj = drift.Value;
                if (handle == UInt64.MinValue)
                {
                    Debug.LogError("handle不对");
                    continue;
                }
                var result = PXR_MixedReality.LocateAnchor(handle, out var position, out var rotation);
                if (result == PxrResult.SUCCESS)
                {
                    anchorObj.transform.position = position;
                    anchorObj.transform.rotation = rotation;
                }
            }
        }


    }

    private void OnRightGrabReleased(InputAction.CallbackContext context)
    {
        MainUI.gameObject.SetActive(false);
    }

    private void OnRightGrabPressed(InputAction.CallbackContext context)
    {
        MainUI.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        rightGrab.action.started -= OnRightGrabPressed;
        rightGrab.action.canceled -= OnRightGrabReleased;

        // 移除 A 键事件
        rprimaryButton_A.action.started -= OnRprimaryButton_APressed;
        rprimaryButton_A.action.canceled -= OnRprimaryButton_ACanceled;

        // 移除射线悬停事件
        interactor.hoverEntered.RemoveListener(HoverEntered);
        interactor.hoverExited.RemoveListener(hoverExit);

        // 移除锚点数据更新事件
        PXR_Manager.SpatialAnchorDataUpdated -= SpatialAnchorDataUpdated;

        RightPushLeft.action.started -= OnRightPushLeft;
        RightPushRight.action.started -= OnRightPushRight;
    }

    public void DestoryAnchor(ulong anchorHandle)
    {
        if (AnchorUIEventsList.ContainsKey(anchorHandle))
        {
            Destroy(AnchorUIEventsList[anchorHandle].gameObject);
            AnchorUIEventsList.Remove(anchorHandle);
        }
    }

    private void SpatialAnchorDataUpdated()
    {
        PressedLoadAllAnchors();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
