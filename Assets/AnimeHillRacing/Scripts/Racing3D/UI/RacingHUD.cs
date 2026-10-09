using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using AnimeHillRacing.Core;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingHUD:MonoBehaviour
    {
        [SerializeField] RaceManager raceManager;
        RectTransform root;Text positionText,lapText,speedText,timerText,countdownText,itemText,driftText,bannerText;RacingEmblem itemIcon;Image chargeFill;
        GameObject lobby,controls,banner,pause,rearIndicator;Text walletText;PlayerRacingInput input;RacingVehiclePhysics vehicle;ItemInventory inventory;RacerProgress progress;
        float nextRefresh,countdownAge,bannerUntil,positionPulse;int lastPosition=12,lastCount=-1;bool paused;
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
            RacingDesign.Panel(slot.transform,"Item charge rim",.05f,.95f,.9f,.025f,RacingDesign.Cyan,0);
            RacingDesign.Label(root,"LMB / LB / TAP",RacingType.Caption,.825f,.705f,.13f,.04f,RacingDesign.Muted,TextAnchor.MiddleCenter);
            walletText=RacingDesign.Label(root,"",RacingType.Caption,.74f,.945f,.21f,.035f,RacingDesign.Gold,TextAnchor.MiddleRight);
            RefreshWallet();SaveManager.CurrencyChanged+=RefreshWallet;
            var rear=RacingDesign.Panel(root,"Rear view active",.43f,.81f,.20f,.05f,RacingDesign.Surface);
            RacingDesign.Label(rear.transform,"◀ REAR VIEW ▶",RacingType.Caption,.04f,.06f,.92f,.88f,RacingDesign.Cyan,TextAnchor.MiddleCenter);
            rearIndicator=rear.gameObject;rearIndicator.SetActive(false);
            RacingDesign.Label(root,"RMB / R3 — REAR VIEW",RacingType.Caption,.34f,.018f,.32f,.028f,RacingDesign.Muted,TextAnchor.MiddleCenter);
            var speed=RacingDesign.Panel(root,"Speed",.83f,.08f,.12f,.13f,RacingDesign.Surface);speedText=RacingDesign.Label(speed.transform,"0",RacingType.Mmr,.10f,.20f,.63f,.70f,RacingDesign.Ink);
            RacingDesign.Label(speed.transform,"KM/H",RacingType.Caption,.62f,.15f,.32f,.25f,RacingDesign.Muted);
            var charge=RacingDesign.Area(root,"Drift charge",.34f,.06f,.32f,.018f).gameObject.AddComponent<Image>();charge.color=new Color(.05f,.13f,.18f,.6f);
            chargeFill=RacingDesign.Area(charge.transform,"Charge",0,0,0,1).gameObject.AddComponent<Image>();chargeFill.color=RacingDesign.Cyan;
            foreach(float tick in new[]{RacingDriftFeedback.GreenCharge,RacingDriftFeedback.CyanCharge})
                RacingDesign.Panel(charge.transform,"Turbo threshold",tick/RacingDriftFeedback.PurpleCharge,0,.007f,1,RacingDesign.Ink,0);
            driftText=RacingDesign.Label(root,"",RacingType.Caption,.34f,.085f,.32f,.04f,RacingDesign.Cyan,TextAnchor.MiddleCenter);
            countdownText=RacingDesign.Label(root,"",RacingType.Position,.32f,.34f,.36f,.28f,RacingDesign.Ink,TextAnchor.MiddleCenter);countdownText.fontSize=132;countdownText.resizeTextMaxSize=132;countdownText.resizeTextMinSize=110;
            var message=RacingDesign.Panel(root,"Race announcement",.30f,.21f,.40f,.07f,RacingDesign.Surface);banner=message.gameObject;bannerText=RacingDesign.Label(message.transform,"",RacingType.Section,.05f,.08f,.90f,.84f,RacingDesign.Ink,TextAnchor.MiddleCenter);banner.SetActive(false);
            
            // Full track minimap panel
            var mapPanel=RacingDesign.Panel(root,"Minimap card",.035f,.46f,.16f,.28f,new Color(.02f,.06f,.09f,.85f));
            RacingDesign.Panel(mapPanel.transform,"Minimap rim",.04f,.96f,.92f,.02f,RacingDesign.Cyan,0);
            var mapRect=RacingDesign.Area(mapPanel.transform,"Race map",.06f,.06f,.88f,.88f);
            Map=mapRect.gameObject.AddComponent<RacingRaceMap>();
            Map.color=RacingDesign.Ink;
            mapPanel.gameObject.SetActive(PlayerPrefs.GetInt("AHR.Racing.Map",1)==1);

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
            vehicle.OnMiniTurboReleased += HandleMiniTurbo;
            inventory.OnItemUsed += HandleItemUsed;
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
            Announce("JUMP!  J / A — AIR TRICK",.65f);
            RacingFeedback.Play(RacingCue.Landing);
        }
        void RefreshWallet(){if(walletText!=null)walletText.text="COINS "+SaveManager.TotalCoins.ToString("N0");}
        void HandleItemUsed(RaceItemType item)
        {
            Announce(item==RaceItemType.Coin10?"COIN +10 — SHARED WALLET":RaceItemUtility.DisplayName(item)+"!",.8f);
            bannerText.color=RaceItemUtility.ColorFor(item);
        }
        void HandleMiniTurbo(MiniTurboStage stage)
        {
            Announce(stage==MiniTurboStage.UltraMiniTurbo?"SUPER TURBO!":stage==MiniTurboStage.SuperMiniTurbo?"MEDIUM TURBO!":"MINI TURBO!",.9f);
            bannerText.color=RacingDriftFeedback.ColorForStage(stage);
            RacingFeedback.Play(RacingCue.MiniTurbo);
        }
        void Update()
        {
            if(raceManager==null)return;Bind();
            if((Keyboard.current?.escapeKey.wasPressedThisFrame==true||Gamepad.current?.startButton.wasPressedThisFrame==true)&&raceManager.State==RaceState.Racing)TogglePause();
            countdownAge+=Time.unscaledDeltaTime;if(lastCount>=0){float t=Mathf.Clamp01(countdownAge/.28f);countdownText.rectTransform.localScale=Vector3.one*Mathf.Lerp(1.35f,1,1-Mathf.Pow(1-t,3));if(lastCount==0&&countdownAge>1f)countdownText.text="";}
            if(banner.activeSelf&&Time.unscaledTime>bannerUntil)banner.SetActive(false);
            if(lobby!=null){bool preparing=raceManager.State==RaceState.Preparing;lobby.SetActive(preparing);if(preparing)UpdateLobby();}
            if(vehicle==null)return;
            bool rearActive=input!=null&&input.LookBack&&vehicle.RaceEnabled&&!paused;
            if(rearIndicator.activeSelf!=rearActive)rearIndicator.SetActive(rearActive);
            float pulse=Mathf.Clamp01((positionPulse-Time.unscaledTime)*4);positionText.rectTransform.localScale=Vector3.one*(1+pulse*.08f);
            chargeFill.rectTransform.anchorMax=new Vector2(vehicle.DriftCharge,1);chargeFill.rectTransform.offsetMax=Vector2.zero;
            if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.12f;
            int place=progress.RacePosition;positionText.text=Ordinal(place);positionText.color=place<lastPosition?RacingDesign.Success:place<=3?RacingDesign.Gold:RacingDesign.Ink;
            if(place!=lastPosition){positionPulse=Time.unscaledTime+.28f;if(place<lastPosition)RacingFeedback.Play(RacingCue.PositionUp);lastPosition=place;}
            lapText.text=$"LAP {Mathf.Min(progress.CurrentLap,raceManager.Rules.TotalLaps)} / {raceManager.Rules.TotalLaps}";timerText.text=RacerResultEntry.FormatTime(raceManager.RaceElapsed);speedText.text=Mathf.RoundToInt(vehicle.SpeedKmh).ToString();
            var stage=vehicle.CurrentMiniTurboStage;Color color=RacingDriftFeedback.ColorForStage(stage);chargeFill.color=color;driftText.color=color;
            driftText.text=vehicle.IsLancerActive?$"LANCER / {vehicle.EffectiveLancerTargetKmh:0} KM/H / {vehicle.LancerRemainingSeconds:0.0}S":vehicle.IsMiniHopping?"MINI HOP / HOLD TO DRIFT":vehicle.IsDrifting?RacingDriftFeedback.ChargeLabel(stage):vehicle.IsBoosting?"TURBO ACTIVE":"SHIFT: HOP / HOLD DRIFT     J / A: JUMP";
            if(vehicle.IsLancerActive)driftText.color=RaceItemUtility.ColorFor(RaceItemType.Lancer);
            var candidates=ItemDistributionTable.WeightsForPosition(progress.RacePosition);
            var item=inventory.IsRolling?candidates[Mathf.FloorToInt((1-Mathf.Pow(1-inventory.RouletteProgress,2.7f))*53)%candidates.Length].Type:inventory.HeldItem;
            if(!inventory.HasItem&&!inventory.IsRolling&&vehicle.IsLancerActive)item=RaceItemType.Lancer;
            if(itemIcon.Item!=item){itemIcon.Item=item;itemIcon.SetVerticesDirty();}
            itemIcon.color=inventory.IsRolling?RacingDesign.Gold:inventory.HasItem||vehicle.IsLancerActive?RaceItemUtility.ColorFor(item):RacingDesign.Muted;itemText.text=inventory.IsRolling?"CHARGING":inventory.HasItem?RacingDesign.ItemName(item):vehicle.IsLancerActive?"LANCER ACTIVE":inventory.HasShield?"SHIELDED":"EMPTY";
        }
        public static string Ordinal(int position)=>position==1?"1ST":position==2?"2ND":position==3?"3RD":position+"TH";
        void Countdown(int count){lastCount=count;countdownAge=0;countdownText.text=count==0?"GO!":count.ToString();countdownText.color=count==0?RacingDesign.Cyan:RacingDesign.Ink;RacingFeedback.Play(count==0?RacingCue.Go:RacingCue.Countdown);}
        void Lap(RacerProgress racer,int lap)
        {
            if(!racer.Identity.isHuman)return;
            if(lap==raceManager.Rules.TotalLaps){Announce("FINAL LAP",2.4f);RacingFeedback.Play(RacingCue.FinalLap);RacingFeedback.Music(RacingMusicPhase.FinalLap);}else Announce("LAP "+lap,1.3f);
        }
        void Announce(string text,float seconds){banner.SetActive(true);bannerText.text=text;bannerText.color=RacingDesign.Ink;bannerUntil=Time.unscaledTime+seconds;var motion=banner.GetComponent<RacingScreenMotion>();if(motion==null)motion=banner.AddComponent<RacingScreenMotion>();motion.Restart();}
        void StateChanged(RaceState state)
        {
            if(state==RaceState.Racing)RacingFeedback.Music(RacingMusicPhase.Race);
            if(state==RaceState.PlayerFinished){Announce("FINISH!  "+Ordinal(progress.RacePosition),3f);RacingFeedback.Play(RacingCue.Finish);}
            if(state==RaceState.Results){root.gameObject.SetActive(false);if(controls!=null)controls.SetActive(false);RacingFeedback.Music(RacingMusicPhase.Results);}
        }
        Text[] lobbyNames=new Text[12], lobbyTitles=new Text[12];
        RacingTitleView[] lobbyTitleViews=new RacingTitleView[12]; RacingRankBadge[] lobbyBadges=new RacingRankBadge[12];
        void BuildLobby()
        {
            var panel=RacingDesign.Panel(root,"Grid assembly",.12f,.18f,.76f,.63f,RacingDesign.Surface);lobby=panel.gameObject;
            RacingDesign.Label(panel.transform,"ASSEMBLING THE GRID",RacingType.Title,.06f,.78f,.88f,.16f,RacingDesign.Ink,TextAnchor.MiddleCenter);
            RacingDesign.Label(panel.transform,"RANKED / OFFLINE OPPONENTS / "+RacingTrackSelection.ShortName,RacingType.Caption,.05f,.70f,.90f,.07f,RacingDesign.Cyan,TextAnchor.MiddleCenter);
            for(int i=0;i<12;i++) {
                float x=.04f+(i%3)*.315f,y=.53f-(i/3)*.13f;
                lobbyBadges[i]=RacingDesign.RankBadge(panel.transform,0,x,y,.055f,.095f);
                lobbyNames[i]=RacingDesign.Label(panel.transform,"",RacingType.Caption,x+.061f,y+.025f,.235f,.075f,RacingDesign.Ink);
                lobbyTitles[i]=RacingDesign.Label(panel.transform,"",RacingType.Caption,x+.061f,y-.007f,.235f,.027f,RacingDesign.Muted);
                lobbyTitles[i].fontSize=12;lobbyTitleViews[i]=lobbyTitles[i].gameObject.AddComponent<RacingTitleView>();
            }
        }
        int lastLobbyReveal=-1;
        void UpdateLobby() {
            if(raceManager.LobbyRacers==null || lastLobbyReveal==raceManager.LobbyRevealCount)return;
            lastLobbyReveal=raceManager.LobbyRevealCount;
            for(int i=0;i<12;i++) {
                bool shown=i<lastLobbyReveal;lobbyBadges[i].gameObject.SetActive(shown);
                if(!shown){lobbyNames[i].text=lobbyTitles[i].text="";continue;}
                var identity=raceManager.LobbyRacers[i];lobbyBadges[i].Configure(identity.mmr);
                lobbyNames[i].text=$"{identity.displayName} / {identity.mmr:N0}\n{MmrTierUtility.GetDisplayName(identity.mmr)}";
                lobbyTitleViews[i].Configure(lobbyTitles[i],identity.titleId,RacingProgressionService.State.currentSeasonNumber,identity.mmr);
            }
        }

        void BuildControls()
        {
            var mobile = RacingMobileControls.Create(root, input);
            controls = mobile.gameObject;
            controls.SetActive(Application.isMobilePlatform || PlayerPrefs.GetInt("AHR.Racing.ShowTouch", 0) == 1);
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
            SaveManager.CurrencyChanged-=RefreshWallet;
            if(inventory!=null)inventory.OnItemUsed-=HandleItemUsed;
            if(vehicle!=null){vehicle.OnTrick-=HandleTrick;vehicle.OnLandingBoost-=HandleLandingBoost;vehicle.OnJump-=HandleJump;vehicle.OnMiniTurboReleased-=HandleMiniTurbo;}
            if(raceManager!=null){raceManager.OnCountdownTick-=Countdown;raceManager.OnStateChanged-=StateChanged;if(raceManager.ProgressTracker!=null)raceManager.ProgressTracker.OnRacerLapCompleted-=Lap;}
            if(paused)Time.timeScale=1;
        }
    }


}
