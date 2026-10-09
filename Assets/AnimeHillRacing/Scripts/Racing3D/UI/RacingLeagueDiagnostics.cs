#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using AnimeHillRacing.Ads;
namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingLeagueDiagnostics : MonoBehaviour
    {
        RectTransform panel; UnityEngine.UI.Text summary; float refresh;
        void Update() {
            if(Keyboard.current!=null && Keyboard.current.f4Key.wasPressedThisFrame && !RacingProgressionService.IsRaceActive)Toggle();
            if(panel==null || RacingProgressionService.IsRaceActive || Time.unscaledTime<refresh)return;refresh=Time.unscaledTime+1;
            var s=RacingProgressionService.State;var b=s.bots;
            var index=RacingProgressionService.Engine.Index;
            summary.text=$"Season {s.currentSeasonNumber} / {b.Length} bots / {s.simulationEventNumber:N0} events\n"+
                $"MMR {RacingProgressionService.Engine.View(index.AtRank(index.Count-1)).currentMmr:N0}–{RacingProgressionService.Engine.View(index.AtRank(0)).currentMmr:N0} / Beyond {index.CountHigher(10000,true)} / queued {RacingProgressionService.PendingDiagnosticEvents}\n"+
                $"Pump {RacingProgressionService.LastPumpMilliseconds:F2} ms / save {RacingProgressionService.LastSaveMilliseconds:F2} ms\n"+
                $"VIP {AdEntitlementService.IsVipActive} / ad-free {AdEntitlementService.IsAdFree} / >10k reset = 10,000";
        }
        void Toggle() {
            if(panel!=null){Destroy(panel.parent.gameObject);panel=null;return;}
            panel=RacingDesign.Canvas("Development local league diagnostics",180);
            RacingDesign.Panel(panel,"Diagnostic background",.05f,.05f,.90f,.90f,RacingDesign.Surface);
            RacingDesign.Label(panel,"DEVELOPMENT DIAGNOSTICS / F4",RacingType.Title,.08f,.83f,.74f,.07f,RacingDesign.Cyan);
            RacingDesign.Button(panel,"CLOSE",.82f,.83f,.10f,.07f,Toggle);
            summary=RacingDesign.Label(panel,"",RacingType.Body,.08f,.64f,.84f,.17f,RacingDesign.Ink);
            string[] names={"+1 DAY","+1 WEEK","+3 MONTHS","NEXT SEASON","10,000 MATCHES","LEADERBOARD","TOP 100 REPORT","SEASON HISTORY","EXPIRE ADS","TEST VIP","TEST +30 DAYS","RESET PREVIEW"};
            for(int i=0;i<names.Length;i++){int action=i;RacingDesign.Button(panel,names[i],.08f+(i%3)*.285f,.52f-(i/3)*.11f,.27f,.085f,()=>Run(action));}
        }
        void Run(int action) {
            if(RacingProgressionService.IsRaceActive)return;
            var s=RacingProgressionService.State;
            switch(action) {
                case 0:RacingProgressionService.DiagnosticAdvance(TimeSpan.FromDays(1));break;
                case 1:RacingProgressionService.DiagnosticAdvance(TimeSpan.FromDays(7));break;
                case 2:var now=new DateTime(s.lastKnownUtcTimestamp,DateTimeKind.Utc);RacingProgressionService.DiagnosticAdvance(now.AddMonths(3)-now);break;
                case 3:RacingProgressionService.DiagnosticAdvance(TimeSpan.FromTicks(Math.Max(0,s.seasonEndTimestamp-s.lastKnownUtcTimestamp)+1));break;
                case 4:RacingProgressionService.DiagnosticSimulate(10000);break;
                case 5:RacingLeaderboardUI.Open();break;
                case 6:var rows=RacingProgressionService.LeaderboardPage(0,100);Debug.Log("LOCAL LEAGUE TOP 100\n"+string.Join("\n",rows.Select(x=>$"{x.id} {x.displayName} {x.currentMmr} {MmrTierUtility.GetDisplayName(x.currentMmr)} / {x.vehiclePreference} + {x.wheelPreference} + {x.characterPreference} + {x.accessoryPreference}")));break;
                case 7:RacingSeasonUI.Open(s.human);break;
                case 8:AdEntitlementService.DiagnosticExpire();break;
                case 9:AdEntitlementService.DiagnosticVip(TimeSpan.FromDays(30));break;
                case 10:AdEntitlementService.DiagnosticPurchase();break;
                case 11:Debug.Log("Local league reset preview\n"+string.Join("\n",new[]{0,1000,5000,9000,9999,10000,10001,12000,15000}.Select(x=>$"{x} -> {RacingProgressionService.Engine.Configuration.ResetMmr(x)}")));break;
            }
        }
    }
}
#endif
