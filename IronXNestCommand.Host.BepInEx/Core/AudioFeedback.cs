using System;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace IronXNestCommand.Host.BepInEx.Core
{
    public static class AudioFeedback
    {
        private static AudioSource _audioSource;
        private static AudioClip _clickClip;
        private static AudioClip _switchClip;
        private static AudioClip _levelUpClip;

        public static void Initialize()
        {
            try
            {
                var go = new GameObject("IronX_AudioFeedback");
                GameObject.DontDestroyOnLoad(go);
                _audioSource = go.AddComponent<AudioSource>();
                if (_audioSource != null)
                {
                    _audioSource.volume = 0.45f;
                    _audioSource.spatialBlend = 0f; // 2D Sound
                }

                _clickClip = GenerateToneClip("Click", 800f, 0.025f, 0.3f);
                _switchClip = GenerateToneClip("Switch", 1200f, 0.04f, 0.4f);
                _levelUpClip = GenerateFanfareClip("LevelUp");
            }
            catch { }
        }

        public static void PlayClick()
        {
            PlayClip(_clickClip, 0.4f);
        }

        public static void PlayTargetSwitch()
        {
            PlayClip(_switchClip, 0.5f);
        }

        public static void PlayLevelUp()
        {
            PlayClip(_levelUpClip, 0.8f);
        }

        public static void PlaySuccess()
        {
            PlayClip(_clickClip, 0.6f);
        }

        private static void PlayClip(AudioClip clip, float vol)
        {
            try
            {
                if (_audioSource != null && clip != null)
                {
                    _audioSource.PlayOneShot(clip, vol);
                }
            }
            catch { }
        }

        private static AudioClip GenerateToneClip(string name, float freq, float duration, float decay)
        {
            try
            {
                int sampleRate = 44100;
                int samples = (int)(sampleRate * duration);

                return AudioClip.Create(name, samples, 1, sampleRate, false, new Action<Il2CppStructArray<float>>(data =>
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        float t = (float)i / sampleRate;
                        float env = (float)Math.Exp(-t * (1.0 / decay) * 25.0);
                        data[i] = (float)Math.Sin(2.0 * Math.PI * freq * t) * env;
                    }
                }));
            }
            catch
            {
                return null;
            }
        }

        private static AudioClip GenerateFanfareClip(string name)
        {
            try
            {
                int sampleRate = 44100;
                float duration = 0.5f;
                int samples = (int)(sampleRate * duration);
                float[] freqs = { 523.25f, 659.25f, 783.99f, 1046.50f }; // C5, E5, G5, C6

                return AudioClip.Create(name, samples, 1, sampleRate, false, new Action<Il2CppStructArray<float>>(data =>
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        float t = (float)i / sampleRate;
                        int noteIndex = Math.Clamp((int)(t / (duration / freqs.Length)), 0, freqs.Length - 1);
                        float freq = freqs[noteIndex];
                        float noteT = t - (noteIndex * (duration / freqs.Length));
                        float env = (float)Math.Exp(-noteT * 12.0);
                        data[i] = (float)Math.Sin(2.0 * Math.PI * freq * t) * env * 0.7f;
                    }
                }));
            }
            catch
            {
                return null;
            }
        }
    }
}
