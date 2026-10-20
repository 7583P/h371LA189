#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using AnimeHillRacing.Racing3D.Experiments;

namespace AnimeHillRacing.EditorTools
{
    public sealed class RacingMmrExperimentWindow : EditorWindow
    {
        int mmr=3000,choice;
        bool ranked=true,qualified=true;
        [MenuItem("Hill Adventurer/QA/MMR and AFTER preview (experimental)")]
        static void Open()=>GetWindow<RacingMmrExperimentWindow>("AFTER experiment");
        void OnGUI()
        {
            EditorGUILayout.HelpBox("EDITOR EXPERIMENT. No player save, earned title, rating formula or production setting is changed.",MessageType.Info);
            choice=EditorGUILayout.Popup("Scale",choice,new[]{"Proposed AFTER >= 3000 / 100","Existing Hill Beyond > 10000 / 1000"});
            mmr=EditorGUILayout.IntField("Visible MMR",Mathf.Max(0,mmr));
            ranked=EditorGUILayout.Toggle("Career placement 3/3",ranked);qualified=EditorGUILayout.Toggle("Season finish 1/1",qualified);
            var scale=choice==0?AfterScale.Unified():AfterScale.HillCompatible();int level=-1;
            bool visible=ranked&&qualified&&scale.TryLevel(mmr,out level);
            EditorGUILayout.LabelField("Current prestige",visible?scale.label+" "+level:ranked&&!qualified?"(Season Placement)":"No qualified prestige");
            var rect=GUILayoutUtility.GetRect(180,180);Vector2 center=rect.center;float size=75;
            if(visible){
                Handles.color=new Color(.16f,.72f,1);Handles.DrawAAConvexPolygon(center+Vector2.up*size,center+Vector2.right*size,center-Vector2.up*size,center-Vector2.right*size);
                var style=new GUIStyle(EditorStyles.boldLabel){alignment=TextAnchor.MiddleCenter,fontSize=32};style.normal.textColor=Color.white;GUI.Label(rect,level.ToString(),style);
            }
            EditorGUILayout.HelpBox("Historical titles freeze the qualified settled peak and the rule used in that season. Final leaderboard position still uses final MMR. Activation requires the owner's decision.",MessageType.None);
        }
    }
}
#endif
