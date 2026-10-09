#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    // The audit consumes the authoritative dirty tree. Automatic content installers
    // must not regenerate unrelated Adventure assets when a QA Editor opens.
    [InitializeOnLoad]
    public static class RacingReleaseAssetGuard
    {
        static RacingReleaseAssetGuard()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--closure-output")<0)return;
            var installer=typeof(AnimeHillRacing.Editor.BuggyMountainsInstaller);
            var sessionField=installer.GetField("SessionKey",BindingFlags.NonPublic|BindingFlags.Static);
            if(sessionField==null)throw new InvalidOperationException("The QA installer guard requires its existing session key.");
            SessionState.SetBool((string)sessionField.GetRawConstantValue(),true);
            // Also cancel the precise queued automatic callback if its initializer
            // ran before this class. Other editor callbacks and manual Install remain.
            var callbacks=EditorApplication.delayCall;
            if(callbacks!=null)foreach(EditorApplication.CallbackFunction callback in callbacks.GetInvocationList())
            {
                var owner=callback.Method.DeclaringType;
                while(owner!=null&&owner!=installer)owner=owner.DeclaringType;
                if(owner==installer&&callback.Method.Name.Contains("QueueAutomaticInstall"))
                    EditorApplication.delayCall-=callback;
            }
            Debug.Log("RC_EDITOR_CONTENT_GUARD: preserved existing Buggy/Mountains content; automatic regeneration disabled only for explicit QA.");
        }
    }
}
#endif
