from pathlib import Path
import re
p=Path('Assets/AnimeHillRacing/Scripts/Racing3D/UI/RacingMainMenuUI.cs')
s=p.read_text(encoding='utf-8-sig')
for alias in ['playAlias','advAlias','raceAlias','threeDAlias','threeDAlias2','vehiclesAlias','storeAlias','tuningAlias','settingsAlias']:
    begin=s.index('            var '+alias+' =')
    end=s.index('\n\n',begin)
    s=s[:begin]+s[end+2:]
s=s.replace('EXPEDITIONS  •  3D MULTIPLAYER RACING','EXPEDITIONS  •  OFFLINE RANKED RACING')
s=s.replace('⚡ 3D MULTIPLAYER  •  RANKED','3D RACING  •  OFFLINE RANKED')
s=s.replace('★ CURRENT COMPETITIVE META: FORMULA APEX','FORMULA APEX  /  SEASON META')
s=s.replace('FORMULA APEX  •  100% CYAN WHEELS  •  PURPLE PILOT','FORMULA APEX  /  READY TO RACE')
start=s.index('            string advFeatures =')
end=s.index('            RacingDesign.Label(advCard.transform, advFeatures',start)
s=s[:start]+'''            string advFeatures = "Explore endless hills and daring jumps.\\n\\n" +
                "Balance every climb. Master the landing.\\n\\n" +
                "Collect rewards and build your next adventure.";
'''+s[end:]
start=s.index('            string raceFeatures =')
end=s.index('            RacingDesign.Label(raceCard.transform, raceFeatures',start)
s=s[:start]+'''            string raceFeatures = "Race eleven rivals through the neon city.\\n\\n" +
                "Chain drifts, turbos and aerial stunts.\\n\\n" +
                "Find faster routes. Climb the ranked ladder.";
'''+s[end:]
s=s.replace('"PLAY ADVENTURE >"','"ADVENTURE"').replace('"ENTER RACING 3D >"','"RACING"')
s=re.sub(r'            // (?:Automation / Contract alias buttons|Extra alias buttons for test coverage)\n','',s)
p.write_text(s,encoding='utf-8')
p=Path('Assets/AnimeHillRacing/Scripts/Racing3D/UI/RacingModeSelectUI.cs')
s=p.read_text(encoding='utf-8-sig').replace('THE CANYON IS CALLING.','CHASE THE CITY LIGHTS.').replace('CANYON RUSH','NIGHT CITY CIRCUIT')
s=s.replace('            RacingFeedback.Ensure();','            RacingTestSession.Clear();\n            RacingFeedback.Ensure();',1)
begin=s.index('        private GameObject mmrToolPanel;')
s=s[:begin]+'''        GameObject mmrToolPanel;
        int testMmr=1000,testSeed=42,selectedRacer=1;
        RectTransform inspector,detail;
        RacerIdentity[] inspectedLobby;
        static readonly int[] Presets={1000,10000,25000,50000,75000,90000,100000,125000};
        void OpenMmrTestTool(RectTransform root)
        {
            if(mmrToolPanel!=null){CloseInspector();return;}
            testMmr=RacingSaveManager.CurrentMmr;
            inspector=RacingDesign.Area(root,"Test MMR Panel",.03f,.04f,.94f,.92f);mmrToolPanel=inspector.gameObject;
            mmrToolPanel.AddComponent<RacingScreenMotion>();RefreshMmrToolView();
        }
        void CloseInspector(){Destroy(mmrToolPanel);mmrToolPanel=null;}
        void RefreshMmrToolView()
        {
            foreach(Transform child in inspector){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            RacingDesign.Panel(inspector,"Modal backdrop",0,0,1,1,new Color(.035f,.05f,.09f,.99f),8);
            RacingDesign.Label(inspector,"TEST MMR",RacingType.Title,.03f,.91f,.55f,.07f,RacingDesign.Cyan,true?TextAnchor.MiddleLeft:TextAnchor.MiddleLeft);
            RacingDesign.Button(inspector,"CLOSE",.85f,.92f,.12f,.055f,CloseInspector);
            RacingDesign.Label(inspector,$"{testMmr:N0} MMR / SEED {testSeed} / YOUR SAVED RANK IS PRESERVED",RacingType.Caption,.03f,.855f,.75f,.05f,RacingDesign.Muted);
            RacingDesign.Button(inspector,"REROLL",.83f,.855f,.14f,.05f,()=>{testSeed+=17;RefreshMmrToolView();});
            for(int p=0;p<Presets.Length;p++)
            {
                int value=Presets[p];string label=value==125000?"100K+":(value/1000)+"K";
                RacingDesign.Button(inspector,label,.03f+p*.118f,.775f,.11f,.06f,()=>{testMmr=value;RefreshMmrToolView();},testMmr==value?RacingDesign.Cyan:RacingDesign.Highlight);
            }
            inspectedLobby=MatchmakingService.GenerateRankedLobby(testMmr,RacingSaveManager.SelectedVehicleId,RacingSaveManager.SelectedWheelId,"TEST PLAYER",testSeed);
            int exact=0,near=0;foreach(var racer in inspectedLobby){if(racer.isHuman)continue;if(racer.buildCategory==MetaBuildCategory.Meta)exact++;if(racer.buildCategory==MetaBuildCategory.NearMeta)near++;}
            RacingDesign.Label(inspector,$"11 AI: {exact} EXACT META / {near} NEAR META / {11-exact-near} ALTERNATIVE    •    SELECT A RACER TO INSPECT ITS SKILLS",RacingType.Caption,.03f,.72f,.94f,.045f,RacingDesign.Ink);
            var grid=RacingDesign.Area(inspector,"Lobby rows",.03f,.265f,.94f,.44f);
            string[] heads={"GRID / RACER","AI MMR","BUILD / META CHANCE","VEHICLE / WHEELS"};float[] xs={.01f,.28f,.44f,.68f},ws={.26f,.15f,.23f,.31f};
            for(int c=0;c<4;c++)RacingDesign.Label(grid,heads[c],RacingType.Caption,xs[c],.935f,ws[c],.06f,RacingDesign.Muted);
            for(int i=0;i<inspectedLobby.Length;i++)
            {
                int index=i;var r=inspectedLobby[i];float y=.865f-i*.077f;
                var row=RacingDesign.Panel(grid,"Inspect "+r.displayName,0,y,1,.071f,i==selectedRacer?RacingDesign.Highlight:(i%2==0?RacingDesign.Surface:RacingDesign.Background),3);
                var button=row.gameObject.AddComponent<Button>();button.targetGraphic=row;row.raycastTarget=true;
                button.onClick.AddListener(()=>{selectedRacer=index;RefreshMmrToolView();});
                RacingDesign.Label(row.transform,$"{r.gridPosition:00} / {r.displayName}",RacingType.Caption,.01f,0,.26f,1,r.isHuman?RacingDesign.Cyan:RacingDesign.Ink);
                RacingDesign.Label(row.transform,r.mmr.ToString("N0"),RacingType.Caption,.28f,0,.15f,1,RacingDesign.TierColor(r.mmr));
                RacingDesign.Label(row.transform,$"{r.buildCategory} / {r.metaProbabilityUsed:P0}",RacingType.Caption,.44f,0,.23f,1,r.buildCategory==MetaBuildCategory.Meta?RacingDesign.Gold:RacingDesign.Ink);
                RacingDesign.Label(row.transform,RacingDesign.VehicleName(r.vehicleId)+" / "+r.wheelId.Replace("wheel-","").ToUpperInvariant(),RacingType.Caption,.68f,0,.31f,1,RacingDesign.Ink);
            }
            detail=RacingDesign.Area(inspector,"Selected AI skills",.03f,.065f,.76f,.18f);
            var selected=inspectedLobby[selectedRacer];var skill=AiSkillModel.CreateProfile(selected.mmr,selected.personalitySeed);
            RacingDesign.Label(detail,selected.displayName+" / "+selected.buildName,RacingType.Section,0,.75f,1,.24f,RacingDesign.Cyan);
            string text=selected.isHuman?"HUMAN INPUT / NO AI ASSISTANCE":
                $"LINE {skill.racingLineSkill:P0}  •  BRAKING {skill.brakingSkill:P0}  •  DRIFT {skill.driftSkill:P0}  •  CHAINING {skill.chainingSkill:P0}\\n"+
                $"SHORTCUTS {skill.shortcutKnowledge:P0} / EXECUTION {skill.shortcutExecution:P0}  •  ITEMS {skill.itemTiming:P0}\\n"+
                $"CONSISTENCY {skill.consistency:P0}  •  AWARENESS {skill.awareness:P0}  •  AGGRESSION {skill.aggression:P0}  •  TRICKS {skill.trickSkill:P0}";
            RacingDesign.Label(detail,text,RacingType.Body,0,0,1,.72f,RacingDesign.Ink);
            RacingDesign.Button(inspector,"DRIVE TEST",.81f,.12f,.16f,.09f,()=>{RacingTestSession.Set(testMmr,testSeed);SceneManager.LoadScene("RacingGarage");});
            RacingDesign.Label(inspector,"TEST RACES DO NOT CHANGE YOUR RATING OR REWARDS.",RacingType.Caption,.03f,.013f,.94f,.04f,RacingDesign.Muted);
        }
    }
    public static class RacingTestSession
    {
        public static bool Active {get;private set;}
        public static int Mmr {get;private set;}
        public static int Seed {get;private set;}
        public static void Set(int mmr,int seed){Mmr=Mathf.Max(0,mmr);Seed=seed;Active=true;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear(){Active=false;}
    }
}
'''
s=s.replace('RacingDesign.Cyan,true?TextAnchor.MiddleLeft:TextAnchor.MiddleLeft','RacingDesign.Cyan')
p.write_text(s,encoding='utf-8')
