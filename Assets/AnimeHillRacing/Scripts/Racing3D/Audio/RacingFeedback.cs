using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public enum RacingCue { UiFocus, UiConfirm, Countdown, Go, Pickup, ItemReady, ItemUse, ItemImpact, DriftCharge, Boost, Collision, Landing, PositionUp, FinalLap, Finish, DriftHop, MiniTurbo }
    public enum RacingMusicPhase { PreRace, Race, FinalLap, Results }
    public sealed class RacingFeedback : MonoBehaviour
    {
        static RacingFeedback instance;
        static readonly Dictionary<int,AudioClip> synthesized = new Dictionary<int,AudioClip>();
        AudioSource[] voices;
        AudioSource[] music;
        RacingAudioBank bank;
        int voiceIndex, musicIndex;
        float lastFocus;
        public static RacingFeedback Ensure()
        {
            if(instance!=null)return instance;
            return instance = new GameObject("Racing sound stage").AddComponent<RacingFeedback>();
        }
        void Awake()
        {
            instance=this;bank=Resources.Load<RacingAudioBank>("RacingAudioBank");
            voices=new AudioSource[8];for(int i=0;i<voices.Length;i++){voices[i]=gameObject.AddComponent<AudioSource>();voices[i].playOnAwake=false;voices[i].priority=80;}
            music=new AudioSource[2];for(int i=0;i<2;i++){music[i]=gameObject.AddComponent<AudioSource>();music[i].loop=true;music[i].playOnAwake=false;}
        }
        public static void Play(RacingCue cue,Vector3 position=default)
        {
            var bus=Ensure();if(cue==RacingCue.UiFocus && Time.unscaledTime-bus.lastFocus<.075f)return;
            if(cue==RacingCue.UiFocus)bus.lastFocus=Time.unscaledTime;
            var source=bus.voices[bus.voiceIndex++%bus.voices.Length];source.Stop();source.transform.position=position;
            // Synthesized original tones are a replaceable fallback, not recorded production assets.
            AudioClip clip=bus.bank!=null&&bus.bank.Cues!=null&&(int)cue<bus.bank.Cues.Length?bus.bank.Cues[(int)cue]:null;
            source.clip=clip!=null?clip:Tone((int)cue);
            source.volume=(bus.bank!=null?bus.bank.MasterVolume:.55f)*(cue==RacingCue.UiFocus?.12f:.34f);
            source.spatialBlend=0;source.Play();
        }
        public static void Music(RacingMusicPhase phase)
        {
            var bus=Ensure();int old=bus.musicIndex;bus.musicIndex=1-old;
            AudioClip clip=bus.bank!=null&&bus.bank.Music!=null&&(int)phase<bus.bank.Music.Length?bus.bank.Music[(int)phase]:null;
            bus.music[bus.musicIndex].clip=clip;bus.music[bus.musicIndex].volume=0;
            if(clip!=null)bus.music[bus.musicIndex].Play();
        }
        void Update()
        {
            for(int i=0;i<2;i++){music[i].volume=Mathf.MoveTowards(music[i].volume,i==musicIndex?.32f:0,Time.unscaledDeltaTime*.6f);if(i!=musicIndex&&music[i].volume==0)music[i].Stop();}
        }
        public static AudioClip Tone(int kind,bool loop=false)
        {
            int key=kind+(loop?100:0);if(synthesized.TryGetValue(key,out var cached)&&cached!=null)return cached;
            int rate=22050,count=loop?rate:Mathf.RoundToInt(rate*(kind==14?.75f:.18f));float[] data=new float[count];
            float frequency=kind==3?740:kind==2?440:kind==100?88:270+kind*31;
            uint seed=7931;
            for(int i=0;i<count;i++)
            {
                float t=i/(float)rate;float envelope=loop?1:Mathf.Min(1,t*80)*Mathf.Exp(-t*18);
                seed=seed*1664525+1013904223;
                float noise=((seed>>8)&65535)/32768f-1f;
                float sample=kind==101?noise*.15f:Mathf.Sin(t*frequency*Mathf.PI*2)+.28f*Mathf.Sin(t*frequency*Mathf.PI*4);
                data[i]=sample*envelope*.23f;
            }
            var clip=AudioClip.Create("Original racing synthesis "+key,count,1,rate,false);clip.SetData(data,0);synthesized[key]=clip;return clip;
        }
        void OnDestroy(){if(instance==this)instance=null;}
    }
    public sealed class RacingVehicleAudio : MonoBehaviour
    {
        RacingVehiclePhysics vehicle;
        AudioSource engine, wind, tires;
        float pitch;
        public void Initialize(bool player)
        {
            vehicle=GetComponent<RacingVehiclePhysics>();var bank=Resources.Load<RacingAudioBank>("RacingAudioBank");
            engine=Create(bank!=null?bank.Engine:null,100,player?.20f:.045f,player?0:1);
            if(player){wind=Create(bank!=null?bank.Wind:null,101,0,0);tires=Create(bank!=null?bank.Tires:null,101,0,0);}
            vehicle.OnBoost+=Boost;vehicle.OnImpact+=Impact;vehicle.OnLanding+=Land;vehicle.OnDriftStage+=Stage;
        }
        AudioSource Create(AudioClip authored,int tone,float volume,float spatial)
        {
            var source=gameObject.AddComponent<AudioSource>();source.clip=authored!=null?authored:RacingFeedback.Tone(tone,true);source.loop=true;source.volume=volume;source.spatialBlend=spatial;source.minDistance=3;source.maxDistance=34;source.priority=140;source.Play();return source;
        }
        void Update()
        {
            if(vehicle==null)return;float speed=Mathf.Clamp01(vehicle.SpeedKmh/vehicle.Stats.SpeedKmh);
            float gear=Mathf.Floor(speed*4);float rpm=Mathf.Repeat(speed*4,1);
            float acceleration=Mathf.Clamp01(vehicle.LastCommands.Throttle-speed*.3f);
            float target=.7f+rpm*.38f+gear*.1f+acceleration*.16f+(vehicle.IsDrifting?.05f:0);
            pitch=Mathf.Lerp(pitch,target,1-Mathf.Exp(-Time.deltaTime*9));engine.pitch=Mathf.Clamp(pitch,.65f,1.8f);
            if(wind!=null){wind.volume=speed*speed*.1f;tires.volume=vehicle.IsDrifting?.07f:vehicle.IsOffroad?.055f:0;tires.pitch=1.2f+speed*.7f;}
        }
        void Boost(){if(wind!=null)RacingFeedback.Play(RacingCue.Boost);}
        void Impact(float value){if(wind!=null)RacingFeedback.Play(RacingCue.Collision);}
        void Land(float value){if(wind!=null)RacingFeedback.Play(RacingCue.Landing);}
        void Stage(MiniTurboStage stage){if(wind!=null)RacingFeedback.Play(RacingCue.DriftCharge);}
        void OnDestroy(){if(vehicle!=null){vehicle.OnBoost-=Boost;vehicle.OnImpact-=Impact;vehicle.OnLanding-=Land;vehicle.OnDriftStage-=Stage;}}
    }
}
