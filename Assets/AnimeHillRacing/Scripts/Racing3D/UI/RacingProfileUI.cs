using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingProfileUI : MonoBehaviour
    {
        RectTransform root,list; LeagueCompetitor owner; Text selectedTitle; RacingTitleView titleView; int page;
        readonly List<string> titleIds=new List<string>();
        public static RacingProfileUI Open(LeagueCompetitor competitor=null)
        {
            var root=RacingDesign.Canvas("Racer profile and titles",110);root.gameObject.AddComponent<RacingMenuOverlay>();var screen=root.gameObject.AddComponent<RacingProfileUI>();screen.root=root;screen.owner=competitor??RacingProgressionService.State.human;screen.Build();return screen;
        }
        void Build()
        {
            var bg=RacingDesign.Panel(root,"Profile backdrop",0,0,1,1,RacingDesign.Background,0);bg.raycastTarget=true;
            RacingDesign.Label(root,owner.displayName,RacingType.Title,.05f,.87f,.70f,.09f,RacingDesign.Ink);
            RacingDesign.Button(root,"CLOSE",.83f,.885f,.12f,.06f,()=>Destroy(root.GetComponentInParent<Canvas>().gameObject));
            RacingDesign.RankBadge(root,owner.currentMmr,.055f,.45f,.30f,.36f);
            RacingComboThumbnail.Create(root,.06f,.28f,.29f,.16f).Configure(owner);
            RacingDesign.Label(root,owner.currentMmr.ToString("N0")+" MMR",RacingType.Mmr,.05f,.205f,.30f,.065f,RacingDesign.Ink,TextAnchor.MiddleCenter);
            RacingDesign.Label(root,MmrTierUtility.GetDisplayName(owner.currentMmr),RacingType.Section,.04f,.15f,.32f,.045f,RacingDesign.TierColor(owner.currentMmr),TextAnchor.MiddleCenter);
            RacingDesign.Label(root,$"PEAK {owner.peakMmr:N0}  /  SEASON PEAK {owner.seasonPeakMmr:N0}\n{owner.races:N0} RACES  ·  {owner.wins:N0} WINS  ·  {owner.podiums:N0} PODIUMS",RacingType.Caption,.05f,.075f,.33f,.065f,RacingDesign.Muted,TextAnchor.MiddleCenter);
            selectedTitle=RacingDesign.Label(root,"",RacingType.Section,.41f,.79f,.54f,.055f);titleView=selectedTitle.gameObject.AddComponent<RacingTitleView>();
            RacingDesign.Label(root,owner==RacingProgressionService.State.human?"CHOOSE YOUR TITLE":"CAREER TITLES",RacingType.Caption,.41f,.855f,.54f,.04f,RacingDesign.Muted);
            foreach(var definition in RacingTitleCatalog.Definitions)titleIds.Add(definition.id);
            foreach(string id in owner.unlockedTitleIds)if(!titleIds.Contains(id))titleIds.Add(id);
            titleIds.Insert(0,"");
            list=RacingDesign.Area(root,"Title selection",.41f,.19f,.54f,.57f);RefreshTitles();
            RacingDesign.Button(root,"PREVIOUS",.41f,.10f,.17f,.06f,()=>{page=Mathf.Max(0,page-1);RefreshTitles();});
            RacingDesign.Button(root,"NEXT",.60f,.10f,.17f,.06f,()=>{page=Mathf.Min((titleIds.Count-1)/7,page+1);RefreshTitles();});
            var seasons=RacingDesign.Button(root,"SEASONS",.79f,.10f,.16f,.06f,()=>RacingSeasonUI.Open(owner));
            RacingDesign.Label(root,RacingDesign.VehicleName(owner.vehiclePreference)+" / "+owner.wheelPreference.Replace("wheel-","").ToUpperInvariant()+"\n"+RacingLoadoutVisuals.DriverName(owner.characterPreference)+" / "+RacingLoadoutVisuals.AccessoryName(owner.accessoryPreference),RacingType.Caption,.04f,.015f,.33f,.05f,RacingDesign.Muted,TextAnchor.MiddleCenter);
            if(owner.id!="player_human")StartCoroutine(LoadHistory(seasons));
        }
        IEnumerator LoadHistory(Button seasons) {
            seasons.interactable=false;var label=seasons.GetComponentInChildren<Text>();label.text="LOADING…";
            var task=RacingProgressionService.ReadBotHistory(owner.id);
            while(!task.IsCompleted)yield return null;
            if(task.IsFaulted){label.text="HISTORY ERROR";UnityEngine.Debug.LogWarning(task.Exception.GetBaseException().Message);yield break;}
            owner.history=task.Result;
            foreach(var record in owner.history)if(record.races>0){string id="season:"+record.seasonNumber+":"+(int)MmrTierUtility.GetTier(record.peakMmr);if(!owner.unlockedTitleIds.Contains(id))owner.unlockedTitleIds.Add(id);if(!titleIds.Contains(id))titleIds.Add(id);}
            RefreshTitles();label.text="SEASONS";seasons.interactable=true;
        }
        void RefreshTitles()
        {
            foreach(Transform child in list){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            titleView.Configure(selectedTitle,owner.selectedTitleId,RacingProgressionService.State.currentSeasonNumber,owner.seasonPeakMmr);
            if(string.IsNullOrEmpty(owner.selectedTitleId)){selectedTitle.text="NO TITLE";selectedTitle.color=RacingDesign.Muted;}
            for(int i=0;i<7;i++) {
                int index=page*7+i;if(index>=titleIds.Count)break;string id=titleIds[index];
                var definition=RacingTitleCatalog.Resolve(id,RacingProgressionService.State.currentSeasonNumber,owner.seasonPeakMmr);
                bool owned=id.Length==0||owner.unlockedTitleIds.Contains(id);
                string label=definition==null?"No title":definition.displayName;
                var button=RacingDesign.Button(list,label,0,.86f-i*.14f,1,.125f,()=>{if(RacingProgressionService.SelectHumanTitle(id))RefreshTitles();},id==owner.selectedTitleId?RacingDesign.Highlight:RacingDesign.Surface);
                button.interactable=owned&&owner==RacingProgressionService.State.human;
                if(!owned&&definition!=null)RacingDesign.Label(button.transform,$"{definition.threshold} {definition.unlockRequirement}",RacingType.Caption,.55f,.015f,.42f,.20f,RacingDesign.Muted,TextAnchor.MiddleRight);
                if(definition!=null)button.GetComponentInChildren<Text>().color=definition.primaryColor;
            }
        }
    }
}
