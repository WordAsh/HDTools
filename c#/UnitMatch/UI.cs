using Rhino.Geometry;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Commands;
using Eto;
using Eto.Forms;
using System.Collections.ObjectModel;

namespace UnitMatch
{

    //用户输入幕墙划分的UV参数
    internal class UnitMatchArgs
    {
        public int Ucount { get; set; } = 0;
        public int Vcount { get; set; } = 0;
        public bool DeleteInputSurface { get; set; } = true;
        public bool AddUnitFrame { get; set; } = false;
    }
    //用户操作窗口
    internal class UnitMatchDialog : Eto.Forms.Form
    {
        UnitMatchArgs m_args;
        Eto.Forms.NumericStepper m_u_count_stepper;
        Eto.Forms.NumericStepper m_v_count_stepper;
        Eto.Forms.CheckBox m_delete_input_surface_checkbox;
        Eto.Forms.CheckBox m_add_unit_frame_checkbox;
        Eto.Forms.Button m_button1;
        Eto.Forms.Button m_button2;
        Eto.Forms.Button m_button3;
        Eto.Forms.Button m_okButton;
        Eto.Forms.Button m_cancelButton;


        //用户操作对话框
        public UnitMatchDialog(UnitMatchArgs args)
        {
            m_args = args ?? throw new ArgumentException(nameof(args));
            Title = "单元匹配工具";
            Padding = new Eto.Drawing.Padding(5);
            Resizable = false;
            Width = 250;
            Height = 280;
            Maximizable = false;
            Minimizable = false;

            var layout = new Eto.Forms.DynamicLayout
            {
                Padding = new Eto.Drawing.Padding(5),
                Spacing = new Eto.Drawing.Size(5, 5)
            };

            layout.AddRow(CreateButtons1());
            layout.AddRow(null);//spacer
            layout.AddRow(CreateSteppers());
            layout.AddRow(null);//spacer
            layout.AddRow(CreateCheckBoxes());
            layout.AddRow(null);//spacer
            layout.AddRow(CreateButtons2());

            Content = layout;
        }
        public UnitMatchArgs Results => m_args;
        //选取流程按钮部分
        public Eto.Forms.DynamicLayout CreateButtons1()
        {
            m_button1 = new Eto.Forms.Button { Text = "选择单元构件" };
            m_button1.Click += M_button1_Click;

            m_button2 = new Eto.Forms.Button { Text = "选择要投影的竖直曲面" };
            m_button2.Click += M_button2_Click;

            m_button3 = new Eto.Forms.Button { Text = "构造单元构件的基准平面" };
            m_button3.Click += M_button3_Click;

            var layout = new Eto.Forms.DynamicLayout { Spacing = new Eto.Drawing.Size(5, 5) };
            layout.AddRow(null, m_button1, null);
            layout.AddRow(null, m_button2, null);
            layout.AddRow(null, m_button3, null);
            return layout;
        }

        private void M_button3_Click(object sender, EventArgs e)
        {
            Manager.ConstructComponentPlane(out Plane basePlane);
            Manager.basePlane = basePlane;
        }

        private void M_button2_Click(object sender, EventArgs e)
        {
            Manager.SelectVerticalSurface(out Surface surface, out Guid srfId);
            Manager.Surface = surface;
            Manager.srfId = srfId;
        }

        public void M_button1_Click(object sender, EventArgs e)
        {
            Manager.Geos = Manager.SelectComponent();
        }

        //划分方式输入部分
        private Eto.Forms.DynamicLayout CreateSteppers()
        {
            var label0 = new Eto.Forms.Label { Text = "横向划分数量:" };
            var label1 = new Eto.Forms.Label { Text = "纵向划分数量:" };

            m_u_count_stepper = new Eto.Forms.NumericStepper
            {
                Value = m_args.Ucount,
                MinValue = 1
            };
            m_v_count_stepper = new Eto.Forms.NumericStepper
            {
                Value = m_args.Vcount,
                MinValue = 1
            };
            var layout = new Eto.Forms.DynamicLayout { Spacing = new Eto.Drawing.Size(5, 5) };
            layout.AddRow(label0, m_u_count_stepper, null);
            layout.AddRow(label1, m_v_count_stepper, null);
            return layout;
        }
        //checkbox部分
        private Eto.Forms.DynamicLayout CreateCheckBoxes()
        {
            m_delete_input_surface_checkbox = new Eto.Forms.CheckBox
            {
                Text = "删除被匹配的竖直曲面",
                Checked = Manager.DeleteInputSurface,
                ThreeState = false,
            };
            m_delete_input_surface_checkbox.CheckedChanged += M_delete_input_surface_checkbox_CheckedChanged;
            m_add_unit_frame_checkbox = new Eto.Forms.CheckBox
            {
                Text = "添加单元边框线",
                Checked = Manager.AddUnitFrame,
                ThreeState = false
            };
            m_add_unit_frame_checkbox.CheckedChanged += M_add_unit_frame_checkbox_CheckedChanged;

            var layout = new Eto.Forms.DynamicLayout { Spacing = new Eto.Drawing.Size(5, 5) };
            layout.AddRow(m_delete_input_surface_checkbox);
            layout.AddRow(m_add_unit_frame_checkbox);
            return layout;
        }

        private void M_add_unit_frame_checkbox_CheckedChanged(object sender, EventArgs e)
        {
            Manager.AddUnitFrame = !Manager.AddUnitFrame;
        }

        private void M_delete_input_surface_checkbox_CheckedChanged(object sender, EventArgs e)
        {
            Manager.DeleteInputSurface = !Manager.DeleteInputSurface;
        }
        //确定取消按钮部分
        private Eto.Forms.DynamicLayout CreateButtons2()
        {
            m_okButton = new Eto.Forms.Button { Text = "确定" };
            m_okButton.Click += OkButton_Click;

            m_cancelButton = new Eto.Forms.Button { Text = "取消" };
            m_cancelButton.Click += CancelButton_Click;

            var layout = new Eto.Forms.DynamicLayout { Spacing = new Eto.Drawing.Size(5, 5) };
            layout.AddRow(null, m_okButton, m_cancelButton, null);
            return layout;
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            Manager.Ucount = (int)m_u_count_stepper.Value;
            Manager.Vcount = (int)m_v_count_stepper.Value;
            UnitMatchCommand.Execute();
            this.Close();
        }

    }

}