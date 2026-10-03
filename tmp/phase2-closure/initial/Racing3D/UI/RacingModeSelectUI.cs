using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingModeSelectUI:MonoBehaviour
    {
        [SerializeField] bool openOnStart;
        void Start()
        {
            RacingFeedback.Ensure();gameObject.AddComponent<RacingQualityScope>();
            var root=RacingDesign.Canvas("Racing hub");root.gameObject.AddComponent<RacingScreenMotion>();
            RacingDesign.Panel(root,"Backdrop",0,0,1,1,RacingDesign.Background,0);
            var contour=RacingDesign.Area(root,"Topography",.46f,.06f,.54f,.88f).gameObject.AddComponent<RacingEmblem>();contour.Contours=true;contour.color=new Color(.18f,.4f,.45f,.22f);contour.raycastTarget=false;
            RacingDesign.Label(root,"HILL ADVENTURER",RacingType.Section,.05f,.86f,.48f,.08f,RacingDesign.Ink,display:true);
            RacingDesign.Label(root,"RACING",RacingType.Display,.05f,.75f,.52f,.12f,RacingDesign.Cyan,display:true);
            RacingDesign.Label(root,"THE CANYON IS CALLING.",RacingType.Caption,.05f,.70f,.46f,.04f,RacingDesign.Muted);
            int mmr=RacingSaveManager.CurrentMmr;
            var badge=RacingDesign.Area(root,"Rank badge",.82f,.83f,.1f,.12f).gameObject.AddComponent<RacingEmblem>();badge.Tier=(int)MmrTierUtility.GetTier(mmr);badge.color=RacingDesign.TierColor(mmr);
            RacingDesign.Label(root,mmr.ToString("N0")+" MMR",RacingType.Stat,.61f,.89f,.2f,.055f,RacingDesign.Ink,TextAnchor.MiddleRight);
            RacingDesign.Label(root,MmrTierUtility.GetTierDisplayName(MmrTierUtility.GetTier(mmr)),RacingType.Caption,.62f,.845f,.19f,.04f,RacingDesign.TierColor(mmr),TextAnchor.MiddleRight);
            var ranked=RacingDesign.Button(root,"RANKED",.05f,.34f,.45f,.31f,()=>SceneManager.LoadScene("RacingGarage"),RacingDesign.Surface);
            RacingDesign.Label(ranked.transform,"12 RACERS / 3 LAPS",RacingType.Caption,.08f,.15f,.75f,.14f,RacingDesign.Cyan);
            RacingDesign.Label(ranked.transform,"BUILD YOUR LINE. EARN YOUR RANK.",RacingType.Caption,.08f,.02f,.79f,.15f,RacingDesign.Muted);
            RacingDesign.Panel(ranked.transform,"Gold edge",0,0,.01f,1,RacingDesign.Gold,0);
            RacingDesign.Label(root,"CANYON RUSH",RacingType.Title,.55f,.67f,.4f,.07f,RacingDesign.Ink);
            var preview=RacingShowroom.Create(root,.53f,.28f,.43f,.38f,RacingSaveManager.SelectedVehicleId,RacingSaveManager.SelectedWheelId);
            RacingDesign.Label(root,RacingDesign.VehicleName(RacingSaveManager.SelectedVehicleId),RacingType.Section,.55f,.25f,.37f,.05f,RacingDesign.Ink);
            RacingDesign.Label(root,$"BEST {RacingSaveManager.HighestMmr:N0}  /  {RacingSaveManager.RacesCount} RACES  /  {RacingSaveManager.WinsCount} WINS",RacingType.Caption,.55f,.20f,.39f,.04f,RacingDesign.Muted);
            RacingDesign.Label(root,"RECENT FORM  "+RacingSaveManager.RecentPerformance,RacingType.Caption,.55f,.15f,.39f,.04f,RacingDesign.Muted);
            string[] modes={"GRAND PRIX","TIME TRIAL","PRACTICE"};
            for(int i=0;i<3;i++)
            {
                var card=RacingDesign.Button(root,modes[i],.05f+i*.155f,.17f,.145f,.125f,null,RacingDesign.Surface);card.interactable=false;
                RacingDesign.Label(card.transform,"IN DEVELOPMENT",RacingType.Caption,.08f,.02f,.86f,.22f,RacingDesign.Muted);
            }
            RacingDesign.Button(root,"MAIN MENU",.05f,.045f,.21f,.075f,()=>SceneManager.LoadScene("MainMenu"));
            RacingDesign.Button(root,"LOW",.58f,.04f,.1f,.065f,()=>RacingQualityScope.Select(RacingQuality.Low));
            RacingDesign.Button(root,"MEDIUM",.69f,.04f,.13f,.065f,()=>RacingQualityScope.Select(RacingQuality.Medium));
            RacingDesign.Button(root,"HIGH",.83f,.04f,.11f,.065f,()=>RacingQualityScope.Select(RacingQuality.High));
            RacingDesign.Focus(ranked);
        }
    }
}
