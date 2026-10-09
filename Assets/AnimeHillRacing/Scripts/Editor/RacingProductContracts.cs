#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using AnimeHillRacing.Racing3D;
using AnimeHillRacing.Ads;
using AnimeHillRacing.Vehicle;
using UnityEngine;
namespace AnimeHillRacing.EditorTools
{
    public static class RacingProductContracts
    {
        // Product-only entry avoids repeating the foundation/physics/ML suites.
        public static void RunTargeted()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--closure-output");
            if(at<0||at+1>=args.Length)throw new ArgumentException("Missing --closure-output");
            string output=args[at+1];Directory.CreateDirectory(output);var report=new ClosureContractReport();
            var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(Application.isBatchMode&&string.IsNullOrEmpty(previous.path))previous=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/AnimeHillRacing/Scenes/PreStart.unity");
            var fixture=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
            try { UnityEngine.SceneManagement.SceneManager.SetActiveScene(fixture);using(new RacingPlayerPrefsTestScope())Run(report,output); }
            finally { if(previous.IsValid()&&previous.isLoaded)UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);UnityEditor.SceneManagement.EditorSceneManager.CloseScene(fixture,true); }
            File.WriteAllText(Path.Combine(output,"product-edit-contracts.json"),JsonUtility.ToJson(report,true));
            if(report.failed!=0)throw new InvalidOperationException("Product contracts failed: "+report.failed);
            Debug.Log("PRODUCT_CONTRACTS passed="+report.passed+" failed="+report.failed);
        }
        [Serializable] sealed class ItemFeatureCase { public AnimeHillRacing.Racing3D.ML.TelemetrySample sample; public int mmr; public float[] normalized; }
        [Serializable] sealed class ItemFeatureFixture { public string encoding; public ItemFeatureCase[] cases; }
        public static void Run(ClosureContractReport report,string output) {
            var ads=Resources.Load<AdEntitlementConfiguration>("AdEntitlementConfiguration");
            var purchases=new AnimeHillRacing.Core.RealMoneyEntitlementService(null,null);
            int vipCount=0;bool vipSubscription=false;
            foreach(var product in purchases.Products)if(product.ProductId=="vip_monthly") {
                vipCount++;vipSubscription=product.ProductType==AnimeHillRacing.Core.StoreProductType.Subscription;
            }
            report.Check(ads!=null && ads.vipSubscriptionProductId=="vip_monthly" && AdEntitlementService.VipProductId=="vip_monthly", "Production VIP asset resolves the store-confirmed vip_monthly SKU");
            report.Check(vipCount==1 && vipSubscription && purchases.Contains("vip_monthly") && purchases.IsVip("vip_monthly") && !purchases.IsConsumable("vip_monthly"), "VIP is registered exactly once as a subscription rather than a consumable or permanent entitlement");
            var authored=Resources.Load<RacingProgressionConfiguration>("RacingProgressionConfiguration");
            report.Check(authored!=null && authored.league.historyLimit==20 && authored.titles.Length>=6,"Production progression ScriptableObject and titles are bound to their native scripts");
            var balance=Resources.Load<RacingItemBalanceConfiguration>("RacingItemBalanceConfiguration");
            report.Check(balance!=null && balance.leader.Length==2 && balance.front.Length==3 && balance.middle.Length==4 && balance.rear.Length==13,"Updated P1 / P2-3 / P4-6 / P7-12 item pools include Golden Rock and rear-only Lancer");
            var found=new HashSet<RaceItemType>();for(int place=1;place<=12;place++) {
                var rng=new DeterministicRng(731+place);for(int i=0;i<3000;i++)found.Add(ItemDistributionTable.RollItem(new ItemRollContext {position=place,lap=3,totalLaps=3,gapLeaderSeconds=15},rng));
            }
            report.Check(found.Count==15 && found.Contains(RaceItemType.GoldenRock) && found.Contains(RaceItemType.Coin10) && found.Contains(RaceItemType.MiniTurbo) && found.Contains(RaceItemType.Lancer)
                && !found.Contains(RaceItemType.PulseMine) && !found.Contains(RaceItemType.None),"All fifteen approved random rewards are attainable; the legacy mine ID remains outside the new tables");
            report.Check(ItemDistributionTable.RollItem(new ItemRollContext {position=12,held=RaceItemType.TurboPulse})==RaceItemType.None,"Occupied inventory prevents a second item reward");
            for(int place=1;place<=12;place++) {
                bool legal=true;var rng=new DeterministicRng(place);
                for(int i=0;i<1000;i++) {
                    var item=ItemDistributionTable.RollItem(place,rng);
                    bool leader=item==RaceItemType.Coin10||item==RaceItemType.ElectroSpike;
                    bool front=item==RaceItemType.MagneticShield||item==RaceItemType.Coin10||item==RaceItemType.GoldenRock;
                    bool middle=front||item==RaceItemType.MiniTurbo;
                    bool rear=item==RaceItemType.TurboPulse||item==RaceItemType.ElectroSpike||item==RaceItemType.KineticMissile||item==RaceItemType.MagneticShield
                        ||item==RaceItemType.AerialRecovery||item==RaceItemType.TripleTurbo||item==RaceItemType.TwinRock||item==RaceItemType.TrackingDart
                        ||item==RaceItemType.HyperCharge||item==RaceItemType.ShockwavePulse||item==RaceItemType.HomingRocket||item==RaceItemType.Lancer||item==RaceItemType.GoldenRock;
                    legal &= place==1?leader:place<=3?front:place<=6?middle:rear;
                }
                report.Check(legal,"Placement "+place+" cannot bypass its approved item power pool");
            }
            var badgeRoot=new GameObject("Native badge renderer contract",typeof(RectTransform));
            var badge=badgeRoot.AddComponent<RacingRankBadge>();badge.Configure(12000);
            report.Check(badgeRoot.GetComponent<CanvasRenderer>()!=null,"Dynamic rank badges bind a CanvasRenderer before clipping or rendering");UnityEngine.Object.DestroyImmediate(badgeRoot);
            var root=new GameObject("Native item mesh contract including Coin +10 and Mini Turbo");
            try {
                for(int i=1;i<=RaceItemUtility.Count;i++) {
                    var model=new GameObject("Item "+i);model.transform.SetParent(root.transform,false);RacingItemArt.Build(model.transform,(RaceItemType)i);
                    int visible=0,vertices=0;bool supported=true;
                    foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>())if(renderer.enabled) {
                        visible++;vertices+=renderer.GetComponent<MeshFilter>().sharedMesh.vertexCount;supported &= renderer.sharedMaterial!=null && renderer.sharedMaterial.shader.isSupported;
                    }
                    report.Check(visible>0 && vertices>50 && supported && model.GetComponentsInChildren<Collider>().Length==0,"Item "+i+" uses visible supported 3D meshes without decorative collision geometry");
                }
                report.Check(typeof(ProjectileItemEntity).Name==UnityEditor.MonoScript.FromMonoBehaviour(root.AddComponent<ProjectileItemEntity>()).name,"Projectile MonoBehaviour has an exact native filename binding");
                report.Check(typeof(TrapItemEntity).Name==UnityEditor.MonoScript.FromMonoBehaviour(root.AddComponent<TrapItemEntity>()).name,"Hazard MonoBehaviour has an exact native filename binding");
            } finally {UnityEngine.Object.DestroyImmediate(root);}
            var state=LocalLeagueEngine.Create(new DateTime(2026,1,31,0,0,0,DateTimeKind.Utc).Ticks,902);
            var store=new LocalLeagueStore(Path.Combine(output,"native-json-journal"),v=>JsonUtility.ToJson(v),JsonUtility.FromJson<LocalLeagueState>);
            store.Load();report.Check(store.Save(state) && new LocalLeagueStore(Path.Combine(output,"native-json-journal"),v=>JsonUtility.ToJson(v),JsonUtility.FromJson<LocalLeagueState>).Load().bots.Length==10000,"Unity metadata and binary journals reload every compact persistent bot");
            string json=JsonUtility.ToJson(state);var clone=JsonUtility.FromJson<LocalLeagueState>(json);
            report.Check(JsonUtility.ToJson(clone)==json,"Unity serializer preserves local season timestamps, skill fields, histories and title lists exactly");
            report.Check(!json.Contains("\"bots\"") && !json.Contains("\"records\"") && LocalLeagueStore.BytesPerBot==108 && LocalLeagueStore.BytesPerSeasonBot==38,"Native metadata excludes population/history arrays and compact records have measured 108/38 byte layouts");
            var sparse=LocalLeagueEngine.Create(state.installationTimestamp,902,1000);
            for(int i=0;i<sparse.bots.Length;i++)sparse.bots[i].currentMmr=i*13;
            sparse.human.currentMmr=20000;var sparseEngine=new LocalLeagueEngine(sparse);
            var sparseOpponents=sparseEngine.SelectOpponents(20000,42,false);
            report.Check(sparseOpponents.Length==11 && new HashSet<string>(Array.ConvertAll(sparseOpponents,b=>b.id)).Count==11 && Array.TrueForAll(sparseOpponents,b=>sparseEngine.TryIndex(b.id,out int index) && index>=904),"Sparse prestige matchmaking uses eleven real rivals from the nearest ranked neighborhood instead of the entire league");
            var identity=new RacerIdentity("fixture-combo","Purple crystal racer","formula-01","wheel-whity",10000,false,42,1){characterId="purple-driver",accessoryId="crystal-crest"};
            var cosmetic=RacingVehicleFactory.Create(identity);
            try {
                var visual=cosmetic.GetComponent<RacingVehicleVisuals>();var head=visual.DriverHead;
                report.Check(head.Find("Cobalt Helmet Shell").GetComponent<MeshRenderer>().sharedMaterial.color==RacingLoadoutVisuals.DriverColor(identity.characterId) && head.Find("Loadout accessory crystal-crest/Faceted crystal crest")!=null,"Live racer visibly uses its persistent purple driver and crystal accessory");
                report.Check(head.GetComponentsInChildren<Collider>().Length==0 && cosmetic.GetComponents<Rigidbody>().Length==1,"Loadout character/accessory decoration introduces no physics actors or collision geometry");
            } finally {UnityEngine.Object.DestroyImmediate(cosmetic);}
            for(int i=0;i<=6;i++)report.Check(RaceItemUtility.LegacyFeatureFamily(i)==i,"Installed ML item encoding "+i+" remains identical");
            var fixture=JsonUtility.FromJson<ItemFeatureFixture>(File.ReadAllText("tmp/product-integration/item-feature-parity.json"));
            float maximum=0;var features=new float[AnimeHillRacing.Racing3D.ML.RacingTelemetryFeatures.Count];
            foreach(var item in fixture.cases) {
                AnimeHillRacing.Racing3D.ML.RacingTelemetryFeatures.Encode(item.sample,item.mmr,features);
                for(int i=0;i<features.Length;i++)maximum=Mathf.Max(maximum,Mathf.Abs(features[i]-item.normalized[i]));
            }
            report.Check(fixture.cases.Length==13 && fixture.encoding=="legacy-mechanical-family-1" && maximum<.000001f,"All twelve items and empty inventory retain Python/native 33-feature parity within 1e-6");
            File.WriteAllText(Path.Combine(output,"item-feature-parity.json"),JsonUtility.ToJson(new ItemParity {cases=fixture.cases.Length,maxFeatureError=maximum},true));
            // Native fixture exercises the actual Adventure relocation API with a
            // rotated chassis and detached wheel bodies. No vehicle force is run.
            var rig=new GameObject("Rotated Adventure teleport contract");var wheelRoot=new GameObject("Detached wheel relocation contract");
            try {
                var body=rig.AddComponent<Rigidbody2D>();var controller=rig.AddComponent<VehiclePhysics2DController>();
                var wheel=wheelRoot.AddComponent<Rigidbody2D>();var suspension=wheelRoot.AddComponent<Suspension2DController>();
                var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
                typeof(Suspension2DController).GetField("wheelBody",flags).SetValue(suspension,wheel);
                typeof(VehiclePhysics2DController).GetField("frontSuspension",flags).SetValue(controller,suspension);
                Vector2 origin=new Vector2(13,27),offset=new Vector2(1.4f,-.7f),destination=new Vector2(140,200);
                body.position=origin;body.rotation=65;rig.transform.SetPositionAndRotation(origin,Quaternion.Euler(0,0,65));
                Vector3 rotated=Quaternion.Euler(0,0,65)*(Vector3)offset;wheel.position=origin+(Vector2)rotated;
                body.linearVelocity=Vector2.one*3;wheel.linearVelocity=Vector2.one*5;wheel.angularVelocity=70;
                controller.TeleportToPosition(destination);float error=Vector2.Distance(wheel.position,destination+offset);
                report.Check(error<.001f && Mathf.Abs(body.rotation)<.001f && wheel.linearVelocity.sqrMagnitude==0 && wheel.angularVelocity==0,"Adventure whole-rig teleport preserves the upright detached-wheel layout after a rotated chassis");
                File.WriteAllText(Path.Combine(output,"adventure-rotated-teleport.txt"),$"initialChassisDegrees=65\nexpectedWheelOffset={offset}\nobservedUprightLayoutError={error}\nvehicleForcesNotInvoked=True\n");
            } finally {UnityEngine.Object.DestroyImmediate(wheelRoot);UnityEngine.Object.DestroyImmediate(rig);}
        }
        [Serializable] sealed class ItemParity { public int cases; public float maxFeatureError; }
    }
}
#endif
