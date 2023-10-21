using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitMatch
{
    public class PlaneTest
    {
        //检查两平面是否共面
        public static bool IsCoplanar(Plane plane1, Plane plane2)
        {
            //先判断两平面是否平行
            var norm1 = plane1.Normal;
            var norm2 = plane2.Normal;
            var vec = Vector3d.CrossProduct(norm1, norm2);
            if (vec.Length < 0.01)//若平行，再判断是否共面
            {
                var pt = plane1.Origin;
                if (plane2.DistanceTo(pt) < 0.01)
                {
                    return true;
                }
                else { return false; }
            }
            else { return false; }
        }
    }
}
