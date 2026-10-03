using UnityEngine;
namespace AnimeHillRacing.Racing3D
{
    [CreateAssetMenu(menuName="Hill Adventurer/Racing/Audio bank")]
    public sealed class RacingAudioBank : ScriptableObject
    {
        public AudioClip Engine, Tires, Wind;
        public AudioClip[] Cues = new AudioClip[15];
        public AudioClip[] Music = new AudioClip[4];
        [Range(0,1)] public float MasterVolume = .65f;
    }
}
