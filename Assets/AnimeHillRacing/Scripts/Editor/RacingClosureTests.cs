#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using AnimeHillRacing.Racing3D;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.EditorTools
{
    // Test fixtures own only these known keys; the outer validation launcher also
    // preserves the full native Windows save before opening Unity.
    public sealed class RacingPlayerPrefsTestScope : IDisposable
    {
        sealed class Value { public string key,text; public int number; public bool present,isString; }
        readonly List<Value> values=new List<Value>();
        public RacingPlayerPrefsTestScope()
        {
            foreach(string key in new[]{"AHR.TotalCoins","AHR.TotalGems","AHR.Racing.MMR.V1","AHR.Racing.HighestMMR.V1","AHR.Racing.RacesCount.V1","AHR.Racing.WinsCount.V1","AHR.Racing.PodiumsCount.V1","AHR.Racing.Migration.V1"}) Save(key,false);
            foreach(string key in new[]{"AHR.Racing.SelectedVehicle.V1","AHR.Racing.SelectedWheel.V1","AHR.Racing.Recent.V1"}) Save(key,true);
            foreach(var value in values)if(value.key.StartsWith("AHR.Racing.",StringComparison.Ordinal))PlayerPrefs.DeleteKey(value.key);
        }
        void Save(string key,bool text){values.Add(new Value{key=key,present=PlayerPrefs.HasKey(key),isString=text,text=text?PlayerPrefs.GetString(key):null,number=text?0:PlayerPrefs.GetInt(key)});}
        public void Dispose()
        {
            foreach(var value in values)
                if(!value.present)PlayerPrefs.DeleteKey(value.key);
                else if(value.isString)PlayerPrefs.SetString(value.key,value.text);
                else PlayerPrefs.SetInt(value.key,value.number);
            PlayerPrefs.Save();
        }
    }
    public static class RacingClosureTests
    {
        public static void Run(string output,Action completed)
        {
            // Edit fixtures create generated meshes and runtime-only components.
            // Keep them out of the user's open scene and Unity's Play Mode snapshot.
            var previous=SceneManager.GetActiveScene();
            // Batch startup has an empty, untitled scene; anchor it to the same
            // authored entry scene this launcher uses before entering Play Mode.
            if(Application.isBatchMode&&string.IsNullOrEmpty(previous.path))
                previous=EditorSceneManager.OpenScene("Assets/AnimeHillRacing/Scenes/PreStart.unity");
            var fixtureScene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try { SceneManager.SetActiveScene(fixtureScene); RunIsolated(output,completed); }
            finally
            {
                if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(fixtureScene,true);
            }
        }
        static void RunIsolated(string output,Action completed)
        {
            var foundation=RacingSystemAuditor.RunAllTests();
            var summary=new ClosureContractReport{passed=foundation.Passed,failed=foundation.Failed,failures=foundation.FailureMessages};
            File.WriteAllText(Path.Combine(output,"foundation-tests.json"),JsonUtility.ToJson(summary,true));
            var additional=new ClosureContractReport();
            RacingClosureContracts.Progress(additional);
            File.WriteAllText(Path.Combine(output,"phase2-edit-contracts.json"),JsonUtility.ToJson(additional,true));
            var improvements=new ClosureContractReport();
            using(new RacingPlayerPrefsTestScope())RacingImprovementContracts.Edit(improvements,output);
            File.WriteAllText(Path.Combine(output,"improvement-edit-contracts.json"),JsonUtility.ToJson(improvements,true));
            var release=new ClosureContractReport();RacingReleaseContracts.Run(release,output);
            File.WriteAllText(Path.Combine(output,"release-edit-contracts.json"),JsonUtility.ToJson(release,true));
            var product=new ClosureContractReport();RacingProductContracts.Run(product,output);
            File.WriteAllText(Path.Combine(output,"product-edit-contracts.json"),JsonUtility.ToJson(product,true));
            SessionState.SetBool("AHR.Racing.Closure.ContractsPassed",product.failed==0&&foundation.Failed==0&&additional.failed==0&&improvements.failed==0&&release.failed==0);
            // Compile the existing test Program.cs with Unity's compiler and real
            // project assemblies, then invoke it inside Unity (no duplicated tests).
            var builder=new AssemblyBuilder(Path.Combine(output,"ExistingRacingTests.dll"),new[]{Path.GetFullPath("tmp/racing-test/Program.cs")});
            builder.flags=AssemblyBuilderFlags.EditorAssembly;
            var excluded=new List<string>();
            foreach(string reference in builder.defaultReferences)if(Path.GetFileName(reference)=="UnityEngine.dll")excluded.Add(reference);
            builder.excludeReferences=excluded.ToArray();
            builder.additionalReferences=new[]{Path.GetFullPath("Library/ScriptAssemblies/Assembly-CSharp.dll"),typeof(Vector3).Assembly.Location};
            builder.buildFinished+=(path,messages)=>
            {
                bool compiled=true;foreach(var message in messages)if(message.type==CompilerMessageType.Error){compiled=false;Debug.LogError(message.message);}
                if(compiled)
                {
                    var console=new StringWriter();var prior=Console.Out;
                    try
                    {
                        Console.SetOut(console);
                        var program=System.Reflection.Assembly.LoadFile(Path.GetFullPath(path)).GetType("RacingTestRunner.Program");
                        int exit=(int)program.GetMethod("Main",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{Array.Empty<string>()});
                        int passed=(int)program.GetField("passed",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
                        int failed=(int)program.GetField("failed",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
                        File.WriteAllText(Path.Combine(output,"existing-cli-tests.json"),JsonUtility.ToJson(new ClosureContractReport{passed=passed,failed=failed},true));
                        SessionState.SetBool("AHR.Racing.Closure.ContractsPassed",SessionState.GetBool("AHR.Racing.Closure.ContractsPassed",false)&&exit==0);
                    }
                    catch(Exception exception){Debug.LogException(exception);SessionState.SetBool("AHR.Racing.Closure.ContractsPassed",false);}
                    finally{Console.SetOut(prior);File.WriteAllText(Path.Combine(output,"existing-cli-tests.txt"),console.ToString());console.Dispose();}
                }
                else SessionState.SetBool("AHR.Racing.Closure.ContractsPassed",false);
                completed();
            };
            if(!builder.Build()){SessionState.SetBool("AHR.Racing.Closure.ContractsPassed",false);completed();}
        }
    }
}
#endif
