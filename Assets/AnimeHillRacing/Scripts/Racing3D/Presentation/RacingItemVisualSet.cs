using UnityEngine;
namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingItemVisualSet : MonoBehaviour
    {
        GameObject[] models; RaceItemType[] types; RaceItemType selected; bool held, defense;
        Transform visualRoot; TrailRenderer trail;
        public void BuildVariants(Transform parent,RaceItemType[] available,bool inventory) {
            held=inventory;types=available;models=new GameObject[types.Length];visualRoot=parent;
            for(int i=0;i<types.Length;i++) {
                var model=new GameObject(RaceItemUtility.DisplayName(types[i]));model.transform.SetParent(parent,false);
                RacingItemArt.Build(model.transform,types[i]);models[i]=model;model.SetActive(false);
            }
            if(!inventory && available[0]==RaceItemType.KineticMissile) {
                trail=parent.gameObject.AddComponent<TrailRenderer>();trail.sharedMaterial=RacingArt.EnergyTrailMaterial();
                trail.time=.14f;trail.startWidth=.12f;trail.endWidth=.015f;trail.minVertexDistance=.25f;trail.emitting=false;
                trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;trail.receiveShadows=false;
            }
        }
        public void Show(RaceItemType type,bool shield) {
            if(models==null) {
                var parent=new GameObject("Held item hologram").transform;parent.SetParent(transform,false);parent.localPosition=new Vector3(0,1.35f,0);
                var all=new RaceItemType[RaceItemUtility.Count];for(int i=0;i<all.Length;i++)all[i]=(RaceItemType)(i+1);
                BuildVariants(parent,all,true);
            }
            if(type==selected && shield==defense)return;
            selected=type;defense=shield;
            for(int i=0;i<models.Length;i++)models[i].SetActive(types[i]==type);
            if(held) {
                visualRoot.localPosition=shield?new Vector3(0,.25f,0):new Vector3(0,1.35f,-.1f);
                visualRoot.localScale=Vector3.one*(shield?2.75f:.65f);
            }
            if(trail!=null) {trail.Clear();trail.startColor=RaceItemUtility.ColorFor(type);trail.endColor=new Color(0,0,0,0);trail.emitting=type!=RaceItemType.None;}
        }
        void LateUpdate() {if(held && selected!=RaceItemType.None && !defense)visualRoot.localRotation=Quaternion.Euler(0,Time.time*65f,12);}
        void OnDisable(){if(trail!=null){trail.emitting=false;trail.Clear();}selected=RaceItemType.None;}
    }
}
