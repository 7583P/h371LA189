using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingGarageUI:MonoBehaviour
    {
        static readonly string[] VehicleIds={"car-01","rally-01","formula-01","buggy-01","sport-bike-01","hill-adventurer-01","aerial-satelite-01"};
        static readonly string[] WheelIds={"wheel-default","wheel-sport","wheel-offroad","wheel-racing","wheel-carbon","wheel-plasma"};
        static readonly string[] WheelNames={"ALL TERRAIN","SPORT SLICK","HEAVY OFFROAD","PRO RACING","LIGHT CARBON","PLASMA TURBO"};
        static readonly string[] StatNames={"SPEED","ACCELERATION","HANDLING","DRIFT","GRIP","WEIGHT","MINI TURBO","OFFROAD","STABILITY"};
        int vehicleIndex,wheelIndex;FinalRacingStats stats;RacingShowroom preview;Text vehicleText,wheelText,deltaText;Image[] fills=new Image[9];float[] displayed=new float[9],desired=new float[9];RectTransform root,rankedPresentation;
        void Start()
        {
            RacingSaveManager.EnsureMigration();vehicleIndex=Mathf.Max(0,Array.IndexOf(VehicleIds,RacingStatsCalculator.NormalizeVehicleId(RacingSaveManager.SelectedVehicleId)));wheelIndex=Mathf.Max(0,Array.IndexOf(WheelIds,RacingSaveManager.SelectedWheelId));
            root=RacingDesign.Canvas("Racing garage");root.gameObject.AddComponent<RacingScreenMotion>();gameObject.AddComponent<RacingQualityScope>();RacingFeedback.Ensure();
            RacingDesign.Panel(root,"Backdrop",0,0,1,1,RacingDesign.Background,0);
            RacingDesign.Label(root,"RACING / GARAGE",RacingType.Section,.05f,.87f,.55f,.07f,RacingDesign.Cyan);
            RacingDesign.Label(root,"MAKE IT YOURS.",RacingType.Title,.05f,.78f,.57f,.08f,RacingDesign.Ink,display:true);
            preview=RacingShowroom.Create(root,.045f,.225f,.54f,.535f,VehicleIds[vehicleIndex],WheelIds[wheelIndex]);
            RacingDesign.Label(root,"DRAG TO ROTATE / SCROLL TO ZOOM",RacingType.Caption,.07f,.18f,.48f,.04f,RacingDesign.Muted,TextAnchor.MiddleCenter);
            var statsPanel=RacingDesign.Panel(root,"Build performance",.62f,.23f,.32f,.57f,RacingDesign.Surface);
            RacingDesign.Label(statsPanel.transform,"PERFORMANCE",RacingType.Section,.06f,.88f,.88f,.085f,RacingDesign.Ink);
            for(int i=0;i<9;i++)
            {
                float y=.77f-i*.077f;RacingDesign.Label(statsPanel.transform,StatNames[i],RacingType.Caption,.07f,y,.38f,.055f,RacingDesign.Muted);
                var bg=RacingDesign.Area(statsPanel.transform,StatNames[i]+" track",.46f,y+.013f,.46f,.03f).gameObject.AddComponent<Image>();bg.color=RacingDesign.Highlight;
                var fill=RacingDesign.Area(bg.transform,"Fill",0,0,1,1).gameObject.AddComponent<Image>();fill.color=RacingDesign.Cyan;fills[i]=fill;
            }
            deltaText=RacingDesign.Label(root,"",RacingType.Caption,.63f,.175f,.30f,.05f,RacingDesign.Success);
            RacingDesign.Button(root,"<",.08f,.105f,.055f,.07f,PreviousVehicle);
            vehicleText=RacingDesign.Label(root,"",RacingType.Section,.145f,.10f,.36f,.075f,RacingDesign.Ink,TextAnchor.MiddleCenter);
            RacingDesign.Button(root,">",.51f,.105f,.055f,.07f,NextVehicle);
            RacingDesign.Button(root,"<",.64f,.10f,.05f,.07f,PreviousWheel);
            wheelText=RacingDesign.Label(root,"",RacingType.Body,.70f,.10f,.18f,.075f,RacingDesign.Ink,TextAnchor.MiddleCenter);
            RacingDesign.Button(root,">",.89f,.10f,.05f,.07f,NextWheel);
            RacingDesign.Button(root,"RACING HUB",.05f,.015f,.2f,.07f,BackToMainMenu);
            var confirm=RacingDesign.Button(root,"CONFIRM BUILD",.65f,.015f,.29f,.07f,ShowRankedPresentation,RacingDesign.Highlight);
            RecalculateStats(false);RacingDesign.Focus(confirm);
        }
        void Update(){for(int i=0;i<9;i++){displayed[i]=Mathf.Lerp(displayed[i],desired[i],1-Mathf.Exp(-Time.unscaledDeltaTime*14));if(fills[i]!=null){var rt=fills[i].rectTransform;rt.anchorMax=new Vector2(displayed[i],1);rt.offsetMax=Vector2.zero;}}}
        public void NextVehicle(){vehicleIndex=(vehicleIndex+1)%VehicleIds.Length;SaveSelection();}
        public void PreviousVehicle(){vehicleIndex=(vehicleIndex+VehicleIds.Length-1)%VehicleIds.Length;SaveSelection();}
        public void NextWheel(){wheelIndex=(wheelIndex+1)%WheelIds.Length;SaveSelection();}
        public void PreviousWheel(){wheelIndex=(wheelIndex+WheelIds.Length-1)%WheelIds.Length;SaveSelection();}
        void SaveSelection(){RacingSaveManager.SelectedVehicleId=VehicleIds[vehicleIndex];RacingSaveManager.SelectedWheelId=WheelIds[wheelIndex];RecalculateStats(true);preview?.Show(VehicleIds[vehicleIndex],WheelIds[wheelIndex]);}
        void RecalculateStats(bool delta)
        {
            FinalRacingStats previous=stats;stats=RacingStatsCalculator.Calculate(VehicleIds[vehicleIndex],WheelIds[wheelIndex]);
            float[] values={stats.SpeedKmh,stats.AccelerationMps2,stats.HandlingRadPerSec,stats.DriftMultiplier,stats.LateralGrip,stats.WeightKg,stats.MiniTurboMultiplier,stats.OffroadRetention,stats.Stability};
            float[] min={125,9,1.8f,.7f,.8f,500,.7f,.3f,.7f},max={190,20,3.8f,1.8f,2,1300,2.1f,1,1.7f};
            for(int i=0;i<9;i++)desired[i]=Mathf.InverseLerp(min[i],max[i],values[i]);
            if(vehicleText!=null)vehicleText.text=RacingDesign.VehicleName(VehicleIds[vehicleIndex]);if(wheelText!=null)wheelText.text=WheelNames[wheelIndex];
            if(delta&&deltaText!=null)deltaText.text=$"SPEED {stats.SpeedKmh-previous.SpeedKmh:+0;-0;0}   HANDLING {(stats.HandlingRadPerSec-previous.HandlingRadPerSec)*10:+0;-0;0}";
        }
        public void ShowRankedPresentation()
        {
            if(rankedPresentation!=null)return;
            rankedPresentation=RacingDesign.Area(root,"Ranked briefing",0,0,1,1);rankedPresentation.gameObject.AddComponent<RacingScreenMotion>();
            var panel=RacingDesign.Panel(rankedPresentation,"Ranked",.18f,.13f,.64f,.75f,RacingDesign.Surface);
            RacingDesign.Label(panel.transform,"RANKED",RacingType.Display,.08f,.72f,.84f,.18f,RacingDesign.Gold,TextAnchor.MiddleCenter,true);
            int mmr=RacingSaveManager.CurrentMmr;
            RacingDesign.Label(panel.transform,mmr.ToString("N0"),RacingType.Mmr,.25f,.55f,.50f,.15f,RacingDesign.Ink,TextAnchor.MiddleCenter);
            RacingDesign.Label(panel.transform,MmrTierUtility.GetTierDisplayName(MmrTierUtility.GetTier(mmr)),RacingType.Section,.15f,.45f,.70f,.09f,RacingDesign.TierColor(mmr),TextAnchor.MiddleCenter);
            RacingDesign.Label(panel.transform,RacingDesign.VehicleName(VehicleIds[vehicleIndex])+" / "+WheelNames[wheelIndex],RacingType.Body,.1f,.33f,.8f,.08f,RacingDesign.Ink,TextAnchor.MiddleCenter);
            RacingDesign.Label(panel.transform,"CANYON RUSH / 3 LAPS / 1 PLAYER + 11 AI",RacingType.Caption,.1f,.23f,.8f,.07f,RacingDesign.Muted,TextAnchor.MiddleCenter);
            var start=RacingDesign.Button(panel.transform,"START RACE",.34f,.065f,.58f,.10f,StartRankedRace,RacingDesign.Highlight);
            RacingDesign.Button(panel.transform,"BACK",.08f,.065f,.23f,.10f,()=>{Destroy(rankedPresentation.gameObject);rankedPresentation=null;});RacingDesign.Focus(start);
        }
        public void StartRankedRace(){SaveSelection();SceneManager.LoadScene("Racing3D");}
        public void BackToMainMenu(){SceneManager.LoadScene("RacingSelect");}
    }
}
