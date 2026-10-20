using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public sealed class SeasonHistoryDocumentSave : MonoBehaviour
    {
        static SeasonHistoryDocumentSave active;
        Action<string> completed;
        string staging;
        public static void Begin(SeasonHistoryStore store,int season,Action<string> result)
        {
            if(active!=null){result("A document save is already in progress.");return;}
            var go=new GameObject("HillSeasonHistoryExport");DontDestroyOnLoad(go);active=go.AddComponent<SeasonHistoryDocumentSave>();active.completed=result;
            active.StartCoroutine(active.Save(store,season));
        }
        IEnumerator Save(SeasonHistoryStore store,int season)
        {
            string name="Hill-Adventurer-Season-"+season+".zip";
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            string destination=null,error=null;
            try{destination=PickWindows(name);}catch(Exception e){error=e.Message;}
            if(error!=null){Finish("Export failed: "+error);yield break;}
            if(destination==null){Finish("Export cancelled. History was preserved.");yield break;}
            completed("Writing and verifying season archive…");
            var task=Task.Run(()=>store.Export(season,destination));while(!task.IsCompleted)yield return null;
            Finish(task.IsFaulted?"Export failed: "+task.Exception.GetBaseException().Message:"Season exported and verified: "+destination);
#elif UNITY_ANDROID
            staging=Path.Combine(Application.temporaryCachePath,"hill-season-"+Guid.NewGuid().ToString("N")+".zip");
            completed("Preparing verified season archive…");var task=Task.Run(()=>store.Export(season,staging));
            while(!task.IsCompleted)yield return null;
            if(task.IsFaulted){Finish("Export failed: "+task.Exception.GetBaseException().Message);yield break;}
            string failure=null;
            try{
                using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using(var activity=unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using(var bridge=new AndroidJavaClass("com.matias.hillhistory.HistoryExportActivity"))bridge.CallStatic("begin",activity,staging,name);
            }catch(Exception e){failure=e.Message;}
            if(failure!=null)Finish("Document picker failed: "+failure);
#else
            Finish("Document save is supported on Windows and Android.");yield break;
#endif
        }
        // Called only by the app's non-exported Android document-save Activity.
        [UnityEngine.Scripting.Preserve]
        public void OnDocumentSaved(string result)
        {
            if(result=="saved")Finish("Season exported and verified in the selected document.");
            else Finish(result=="cancelled"?"Export cancelled. History was preserved.":"Export did not complete. History was preserved.");
        }
        void Finish(string message)
        {
            try{if(!string.IsNullOrEmpty(staging)&&File.Exists(staging))File.Delete(staging);}catch(IOException){}
            completed?.Invoke(message);active=null;Destroy(gameObject);
        }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        sealed class OpenFileName
        {
            public int structSize=Marshal.SizeOf<OpenFileName>();public IntPtr owner,instance;
            public string filter="ZIP archive\0*.zip\0\0";public IntPtr customFilter;public int maxCustomFilter,filterIndex=1;
            public StringBuilder file;public int maxFile;public IntPtr fileTitle;public int maxFileTitle;
            public string initialDirectory,title="Export completed local season";public int flags=0x00080000|0x00000800|0x00000008;
            public short fileOffset,extensionOffset;public string defaultExtension="zip";public IntPtr data,hook,templateName,reserved;
            public int reservedValue,flagsEx;
        }
        [DllImport("comdlg32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool GetSaveFileNameW([In,Out]OpenFileName data);
        [DllImport("comdlg32.dll")]static extern int CommDlgExtendedError();
        static string PickWindows(string name)
        {
            var data=new OpenFileName{file=new StringBuilder(name,1024),maxFile=1024};
            if(GetSaveFileNameW(data))return data.file.ToString();
            int error=CommDlgExtendedError();if(error!=0)throw new IOException("Windows document picker error "+error);return null;
        }
#endif
    }
}
