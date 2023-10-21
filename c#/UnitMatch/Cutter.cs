using CurtainWallMatch;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitMatch
{
    public class Cutter
    {
        //求横向竖向基础分割线
        public static List<Curve> GetVerticalFaceBaseCutter(Surface srf)
        {   
            var brep=srf.ToBrep();
            var edges = brep.DuplicateEdgeCurves();
            var crvs = new List<Curve>();

            foreach (var edge in edges)
            {
                var vec = edge.TangentAtStart;
                if (Vector3d.VectorAngle(new Vector3d(0, 0, 1), vec) - 0 <= 0.01)
                {
                    Curve vCutter = edge;
                    crvs.Add(vCutter);
                }
            }
            foreach (var edge in edges)
            {
                var vec = edge.TangentAtStart;
                if (Vector3d.VectorAngle(new Vector3d(0, 0, 1), vec) - Math.PI / 2 <= 0.01)
                {
                    Curve uCutter = edge;
                    crvs.Add(uCutter);
                }
            }
            return crvs;
        }
        //用向量复制线
        public static Curve DuplicateCrvWithVec(Curve crv, Vector3d vec)
        {
            var trans = Transform.Translation(vec);
            var newCrv = crv.Duplicate();
            newCrv.Transform(trans);
            return (Curve)newCrv;
        }
        //得到面的分割线
        public static void GetVerticalFaceCutters(Surface srf, int uCount, int vCount,out List<Curve> verticalCutters,out List<Curve> horizontalCutters)
        {
            var baseCutter = GetVerticalFaceBaseCutter(srf);

            var uCutter = baseCutter[1];
            var vCutter = baseCutter[0];

            var uVec = -uCutter.TangentAtStart;
            var vVec = vCutter.TangentAtStart;
            var uStep = uCutter.GetLength() / uCount;
            var vStep = vCutter.GetLength() / vCount;

            verticalCutters = new List<Curve>();
            horizontalCutters = new List<Curve>();

            for (int i = 1; i < uCount; i++)
            {
                var vec = i * uStep * uVec;
                verticalCutters.Add(DuplicateCrvWithVec(vCutter, vec));
            }
            for (int i = 1; i < vCount; i++)
            {
                var vec = i * vStep * vVec;
                horizontalCutters.Add(DuplicateCrvWithVec(uCutter, vec));
            }
        }
        //在竖向面上生成定位单元
        public static List<Unit> GenerateUnits(Surface srf, List<Curve> verticalCutters, List<Curve> horizontalCuttters)
        {
            List<Unit> units = new List<Unit>();

            var face = srf.ToBrep();
            var verticalBreps = face.Split(verticalCutters, 0.01);

            //split本无序，对其进行排序
            Array.Sort(verticalBreps, (x, y) =>
            (x.GetBoundingBox(Plane.WorldXY).Center.X).CompareTo(y.GetBoundingBox(Plane.WorldXY).Center.X));
            Array.Sort(verticalBreps, (x, y) =>
            (x.GetBoundingBox(Plane.WorldXY).Center.Y).CompareTo(y.GetBoundingBox(Plane.WorldXY).Center.Y));

            foreach (var verticalBrep in verticalBreps)
            {
                var breps = verticalBrep.Split(horizontalCuttters, 0.01);

                Array.Sort(breps, (x, y) => (x.GetBoundingBox(Plane.WorldXY).Center.Z).CompareTo(y.GetBoundingBox(Plane.WorldXY).Center.Z));
                foreach (var brep in breps)
                {
                    Plane plane;
                    var result = srf.TryGetPlane(out plane);
                    var frame = brep.DuplicateEdgeCurves().ToList<Curve>();
                    var unit = new Unit(plane, frame);
                    unit.SetUnitSize();
                    units.Add(unit);
                }
            }

            return units;
        }
        //按距离进行分割，冗余代码，舍不得删
        #region
        //public static List<Curve> GetVerticalFaceCutters(Surface srf, List<double> uSteps, List<double> vSteps)
        //{
        //    var baseCutter = GetVerticalFaceBaseCutter(srf);

        //    var uCutter = baseCutter[1];
        //    var vCutter = baseCutter[0];

        //    var uVec = -uCutter.TangentAtStart;
        //    var vVec = vCutter.TangentAtStart;

        //    List<Curve> cutters = new List<Curve>();
        //    var uCount = uSteps.Count;
        //    var vCount = vSteps.Count;

        //    for (int i = 1; i < uCount + 1; i++)
        //    {
        //        var vec = (uSteps.GetRange(0, i).Sum()) * uVec;
        //        cutters.Add(DuplicateCrvWithVec(vCutter, vec));
        //    }
        //    for (int i = 1; i < vCount + 1; i++)
        //    {
        //        var vec = (vSteps.GetRange(0, i).Sum()) * vVec;
        //        cutters.Add(DuplicateCrvWithVec(uCutter, vec));
        //    }
        //    return cutters;
        //}
        //public static List<Curve> GetVerticalFaceCutters(Surface srf, int uCount, List<double> vSteps)
        //{
        //    var baseCutter = GetVerticalFaceBaseCutter(srf);

        //    var uCutter = baseCutter[1];
        //    var vCutter = baseCutter[0];

        //    var uVec = -uCutter.TangentAtStart;
        //    var vVec = vCutter.TangentAtStart;

        //    List<Curve> cutters = new List<Curve>();
        //    var uStep = uCutter.GetLength() / uCount;
        //    var vCount = vSteps.Count;

        //    for (int i = 1; i < uCount; i++)
        //    {
        //        var vec = i * uStep * uVec;
        //        cutters.Add(DuplicateCrvWithVec(vCutter, vec));
        //    }
        //    for (int i = 1; i < vCount + 1; i++)
        //    {
        //        var vec = (vSteps.GetRange(0, i).Sum()) * vVec;
        //        cutters.Add(DuplicateCrvWithVec(uCutter, vec));
        //    }
        //    return cutters;
        //}
        //public static List<Curve> GetVerticalFaceCutters(Surface srf, List<double> uSteps, int vCount)
        //{
        //    var baseCutter = GetVerticalFaceBaseCutter(srf);

        //    var uCutter = baseCutter[1];
        //    var vCutter = baseCutter[0];

        //    var uVec = -uCutter.TangentAtStart;
        //    var vVec = vCutter.TangentAtStart;

        //    List<Curve> cutters = new List<Curve>();
        //    var vStep = uCutter.GetLength() / vCount;
        //    var uCount = uSteps.Count;

        //    for (int i = 1; i < vCount; i++)
        //    {
        //        var vec = i * vStep * vVec;
        //        cutters.Add(DuplicateCrvWithVec(uCutter, vec));
        //    }
        //    for (int i = 1; i < uCount + 1; i++)
        //    {
        //        var vec = (uSteps.GetRange(0, i).Sum()) * uVec;
        //        cutters.Add(DuplicateCrvWithVec(vCutter, vec));
        //    }
        //    return cutters;
        //}
        #endregion




    }
}
