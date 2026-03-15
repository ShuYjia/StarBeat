using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.PXR;
using UnityEngine;
using UnityEngine.UI;

public class AnchorCTR : MonoBehaviour
{
    //模型
    public GameObject[] modelArr;
    public int currentModelIndex=0;

    public Text uuidText;
    public GameObject MainUI;
    public Text Save;

    public Button PersistedBtn;
    public Button DestoryBtn;
    public Button UnPersistedBtn;
    private ulong anchorHandle;
    private Guid uuid;
    private void Awake()
    {
        PersistedBtn.onClick.AddListener(OnClickPersistedBtn);
        DestoryBtn.onClick.AddListener(OnClickDestoryBtn);
        UnPersistedBtn.onClick.AddListener(OnClickUnPersistedBtn);
        ShowMainUI(false);
    }
    public void OnInit(ulong anchorHandle, Guid uuid,int currentModelIndex=0)
    {
        this.anchorHandle = anchorHandle;
        this.uuid = uuid;
        uuidText.text = this.anchorHandle.ToString();
        this.currentModelIndex = currentModelIndex;
        SetModelArrShow(currentModelIndex);
    }
    private void SetModelArrShow(int index=0)
    {
        for (int i = 0; i < modelArr.Length; i++)
        {
            modelArr[i].SetActive(i==index);
        }
    }
    public void ShowMainUI(bool v)
    {
        MainUI.SetActive(v);
    }

    public void ShowSavedUI(bool v)
    {
        //(a > b) ? a : b; 
        Save.text = v ? "已保存" : "未保存";
    }

    private async void OnClickUnPersistedBtn()
    {
        //async Task<PxrResult> UnPersistSpatialAnchorAsync(ulong anchorHandle)
        var result = await PXR_MixedReality.UnPersistSpatialAnchorAsync(anchorHandle);//持久化删除
        if (result == PxrResult.SUCCESS)
        {
            PlayerPrefs.DeleteKey(uuid.ToString());
            //场景删
            PXR_MixedReality.DestroyAnchor(anchorHandle);
            SpatialAnchor.Instance.DestoryAnchor(anchorHandle);

        }
    }

    private void OnClickDestoryBtn()
    {
        var result = PXR_MixedReality.DestroyAnchor(anchorHandle);
        if (result == PxrResult.SUCCESS)
            SpatialAnchor.Instance.DestoryAnchor(anchorHandle);
    }

    private async void OnClickPersistedBtn()
    {
        //async Task<PxrResult> PersistSpatialAnchorAsync(ulong anchorHandle)
        var result = await PXR_MixedReality.PersistSpatialAnchorAsync(anchorHandle);
        if (result == PxrResult.SUCCESS)
        {
            ShowSavedUI(true);
            PlayerPrefs.SetInt(uuid.ToString(), currentModelIndex);
        }

    }



    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}


