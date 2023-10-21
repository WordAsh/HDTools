using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Rhino.Render.TextureGraphInfo;

namespace UnitMatch
{
    public class Component
    {
        public List<GeometryBase> Geometry { get; set; }
        public double W { get; set; }
        public double H { get; set; }
        public Plane Plane { get; set; }
        public Component(List<GeometryBase> geos, Plane plane)
        {

            this.Geometry = geos;
            this.Plane = plane;
        }
        //深复制component
        public static Component DeepcopyComponent(Component comp)
        {
            var plane = comp.Plane;
            List<GeometryBase> newGeos = new List<GeometryBase>();
            foreach (var geom in comp.Geometry)
            {
                var newGeom = geom.Duplicate();
                newGeos.Add(newGeom);
            }
            return new Component(newGeos, plane);


        }
        //在WorldXY平面得到构件的基准面
        public static void GetBaseSurface(Component comp, out Surface baseSrf)
        {
            baseSrf = null;
            var brep = comp.GetCompBoundingBox().ToBrep();
            foreach (var face in brep.Faces)
            {
                Plane p;
                var result = face.TryGetPlane(out p);
                if (PlaneTest.IsCoplanar(p, Plane.WorldXY))
                {
                    baseSrf = face;
                }
            }
        }
        //在原点得到Component的包围盒
        public BoundingBox GetCompBoundingBox()
        {
            var geos = this.Geometry;
            var bbox = geos[0].GetBoundingBox(this.Plane);
            if (geos.Count > 1)
            {
                for (int i = 0; i < geos.Count; i++)
                {
                    var bboxi = geos[i].GetBoundingBox(this.Plane);
                    var bboxNew = BoundingBox.Union(bbox, bboxi);
                    bbox = bboxNew;
                }
                return bbox;
            }
            return bbox;
        }
        //得到单元构件的长宽尺寸
        public void GetSize()
        {
            var bbox = this.GetCompBoundingBox();
            Surface baseSrf;
            GetBaseSurface(this, out baseSrf);
            //将曲面uv方向与构件平面的xy方向进行匹配（x为宽度方向，y为高度方向）
            Vector3d vecU;
            Vector3d vecV;
            Vector3d vecN;
            var result = baseSrf.ToNurbsSurface().UVNDirectionsAt(0, 0, out vecU, out vecV, out vecN);
            var vec = Vector3d.CrossProduct(vecU, this.Plane.XAxis);
            //若u方向与构件X方向即w方向共线，则曲面u宽度设置为单元宽度
            double w;
            double h;
            baseSrf.GetSurfaceSize(out w, out h);
            if (vec.Length < 0.01)
            {
                W = w; H = h;
            }
            else
            {
                W = h; H = w;
            }
        }
        //得到构件基准面的中点
        public Point3d GetCenterPt()
        {
            var bbox = this.GetCompBoundingBox();
            var bboxCenter = bbox.Center;
            var pt = Plane.WorldXY.ClosestPoint(bboxCenter);
            return pt;
        }

        //将单元构件映射至原点WorldXY平面
        public Component MapToOrigin()
        {
            var trans = Transform.PlaneToPlane(this.Plane, Plane.WorldXY);
            var geos = new List<GeometryBase>();
            foreach (var geo in this.Geometry)
            {
                geo.Transform(trans);
                geos.Add(geo);
            }
            return new Component(geos, Plane.WorldXY);
        }

    }



    //扩展类扩展方法，用于拆分列表
    public static class Extentions
    {
        public static IEnumerable<List<T>> partition<T>(this List<T> values, int chunkSize)
        {
            for (int i = 0; i < values.Count; i += chunkSize)
            {
                yield return values.GetRange(i, Math.Min(chunkSize, values.Count - i));
            }
        }
    }
}
