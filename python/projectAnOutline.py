#encoding: utf-8
import Rhino.Geometry as rg
import Rhino.RhinoDoc as doc
from LineCluster import DelOverlapLines

class Projector():
    def __init__(self,breps):
        self.breps=breps

    @staticmethod
    def set_brep_edges_to_plane(brep):
        #将一个brep边缘线投影至原点平面，并返回投影线
        crvs=brep.DuplicateEdgeCurves()
        crvList=[]
        for crv in crvs:
            projectedCrv=rg.Curve.ProjectToPlane(crv,rg.Plane.WorldXY)
            crvList.append(projectedCrv)
        return crvList

    def project_breps(self,breps):
        #将体量映射至原点坐标平面并得到投影线
        projectCrvs=[]
        for brep in self.breps:
            crvs=self.set_brep_edges_to_plane(brep)
            projectCrvs.extend(crvs)
        return projectCrvs

class CurvesCleaner():
    def __init__(self,crvs):
        self.crvs=crvs

    def clean_crvs(self):
        #清理一组线，删除重合部分线并将其组合
        dolInstance=DelOverlapLines(self.crvs)
        dolInstance.clean_crvs(self.crvs)
        simpleCrvs=dolInstance.checked_lines
        cleanedCrvs=rg.Curve.JoinCurves(simpleCrvs)
        return cleanedCrvs

    @staticmethod
    def create_curves_region_union(crvs):
        #获得一组平面曲线的最外轮廓线
        tol=doc.ActiveDoc.ModelAbsoluteTolerance
        regions=rg.Curve.CreateBooleanRegions(crvs,rg.Plane.WorldXY,True,tol)
        outline=regions.RegionCurves(0)
        return outline



if __name__ =="__main__":
    #得到投影线
    projector=Projector(breps)
    crvs=projector.project_breps(breps)
    #清理投影线
    crvCleaner=CurvesCleaner(crvs)
    cleanedCrvs=crvCleaner.clean_crvs()
    #得到外轮廓线
    outline=crvCleaner.create_curves_region_union(cleanedCrvs)