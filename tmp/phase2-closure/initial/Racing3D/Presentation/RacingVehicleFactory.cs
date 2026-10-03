using UnityEngine;
namespace AnimeHillRacing.Racing3D
{
    public static class RacingVehicleFactory
    {
        static PhysicsMaterial contact;
        public static GameObject Create(RacerIdentity identity)
        {
            var root=new GameObject(identity.displayName);
            var collider=root.AddComponent<BoxCollider>();collider.center=new Vector3(0,.05f,0);collider.size=new Vector3(1.65f,.55f,3.15f);
            if(contact==null)contact=new PhysicsMaterial("Racing glancing contact"){dynamicFriction=.06f,staticFriction=.06f,bounciness=0,frictionCombine=PhysicsMaterialCombine.Minimum,bounceCombine=PhysicsMaterialCombine.Minimum};
            collider.sharedMaterial=contact;root.AddComponent<RacingVehiclePhysics>();
            Color[] colors={RacingDesign.Cyan,RacingDesign.Secondary,RacingDesign.Gold,new Color(.32f,.65f,.94f),new Color(.52f,.84f,.64f),RacingDesign.Apex};
            Color paint=identity.isHuman?RacingDesign.Secondary:colors[Mathf.Abs(identity.personalitySeed%colors.Length)];
            RacingVehicleVisuals.Build(root,identity.vehicleId,identity.wheelId,paint);
            return root;
        }
    }
}
