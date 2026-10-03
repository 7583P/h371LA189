using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingResultsUI:MonoBehaviour
    {
        [SerializeField] RaceManager raceManager;
        RaceResults results;RectTransform root;Text mmrText,tierText;Image progress;float age;RacerResultEntry player;
        void Start(){if(raceManager==null)raceManager=FindFirstObjectByType<RaceManager>();if(raceManager!=null)raceManager.OnRaceCompleted+=ShowResults;}
        public void ShowResults(RaceResults raceResults)
        {
            if(root!=null)return;results=raceResults;player=results.GetPlayerEntry();if(player==null)return;
            root=RacingDesign.Canvas("Ranked results",50);root.gameObject.AddComponent<RacingScreenMotion>();RacingDesign.Panel(root,"Backdrop",0,0,1,1,RacingDesign.Background,0);
            RacingDesign.Label(root,"RANKED / "+results.trackDisplayName,RacingType.Section,.05f,.87f,.86f,.065f,RacingDesign.Cyan);
            RacingDesign.Label(root,RacingHUD.Ordinal(player.finishPosition)+" PLACE",RacingType.Display,.05f,.72f,.39f,.13f,RacingDesign.Ink,display:true);
            var summary=RacingDesign.Panel(root,"Progression",.05f,.25f,.33f,.43f,RacingDesign.Surface);
            RacingDesign.Label(summary.transform,"RATING",RacingType.Caption,.08f,.81f,.84f,.08f,RacingDesign.Muted);
            mmrText=RacingDesign.Label(summary.transform,player.oldMmr.ToString("N0"),RacingType.Mmr,.08f,.53f,.84f,.26f,RacingDesign.Ink);
            RacingDesign.Label(summary.transform,$"{player.oldMmr:N0}  {player.mmrDelta:+0;-0;0}  =  {player.newMmr:N0}",RacingType.Body,.08f,.40f,.84f,.11f,player.mmrDelta>=0?RacingDesign.Success:RacingDesign.Danger);
            tierText=RacingDesign.Label(summary.transform,"",RacingType.Section,.08f,.23f,.84f,.13f,RacingDesign.TierColor(player.newMmr));
            var bg=RacingDesign.Area(summary.transform,"Rank progress",.08f,.15f,.84f,.02f).gameObject.AddComponent<Image>();bg.color=RacingDesign.Highlight;
            progress=RacingDesign.Area(bg.transform,"Progress",0,0,1,1).gameObject.AddComponent<Image>();progress.color=RacingDesign.TierColor(player.newMmr);
            RacingDesign.Label(summary.transform,player.newMmr>=10000?"APEX / OPEN ENDED PROGRESSION":"NEXT TIER / KEEP CLIMBING",RacingType.Caption,.08f,.015f,.84f,.09f,RacingDesign.Muted);
            var table=RacingDesign.Panel(root,"Race classification",.42f,.18f,.53f,.64f,RacingDesign.Surface);
            Row(table.transform,"POS","RACER","MMR","VEHICLE","TIME",.91f,RacingDesign.Muted);
            for(int i=0;i<results.entries.Length;i++)
            {
                var entry=results.entries[i];float y=.84f-i*.062f;
                if(entry.isHuman)RacingDesign.Panel(table.transform,"Player highlight",.015f,y-.01f,.97f,.055f,RacingDesign.Highlight,6);
                Row(table.transform,entry.finishPosition.ToString("00"),entry.displayName+(entry.isHuman?" / YOU":""),entry.oldMmr.ToString("N0"),RacingDesign.VehicleName(entry.vehicleId),entry.finished?RacerResultEntry.FormatTime(entry.totalTime):"DNF",y,entry.isHuman?RacingDesign.Cyan:RacingDesign.Ink);
            }
            var retry=RacingDesign.Button(root,"RACE AGAIN",.42f,.055f,.25f,.085f,()=>raceManager.RetryRace(),RacingDesign.Highlight);
            RacingDesign.Button(root,"RACING HUB",.70f,.055f,.25f,.085f,()=>raceManager.ExitToMenu());
            RacingDesign.Button(root,"GARAGE",.05f,.055f,.24f,.085f,()=>SceneManager.LoadScene("RacingGarage"));RacingDesign.Focus(retry);
        }
        static void Row(Transform table,string pos,string name,string mmr,string vehicle,string time,float y,Color color)
        {
            RacingDesign.Label(table,pos,RacingType.Caption,.025f,y,.06f,.05f,color);
            RacingDesign.Label(table,name,RacingType.Caption,.10f,y,.26f,.05f,color);
            RacingDesign.Label(table,mmr,RacingType.Caption,.37f,y,.13f,.05f,color,TextAnchor.MiddleRight);
            RacingDesign.Label(table,vehicle,RacingType.Caption,.53f,y,.25f,.05f,color);
            RacingDesign.Label(table,time,RacingType.Caption,.80f,y,.18f,.05f,color,TextAnchor.MiddleRight);
        }
        void Update()
        {
            if(player==null||mmrText==null)return;age+=Time.unscaledDeltaTime;float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((age-.3f)/1f));int mmr=Mathf.RoundToInt(Mathf.Lerp(player.oldMmr,player.newMmr,t));
            mmrText.text=mmr.ToString("N0");tierText.text=MmrTierUtility.GetTierDisplayName(MmrTierUtility.GetTier(mmr));progress.rectTransform.anchorMax=new Vector2(mmr>=10000?Mathf.Repeat(mmr,1000)/1000f:Mathf.Repeat(mmr,1000)/1000f,1);progress.rectTransform.offsetMax=Vector2.zero;
            if(age>1.4f)enabled=false;
        }
        void OnDestroy(){if(raceManager!=null)raceManager.OnRaceCompleted-=ShowResults;}
    }
}
