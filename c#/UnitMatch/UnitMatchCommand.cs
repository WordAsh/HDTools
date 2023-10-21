using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnitMatch
{
    [Guid("0874C691-A083-4C05-9E31-96C2C63F7C7E")]
    public class UnitMatchWPFHost:RhinoWindows.Controls.WpfElementHost
    {
        public UnitMatchWPFHost(uint docSn)
            : base(new UnitMatchWPF(docSn), null)
        { }
    }
    public class UnitMatchCommand : Rhino.Commands.Command
    {
        public UnitMatchCommand()
        {
            Instance = this;
            Panels.RegisterPanel(UnitMatchPlugin.Instance, typeof(UnitMatchWPFHost), "单元匹配面板", null);
        }

        public static UnitMatchCommand Instance { get; private set; }
        public override string EnglishName => "UnitMatchCommand";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            //eto界面
            #region
            //var args = new UnitMatchArgs
            //{
            //    Ucount = 1,
            //    Vcount = 1,
            //    DeleteInputSurface = true,
            //    AddUnitFrame = false,
            //};

            //var dlg = new UnitMatchDialog(args) { Owner = Rhino.UI.RhinoEtoApp.MainWindow };
            //dlg.Show();
            #endregion

            //wpf界面
            var panel_id = typeof(UnitMatchWPFHost).GUID;
            Panels.OpenPanel(panel_id);


            return Result.Success;
        }

        //执行函数，静态类,eto界面
        internal static void Execute()
        {
            var doc = RhinoDoc.ActiveDoc;
            if (Manager.IsValid)
            {

                var uCount = Manager.Ucount;
                var vCount = Manager.Vcount;

                List<Curve> verticalCutters;
                List<Curve> horizontalCutters;
                Cutter.GetVerticalFaceCutters(Manager.Surface, uCount, vCount,out verticalCutters,out horizontalCutters);
                var units = Cutter.GenerateUnits(Manager.Surface,verticalCutters,horizontalCutters);
                var comp = new Component(Manager.Geos, Manager.basePlane);

                if (Manager.AddUnitFrame)
                {
                    foreach (var unit in units)
                    {
                        doc.Objects.AddCurve(Curve.JoinCurves(unit.Frame)[0]);
                    }
                }
                if (Manager.DeleteInputSurface)
                {
                    doc.Objects.Delete(Manager.srfId, true);
                }

                //成组后将匹配后物体添加至文档
                var matchedUnits = Match.MatchToAllUnits(comp, units);

                var partitions = matchedUnits.partition(comp.Geometry.Count);//将每个component分为一组
                foreach (var partition in partitions)
                {

                    List<Guid> ids = new List<Guid>();
                    foreach (var geo in partition)
                    {
                        var id = doc.Objects.Add(geo);
                        ids.Add(id);
                    }
                    doc.Groups.Add(ids);
                }


                ////测试匹配顺序
                //var i = 0;
                //foreach (var x in units)
                //{
                //    var crv = Curve.JoinCurves(x.Frame)[0];
                //    var bbox = crv.GetBoundingBox(Plane.WorldXY);
                //    var center = bbox.Center;
                //    var plane = new Plane(center, new Vector3d(1, 0, 0));
                //    doc.Objects.AddText(new Rhino.Display.Text3d(i.ToString(), plane, 2));
                //    i++;
                //}

            }
            else {
                RhinoApp.WriteLine("执行命令失败：未选择有效几何体或未正确输入划分数量");
            }
            doc.Views.Redraw();
        }

        //执行函数,实例化对象,wpf界面
        internal static void Execute2(Manager2 manager)
        {
            var doc = RhinoDoc.ActiveDoc;
            if (manager.IsValid)
            {
                var uCount = manager.Ucount;
                var vCount = manager.Vcount;

                List<Curve> verticalCutters;
                List<Curve> horizontalCutters;
                Cutter.GetVerticalFaceCutters(manager.Surface, uCount, vCount, out verticalCutters, out horizontalCutters);
                var units = Cutter.GenerateUnits(manager.Surface, verticalCutters, horizontalCutters);
                var comp = new Component(manager.Geos, manager.basePlane);

                if (manager.AddUnitFrame)
                {
                    foreach (var unit in units)
                    {
                        var id=doc.Objects.AddCurve(Curve.JoinCurves(unit.Frame)[0]);
                        manager.Guids.Add(id);
                    }
                }
                if (manager.DeleteInputSurface)
                {
                    doc.Objects.Delete(manager.srfId, true);
                }
                //if (manager.AddStatisticForm)
                //{

                //}

                //成组后将匹配后物体添加至文档
                var matchedUnits = Match.MatchToAllUnits(comp, units);

                var partitions = matchedUnits.partition(comp.Geometry.Count);//将每个component分为一组
                foreach (var partition in partitions)
                {

                    List<Guid> ids = new List<Guid>();
                    foreach (var geo in partition)
                    {
                        var id = doc.Objects.Add(geo);
                        ids.Add(id);
                        manager.Guids.Add(id);//记录物体的guid，便于撤销操作
                    }
                    doc.Groups.Add(ids);                 
                }
            }
            else
            {
                RhinoApp.WriteLine("执行命令失败：未选择有效几何体或未正确输入划分数量");
            }
            doc.Views.Redraw();
        }

        //撤销匹配操作，删除匹配的物体
        internal static void Cancel(Manager2 manager)
        {
            var doc = RhinoDoc.ActiveDoc;
            doc.Objects.Delete(manager.Guids, true);
            doc.Views.Redraw();
        }

    }


}
