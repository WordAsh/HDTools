using Rhino.DocObjects;
using Rhino;
using Rhino.Geometry;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Input;
using Rhino.UI;


namespace UnitMatch
{
    //存储数据静态类
    public static class Manager
    {
        public static List<GeometryBase> Geos { get; set; }
        public static Surface Surface { get; set; }
        public static Guid srfId { get; set; }
        public static Plane basePlane { get; set; }
        public static int Ucount { get; set; }
        public static int Vcount { get; set; }
        public static bool DeleteInputSurface { get; set; } = true;
        public static bool AddUnitFrame { get; set; } = false;
        public static bool IsValid
        {
            get
            {
                if (Ucount <= 0
                || Vcount <= 0
                || Ucount % 1 != 0
                || Vcount % 1 != 0
                ||Geos == null
                ||Surface == null
                ||basePlane == null
                )
                    return false;
                return true;
            }
        }
        //选择单元构件
        public static List<GeometryBase> SelectComponent()
        {
            //选择Component包含的物体
            var go = new GetObject();
            go.SetCommandPrompt("请选择想要进行匹配的单元构件");
            go.GetMultiple(1, 0);
            List<GeometryBase> geometries = new List<GeometryBase>();

            foreach (var objRef in go.Objects())
            {
                GeometryBase newGeo = objRef.Geometry();
                geometries.Add(newGeo);
            }
            var geos = geometries;
            return geos;
        }

        //选择要投影得竖直曲面
        public static void SelectVerticalSurface(out Surface surface, out Guid srfId)
        {
            surface = null;
            srfId = Guid.Empty;
            var go = new GetObject();

            go.SetCommandPrompt("请选择要进行匹配的竖直曲面 ");
            go.GeometryFilter = ObjectType.Surface;
            go.GetMultiple(1, 1);
            var srf = go.Object(0).Surface();
            srfId = go.Object(0).ObjectId;
            //检查选择曲面是否是竖直面
            var vec = srf.NormalAt(0, 0);
            if (Vector3d.Multiply(vec, new Vector3d(0, 0, 1)) > 0.01)
            {
                RhinoApp.WriteLine("选择的曲面非竖直面！");
                return;
            }
            surface = srf;
        }

        //构造单元构件平面
        public static void ConstructComponentPlane(out Plane basePlane)
        {
            basePlane = Plane.Unset;
            Point3d basePt;
            using (GetPoint getPointAction = new GetPoint())
            {
                getPointAction.SetCommandPrompt("请确定单元构件的基准角点");
                if (getPointAction.Get() != GetResult.Point)
                {
                    RhinoApp.WriteLine("No end point was selected.");
                    return;
                }
                basePt = getPointAction.Point();
            }
            Point3d pt0;
            using (GetPoint getPointAction = new GetPoint())
            {
                getPointAction.SetCommandPrompt("请再次点击基准点，确定构件宽度方向起始点");
                if (getPointAction.Get() != GetResult.Point)
                {
                    RhinoApp.WriteLine("No start point was selected.");
                    return;
                }
                pt0 = getPointAction.Point();
            }
            Point3d pt1;
            using (GetPoint getPointAction = new GetPoint())
            {
                getPointAction.SetCommandPrompt("沿构件宽度方向，确定第二个点");
                getPointAction.SetBasePoint(pt0, true);
                getPointAction.DynamicDraw +=
                  (sender, e) => e.Display.DrawLine(pt0, e.CurrentPoint, System.Drawing.Color.DarkRed);
                if (getPointAction.Get() != GetResult.Point)
                {
                    RhinoApp.WriteLine("No end point was selected.");
                    return;
                }
                pt1 = getPointAction.Point();
            }
            Point3d pt2;
            using (GetPoint getPointAction = new GetPoint())
            {
                getPointAction.SetCommandPrompt("请再次点击基准点，确定构件高度方向起始点");
                if (getPointAction.Get() != GetResult.Point)
                {
                    RhinoApp.WriteLine("No start point was selected.");
                    return;
                }
                pt2 = getPointAction.Point();
            }
            Point3d pt3;
            using (GetPoint getPointAction = new GetPoint())
            {
                getPointAction.SetCommandPrompt("沿构件高度方向，确定第二个点");
                getPointAction.SetBasePoint(pt0, true);
                getPointAction.DynamicDraw +=
                  (sender, e) => e.Display.DrawLine(pt0, e.CurrentPoint, System.Drawing.Color.DarkRed);
                if (getPointAction.Get() != GetResult.Point)
                {
                    RhinoApp.WriteLine("No end point was selected.");
                    return;
                }
                pt3 = getPointAction.Point();
            }
            basePlane = new Plane(basePt, pt1 - pt0, pt3 - pt2);
        }


    }

