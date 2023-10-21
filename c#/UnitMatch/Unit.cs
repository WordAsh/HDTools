using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CurtainWallMatch
{
    public class Unit
    {
        public Rhino.Geometry.Plane Plane { get; set; }
        public List<Curve> Frame { get; set; }
        public double W { get; set; }
        public double H { get; set; }
        public Unit(Rhino.Geometry.Plane plane,List<Curve> frame)
        {
            Plane = plane;
            Frame = frame;
        }
        //得到物体中心点
        public  Point3d GetCenterPt()
        {
            var border= Curve.JoinCurves(Frame)[0];
            var bbox = border.GetBoundingBox(Rhino.Geometry.Plane.WorldXY);
            var centerPt = bbox.Center;
            return centerPt;
        }
        //得到unit的尺寸
        public void SetUnitSize()
        {
            foreach (Curve curve in Frame)
            {
                Vector3d vec = curve.TangentAtStart;
                var result = Vector3d.CrossProduct(vec,new Vector3d(0,0,1));
                if (result.Length < 0.01)
                {
                    H = curve.GetLength();
                }
                else {
                    W = curve.GetLength();
                }
            }
        }
    }
}
