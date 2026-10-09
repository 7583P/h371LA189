using UnityEngine;
namespace AnimeHillRacing.Racing3D
{
    public static class RacingCityPolish
    {
        // Decorative geometry only. No road samples, route portals or colliders change.
        public static void Build(Transform parent,TrackNode[] nodes) {
            var frame=RacingArt.Material(new Color(.027f,.043f,.081f),.8f);
            var cyan=RacingArt.Material(RacingDesign.Cyan,.6f,true);
            var pink=RacingArt.Material(new Color(.94f,.13f,.66f),.6f,true);
            var silver=RacingArt.Material(new Color(.35f,.43f,.57f),.76f);
            string[] messages={"FASTER\nHIGHER\nTOGETHER","SAME ROADS\nNEW STORIES","KARTS MAKE A\nBRIGHTER TOMORROW","NIGHT CITY\nNEVER SLEEPS"};
            int[] places={12,51,121,163};
            for(int i=0;i<places.Length;i++) {
                var road=nodes[places[i]];int side=i%2==0?-1:1;
                var board=new GameObject("Neon city story sign "+i).transform;board.SetParent(parent,false);board.position=road.Position+road.Right*side*36+Vector3.up*18;board.rotation=Quaternion.LookRotation(road.Forward,Vector3.up);
                RacingArt.Box(board,"Sign support",Vector3.zero,new Vector3(12,18,.6f),frame,.6f);
                RacingArt.Box(board,"Cyan edge",new Vector3(-5.7f,0,-.35f),new Vector3(.16f,17,.08f),cyan,.02f);
                RacingArt.Box(board,"Magenta edge",new Vector3(5.7f,0,-.35f),new Vector3(.16f,17,.08f),pink,.02f);
                var textRoot=new GameObject("Original Hill Adventurer city message");textRoot.transform.SetParent(board,false);textRoot.transform.localPosition=new Vector3(0,0,-.4f);textRoot.transform.localRotation=Quaternion.Euler(0,180,0);
                var text=textRoot.AddComponent<TextMesh>();text.text=messages[i];text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=64;text.characterSize=.48f;text.color=i%2==0?RacingDesign.Cyan:new Color(1,.26f,.74f);
            }
            var blimp=new GameObject("Hill Adventurer skyline blimp").transform;blimp.SetParent(parent,false);blimp.position=new Vector3(140,122,90);blimp.rotation=Quaternion.Euler(0,-25,0);
            RacingArt.Part(blimp,"Silver airborne hull",RacingArt.Ellipsoid("city_blimp",new Vector3(8,3.7f,18)),silver,Vector3.zero,Quaternion.identity);
            RacingArt.Box(blimp,"Neon gondola",new Vector3(0,-4,0),new Vector3(4,1.7f,8),frame,.5f);
            for(int side=-1;side<=1;side+=2)RacingArt.Box(blimp,"Airship energy stripe",new Vector3(side*7.7f,0,0),new Vector3(.10f,.35f,23),side<0?cyan:pink,.03f);
            var insignia=new GameObject("Airship original racing identity").transform;insignia.SetParent(blimp,false);insignia.localPosition=new Vector3(-8.05f,0,0);insignia.localRotation=Quaternion.Euler(0,90,0);
            var brand=insignia.gameObject.AddComponent<TextMesh>();brand.text="HILL\nADVENTURER";brand.anchor=TextAnchor.MiddleCenter;brand.alignment=TextAlignment.Center;brand.characterSize=.55f;brand.fontSize=72;brand.color=RacingDesign.Cyan;
        }
    }
}