    //实例化对象
    public class Manager2
    {
        public  List<GeometryBase> Geos { get; set; }=new List<GeometryBase>();
        public  Surface Surface { get; set; }=null;
        public Guid srfId { get; set; } = Guid.Empty;
        public Plane basePlane { get; set; } = Plane.Unset;
        public int Ucount { get; set; } 
        public  int Vcount { get; set; }
        public  bool DeleteInputSurface { get; set; } = true;
        public  bool AddUnitFrame { get; set; } = false;
        //public  bool AddStatisticForm { get; set; } = true;
        public List<Guid> Guids { get; set; }= new List<Guid>();
        public bool IsValid
        {
            get
            {
                if (Ucount <= 0
                || Vcount <= 0
                || Ucount % 1 != 0
                || Vcount % 1 != 0
                || Geos == null
                || Surface == null
                || basePlane == null
                )
                    return false;
                return true;
            }
        }
        //选择单元构件
        public  List<GeometryBase> SelectComponent()
        {
            //选择Component包含的物体
            var go = new GetObject();
            go.SetCommandPrompt("请选择想要进行匹配的单元构件");
            go.GetMultiple(1, 0);
            List<GeometryBase> geometries = new List<GeometryBase>();

            foreach (var objRef in go.Objects())
            {
                GeometryBase newGeo = objRef.Geometry();
                geometries.Add(newGeo);
            }
            var geos = geometries;
            return geos;
        }

        //选择要投影得竖直曲面
        public void SelectVerticalSurface(out Surface surface, out Guid srfId)
        {
            surface = null;
            srfId = Guid.Empty;
            var go = new GetObject();

            go.SetCommandPrompt("请选择要进行匹配的竖直曲面 ");
            go.GeometryFilter = ObjectType.Surface;
            go.GetMultiple(1, 1);
            var srf = go.Object(0).Surface();
            srfId = go.Object(0).ObjectId;
            //检查选择曲面是否是竖直面
            var vec = srf.NormalAt(0, 0);
            if (Vector3d.Multiply(vec, new Vector3d(0, 0, 1)) > 0.01)
            {
                RhinoApp.WriteLine("选择的曲面非竖直面！");
                return;
            }
            surface = srf;
        }

        //构造单元构件平面
        public  void ConstructComponentPlane(out Plane basePlane)
        {
            basePlane = Plane.Unset;
            Point3d basePt;
            using (GetPoint getPointAction = new GetPoint())
            {
                getPointAction.SetCommandPrompt("请确定单元构件的基准角点");
                if (getPointAction.Get() != GetResult.Point)
                {
                    RhinoApp.WriteLine("未选择基准点");
                    return;
                }
                basePt = getPointAction.Point();
            }

            Point3d pt0;
            using (GetPoint getPointAction = new GetPoint())
            {
                getPointAction.SetCommandPrompt("请再次点击基准点，确定构件宽度方向起始点");
                if (getPointAction.Get() != GetResult.Point)
                {
                    RhinoApp.WriteLine("未选择起始点");
                    return;
                }
                pt0 = getPointAction.Point();
            }
            Point3d pt1;
            using (GetPoint getPointAction = new GetPoint())
            {
                getPointAction.SetCommandPrompt("沿构件宽度方向，确定第二个点");
                getPointAction.SetBasePoint(pt0, true);
                getPointAction.DynamicDraw +=
                  (sender, e) => e.Display.DrawLine(pt0, e.CurrentPoint, System.Drawing.Color.DarkRed);
                if (getPointAction.Get() != GetResult.Point)
                {
                    RhinoApp.WriteLine("未选择定位点");
                    return;
                }
                pt1 = getPointAction.Point();
            }
            Point3d pt2;
            using (GetPoint getPointAction = new GetPoint())
            {
                getPointAction.SetCommandPrompt("请再次点击基准点，确定构件高度方向起始点");
                if (getPointAction.Get() != GetResult.Point)
                {
                    RhinoApp.WriteLine("未选择起始点");
                    return;
                }
                pt2 = getPointAction.Point();
            }
            Point3d pt3;
            using (GetPoint getPointAction = new GetPoint())
            {
                getPointAction.SetCommandPrompt("沿构件高度方向，确定第二个点");
                getPointAction.SetBasePoint(pt0, true);
                getPointAction.DynamicDraw +=
                  (sender, e) => e.Display.DrawLine(pt0, e.CurrentPoint, System.Drawing.Color.DarkRed);
                if (getPointAction.Get() != GetResult.Point)
                {
                    RhinoApp.WriteLine("未选择定位点");
                    return;
                }
                pt3 = getPointAction.Point();
            }
            basePlane = new Plane(basePt, pt1 - pt0, pt3 - pt2);
        }

    }
}
