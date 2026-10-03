using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingHUD:MonoBehaviour
    {
        [SerializeField] RaceManager raceManager;
        RectTransform root;Text positionText,lapText,speedText,timerText,countdownText,itemText,driftText,bannerText;RacingEmblem itemIcon;Image chargeFill;
        GameObject lobby,controls,banner,pause;PlayerRacingInput input;RacingVehiclePhysics vehicle;ItemInventory inventory;RacerProgress progress;
        float nextRefresh,countdownAge,bannerUntil,positionPulse;int lastPosition=12,lastLap=1,lastCount=-1;bool paused;
        public RacingRaceMap Map{get;private set;}
        void Start()
        {
            if(raceManager==null)raceManager=FindFirstObjectByType<RaceManager>();
            root=RacingDesign.Canvas("Racing HUD",30);
            var pos=RacingDesign.Panel(root,"Position",.04f,.77f,.13f,.18f,RacingDesign.Surface);positionText=RacingDesign.Label(pos.transform,"12TH",RacingType.Position,.08f,.08f,.80f,.80f,RacingDesign.Ink);
            RacingDesign.Label(pos.transform,"/ 12",RacingType.Caption,.73f,.025f,.23f,.18f,RacingDesign.Muted,TextAnchor.MiddleRight);
            lapText=RacingDesign.Label(root,"LAP 1 / 3",RacingType.Section,.19f,.88f,.24f,.06f,RacingDesign.Ink);
            timerText=RacingDesign.Label(root,"00:00.000",RacingType.Timer,.19f,.81f,.25f,.05f,RacingDesign.Muted);
            var slot=RacingDesign.Button(root,"",.83f,.75f,.12f,.20f,()=>input?.TriggerTouchItem(),RacingDesign.Surface);
            itemIcon=RacingDesign.Area(slot.transform,"Item silhouette",.18f,.28f,.64f,.60f).gameObject.AddComponent<RacingEmblem>();itemIcon.color=RacingDesign.Cyan;itemIcon.raycastTarget=false;
            itemText=RacingDesign.Label(slot.transform,"EMPTY",RacingType.Caption,.05f,.06f,.90f,.2f,RacingDesign.Muted,TextAnchor.MiddleCenter);
            RacingDesign.Label(root,"E / LB / TAP",RacingType.Caption,.825f,.705f,.13f,.04f,RacingDesign.Muted,TextAnchor.MiddleCenter);
            var speed=RacingDesign.Panel(root,"Speed",.83f,.08f,.12f,.13f,RacingDesign.Surface);speedText=RacingDesign.Label(speed.transform,"0",RacingType.Mmr,.10f,.20f,.63f,.70f,RacingDesign.Ink);
            RacingDesign.Label(speed.transform,"KM/H",RacingType.Caption,.62f,.15f,.32f,.25f,RacingDesign.Muted);
            var charge=RacingDesign.Area(root,"Drift charge",.34f,.06f,.32f,.018f).gameObject.AddComponent<Image>();charge.color=new Color(.05f,.13f,.18f,.6f);
            chargeFill=RacingDesign.Area(charge.transform,"Charge",0,0,0,1).gameObject.AddComponent<Image>();chargeFill.color=RacingDesign.Cyan;
            driftText=RacingDesign.Label(root,"",RacingType.Caption,.34f,.085f,.32f,.04f,RacingDesign.Cyan,TextAnchor.MiddleCenter);
            countdownText=RacingDesign.Label(root,"",RacingType.Position,.32f,.34f,.36f,.28f,RacingDesign.Ink,TextAnchor.MiddleCenter);countdownText.fontSize=132;countdownText.resizeTextMaxSize=132;countdownText.resizeTextMinSize=110;
            var message=RacingDesign.Panel(root,"Race announcement",.25f,.47f,.50f,.16f,RacingDesign.Surface);banner=message.gameObject;bannerText=RacingDesign.Label(message.transform,"",RacingType.Title,.05f,.10f,.90f,.80f,RacingDesign.Ink,TextAnchor.MiddleCenter);banner.SetActive(false);
            var mapRect=RacingDesign.Area(root,"Race map",.04f,.52f,.14f,.20f);Map=mapRect.gameObject.AddComponent<RacingRaceMap>();Map.color=RacingDesign.Ink;Map.gameObject.SetActive(PlayerPrefs.GetInt("AHR.Racing.Map",0)==1);
            var pauseButton=RacingDesign.Button(root,"II",.47f,.90f,.055f,.06f,TogglePause);var nav=pauseButton.navigation;nav.mode=Navigation.Mode.None;pauseButton.navigation=nav;
            raceManager.OnCountdownTick+=Countdown;raceManager.OnStateChanged+=StateChanged;
            BuildLobby();
        }
        void Bind()
        {
            if(vehicle!=null||raceManager.PlayerVehicle==null)return;
            vehicle=raceManager.PlayerVehicle;input=vehicle.GetComponent<PlayerRacingInput>();inventory=vehicle.GetComponent<ItemInventory>();progress=raceManager.ProgressTracker.GetPlayerProgress();
            raceManager.ProgressTracker.OnRacerLapCompleted+=Lap;Map.Bind(raceManager.ProgressTracker);
            vehicle.OnTrick += HandleTrick;
            vehicle.OnLandingBoost += HandleLandingBoost;
            vehicle.OnJump += HandleJump;
            BuildControls();
        }
        void HandleTrick(int count)
        {
            Announce(count > 1 ? $"STUNT COMBO x{count}!" : "AIR STUNT!", 0.8f);
            RacingFeedback.Play(RacingCue.DriftCharge);
        }
        void HandleLandingBoost()
        {
            Announce("LANDING BOOST!", 1.0f);
            RacingFeedback.Play(RacingCue.Boost);
        }
        void HandleJump()
        {
            RacingFeedback.Play(RacingCue.Landing);
        }
        void Update()
        {
            if(raceManager==null)return;Bind();
            if((Keyboard.current?.escapeKey.wasPressedThisFrame==true||Gamepad.current?.startButton.wasPressedThisFrame==true)&&raceManager.State==RaceState.Racing)TogglePause();
            countdownAge+=Time.unscaledDeltaTime;if(lastCount>=0){float t=Mathf.Clamp01(countdownAge/.28f);countdownText.rectTransform.localScale=Vector3.one*Mathf.Lerp(1.35f,1,1-Mathf.Pow(1-t,3));if(lastCount==0&&countdownAge>1f)countdownText.text="";}
            if(banner.activeSelf&&Time.unscaledTime>bannerUntil)banner.SetActive(false);
            if(lobby!=null){lobby.SetActive(raceManager.State==RaceState.Preparing);UpdateLobby();}
            if(vehicle==null)return;
            float pulse=Mathf.Clamp01((positionPulse-Time.unscaledTime)*4);positionText.rectTransform.localScale=Vector3.one*(1+pulse*.08f);
            chargeFill.rectTransform.anchorMax=new Vector2(vehicle.DriftCharge,1);chargeFill.rectTransform.offsetMax=Vector2.zero;
            if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.12f;
            int place=progress.RacePosition;positionText.text=Ordinal(place);positionText.color=place<lastPosition?RacingDesign.Success:place<=3?RacingDesign.Gold:RacingDesign.Ink;
            if(place!=lastPosition){positionPulse=Time.unscaledTime+.28f;if(place<lastPosition)RacingFeedback.Play(RacingCue.PositionUp);lastPosition=place;}
            lapText.text=$"LAP {Mathf.Min(progress.CurrentLap,raceManager.Rules.TotalLaps)} / {raceManager.Rules.TotalLaps}";timerText.text=RacerResultEntry.FormatTime(raceManager.RaceElapsed);speedText.text=Mathf.RoundToInt(vehicle.SpeedKmh).ToString();
            var stage=vehicle.CurrentMiniTurboStage;Color color=stage==MiniTurboStage.UltraMiniTurbo?RacingDesign.Apex:stage==MiniTurboStage.SuperMiniTurbo?RacingDesign.Gold:RacingDesign.Cyan;chargeFill.color=color;driftText.color=color;
            driftText.text=vehicle.IsBoosting?"TURBO ACTIVE":vehicle.IsDrifting?stage==MiniTurboStage.UltraMiniTurbo?"OVERDRIVE":stage==MiniTurboStage.SuperMiniTurbo?"SURGE":stage==MiniTurboStage.MiniTurbo?"FLOW":"CHARGING":"";
            var item=inventory.IsRolling?(RaceItemType)(1+Mathf.FloorToInt((1-Mathf.Pow(1-inventory.RouletteProgress,2.7f))*27)%6):inventory.HeldItem;
            if(itemIcon.Item!=item){itemIcon.Item=item;itemIcon.SetVerticesDirty();}
            itemIcon.color=inventory.IsRolling?RacingDesign.Gold:inventory.HasItem?RacingDesign.Cyan:RacingDesign.Muted;itemText.text=inventory.IsRolling?"CHARGING":inventory.HasItem?RacingDesign.ItemName(item):inventory.HasShield?"SHIELDED":"EMPTY";
        }
        public static string Ordinal(int position)=>position==1?"1ST":position==2?"2ND":position==3?"3RD":position+"TH";
        void Countdown(int count){lastCount=count;countdownAge=0;countdownText.text=count==0?"GO!":count.ToString();countdownText.color=count==0?RacingDesign.Cyan:RacingDesign.Ink;RacingFeedback.Play(count==0?RacingCue.Go:RacingCue.Countdown);}
        void Lap(RacerProgress racer,int lap)
        {
            if(!racer.Identity.isHuman)return;
            if(lap==raceManager.Rules.TotalLaps){Announce("FINAL LAP",2.4f);RacingFeedback.Play(RacingCue.FinalLap);RacingFeedback.Music(RacingMusicPhase.FinalLap);}else Announce("LAP "+lap,1.3f);
        }
        void Announce(string text,float seconds){banner.SetActive(true);bannerText.text=text;bannerUntil=Time.unscaledTime+seconds;var motion=banner.GetComponent<RacingScreenMotion>();if(motion==null)motion=banner.AddComponent<RacingScreenMotion>();motion.Restart();}
        void StateChanged(RaceState state)
        {
            if(state==RaceState.Racing)RacingFeedback.Music(RacingMusicPhase.Race);
            if(state==RaceState.PlayerFinished){Announce("FINISH!  "+Ordinal(progress.RacePosition),3f);RacingFeedback.Play(RacingCue.Finish);}
            if(state==RaceState.Results){root.gameObject.SetActive(false);if(controls!=null)controls.SetActive(false);RacingFeedback.Music(RacingMusicPhase.Results);}
        }
        Text[] lobbyNames=new Text[12];
        void BuildLobby()
        {
            var panel=RacingDesign.Panel(root,"Grid assembly",.12f,.18f,.76f,.63f,RacingDesign.Surface);lobby=panel.gameObject;
            RacingDesign.Label(panel.transform,"ASSEMBLING THE GRID",RacingType.Title,.06f,.78f,.88f,.16f,RacingDesign.Ink,TextAnchor.MiddleCenter);
            RacingDesign.Label(panel.transform,"RANKED / OFFLINE OPPONENTS / NIGHT CITY",RacingType.Caption,.05f,.70f,.90f,.07f,RacingDesign.Cyan,TextAnchor.MiddleCenter);
            for(int i=0;i<12;i++)lobbyNames[i]=RacingDesign.Label(panel.transform,"",RacingType.Body,.05f+(i%3)*.31f,.53f-(i/3)*.13f,.28f,.10f,RacingDesign.Ink);
        }
        void UpdateLobby(){if(raceManager.LobbyRacers==null)return;for(int i=0;i<12;i++)lobbyNames[i].text=i<raceManager.LobbyRevealCount?$"{raceManager.LobbyRacers[i].displayName}\n{raceManager.LobbyRacers[i].mmr:N0} MMR":"";}
        void BuildControls()
        {
            var rt=RacingDesign.Area(root,"Touch controls",0,0,1,1);controls=rt.gameObject;controls.SetActive(Application.isMobilePlatform||PlayerPrefs.GetInt("AHR.Racing.ShowTouch",0)==1);
            Hold(rt,"<",.04f,.07f,.09f,.14f,pressed=>input.SetTouchSteer(pressed?-1:0));Hold(rt,">",.145f,.07f,.09f,.14f,pressed=>input.SetTouchSteer(pressed?1:0));
            Hold(rt,"GAS",.72f,.23f,.12f,.13f,input.SetTouchGas);Hold(rt,"BRAKE",.86f,.23f,.10f,.13f,input.SetTouchBrake);Hold(rt,"DRIFT",.58f,.23f,.12f,.13f,input.SetTouchDrift);
            Hold(rt,"JUMP",.45f,.23f,.11f,.13f,input.SetTouchJump);
        }
        void Hold(Transform parent,string label,float x,float y,float w,float h,System.Action<bool> action)
        {
            var panel=RacingDesign.Panel(parent,label,x,y,w,h,new Color(.07f,.15f,.2f,.65f));panel.raycastTarget=true;RacingDesign.Label(panel.transform,label,RacingType.Body,.06f,.1f,.88f,.8f,RacingDesign.Ink,TextAnchor.MiddleCenter);panel.gameObject.AddComponent<RacingHoldControl>().Action=action;
        }
        public void TogglePause()
        {
            if(raceManager.State!=RaceState.Racing)return;
            paused=!paused;Time.timeScale=paused?0:1;input?.SetInputEnabled(!paused);
            if(!paused){if(pause!=null)Destroy(pause);EventSystem.current?.SetSelectedGameObject(null);return;}
            var panel=RacingDesign.Panel(root,"Pause",.28f,.2f,.44f,.58f,RacingDesign.Surface);pause=panel.gameObject;
            RacingDesign.Label(panel.transform,"PAUSED",RacingType.Title,.1f,.74f,.8f,.16f,RacingDesign.Ink,TextAnchor.MiddleCenter);
            var resume=RacingDesign.Button(panel.transform,"RESUME",.1f,.5f,.8f,.14f,TogglePause);RacingDesign.Button(panel.transform,"RESTART",.1f,.3f,.8f,.14f,()=>{Time.timeScale=1;raceManager.RetryRace();});
            RacingDesign.Button(panel.transform,"RACING HUB",.1f,.1f,.8f,.14f,()=>{Time.timeScale=1;raceManager.ExitToMenu();});RacingDesign.Focus(resume);
        }
        void OnDestroy()
        {
            if(vehicle!=null){vehicle.OnTrick-=HandleTrick;vehicle.OnLandingBoost-=HandleLandingBoost;vehicle.OnJump-=HandleJump;}
            if(raceManager!=null){raceManager.OnCountdownTick-=Countdown;raceManager.OnStateChanged-=StateChanged;if(raceManager.ProgressTracker!=null)raceManager.ProgressTracker.OnRacerLapCompleted-=Lap;}
            if(paused)Time.timeScale=1;
        }
    }
    public sealed class RacingHoldControl:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
    {
        public System.Action<bool> Action;
        int pointer=int.MinValue;
        public void OnPointerDown(PointerEventData e){if(pointer!=int.MinValue)return;pointer=e.pointerId;Action?.Invoke(true);}
        public void OnPointerUp(PointerEventData e){if(e.pointerId!=pointer)return;pointer=int.MinValue;Action?.Invoke(false);}
        public void OnPointerExit(PointerEventData e){OnPointerUp(e);}
        void OnDisable(){pointer=int.MinValue;Action?.Invoke(false);}
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RacingRaceMap:MaskableGraphic
    {
        TrackProgressTracker tracker;float nextRefresh;Vector2 min,max;
        public void Bind(TrackProgressTracker progress)
        {
            tracker=progress;min=new Vector2(float.MaxValue,float.MaxValue);max=new Vector2(float.MinValue,float.MinValue);
            for(int i=0;i<tracker.TrackGraph.NodeCount;i++){var p=tracker.TrackGraph.GetNode(i).Position;min=Vector2.Min(min,new Vector2(p.x,p.z));max=Vector2.Max(max,new Vector2(p.x,p.z));}
        }
        void Update(){if(Time.unscaledTime>nextRefresh){nextRefresh=Time.unscaledTime+.1f;SetVerticesDirty();}}
        Vector2 Project(Vector3 p){Rect r=rectTransform.rect;float scale=Mathf.Min(r.width/Mathf.Max(1,max.x-min.x),r.height/Mathf.Max(1,max.y-min.y))*.86f;return r.center+(new Vector2(p.x,p.z)-(min+max)*.5f)*scale;}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(tracker==null)return;
            for(int i=0;i<tracker.TrackGraph.NodeCount;i++)RacingEmblem.Line(vh,Project(tracker.TrackGraph.GetNode(i).Position),Project(tracker.TrackGraph.GetNode(i+1).Position),2,new Color(.65f,.78f,.82f,.55f));
            foreach(var racer in tracker.Leaderboard){Vector2 p=Project(racer.Vehicle.transform.position);float size=racer.Identity.isHuman?5:3;RacingEmblem.Line(vh,p-Vector2.right*size,p+Vector2.right*size,size*2,racer.Identity.isHuman?RacingDesign.Secondary:RacingDesign.Cyan);}
        }
    }
}
