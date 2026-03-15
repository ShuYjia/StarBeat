using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Unity.XR.PXR;
using UnityEngine;

public class SpaceCalibration : MonoBehaviour
{
    private IList<IAnchorData> _roomAnchors = new List<IAnchorData>();
    public GameObject AnchorModel;
    private async void Start()
    {
        await InitAsync();
    }
    //开启场景标注工具
    private async UniTask StartSceneCaptureProvider()
    {
        var result0 = await PXR_MixedReality.StartSenseDataProvider(PxrSenseDataProviderType.SceneCapture);
        Debug.Log($"StartSceneCaptureProvider:SceneCapture: {result0}");
    }
    //开启锚点功能
    private async UniTask StartSpatialAnchorProvider()
    {
        var result0 = await PXR_MixedReality.StartSenseDataProvider(PxrSenseDataProviderType.SpatialAnchor);
        Debug.Log($"StartSenseDataProvider: {result0}");
    }

    //异步初始化
    private async UniTask InitAsync()
    {

        await StartSceneCaptureProvider();
        await StartSpatialAnchorProvider();
        Debug.Log($"Init Provider Success");
        var result = await PXR_MixedReality.StartSceneCaptureAsync();
        Debug.Log($"StartSceneCaptureAsync: {result}");
        Debug.Log("StartSceneCaptureAsync Success");

        await LoadRoomModel();
        Debug.Log("LoadRoomEntities Success");

    }


    //加载房间锚点
    public async UniTask<IList<IAnchorData>> LoadRoomAnchors()
    {
        _roomAnchors.Clear();
        var result = await PXR_MixedReality.QuerySceneAnchorAsync(default);
        Debug.Log($"LoadSceneDataAsync: {result.anchorDictionary.Count} ,{result.result}");
        if (result.result == PxrResult.SUCCESS)
        {
            if (result.anchorDictionary.Count > 0)
            {
                foreach (var item in result.anchorDictionary)
                {
                    IAnchorData anchorData = new AnchorData(item.Key, item.Value);
                    _roomAnchors.Add(anchorData);
                }
            }
        }
        return _roomAnchors;
    }
    //加载房间模型
    public async UniTask LoadRoomModel()
    {
        var anchors = await LoadRoomAnchors(); //加载房间锚点数据
        Debug.Log($"Load Room Anchors Finished, total anchors: {anchors.Count}");


        foreach (var anchor in anchors)
        {
            Debug.Log($"Load Room Anchor Key: {anchor.Handle}, Guid: {anchor.Uuid} SceneLabel: {anchor.SceneLabel}");
            var anchorOBJ = Instantiate(AnchorModel, anchor.Position, anchor.Rotation); //实例化锚点
        }
    }


}
