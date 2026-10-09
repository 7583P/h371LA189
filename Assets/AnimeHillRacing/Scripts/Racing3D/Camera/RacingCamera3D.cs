using UnityEngine;
namespace AnimeHillRacing.Racing3D
{
    public enum RacingCameraPreset { Sport, Close, Comfortable }
    public sealed class RacingCamera3D:MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] RacingVehiclePhysics targetPhysics;
        [SerializeField] Camera cam;
        [SerializeField] float positionSmoothTime=.085f;
        [SerializeField] LayerMask obstacleLayer=~0;
        readonly RaycastHit[] hits=new RaycastHit[32];
        TrackSplineGraph graph;
        Vector3 velocity,followForward=Vector3.forward;
        float impact,landing,introEnd,introDuration;
        bool reversing;
        PlayerRacingInput targetInput;
        bool lastRearView;
        public bool IsLookingBack => targetInput != null && targetInput.LookBack && targetPhysics != null && targetPhysics.RaceEnabled;
        public RacingCameraPreset Preset => (RacingCameraPreset)Mathf.Clamp(PlayerPrefs.GetInt("AHR.Racing.Camera",0),0,2);
        void Awake(){cam=GetComponent<Camera>();if(cam!=null){cam.nearClipPlane=.18f;cam.farClipPlane=1500;}}
        public void SetTarget(Transform newTarget,RacingVehiclePhysics physics=null)
        {
            Unsubscribe();target=newTarget;targetPhysics=physics!=null?physics:newTarget?.GetComponent<RacingVehiclePhysics>();
            targetInput=newTarget!=null?newTarget.GetComponent<PlayerRacingInput>():null;lastRearView=false;
            if(targetPhysics!=null){targetPhysics.OnImpact+=Impact;targetPhysics.OnLanding+=Land;}
            graph=FindFirstObjectByType<TrackSplineGraph>();SnapToTarget();
        }
        public void SetPreset(RacingCameraPreset preset){PlayerPrefs.SetInt("AHR.Racing.Camera",(int)preset);PlayerPrefs.Save();}
        public void PlayIntroduction(TrackSplineGraph track,float seconds){graph=track;introDuration=seconds;introEnd=Time.time+seconds;}
        public void SnapToTarget()
        {
            if(target==null)return;followForward=Vector3.ProjectOnPlane(target.forward,Vector3.up).normalized;velocity=Vector3.zero;
            transform.position=target.position-followForward*7.3f+Vector3.up*3.5f;transform.LookAt(target.position+followForward*4+Vector3.up*.8f);
        }
        void LateUpdate()
        {
            if(target==null||targetPhysics==null)return;
            if(Time.time<introEnd&&graph!=null)
            {
                float t=1-(introEnd-Time.time)/introDuration;var start=graph.SampleDistance(graph.TotalLength-12f);
                Vector3 destination=t<.42f?start.Position-start.Forward*20+start.Right*22+Vector3.up*12:start.Position-start.Forward*28-start.Right*10+Vector3.up*6;
                transform.position=Vector3.Lerp(transform.position,destination,1-Mathf.Exp(-Time.deltaTime*5));transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(target.position+Vector3.up*.6f-transform.position,Vector3.up),1-Mathf.Exp(-Time.deltaTime*7));return;
            }
            float dt=Time.deltaTime;if(dt<=0)return;
            float ratio=Mathf.Clamp01(targetPhysics.SpeedKmh/targetPhysics.Stats.SpeedKmh);
            reversing=targetPhysics.ForwardSpeedMps< -1.5f;
            Vector3 forward=Vector3.ProjectOnPlane(target.forward,Vector3.up).normalized;
            if(forward.sqrMagnitude<.1f)forward=followForward;
            if(reversing)forward=-forward;
            float yawAnticipation=targetPhysics.LastCommands.Steering*ratio*(targetPhysics.IsDrifting?4:6);
            forward=Quaternion.AngleAxis(yawAnticipation,Vector3.up)*forward;
            followForward=Vector3.Slerp(followForward,forward,1-Mathf.Exp(-dt*(targetPhysics.IsDrifting?9:12))).normalized;
            bool rear=IsLookingBack;bool viewChanged=rear!=lastRearView;lastRearView=rear;
            Vector3 viewForward=rear?-followForward:followForward;
            if(viewChanged)velocity=Vector3.zero;
            float baseDistance=Preset==RacingCameraPreset.Close?5.7f:Preset==RacingCameraPreset.Comfortable?8.5f:7.3f;
            float height=Preset==RacingCameraPreset.Close?2.7f:Preset==RacingCameraPreset.Comfortable?4:3.3f;
            float distance=baseDistance+ratio*1.2f+(targetPhysics.IsBoosting?.45f:0);
            Vector3 look=target.position+Vector3.up*.7f+viewForward*(.8f+ratio*1.4f);
            if(graph!=null&&!reversing&&!rear)
            {
                graph.ProjectPosition(target.position,out _,out float along,out _);
                Vector3 curve=graph.SampleDistance(along+8+ratio*7).Position-target.position;
                curve.y=0;look+=Vector3.ClampMagnitude(curve-followForward*curve.magnitude,3f)*.23f;
            }
            Vector3 anchor=target.position+Vector3.up*1.0f;
            Vector3 desired=target.position-viewForward*distance+Vector3.up*(height+ratio*.25f+(targetPhysics.IsGrounded?0:.35f));
            Vector3 ray=desired-anchor;float allowed=ray.magnitude;
            int count=Physics.SphereCastNonAlloc(anchor,.3f,ray.normalized,hits,allowed,obstacleLayer,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                if(hits[i].collider.attachedRigidbody!=null)continue;
                if(hits[i].distance<allowed)allowed=Mathf.Max(.75f,hits[i].distance-.3f);
            }
            Vector3 corrected=anchor+ray.normalized*allowed;
            Vector3 smooth=viewChanged?corrected:Vector3.SmoothDamp(transform.position,corrected,ref velocity,positionSmoothTime,Mathf.Infinity,dt);
            // Apply obstruction correction after smoothing too; easing must never move the camera through a wall.
            Vector3 finalRay=smooth-anchor;int finalCount=Physics.SphereCastNonAlloc(anchor,.25f,finalRay.normalized,hits,finalRay.magnitude,obstacleLayer,QueryTriggerInteraction.Ignore);
            float finalDistance=finalRay.magnitude;
            for(int i=0;i<finalCount;i++)if(hits[i].collider.attachedRigidbody==null)finalDistance=Mathf.Min(finalDistance,Mathf.Max(.55f,hits[i].distance-.25f));
            impact=Mathf.MoveTowards(impact,0,dt*3);landing=Mathf.MoveTowards(landing,0,dt*1.8f);
            Vector3 shake=new Vector3(Mathf.Sin(Time.time*47),Mathf.Sin(Time.time*53)*.4f,0)*impact*.035f+Vector3.down*landing*.075f;
            transform.position=anchor+finalRay.normalized*finalDistance+shake;
            if((look-transform.position).sqrMagnitude>.01f) {
                Quaternion rotation=Quaternion.LookRotation(look-transform.position,Vector3.up);
                transform.rotation=viewChanged?rotation:Quaternion.Slerp(transform.rotation,rotation,1-Mathf.Exp(-dt*15));
            }
            // Keep the complete vehicle readable when yaw damping lags during a drift.
            Vector3 framing=cam.WorldToViewportPoint(target.position+Vector3.up*.35f);
            if(framing.z>0&&(framing.x<.22f||framing.x>.78f||framing.y<.22f||framing.y>.60f))
                transform.rotation=Quaternion.LookRotation(target.position+Vector3.up*.65f+viewForward*.6f-transform.position,Vector3.up);
            float fov=(Preset==RacingCameraPreset.Comfortable?59:63)+ratio*9+(targetPhysics.IsBoosting?3:0);
            cam.fieldOfView=Mathf.Lerp(cam.fieldOfView,fov,1-Mathf.Exp(-dt*5));
        }
        void Impact(float value){impact=Mathf.Max(impact,value);}
        void Land(float value){landing=value;}
        void Unsubscribe(){if(targetPhysics!=null){targetPhysics.OnImpact-=Impact;targetPhysics.OnLanding-=Land;}}
        void OnDestroy(){Unsubscribe();}
    }
}
