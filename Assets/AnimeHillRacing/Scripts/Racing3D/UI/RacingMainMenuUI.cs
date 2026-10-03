using AnimeHillRacing.Core;
using AnimeHillRacing.UI;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingMainMenuUI : MonoBehaviour
    {
        Button adventureCardBtn;
        void Start()
        {
            foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if(canvas.sortingOrder<200)canvas.gameObject.SetActive(false);
            RacingFeedback.Ensure();gameObject.AddComponent<RacingQualityScope>();
            var root=RacingDesign.Canvas("Hill Adventurer Main Menu",30);
            RacingFrontEndArt.Background(root);
            var content=RacingDesign.Area(root,"Menu navigation",0,0,1,1);
            content.gameObject.AddComponent<RacingScreenMotion>();
            var profile=RacingDesign.Panel(content,"Pilot profile",.67f,.89f,.29f,.075f,RacingDesign.Surface,9);
            RacingDesign.Panel(profile.transform,"Profile cyan crest",0,.96f,1,.04f,RacingDesign.Cyan,0);
            RacingDesign.Label(profile.transform,"COINS",RacingType.Caption,.045f,.53f,.27f,.33f,RacingDesign.Gold);
            RacingDesign.Label(profile.transform,SaveManager.TotalCoins.ToString("N0"),RacingType.Body,.045f,.07f,.29f,.46f,RacingDesign.Ink);
            RacingDesign.Label(profile.transform,"GEMS",RacingType.Caption,.36f,.53f,.22f,.33f,RacingDesign.Cyan);
            RacingDesign.Label(profile.transform,SaveManager.TotalGems.ToString("N0"),RacingType.Body,.36f,.07f,.22f,.46f,RacingDesign.Ink);
            RacingDesign.Label(profile.transform,"RANKED MMR",RacingType.Caption,.64f,.53f,.33f,.33f,RacingDesign.Apex);
            RacingDesign.Label(profile.transform,RacingSaveManager.CurrentMmr.ToString("N0"),RacingType.Body,.64f,.07f,.33f,.46f,RacingDesign.Ink);
            var edition=RacingDesign.Panel(content,"Blackline edition",.045f,.29f,.275f,.054f,RacingDesign.Surface,8);
            RacingDesign.Panel(edition.transform,"Red signature",0,0,.013f,1,RacingVehicleFactory.MetaRed,0);
            RacingDesign.Label(edition.transform,"BLACKLINE / NEON CITY",RacingType.Section,.055f,.08f,.9f,.84f,RacingDesign.Ink);
            RacingDesign.Label(content,"Climb higher. Race harder.",RacingType.Body,.047f,.245f,.40f,.043f,RacingDesign.Platinum);
            adventureCardBtn=ModeCard(content,"Adventure Main Pillar Card","ADVENTURE","HILLS / EXPLORATION / SOLO",.045f,.06f,.36f,.165f,RacingDesign.Gold,Adventure);
            ModeCard(content,"Racing 3D Pillar Card","RACING 3D","12 RACERS / DRIFT / RANKED",.425f,.06f,.36f,.165f,RacingDesign.Cyan,EnterRacing);
            RacingDesign.Button(content,"GARAGE",.805f,.149f,.15f,.076f,()=>OpenGarage(GarageSection.Vehicle));
            RacingDesign.Button(content,"SHOP",.805f,.06f,.15f,.076f,()=>OpenGarage(GarageSection.Store));
            RacingDesign.Label(content,"HILL ADVENTURER / YOUR NEXT CLIMB STARTS HERE",RacingType.Caption,.045f,.013f,.70f,.029f,RacingDesign.Muted);
            RacingDesign.Focus(adventureCardBtn);
        }
        static Button ModeCard(Transform parent,string name,string title,string detail,float x,float y,float w,float h,Color accent,System.Action action)
        {
            var panel=RacingDesign.Panel(parent,name,x,y,w,h,RacingDesign.Surface,14);
            RacingDesign.Panel(panel.transform,"Mode accent",0,.97f,1,.03f,accent,0);
            RacingDesign.Label(panel.transform,title,RacingType.Title,.045f,.54f,.91f,.35f,RacingDesign.Ink);
            RacingDesign.Label(panel.transform,detail,RacingType.Caption,.048f,.35f,.90f,.20f,accent);
            return RacingDesign.Button(panel.transform,title=="ADVENTURE"?"ADVENTURE":"RACING",.045f,.08f,.91f,.25f,action,accent);
        }
        static void OpenGarage(GarageSection section)
        {
            GameSession.RequestDirectGarage(section);RacingLoadingScreen.Load("Garage","YOUR GARAGE");
        }
        public void Adventure()
        {
            GameSession.BeginAdventureTrackSelection();RacingLoadingScreen.Load("PreStart","CHOOSE YOUR ADVENTURE");
        }
        public void EnterRacing()
        {
            GameSession.ClearGarageContext();RacingLoadingScreen.Load("RacingSelect","RACING / NEON CITY");
        }
        // Existing scene callbacks. Garage owns the live 3D vehicle preview.
        public void ShowAdventureVehicle() { }
        public void ShowMetaVehicle() { }
        public void ShowPlayChoices()=>Adventure();
    }
}
