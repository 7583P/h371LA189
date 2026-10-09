using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public sealed partial class TrackSplineGraph
    {
        struct ProjectionBranch { public Vector3 min, max; public int start, count, left, right; }
        ProjectionBranch[] projectionIndex;
        int[] projectionSegments;
        int projectionBranchCount;
        sealed class SegmentOrder : IComparer<int>
        {
            readonly TrackNode[] points; readonly int axis;
            public SegmentOrder(TrackNode[] points, int axis) { this.points=points;this.axis=axis; }
            public int Compare(int a,int b) => (points[a].Position[axis]+points[(a+1)%points.Length].Position[axis]).CompareTo(points[b].Position[axis]+points[(b+1)%points.Length].Position[axis]);
        }
        void BuildProjectionIndex()
        {
            projectionSegments = new int[nodes.Length];
            projectionIndex = new ProjectionBranch[nodes.Length*2];
            for(int i=0;i<nodes.Length;i++)projectionSegments[i]=i;
            projectionBranchCount=0;BuildBranch(0,nodes.Length);
        }
        int BuildBranch(int start,int count)
        {
            int at=projectionBranchCount++;
            var min=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity);
            var max=new Vector3(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity);
            for(int i=start;i<start+count;i++) { int s=projectionSegments[i];min=Vector3.Min(min,Vector3.Min(nodes[s].Position,nodes[(s+1)%nodes.Length].Position));max=Vector3.Max(max,Vector3.Max(nodes[s].Position,nodes[(s+1)%nodes.Length].Position)); }
            var branch=new ProjectionBranch{min=min,max=max,start=start,count=count,left=-1,right=-1};
            if(count>8) {
                Vector3 span=max-min;int axis=span.x>span.z?0:2;if(span.y>span[axis])axis=1;
                Array.Sort(projectionSegments,start,count,new SegmentOrder(nodes,axis));
                int half=count/2;branch.left=BuildBranch(start,half);branch.right=BuildBranch(start+half,count-half);branch.count=0;
            }
            projectionIndex[at]=branch;return at;
        }
        static float BoxDistance(Vector3 point,ProjectionBranch b)
        {
            float x=Mathf.Max(b.min.x-point.x,Mathf.Max(0,point.x-b.max.x));
            float y=Mathf.Max(b.min.y-point.y,Mathf.Max(0,point.y-b.max.y));
            float z=Mathf.Max(b.min.z-point.z,Mathf.Max(0,point.z-b.max.z));return x*x+y*y+z*z;
        }
        bool TryIndexedProjection(Vector3 point,out int segment,out float along,out float lateral)
        {
            segment=0;along=lateral=0;if(projectionIndex==null||nodes.Length<2)return false;
            float closest=float.MaxValue;int best=int.MaxValue;float fraction=0;
            SearchProjection(0,point,ref closest,ref best,ref fraction);
            if(best==int.MaxValue)return false;
            segment=best;Vector3 delta=nodes[(best+1)%nodes.Length].Position-nodes[best].Position;
            along=nodes[best].CumulativeDistance+fraction*Mathf.Sqrt(delta.sqrMagnitude);
            lateral=Vector3.Dot(point-(nodes[best].Position+delta*fraction),nodes[best].Right);return true;
        }
        void SearchProjection(int branchIndex,Vector3 point,ref float closest,ref int best,ref float bestFraction)
        {
            var b=projectionIndex[branchIndex];if(BoxDistance(point,b)>closest+Mathf.Max(.001f,closest*.00001f))return;
            if(b.count>0) {
                for(int j=b.start;j<b.start+b.count;j++) {
                    int s=projectionSegments[j];Vector3 delta=nodes[(s+1)%nodes.Length].Position-nodes[s].Position;
                    float length=delta.sqrMagnitude;if(length<.0001f)continue;
                    float t=Mathf.Clamp01(Vector3.Dot(point-nodes[s].Position,delta)/length);
                    float distance=(nodes[s].Position+delta*t-point).sqrMagnitude;
                    if(distance<closest||(distance==closest&&s<best)){closest=distance;best=s;bestFraction=t;}
                }
                return;
            }
            bool leftFirst=BoxDistance(point,projectionIndex[b.left])<=BoxDistance(point,projectionIndex[b.right]);
            SearchProjection(leftFirst?b.left:b.right,point,ref closest,ref best,ref bestFraction);
            SearchProjection(leftFirst?b.right:b.left,point,ref closest,ref best,ref bestFraction);
        }
    }
}
