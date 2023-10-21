using Rhino;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UnitMatch
{
    /// <summary>
    /// Interaction logic for UnitMatchWPF.xaml
    /// </summary>
    public partial class UnitMatchWPF 
    {
        
        Manager2 manager=new Manager2();
        public UnitMatchWPF(uint documentSerialNumber)
        {
            InitializeComponent();

            Binding bindingDeleteSurface =new Binding("DeleteInputSurface") { Source=manager};
            this.delect_surface_checkbox.SetBinding(CheckBox.IsCheckedProperty,bindingDeleteSurface);

            Binding bindingAddFrame = new Binding("AddUnitFrame") { Source = manager };
            this.add_frame_checkbox.SetBinding(CheckBox.IsCheckedProperty,bindingAddFrame);

            //Binding bindingAddForm = new Binding("AddStatisticForm") { Source = manager };
            //this.add_form_checkbox.SetBinding(CheckBox.IsCheckedProperty,bindingAddForm);

        }

        private void select_component_button_Click(object sender, RoutedEventArgs e)
        {
            manager.Geos = manager.SelectComponent();     
        }

        private void select_surface_button_Click(object sender, RoutedEventArgs e)
        {
            manager.SelectVerticalSurface(out Surface surface, out Guid srfId);
            manager.Surface = surface;
            manager.srfId = srfId;
        }

        private void construct_plane_button_Click(object sender, RoutedEventArgs e)
        {
            manager.ConstructComponentPlane(out Plane basePlane);
            manager.basePlane = basePlane;
        }

        private void delect_surface_checkbox_Checked(object sender, RoutedEventArgs e)
        {
           manager.DeleteInputSurface = true;
        }

        private void add_frame_checkbox_Checked(object sender, RoutedEventArgs e)
        {
            manager.AddUnitFrame = true;
        }

        //private void add_form_checkbox_Checked(object sender, RoutedEventArgs e)
        //{
        //    manager.AddStatisticForm = true;
        //}

        private void ok_button_Click(object sender, RoutedEventArgs e)
        {
            int uCount;
            int vCount;

            bool result1 = int.TryParse(this.tb1.Text, out uCount);
            bool result2= int.TryParse(this.tb2.Text, out vCount);
            if (result1 && result2)
            {
                manager.Ucount = uCount;
                manager.Vcount = vCount;
                UnitMatchCommand.Execute2(manager);
            }
            else {
                RhinoApp.WriteLine("未正确输入划分数量");
            }
        }

        private void cancel_button_Click(object sender, RoutedEventArgs e)
        {
            UnitMatchCommand.Cancel(manager);
        }
    }
 

}
