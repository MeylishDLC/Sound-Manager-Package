using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.Serialization;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace Runtime
{
    public class SoundManager: MonoBehaviour
    {
        [Header("Volume")] 
        [Range(0, 1)] public float masterVolume = 1;
        [Range(0, 1)] public float musicVolume = 1;
        [Range(0, 1)] public float SFXVolume = 1;
        
        public Bus MasterBus;
        public Bus MusicBus; 
        public Bus SfxBus;

        private Dictionary<string, EventReference> _musicList = new();
        private readonly List<EventInstance> _eventInstances = new();
        private EventInstance _currentMusic;
        private void Start()
        {
            MasterBus = RuntimeManager.GetBus("bus:/");
            MusicBus = RuntimeManager.GetBus("bus:/Music");
            SfxBus = RuntimeManager.GetBus("bus:/SFX");
        }
        private void Update()
        {
            MasterBus.setVolume(masterVolume);
            MusicBus.setVolume(musicVolume);
            SfxBus.setVolume(SFXVolume);
        }
        public void InitializeGameMusic(Dictionary<string, EventReference> musicList)
        {
            _musicList = musicList;
        }
        public void ChangeMusic(string musicName)
        {
            if (_musicList.ContainsKey(musicName))
            {
                StopCurrentMusic();
                InitializeMusic(_musicList[musicName]);
            }
            else
            {
                Debug.LogError($"No music with the name {musicName} was found. Make sure you initialized game music.");
            }
        }
        public void PauseCurrentMusic(bool paused)
        {
            _currentMusic.setPaused(paused);
        }
        public void StopCurrentMusic()
        {
            _currentMusic.stop(STOP_MODE.ALLOWFADEOUT);
            _currentMusic.release();
        }
        public void PlayOneShot(EventReference sound)
        {
            RuntimeManager.PlayOneShot(sound);
        }
        public void PlayMusicDuringTime(float time, EventReference music)
        {
            var instance = CreateInstance(music);
    
            var wrapper = new EventInstanceWrapper(instance);
    
            wrapper.Instance.start();

            StopMusicAfterTime(wrapper, time).Forget();
        }
        private async UniTask StopMusicAfterTime(EventInstanceWrapper wrapper, float time)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(time));
    
            wrapper.Instance.stop(STOP_MODE.ALLOWFADEOUT);
    
            wrapper.Instance.release();
    
            _eventInstances.Remove(wrapper.Instance);
        }
        private void InitializeMusic(EventReference musicEventReference)
        {
            _currentMusic = CreateInstance(musicEventReference);
            _currentMusic.start();
        }
        private EventInstance CreateInstance(EventReference eventReference)
        {
            var eventInstance = RuntimeManager.CreateInstance(eventReference);
            _eventInstances.Add(eventInstance);
            return eventInstance;
        }
        private void CleanUp()
        {
            foreach (var eventInstance in _eventInstances)
            {
                eventInstance.stop(STOP_MODE.IMMEDIATE);
                eventInstance.release();
            }
        }
        private void OnDestroy()
        {
            CleanUp();
        }
    }
    public class EventInstanceWrapper
    {
        public EventInstance Instance { get; }

        public EventInstanceWrapper(EventInstance instance)
        {
            Instance = instance;
        }
    }
}