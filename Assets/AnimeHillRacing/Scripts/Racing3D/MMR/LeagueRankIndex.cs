using System;

namespace AnimeHillRacing.Racing3D
{
    // Array-backed order-statistics treap. Five integer arrays (20 bytes/entity),
    // incremental O(log N) updates and exact rank/page/neighbor queries. No rich
    // profile materialization and no full sort on a UI refresh or simulation batch.
    public sealed class LeagueRankIndex
    {
        readonly int[] left,right,parent,size,rating;
        readonly bool[] qualified;
        readonly LocalLeagueState state;
        public int QualifiedCount {get;private set;}
        int root=-1;
        public int Count=>rating.Length;
        public long Comparisons {get;private set;}
        public LeagueRankIndex(LocalLeagueState state, bool qualifiedFirst=false)
        {
            this.state=qualifiedFirst?state:null;
            int n=state.bots.Length+1;left=new int[n];right=new int[n];parent=new int[n];size=new int[n];rating=new int[n];
            qualified=new bool[n];
            for(int i=0;i<n;i++){left[i]=right[i]=parent[i]=-1;rating[i]=i<n-1?state.bots[i].currentMmr:state.human.currentMmr;qualified[i]=qualifiedFirst&&SeasonQualification.IsOfficial(state,i);if(qualified[i])QualifiedCount++;Insert(i);}
        }
        static uint Priority(int index){uint x=unchecked((uint)index+0x9e3779b9u);x^=x>>16;x*=0x7feb352du;x^=x>>15;x*=0x846ca68bu;return x^(x>>16);}
        int Size(int node)=>node<0?0:size[node];
        void Recount(int node){if(node>=0)size[node]=1+Size(left[node])+Size(right[node]);}
        bool Before(int a,int b){Comparisons++;return qualified[a]!=qualified[b]?qualified[a]:rating[a]>rating[b] || (rating[a]==rating[b] && a<b);}
        void Replace(int old,int next){int p=parent[old];if(p<0)root=next;else if(left[p]==old)left[p]=next;else right[p]=next;if(next>=0)parent[next]=p;}
        void Rotate(int node)
        {
            int p=parent[node],g=parent[p];bool isLeft=left[p]==node;
            int middle=isLeft?right[node]:left[node];
            if(isLeft){left[p]=middle;right[node]=p;}else{right[p]=middle;left[node]=p;}
            if(middle>=0)parent[middle]=p;parent[p]=node;parent[node]=g;
            if(g<0)root=node;else if(left[g]==p)left[g]=node;else right[g]=node;
            Recount(p);Recount(node);
        }
        void Insert(int node)
        {
            left[node]=right[node]=-1;size[node]=1;parent[node]=-1;
            if(root<0){root=node;return;}
            int current=root;
            while(true){size[current]++;bool before=Before(node,current);int next=before?left[current]:right[current];
                if(next<0){if(before)left[current]=node;else right[current]=node;parent[node]=current;break;}current=next;}
            while(parent[node]>=0 && Priority(node)<Priority(parent[node]))Rotate(node);
        }
        void Remove(int node)
        {
            while(left[node]>=0 || right[node]>=0){int child=left[node]<0?right[node]:right[node]<0?left[node]:Priority(left[node])<Priority(right[node])?left[node]:right[node];Rotate(child);}
            int p=parent[node];Replace(node,-1);while(p>=0){Recount(p);p=parent[p];}
        }
        public void Update(int node,int mmr){bool q=state!=null&&SeasonQualification.IsOfficial(state,node);if(rating[node]==mmr&&qualified[node]==q)return;Remove(node);if(qualified[node])QualifiedCount--;qualified[node]=q;if(q)QualifiedCount++;rating[node]=mmr;Insert(node);}
        public int AtRank(int zeroBased)
        {
            if(zeroBased<0 || zeroBased>=Count)throw new ArgumentOutOfRangeException(nameof(zeroBased));
            int node=root;while(node>=0){int l=Size(left[node]);if(zeroBased==l)return node;if(zeroBased<l)node=left[node];else{zeroBased-=l+1;node=right[node];}}
            throw new InvalidOperationException("Damaged rank index.");
        }
        public int RankOf(int node){int rank=Size(left[node]);while(parent[node]>=0){int p=parent[node];if(right[p]==node)rank+=1+Size(left[p]);node=p;}return rank;}
        public int CountHigher(int mmr,bool includeEqual=false)
        {
            // Used only by legacy fixture matchmaking. Qualification partitions
            // have independent rating orders; never binary-search across them.
            if(state!=null){int total=0;foreach(int value in rating)if(value>mmr||(includeEqual&&value==mmr))total++;return total;}
            int node=root,count=0;while(node>=0){if(rating[node]>mmr || (includeEqual && rating[node]==mmr)){count+=1+Size(left[node]);node=right[node];}else node=left[node];}return count;
        }
        public bool Valid()
        {
            if(Size(root)!=Count)return false;
            for(int i=0;i<Count;i++){int node=AtRank(i);if(RankOf(node)!=i || size[node]!=1+Size(left[node])+Size(right[node]))return false;if(i>0 && !Before(AtRank(i-1),node))return false;}
            return true;
        }
    }
}
