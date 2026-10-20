using System;

namespace AnimeHillRacing.Racing3D.ML
{
    public enum MlExecutionMode
    {
        Off=0, Shadow=1, Assist=2, Authority=3
    }

    public static class RacingMlExecutionPolicy
    {
        // Read once per adapter. A later spawner default must not replace an
        // explicit QA choice. The command line cannot change production policy.
        public static MlExecutionMode? QaOverride(bool development,string company,string[] arguments)
        {
            if(!development||company!="HillAdventurer.BlacklineQA"||arguments==null)return null;
            int at=Array.IndexOf(arguments,"--racing-ml-mode");
            return at>=0&&at+1<arguments.Length&&Enum.TryParse(arguments[at+1],true,out MlExecutionMode mode)&&Enum.IsDefined(typeof(MlExecutionMode),mode)?mode:(MlExecutionMode?)null;
        }
        public static MlExecutionMode Effective(MlExecutionMode configured,MlExecutionMode? qaOverride)
            =>(qaOverride??configured)==MlExecutionMode.Off?MlExecutionMode.Off:MlExecutionMode.Shadow;
    }
}
