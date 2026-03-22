using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//传送门显示的类型
public enum PortealDisplayType
{
    VR, //VR虚拟世界
    VST //彩色透视真实世界
}

public class PortalCTR : MonoBehaviour
{
    public bool isVR=false;
    [Tooltip("处理彩虹显示bug")]
    public GameObject rainBow;
    public bool tdebug=true;
    public GameObject[] portealDisplay;//传送门显示画面 0 是传送门内容 1是现实透视内容
    public GameObject child; //传送门子物体
    public GameObject scenesModel; //传送门关联场景

    private List<Transform> scenesModelChild = new List<Transform>(); //关联场景中所有子物体
    private Vector3 EnterDirection;// 玩家进入时的方向
    private Vector3 ExitDirection;// 玩家退出时的方向

    public PortealDisplayType nextShow; //用于记录切换虚拟场景还是现实场景
    public int scenesModellayer;  //关联场景要设置的蒙版测试的layer

    private void Awake()
    {
        Init();

    }
    public void changeRainbowShow(bool v)
    {
        if (rainBow != null)
        {
            rainBow.SetActive(v);
        }
    }
    void Init()
    {
        Transform[] transform = scenesModel.GetComponentsInChildren<Transform>();
        scenesModelChild = new List<Transform>(transform);

        ChangeScenesModelLayer(nextShow != PortealDisplayType.VR); //初始化设置layer

    }
    //玩家进入传送门
    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.tag == "MainCamera")
    //    {
    //        Vector3 selfPoint = this.transform.position;
    //        Vector3 otherPoint = other.transform.position;
    //        otherPoint.y = this.transform.position.y;
    //        EnterDirection = Vector3.ProjectOnPlane(otherPoint - selfPoint, this.transform.right);

    //        Debug.Log("EnterDirection " + EnterDirection);
    //    }
    //}
    //玩家离开传送门 ，进去方向判断是进入了还是退回了
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "MainCamera")
        {
            Debug.Log("234asasdfg");
            Vector3 selfPoint = this.transform.position;
            Vector3 otherPoint = other.transform.position;
            otherPoint.y = this.transform.position.y;
            ExitDirection = Vector3.ProjectOnPlane(otherPoint - selfPoint, this.transform.right);
            float dotRes = Vector3.Dot(EnterDirection, ExitDirection); //根据点乘判断方向 
            Debug.Log(dotRes+"sad");
       //     if (dotRes < 0)// 穿越
       //     {
                Debug.Log("asasdfg");
                bool isEnterVR = nextShow == PortealDisplayType.VR; //判断要进入的场景是否是VR 或者从VR回到真实世界
                SetPlayShowPortealDisplay(isEnterVR ? 1 : 0);
                ChangeScenesModelLayer(isEnterVR);
                nextShow = isEnterVR ? PortealDisplayType.VST : PortealDisplayType.VR; //切换下一个要进入时的传送门类型
            if (isVR)
            {
                isVR = false;
                Debug.Log("fgd");
            }
            else
            {
                isVR = true;
                Debug.Log("12d");
            }
            changeRainbowShow(isVR);
            //     }
            //   else//返回
            //  {
            Debug.Log("123asasdasdfg");
        //   }

        }
    }

    //变换关联场景的layer
    public void ChangeScenesModelLayer(bool isDefault = true)
    {

        if (isDefault)
        {
            foreach (var item in scenesModelChild)
            {
                item.gameObject.layer = 0;
            }
            //changeRainbowShow(false);
            //Debug.Log("asd");
        }
        else
        {
            foreach (var item in scenesModelChild)
            {
                item.gameObject.layer = scenesModellayer;
            }
            //changeRainbowShow(true);
            //Debug.Log("jkl");
        }

    }
    //设置传送门要显示 VR还是 vst
    private void SetPlayShowPortealDisplay(int index)
    {
        for (int i = 0; i < portealDisplay.Length; i++)
        {
            portealDisplay[i].gameObject.SetActive(index == i);
        }
    }


}
