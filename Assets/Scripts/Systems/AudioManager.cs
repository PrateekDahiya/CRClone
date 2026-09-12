using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using CRClone.Core;

namespace CRClone.Systems
{
    public class AudioManager : MonoBehaviour
    {
        [Header("Mixer")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private AudioMixerGroup _masterGroup;
        [SerializeField] private AudioMixerGroup _musicGroup;
        [SerializeField] private AudioMixerGroup _sfxGroup;
        [SerializeField] private AudioMixerGroup _voiceGroup;

        [Header("Settings")]
        [SerializeField] private int _maxAudioSources = 32;

        private Dictionary<string, AudioClip> _clips = new();
        private Queue<AudioSource> _availableSources = new();
        private List<AudioSource> _activeSources = new();
        private AudioSource _musicSource;
        private Coroutine _musicFadeCoroutine;

        public float MasterVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 0.8f;
        public float SFXVolume { get; private set; } = 1f;
        public float VoiceVolume { get; private set; } = 1f;

        public void Initialize()
        {
            // Create audio sources pool
            for (int i = 0; i < _maxAudioSources; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                _availableSources.Enqueue(source);
            }

            // Create dedicated music source
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.outputAudioMixerGroup = _musicGroup;

            LoadSettings();
            Debug.Log("[AudioManager] Initialized");
        }

        private void LoadSettings()
        {
            var settings = Services.Get<GameManager>()?.LocalPlayer?.settings;
            if (settings != null)
            {
                SetMasterVolume(settings.masterVolume / 100f);
                SetMusicVolume(settings.musicVolume / 100f);
                SetSFXVolume(settings.sfxVolume / 100f);
                SetVoiceVolume(settings.voiceVolume / 100f);
            }
        }

        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            _audioMixer?.SetFloat("MasterVolume", VolumeToDb(MasterVolume));
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            _audioMixer?.SetFloat("MusicVolume", VolumeToDb(MusicVolume));
            _musicSource.volume = MusicVolume;
        }

        public void SetSFXVolume(float volume)
        {
            SFXVolume = Mathf.Clamp01(volume);
            _audioMixer?.SetFloat("SFXVolume", VolumeToDb(SFXVolume));
        }

        public void SetVoiceVolume(float volume)
        {
            VoiceVolume = Mathf.Clamp01(volume);
            _audioMixer?.SetFloat("VoiceVolume", VolumeToDb(VoiceVolume));
        }

        private float VolumeToDb(float volume)
        {
            return volume > 0 ? Mathf.Log10(volume) * 20f : -80f;
        }

        public void PlayMusic(string clipName, float fadeDuration = 1f)
        {
            if (_clips.TryGetValue(clipName, out var clip))
            {
                if (_musicFadeCoroutine != null)
                    StopCoroutine(_musicFadeCoroutine);
                _musicFadeCoroutine = StartCoroutine(FadeMusic(clip, fadeDuration));
            }
            else
            {
                Debug.LogWarning($"[AudioManager] Music clip not found: {clipName}");
            }
        }

        private System.Collections.IEnumerator FadeMusic(AudioClip newClip, float duration)
        {
            float startVolume = _musicSource.volume;
            float targetVolume = MusicVolume;

            // Fade out
            float t = 0f;
            while (t < duration * 0.5f)
            {
                t += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(startVolume, 0f, t / (duration * 0.5f));
                yield return null;
            }

            _musicSource.clip = newClip;
            _musicSource.Play();

            // Fade in
            t = 0f;
            while (t < duration * 0.5f)
            {
                t += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(0f, targetVolume, t / (duration * 0.5f));
                yield return null;
            }

            _musicSource.volume = targetVolume;
        }

        public void StopMusic(float fadeDuration = 1f)
        {
            if (_musicFadeCoroutine != null)
                StopCoroutine(_musicFadeCoroutine);
            _musicFadeCoroutine = StartCoroutine(FadeOutMusic(fadeDuration));
        }

        private System.Collections.IEnumerator FadeOutMusic(float duration)
        {
            float startVolume = _musicSource.volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(startVolume, 0f, t / duration);
                yield return null;
            }
            _musicSource.Stop();
            _musicSource.volume = MusicVolume;
        }

        public AudioSource PlaySFX(string clipName, Vector3 position, float volume = 1f, float pitch = 1f, bool spatial = true)
        {
            if (!_clips.TryGetValue(clipName, out var clip))
            {
                Debug.LogWarning($"[AudioManager] SFX clip not found: {clipName}");
                return null;
            }

            var source = GetSource();
            source.clip = clip;
            source.volume = volume * SFXVolume;
            source.pitch = pitch;
            source.spatialBlend = spatial ? 1f : 0f;
            source.outputAudioMixerGroup = _sfxGroup;
            source.transform.position = position;
            source.Play();

            StartCoroutine(ReturnSourceWhenDone(source, clip.length / pitch));
            return source;
        }

        public AudioSource PlaySFX(string clipName, float volume = 1f, float pitch = 1f)
        {
            return PlaySFX(clipName, Vector3.zero, volume, pitch, false);
        }

        public AudioSource PlayVoice(string clipName, Vector3 position, float volume = 1f)
        {
            if (!_clips.TryGetValue(clipName, out var clip)) return null;

            var source = GetSource();
            source.clip = clip;
            source.volume = volume * VoiceVolume;
            source.spatialBlend = 0f; // Voice is usually 2D
            source.outputAudioMixerGroup = _voiceGroup;
            source.transform.position = position;
            source.Play();

            StartCoroutine(ReturnSourceWhenDone(source, clip.length));
            return source;
        }

        private AudioSource GetSource()
        {
            if (_availableSources.Count > 0)
            {
                return _availableSources.Dequeue();
            }

            // All sources in use - reuse oldest
            var source = _activeSources[0];
            _activeSources.RemoveAt(0);
            source.Stop();
            return source;
        }

        private System.Collections.IEnumerator ReturnSourceWhenDone(AudioSource source, float duration)
        {
            _activeSources.Add(source);
            yield return new WaitForSeconds(duration + 0.1f);
            _activeSources.Remove(source);
            source.Stop();
            _availableSources.Enqueue(source);
        }

        public void RegisterClip(string name, AudioClip clip)
        {
            _clips[name] = clip;
        }

        public void RegisterClips(AudioClip[] clips)
        {
            foreach (var clip in clips)
            {
                _clips[clip.name] = clip;
            }
        }

        public AudioClip GetClip(string name)
        {
            _clips.TryGetValue(name, out var clip);
            return clip;
        }

        public void PauseAll()
        {
            _musicSource.Pause();
            foreach (var source in _activeSources)
            {
                source.Pause();
            }
        }

        public void ResumeAll()
        {
            _musicSource.UnPause();
            foreach (var source in _activeSources)
            {
                source.UnPause();
            }
        }

        public void MuteAll(bool mute)
        {
            _audioMixer?.SetFloat("MasterVolume", mute ? -80f : VolumeToDb(MasterVolume));
        }
    }
}