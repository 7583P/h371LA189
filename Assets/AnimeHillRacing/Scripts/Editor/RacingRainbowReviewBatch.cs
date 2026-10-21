#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using AnimeHillRacing.Racing3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    // Real D3D renders, fixed cameras, no gameplay or save initialization.
    public static class RacingRainbowReviewBatch
    {
        [Serializable] sealed class Navigation { public TrackNode[] nodes; public TrackShortcut[] routes; public int[] checkpoints; }
        [Serializable] sealed class View { public string file; public Vector3 position, target; public float fov; public string lighting, quality; public int nonDarkPixels, pinkPixels; }
        [Serializable] sealed class Evidence
        {
            public string classification="EDITOR_D3D_RENDER_NOT_ANDROID_OR_HUMAN_APPROVAL", unity, gpu, geometrySha256, navigationSha256;
            public int colliders, renderers, triangles, shaderErrors;
            public bool humanApproved=false;
            public View[] views;
        }

        public static void Run()
        {
            string output=Path.GetFullPath(RacingFrameForensics.Argument("--closure-output"));
            CaptureStage(output);
        }

        public static void InspectItems()
        {
            string output=Path.GetFullPath(RacingFrameForensics.Argument("--closure-output"));
            if(Directory.Exists(output))throw new InvalidOperationException("Preserve diagnostics");
            Directory.CreateDirectory(output);
            using(var rows=new StreamWriter(Path.Combine(output,"item-mesh-diagnostics.tsv"))) {
                rows.WriteLine("item\trenderer\tvertices\tshader\tsupported\tcolliders");
                for(int i=1;i<=RaceItemUtility.Count;i++) {
                    var root=new GameObject("Item diagnostic "+i);RacingItemArt.Build(root.transform,(RaceItemType)i);
                    foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())if(renderer.enabled)
                        rows.WriteLine(i+"\t"+renderer.name+"\t"+renderer.GetComponent<MeshFilter>().sharedMesh.vertexCount+"\t"+renderer.sharedMaterial.shader.name+"\t"+renderer.sharedMaterial.shader.isSupported+"\t"+root.GetComponentsInChildren<Collider>().Length);
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
            RacingProductContracts.RunTargeted();
        }

        public static void BuildValidated()
        {
            if(File.Exists(Path.GetFullPath(RacingFrameForensics.Argument("--rc-build-path"))))throw new InvalidOperationException("Preserve prior build");
            RacingSeasonExpansionBatch.ValidateAllContracts();
            RacingRcEvidenceBuilder.BuildWindowsBatch();
        }

        public static void ReviewAndBuild()
        {
            string output=Path.GetFullPath(RacingFrameForensics.Argument("--closure-output"));
            string build=Path.GetFullPath(RacingFrameForensics.Argument("--rc-build-path"));
            if(Directory.Exists(output)||File.Exists(build))throw new InvalidOperationException("Preserve existing review/build");
            bool? prior=RacingRainbowScenery.EditorPreviewOverride;
            try {
                RacingRainbowScenery.EditorPreviewOverride=false;
                var before=CaptureStage(Path.Combine(output,"before"));
                RacingRainbowScenery.EditorPreviewOverride=true;
                var after=CaptureStage(Path.Combine(output,"after"));
                if(before.geometrySha256!=after.geometrySha256||before.navigationSha256!=after.navigationSha256||before.colliders!=after.colliders)
                    throw new InvalidOperationException("Visual proposal changed navigation or collision");
                File.WriteAllText(Path.Combine(output,"geometry-parity.json"),"{\"sameNavigation\":true,\"sameCollisions\":true,\"humanApproved\":false}");
                RacingRainbowScenery.EditorPreviewOverride=false;
                RacingSeasonExpansionBatch.ValidateAllContracts();
                RacingRcEvidenceBuilder.BuildWindowsBatch();
            }
            finally {RacingRainbowScenery.EditorPreviewOverride=prior;}
        }

        static Evidence CaptureStage(string output)
        {
            int aa=QualitySettings.antiAliasing;var shadows=QualitySettings.shadows;
            try {return RenderStage(output);}
            finally {QualitySettings.antiAliasing=aa;QualitySettings.shadows=shadows;}
        }

        static Evidence RenderStage(string output)
        {
            if(Directory.Exists(output))throw new InvalidOperationException("Preserve previous visual evidence");
            Directory.CreateDirectory(output);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cameraRoot=new GameObject("Fixed review camera",typeof(Camera));
            cameraRoot.tag="MainCamera";
            var camera=cameraRoot.GetComponent<Camera>();
            camera.clearFlags=CameraClearFlags.Skybox;camera.fieldOfView=64;camera.nearClipPlane=.15f;camera.farClipPlane=5000;camera.allowHDR=false;
            RacingQualityScope.OverrideForValidation(RacingQuality.High);
            QualitySettings.antiAliasing=4;
            var root=new GameObject("Review circuit");var builder=root.AddComponent<CanyonRushBuilder>();builder.Build();
            Physics.SyncTransforms();
            var nodes=NightCityCircuit.CreateNodes();
            var evidence=new Evidence {unity=Application.unityVersion,gpu=SystemInfo.graphicsDeviceName,
                navigationSha256=Hash(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Navigation{nodes=nodes,routes=NightCityCircuit.CreateShortcuts(nodes),checkpoints=NightCityCircuit.Checkpoints}))),
                geometrySha256=CollisionHash(root.transform),colliders=root.GetComponentsInChildren<Collider>(true).Length};
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))if(filter.sharedMesh!=null)evidence.triangles+=filter.sharedMesh.triangles.Length/3;
            evidence.renderers=root.GetComponentsInChildren<Renderer>(true).Length;
            foreach(string shaderName in new[]{"RacingPrismaticRoad","RacingSkyTrackSky","RacingRainbowStructure","RacingRainbowRoad"}) {
                var shader=Resources.Load<Shader>(shaderName);if(shader==null)continue;
                foreach(var message in ShaderUtil.GetShaderMessages(shader))if(message.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)evidence.shaderErrors++;
            }
            var views=new List<View>();
            foreach(var entry in new[]{(0,"finish"),(26,"curve"),(56,"shortcut"),(108,"sweep"),(168,"jump"),(184,"landing")}) {
                var n=nodes[entry.Item1];Vector3 position=n.Position-n.Forward*19+Vector3.up*5.2f;
                Vector3 look=n.Position+n.Forward*34+Vector3.up*1.4f;
                views.Add(Capture(camera,output,entry.Item2+"-night-high",position,look,"night","High"));
            }
            views.Add(Capture(camera,output,"overview-night-high",new Vector3(100,590,-710),new Vector3(90,5,-15),"night","High"));
            var sun=RenderSettings.sun;sun.intensity=1.25f;sun.color=new Color(1,.94f,.83f);
            RenderSettings.ambientSkyColor=new Color(.55f,.65f,.8f);RenderSettings.fogColor=new Color(.13f,.18f,.26f);
            views.Add(Capture(camera,output,"curve-day-high",nodes[26].Position-nodes[26].Forward*19+Vector3.up*5.2f,nodes[26].Position+nodes[26].Forward*34+Vector3.up*1.4f,"day lighting host simulation","High"));
            QualitySettings.antiAliasing=0;QualitySettings.shadows=ShadowQuality.Disable;
            views.Add(Capture(camera,output,"curve-day-low",nodes[26].Position-nodes[26].Forward*19+Vector3.up*5.2f,nodes[26].Position+nodes[26].Forward*34+Vector3.up*1.4f,"day lighting host simulation","Low host approximation; NOT Android"));
            evidence.views=views.ToArray();
            File.WriteAllText(Path.Combine(output,"visual-evidence.json"),JsonUtility.ToJson(evidence,true));
            UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraRoot);
            if(evidence.shaderErrors>0)throw new InvalidOperationException("Shader compilation errors in review");
            return evidence;
        }

        static View Capture(Camera camera,string output,string name,Vector3 position,Vector3 look,string lighting,string quality)
        {
            camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(look-position,Vector3.up));
            var target=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32){antiAliasing=Mathf.Max(1,QualitySettings.antiAliasing)};
            var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);var prior=RenderTexture.active;
            try {
                camera.targetTexture=target;camera.aspect=1600f/900;camera.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();
                File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());
                var view=new View{file=name+".png",position=position,target=look,fov=camera.fieldOfView,lighting=lighting,quality=quality};
                foreach(var color in texture.GetPixels32()) {if(color.r+color.g+color.b>70)view.nonDarkPixels++;if(color.r>230&&color.b>230&&color.g<25)view.pinkPixels++;}
                return view;
            }
            finally {camera.targetTexture=null;RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(texture);}
        }

        static string CollisionHash(Transform root)
        {
            using(var bytes=new MemoryStream())using(var writer=new BinaryWriter(bytes)) {
                foreach(var collider in root.GetComponentsInChildren<Collider>(true)) {
                    writer.Write(PathOf(collider.transform,root));writer.Write(collider.GetType().Name);writer.Write(collider.isTrigger);writer.Write(collider.enabled);
                    Write(writer,collider.transform.position);Write(writer,collider.transform.lossyScale);
                    var rotation=collider.transform.rotation;writer.Write(rotation.x);writer.Write(rotation.y);writer.Write(rotation.z);writer.Write(rotation.w);
                    if(collider is BoxCollider box){Write(writer,box.center);Write(writer,box.size);}
                    if(collider is MeshCollider mesh&&mesh.sharedMesh!=null){foreach(var p in mesh.sharedMesh.vertices)Write(writer,p);foreach(int t in mesh.sharedMesh.triangles)writer.Write(t);}
                    if(collider is SphereCollider sphere){Write(writer,sphere.center);writer.Write(sphere.radius);}
                }
                writer.Flush();return Hash(bytes.ToArray());
            }
        }
        static void Write(BinaryWriter writer,Vector3 p){writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);}
        static string PathOf(Transform t,Transform root)=>t==root?"":PathOf(t.parent,root)+"/"+t.name;
        static string Hash(byte[] bytes){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
    }
}
#endif
