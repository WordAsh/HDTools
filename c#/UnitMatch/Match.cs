using CurtainWallMatch;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Rhino.DocObjects.PhysicallyBasedMaterial;

namespace UnitMatch
{
    public class Match
    {
        //确定w方向，h方向缩放比例
        public static void GetScaleFactor(Unit unit, Component comp, out double wFactor, out double hFacotr)
        {
            comp.GetSize();
            var w0 = unit.W;
            var h0 = unit.H;
            var w1 = comp.W;
            var h1 = comp.H;
            wFactor = w0 / w1;
            hFacotr = h0 / h1;
        }
        //根据w，h值中心缩放单元
        public static List<GeometryBase> ScaleComponent(Component comp, Point3d center, double wFactor, double hFactor)
        {
            var plane = new Plane(center, Plane.WorldXY.XAxis,Plane.WorldXY.YAxis);
            var trans = Transform.Scale(plane, wFactor, hFactor, 1);
            var geos = new List<GeometryBase>();
            foreach (var geo in comp.Geometry)
            {
                geo.Transform(trans);
                geos.Add(geo);
            }
            return geos;
        }

        //将geometries从基准面映射至目标平面
        public static List<GeometryBase> MapGeometriesToPlane(List<GeometryBase> geos, Plane planeFrom, Plane planeTo)
        {
            var trans = Transform.PlaneToPlane(planeFrom, planeTo);
            var newGeos = new List<GeometryBase>();
            foreach (var geo in geos)
            {
                geo.Transform(trans);
                newGeos.Add(geo);
            }
            return newGeos;
        }
        //将点从基准面映射至目标平面
        public static Point3d MapPointToPlane(Point3d pt, Plane planeFrom, Plane planeTo)
        {
            var newPt=new Point3d(pt.X,pt.Y,pt.Z);
            var trans = Transform.PlaneToPlane(planeFrom, planeTo);
            newPt.Transform(trans);
            return newPt;
        }

        //将geometries从基准点移动到目标点
        public static List<GeometryBase> MapToPoint(List<GeometryBase> geos, Point3d ptFrom, Point3d ptTo)
        {
            var vec = new Vector3d(ptTo - ptFrom);
            var trans = Transform.Translation(vec);
            var newGeos = new List<GeometryBase>();
            foreach (var geo in geos)
            {
                geo.Transform(trans);
                newGeos.Add(geo);
            }
            return newGeos;
        }

        //将component匹配至一个unit中
        public static List<GeometryBase> MatchToOneUnit(Component comp,Unit unit)
        {
            //第一步对Component进行缩放
            double wFactor;
            double hFactor;

            GetScaleFactor(unit,comp,out wFactor,out hFactor);
            var centerPt=comp.GetCenterPt();
            var newComp = comp.MapToOrigin();//将component映射至原点
            var scaled=ScaleComponent(newComp,centerPt,wFactor,hFactor);

            //第二步将缩放后的component映射至unit平面
            var planeMapped = MapGeometriesToPlane(scaled, Plane.WorldXY, unit.Plane);
            var centerPtMapped = MapPointToPlane(centerPt, Plane.WorldXY, unit.Plane);

            //第三步将平面映射后的geometries移动至unit中心
            var ptTarget = unit.GetCenterPt();
            var ptMapped = MapToPoint(planeMapped, centerPtMapped, ptTarget);

            return ptMapped;
        }
        //将component匹配至所有unit单元中
        public static List<GeometryBase> MatchToAllUnits(Component comp, List<Unit> units)
        {
            var geoList=new List<GeometryBase>();
            for (var i = 0; i < units.Count; i++)
            {
                var geos = MatchToOneUnit(Component.DeepcopyComponent(comp),units[i]);
                geoList.AddRange(geos);
            }
            return geoList;
        }
    }
    

}
