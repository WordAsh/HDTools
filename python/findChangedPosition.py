import Rhino.Geometry as rg
import Rhino.RhinoDoc as doc
class JudgeChanged():

    @staticmethod
    def is_two_crvs_overlap(crv1,crv2):
        #检查两条线是否重叠
        tol = doc.ActiveDoc.ModelAbsoluteTolerance
        crv11=crv1.ToNurbsCurve()#将要判断的线转换为nurbs curve
        crv22=crv2.ToNurbsCurve()
        events=rg.Intersect.Intersection.CurveCurve(crv11,crv22,tol,tol)
        if events:
            return events[0].IsOverlap

    @staticmethod
    def join_continue_intervals(intervals):
        #检查接缝点是否位于重合部分的内部，若位于内部，将位于重合部分内部的接缝点两侧的t值区间进行合并
        if len(intervals)==1:
            return intervals
        else:
            #合并连续区间
            for i in range(len(intervals)-1):
                if intervals[i].T1==intervals[i+1].T0:
                    interval=rg.Interval.FromUnion(intervals[i],intervals[i+1])
                    intervals.pop(i)
                    intervals[i]=interval
                    break
            return intervals

    def get_t_intervals(self,preCrv,changedCrv):
        #得到轮廓线发生变动位置的t值区间
        t_intervals1=[]
        t_intervals2=[]
        tol = doc.ActiveDoc.ModelAbsoluteTolerance
        #先得到未发生变动的t值区间
        for i in rg.Intersect.Intersection.CurveCurve(preCrv,changedCrv,tol,tol):
            t_intervals1.append(i.OverlapA)
        t_intervals=self.join_continue_intervals(t_intervals1)

        #构造发生变动的t值区间
        #检查，解决接缝点位于变动部分内部的情况
        if len(t_intervals)==1:
            interval=rg.Interval(t_intervals[0].T1,t_intervals[0].T0)
            #若得到的t值区间长度与原轮廓线相同，说明未发生变化，否则变动区间为interval
            if abs(interval.Length) != abs(preCrv.Domain.Length):
                t_intervals2.append(interval)

        else:
            for j in range(len(t_intervals)-1):
                interval=rg.Interval(t_intervals[j].T1,t_intervals[j+1].T0)
                t_intervals2.append(interval)
        if t_intervals2 is not None:
            return t_intervals2#返回原轮廓线上发生变动的t值区间

class GetSegments():
    @staticmethod
    def get_segments(preCrv,changedCrv):
        t_values=[]
        segments=[]
        judger=JudgeChanged()
        #得到原轮廓线发生变动的t值区间,若未发生变动则打印未变化
        intervals=judger.get_t_intervals(preCrv,changedCrv)
        if intervals:
            #根据t值对原轮廓线进行分割
            for i in intervals:
                min=i.T0
                max=i.T1
                t_values.append(min)
                t_values.append(max)
            crvs=preCrv.Split(t_values)
            for crv in crvs:
                #如果分割后的片段与改动后轮廓线没有重合，则该片段的端点为发生变动位置
                if not judger.is_two_crvs_overlap(crv,changedCrv):
                    segments.append(crv)
            return segments
        else:
            print("Unchanged!")


if __name__ =="__main__":
    getter=GetSegments()
    preCrvs=getter.get_segments(x,y)#得到变动的片段
    changedCrvs=getter.get_segments(y,x)#换位思考，得到改变后的片段



