using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShapeInfo//保存对象信息
{
    public class SceneBox3DData
    {
        public Vector3 Center { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Extent { get; set; }
    }
    public class SceneBox2DData
    {
        public Vector3 Center { get; set; }
        public Vector3 Extent { get; set; }
    }
    public class ScenePolygonData
    {
        public IList<Vector3> Vertices { get; set; }//保存多边形信息
    }
}
