using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LC
{
    public class LCCommand : Command
    {
        public LCCommand()
        {
            Instance = this;
        }
        public static LCCommand  Instance { get; private set; }
        public override string EnglishName => "DetermineClusters";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var go = new GetObject();
            go.SetCommandPrompt("Select curves");
            go.GeometryFilter = ObjectType.Curve;
            go.GetMultiple(1, 0);
            if (go.CommandResult() != Result.Success) { return go.CommandResult(); }

            List<Curve> curves = new List<Curve>();
            for (var i = 0; i < go.ObjectCount; i++)
            {
                Curve curve = go.Object(i).Curve();
                if (null != curve)
                {
                    curves.Add(curve);
                }
            }
            //炸开选取的线
            ExplodeCurves.ExplodeCrvs(curves);
            var explodedCrvs = ExplodeCurves.ExplodedCrvs;

            //去除重叠的线
            var cleanedCrvs=DelOverlapCrvs.CleanCrvs(explodedCrvs);

            //判断线有几簇
            DetermineClusters.SortAllCluster(cleanedCrvs);
            int x = DetermineClusters.ClusterList.Count;
            RhinoApp.WriteLine("{0} clusters found.", x.ToString());

            return Result.Success;
        }
    }
    public class JoinTwoOverlapCurves
    {
        //合并两条有重叠的曲线
        public static bool IsTwoCrvsOverlap(Curve crv1, Curve crv2)
        {
            //检查两条线是否重叠
            var tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            var crv11 = crv1.ToNurbsCurve();
            var crv22 = crv2.ToNurbsCurve();
            var events = Intersection.CurveCurve(crv11, crv22, tol, tol);
            if (events.Count != 0 && events[0].IsOverlap)
            {
                return true;
            }
            else { return false; }

        }

        public static Curve MergeCurves(Curve shortCrv, Curve longCrv)
        {
            //去除重合线，并组合曲线
            var tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            var sPt = shortCrv.PointAtStart;
            var ePt = shortCrv.PointAtEnd;
            bool result1 = longCrv.ClosestPoint(sPt, out double t1);
            var crvPt1 = longCrv.PointAt(t1);
            bool result2 = longCrv.ClosestPoint(ePt, out double t2);
            var crvPt2 = longCrv.PointAt(t2);

            if (crvPt1.DistanceTo(sPt) < tol && crvPt2.DistanceTo(ePt) < tol)
            {
                return longCrv;
            }
            else
            {
                var lines = new List<Curve>();
                if (crvPt1.DistanceTo(sPt) < tol)
                {
                    var subLine = longCrv.Split(t1)[0];
                    if (IsTwoCrvsOverlap(subLine, shortCrv) == false)
                    {
                        lines.Add(subLine);
                        lines.Add(shortCrv);
                        return Curve.JoinCurves(lines, tol)[0];
                    }
                    else
                    {
                        lines.Add(longCrv.Split(t1)[1]);
                        lines.Add(shortCrv);
                        return Curve.JoinCurves(lines, tol)[0];
                    }
                }
                else
                {
                    var subLine = longCrv.Split(t2)[0];
                    if (IsTwoCrvsOverlap(subLine, shortCrv) is false)
                    {
                        lines.Add(subLine);
                        lines.Add(shortCrv);
                        return Curve.JoinCurves(lines, tol)[0];
                    }
                    else
                    {
                        lines.Add(longCrv.Split(t2)[1]);
                        lines.Add(shortCrv);
                        return Curve.JoinCurves(lines, tol)[0];
                    }
                }
            }
        }
        public static Curve MergeTwoOverlapCurve(Curve crv1, Curve crv2)
        {
            //将两条重合线合并为一条线，并返回这条线
            var tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            if (crv1.GetLength() > crv2.GetLength())
            {
                return MergeCurves(crv2, crv1);
            }
            else if (crv1.GetLength() == crv2.GetLength())
            {
                return MergeCurves(crv1, crv2);
            }
            else
            {
                return MergeCurves(crv1, crv2);
            }
        }
    }
    public class DelOverlapCrvs
    {
        //删除一组曲线中有重叠部分的线，并将删除后的线合并
        public static List<Curve> CleanCrvs(List<Curve> crvs)
        {
            //去除一个列表中的重叠部分的线
            if (crvs.Count == 1)
            {
                return crvs;
            }
            else
            {
                int x = crvs.Count;
                var list1 = crvs.Take(x / 2).ToList();
                var list2 = crvs.Skip(x / 2).ToList();
                var crvs1 = CleanCrvs(list1);
                var crvs2= CleanCrvs(list2);

                Excute:
                    foreach (var crvi in crvs1)
                    {
                        foreach (var crvj in crvs2)
                        {
                            if (JoinTwoOverlapCurves.IsTwoCrvsOverlap(crvi, crvj))
                            {
                                var newCrv = JoinTwoOverlapCurves.MergeTwoOverlapCurve(crvi, crvj);
                                crvs2.Remove(crvj);
                                crvs2.Add(newCrv);
                                crvs1.Remove(crvi);
                            goto Excute;
                            }
                        }
                        crvs2.Add(crvi);
                    }
                return crvs2;
            }
        }
    }
    public class ExplodeCurves
    {   
        private static List<Curve> _explodedCrvs = new List<Curve>();
        public static List<Curve> ExplodedCrvs { 
            get { return _explodedCrvs; }
            set { _explodedCrvs = value; } }
        public static List<Curve> ExplodePolyCrv(Curve crv)
        {
            //接受一个值，返回一个列表
            var sPt = crv.PointAtStart;
            var ePt = crv.PointAtEnd;
            bool result1 = crv.ClosestPoint(sPt, out double t_start);
            bool result2=crv.ClosestPoint(ePt, out double t_end);
            if (crv.GetNextDiscontinuity(Continuity.C1_continuous, t_start, t_end, out double t))
            //检查是多段线还是一条线
            {
                var crvs=new List<Curve>();

                while (crv.GetNextDiscontinuity(Continuity.C1_continuous, t_start, t_end,out double t1)==true)
                {
                    double t0;
                    crv.GetNextDiscontinuity(Continuity.C1_continuous, t_start, t_end, out t0);
                    var new_line = crv.Split(t0)[0];
                    crv = crv.Split(t0)[1];
                    crvs.Add(new_line);
                    t_start = t0;
                }
                crvs.Add(crv);
                return crvs;
            }
            else {
                var lines=new List<Curve>();
                lines.Add(crv);
                return lines;
            }

        }

        public static void ExplodeCrvs(List<Curve> crvs)
        {
            //接收线列表，炸开一组线，返回一个列表
            if (crvs.Count > 1)
            {
                foreach (Curve crv in crvs)
                {
                    var newCrvList = ExplodePolyCrv(crv);
                    foreach (var newCrv in newCrvList)
                    {
                        ExplodedCrvs.Add(newCrv);
                    }
                }
            }
            else {
                var newCrvList = ExplodePolyCrv(crvs[0]);
                foreach (var newCrv in newCrvList)
                {
                    ExplodedCrvs.Add((newCrv));
                }
            }
        }
    }
    public class DetermineClusters
    {
        //判定线有多少簇
        private static Dictionary<int, List<Curve>> _clusterList = new Dictionary<int, List<Curve>>();
        public static Dictionary<int, List<Curve>> ClusterList
        {
            get { return _clusterList; }
            set { _clusterList = value; }
        }
        public static bool IsTwoCrvsIntersected(Curve crv1, Curve crv2)
        {
            //检查两条线是否相交
            var tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            var events = Rhino.Geometry.Intersect.Intersection.CurveCurve(crv1, crv2, tol, 0.0);
            if (events.Count != 0) { return true; }
            return false;
        }
        public static List<Curve> DelDupCrvs(List<Curve> cluster, List<Curve> crvList)
        {
            //在线段列表中删除已经加入至簇的线
            foreach (var curve in cluster)
            {
                if (crvList.Contains(curve))
                    crvList.Remove(curve);
            }
            return crvList;

        }
        public static List<Curve> FirstSortCluster(List<Curve> crvs)
        {
            //对线列表进行初始筛选，将与其中一条线相交的线同这条线一起放入一个簇
            List<Curve> cluster = new List<Curve>
            {
                crvs[0]
            };
            crvs.RemoveAt(0);
            foreach (var crv in crvs)
            {
                if (IsTwoCrvsIntersected(cluster[0], crv))
                {
                    cluster.Add(crv);
                }
            }
            return cluster;
        }
        public static List<Curve> SortOneCluster(List<Curve> crvs, int i)
        {
            //选出一个线段簇
            var cluster = FirstSortCluster(crvs);
            var leftCrvs = DelDupCrvs(cluster, crvs);
            var newLeftCrvs = new List<Curve>();
            while (true)
            {
                var tempCluster = new List<Curve>();
                foreach (var leftCrv in leftCrvs)
                {
                    foreach (var crv in cluster)
                    {
                        if (IsTwoCrvsIntersected(crv, leftCrv))
                        {
                            tempCluster.Add(leftCrv);
                            break;
                        }
                    }
                    if (tempCluster.Count != 0) { break; }
                    else { continue; }
                }
                if (tempCluster.Count != 0)
                {
                    cluster.AddRange(tempCluster);
                    leftCrvs = DelDupCrvs(cluster, leftCrvs);
                }
                else
                {
                    ClusterList.Add(i, cluster);
                    newLeftCrvs = leftCrvs;
                    break;
                }
            }
            return newLeftCrvs;
        }
        public static void SortAllCluster(List<Curve> curves)
        {
            //筛选出所有线段簇
            List<Curve> leftCrvs = new List<Curve>();
            leftCrvs = curves;
            int i = 0;
            while (true)
            {

                leftCrvs = SortOneCluster(leftCrvs, i);
                if (leftCrvs.Count == 0)
                {
                    break;
                }
                else { i++; continue; }
            }
        }
    }
}
