using UnityEngine;
namespace AnimeHillRacing.Racing3D
{
    // Observes player route traversal and landings without issuing a control command.
    public sealed class RacingTechniqueObserver : MonoBehaviour
    {
        RacingVehiclePhysics vehicle; TrackSplineGraph graph; float nextSample;
        int[] furthest; bool[] entered; int observedRecoveries; VehicleRecovery recovery;
        public int ShortcutSuccesses {get;private set;}
        public int AerialSuccesses {get;private set;}
        public void Initialize(RacingVehiclePhysics car,TrackSplineGraph track) {
            vehicle=car;graph=track;recovery=GetComponent<VehicleRecovery>();furthest=new int[graph.Shortcuts.Length];entered=new bool[furthest.Length];
            vehicle.OnLanding+=Landing;
        }
        void Landing(float strength) {
            if(vehicle.RaceEnabled && vehicle.TrickCount>0 && !vehicle.IsSpunOut && Vector3.Dot(transform.up,Vector3.up)>.4f)AerialSuccesses++;
        }
        void Update() {
            if(vehicle==null || !vehicle.RaceEnabled || Time.time<nextSample)return;nextSample=Time.time+.10f;
            if(recovery!=null && recovery.RecoveryCount!=observedRecoveries) {observedRecoveries=recovery.RecoveryCount;System.Array.Clear(entered,0,entered.Length);}
            for(int r=0;r<graph.Shortcuts.Length;r++) {
                var route=graph.Shortcuts[r];var points=route.pathPoints;float best=float.MaxValue;int segment=0;
                for(int i=0;i<points.Length-1;i++) {
                    Vector3 delta=points[i+1]-points[i];float t=Mathf.Clamp01(Vector3.Dot(transform.position-points[i],delta)/Mathf.Max(.01f,delta.sqrMagnitude));
                    float dist=(transform.position-points[i]-delta*t).sqrMagnitude;if(dist<best){best=dist;segment=i;}
                }
                if(best>64){entered[r]=false;continue;}
                if(segment<=4 && best<route.width*route.width*.16f){entered[r]=true;furthest[r]=segment;}
                if(!entered[r])continue;
                if(segment>furthest[r]+4){entered[r]=false;continue;}
                furthest[r]=Mathf.Max(furthest[r],segment);
                var exit=graph.GetNode(route.exitNodeIndex);Vector3 offset=transform.position-exit.Position;
                if(furthest[r]>=points.Length-3 && vehicle.IsGrounded && Mathf.Abs(offset.y)<2 && Mathf.Abs(Vector3.Dot(offset,exit.Right))<exit.TrackWidth*.43f && Vector3.Dot(transform.forward,exit.Forward)>.5f) {
                    ShortcutSuccesses++;entered[r]=false;
                }
            }
        }
        void OnDestroy(){if(vehicle!=null)vehicle.OnLanding-=Landing;}
    }
}
