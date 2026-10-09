using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    // Cosmetic loadout shared by the showroom and the twelve live racers.
    // No colliders, rigidbodies, stat modifiers or per-bot Update.
    public static class RacingLoadoutVisuals
    {
        public static Color DriverColor(string id)=>id=="purple-driver"?new Color(.64f,.15f,.94f):id=="cyan-driver"?RacingDesign.Cyan:id=="ruby-driver"?new Color(.94f,.10f,.25f):RacingVehicleFactory.MetaBlueHelmet;
        public static string DriverName(string id)=>id=="purple-driver"?"PURPLE DRIVER":id=="cyan-driver"?"CYAN DRIVER":id=="ruby-driver"?"RUBY DRIVER":"COBALT DRIVER";
        public static string AccessoryName(string id)=>id=="signal-aerial"?"SIGNAL AERIAL":id=="crystal-crest"?"CRYSTAL CREST":"NO ACCESSORY";
        public static void Apply(RacingVehicleVisuals visual,string character,string accessory) {
            if(visual==null || visual.DriverHead==null)return;
            var head=visual.DriverHead;
            var material=RacingArt.IdentityMaterial("League driver / "+character,DriverColor(character),.18f);
            material.SetFloat("_Glossiness",.82f);material.SetFloat("_Metallic",.25f);
            foreach(var renderer in head.GetComponentsInChildren<MeshRenderer>())
                if(renderer.name=="Cobalt Helmet Shell" || renderer.name=="Chin aero")renderer.sharedMaterial=material;
            var attachment=new GameObject("Loadout accessory "+accessory);attachment.transform.SetParent(head,false);
            if(accessory=="signal-aerial") {
                RacingArt.Box(attachment.transform,"Aerial stem",new Vector3(.20f,.30f,-.18f),new Vector3(.022f,.30f,.022f),RacingArt.Material(RacingDesign.Ink,.5f),.005f);
                RacingArt.Part(attachment.transform,"Signal light",RacingArt.Ellipsoid("league_signal",new Vector3(.035f,.035f,.035f)),RacingArt.Material(RacingDesign.Cyan,.6f,true),new Vector3(.20f,.46f,-.18f),Quaternion.identity);
            } else if(accessory=="crystal-crest") {
                var crest=RacingArt.Box(attachment.transform,"Faceted crystal crest",new Vector3(0,.30f,-.05f),new Vector3(.075f,.17f,.13f),RacingArt.Material(RacingDesign.Apex,.7f,true),.035f);
                crest.transform.localRotation=Quaternion.Euler(0,0,30);
            }
        }
    }
}
