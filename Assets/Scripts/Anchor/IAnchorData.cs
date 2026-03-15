using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.PXR;
using UnityEngine;
using static ShapeInfo;

public interface IAnchorData //保存锚点数据的接口
{
    ulong Handle { get; }
    Guid Uuid { get; }
    Vector3 Position { get; }
    Quaternion Rotation { get; }
    PxrSemanticLabel SceneLabel { get; }//锚点的类型语义
    IList<PxrSceneComponentType>ComponentTypeFlagsList { get; }
    SceneBox3DData SceneBox3DData {  get; }
    SceneBox2DData SceneBox2DData { get; }
    ScenePolygonData ScenePolygonData { get; }
}
