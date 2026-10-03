using AnimeHillRacing.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingMainMenuUI:MonoBehaviour
    {
        RectTransform root,choices;
        void Start()
        {
            foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.gameObject.SetActive(false);
            RacingFeedback.Ensure();gameObject.AddComponent<RacingQualityScope>();
            root=RacingDesign.Canvas("Hill Adventurer front door",30);root.gameObject.AddComponent<RacingScreenMotion>();
            RacingDesign.Panel(root,"Backdrop",0,0,1,1,RacingDesign.Background,0);
            var contours=RacingDesign.Area(root,"Adventure contours",.36f,.1f,.63f,.78f).gameObject.AddComponent<RacingEmblem>();contours.Contours=true;contours.color=new Color(.14f,.46f,.49f,.28f);contours.raycastTarget=false;
            RacingDesign.Label(root,"HILL ADVENTURER",RacingType.Title,.05f,.83f,.67f,.10f,RacingDesign.Ink,display:true);
            RacingDesign.Label(root,"ADVENTURE / SPEED / YOUR NEXT HORIZON",RacingType.Caption,.055f,.78f,.55f,.04f,RacingDesign.Cyan);
            RacingDesign.Label(root,"CHASE THE\nHORIZON",RacingType.Display,.055f,.42f,.45f,.27f,RacingDesign.Ink,display:true);
            RacingDesign.Label(root,"Explore beyond the road. Race beyond your limits.",RacingType.Body,.055f,.34f,.46f,.09f,RacingDesign.Muted);
            RacingShowroom.Create(root,.51f,.25f,.45f,.51f,RacingSaveManager.SelectedVehicleId,RacingSaveManager.SelectedWheelId);
            var play=RacingDesign.Button(root,"PLAY",.055f,.18f,.37f,.12f,ShowPlayChoices,RacingDesign.Highlight);
            RacingDesign.Label(root,"YOUR VEHICLE. TWO WORLDS.",RacingType.Caption,.54f,.19f,.37f,.05f,RacingDesign.Muted,TextAnchor.MiddleCenter);
            RacingDesign.Button(root,"GARAGE",.055f,.045f,.18f,.08f,()=>OpenGarage(GarageSection.Vehicle));
            RacingDesign.Button(root,"SHOP",.25f,.045f,.16f,.08f,()=>OpenGarage(GarageSection.Store));
            RacingDesign.Button(root,"SETTINGS",.43f,.045f,.21f,.08f,()=>OpenGarage(GarageSection.Settings));
            RacingDesign.Label(root,"HILL ADVENTURER RACING / CANYON RUSH",RacingType.Caption,.67f,.04f,.28f,.08f,RacingDesign.Muted,TextAnchor.MiddleRight);
            RacingDesign.Focus(play);
        }
        static void OpenGarage(GarageSection section){GameSession.RequestDirectGarage(section);SceneManager.LoadScene("Garage");}
        public void ShowPlayChoices()
        {
            if(choices!=null)return;
            choices=RacingDesign.Area(root,"Play experiences",0,0,1,1);choices.gameObject.AddComponent<RacingScreenMotion>();RacingDesign.Panel(choices,"Choose backdrop",0,0,1,1,RacingDesign.Background,0);
            RacingDesign.Label(choices,"CHOOSE YOUR HORIZON",RacingType.Title,.08f,.78f,.84f,.12f,RacingDesign.Ink,TextAnchor.MiddleCenter,true);
            var adventure=RacingDesign.Button(choices,"ADVENTURE",.07f,.25f,.41f,.45f,Adventure,RacingDesign.Surface);
            RacingDesign.Label(adventure.transform,"EXPLORE / CLIMB / GO FURTHER",RacingType.Caption,.08f,.19f,.84f,.13f,RacingDesign.Secondary);
            RacingDesign.Label(adventure.transform,"Terrain. Fuel. Distance.\nAn open road and your next discovery.",RacingType.Body,.08f,.025f,.82f,.19f,RacingDesign.Muted);
            var contour=RacingDesign.Area(adventure.transform,"Landscape lines",.06f,.50f,.84f,.45f).gameObject.AddComponent<RacingEmblem>();contour.Contours=true;contour.color=new Color(.96f,.46f,.29f,.3f);contour.raycastTarget=false;
            var racing=RacingDesign.Button(choices,"RACING",.52f,.25f,.41f,.45f,()=>{GameSession.ClearGarageContext();SceneManager.LoadScene("RacingSelect");},RacingDesign.Surface);
            RacingDesign.Label(racing.transform,"12 RACERS / RANKED / ORIGINAL CIRCUITS",RacingType.Caption,.08f,.19f,.84f,.13f,RacingDesign.Cyan);
            RacingDesign.Label(racing.transform,"Drift. Items. MMR.\nMake every corner count.",RacingType.Body,.08f,.025f,.82f,.19f,RacingDesign.Muted);
            var symbol=RacingDesign.Area(racing.transform,"Mountain racing mark",.26f,.51f,.46f,.45f).gameObject.AddComponent<RacingEmblem>();symbol.Tier=7;symbol.color=RacingDesign.Cyan;symbol.raycastTarget=false;
            RacingDesign.Button(choices,"BACK",.07f,.08f,.22f,.09f,()=>{Destroy(choices.gameObject);choices=null;});RacingDesign.Focus(racing);
        }
        public void Adventure(){GameSession.BeginAdventureTrackSelection();SceneManager.LoadScene("PreStart");}
    }
}
