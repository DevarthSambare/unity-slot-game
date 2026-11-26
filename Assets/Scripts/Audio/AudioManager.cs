using System.Collections;
using SlotGame.Feel;
using UnityEngine;

namespace SlotGame.Audio
{
    /// <summary>
    /// Plays every sound in the game.
    /// - small pool of 2D AudioSources for one shots, so overlapping sounds don't cut each other off
    ///   and each one can get its own pitch variation
    /// - a dedicated looping source for the reel spin
    /// - no background ambience on purpose, the crowd noise got tiring fast. The reels and wins
    ///   carry the soundscape instead (ticks, stop thunks, match chimes)
    /// - one master mute (M key / speaker button), remembered in PlayerPrefs
    ///
    /// No AudioMixer on purpose: for a handful of 2D sounds a simple gain layer is enough and it
    /// keeps the WebGL build simple.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const string MutedPrefKey = "slot.audio.muted";

        [SerializeField] private SoundLibrary sounds;
        [SerializeField] private int oneShotVoices = 8;

        [Header("Mix")]
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.9f;
        [Range(0f, 1f)] [SerializeField] private float reelLoopVolume = 0.4f;

        private AudioSource[] _voices;
        private int _nextVoice;
        private AudioSource _reelLoop;
        private Coroutine _reelFade;

        public SoundLibrary Sounds => sounds;
        public bool IsMuted { get; private set; }

        private void Awake()
        {
            _voices = new AudioSource[oneShotVoices];
            for (int i = 0; i < _voices.Length; i++)
                _voices[i] = CreateSource($"Voice {i}", loop: false);

            _reelLoop = CreateSource("Reel Loop", loop: true);

            IsMuted = PlayerPrefs.GetInt(MutedPrefKey, 0) == 1;
            AudioListener.volume = IsMuted ? 0f : 1f;
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f; // everything is UI/2D
            return source;
        }

        /// <param name="pitchVariance">Random +/- pitch so repeated sounds (reel stops, clicks) don't sound robotic.</param>
        public void Play(AudioClip clip, float volume = 1f, float pitch = 1f, float pitchVariance = 0f)
        {
            if (clip == null)
                return;

            // round robin through the pool, oldest voice gets reused
            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;

            voice.clip = clip;
            voice.volume = volume * sfxVolume;
            voice.pitch = pitch + Random.Range(-pitchVariance, pitchVariance);
            voice.Play();
        }

        public void PlayClick() => Play(sounds.uiClick, 0.8f, 1f, 0.05f);

        public void StartReelLoop()
        {
            if (sounds.reelSpinLoop == null) return;
            if (_reelFade != null) StopCoroutine(_reelFade);

            _reelLoop.clip = sounds.reelSpinLoop;
            _reelLoop.volume = reelLoopVolume;
            _reelLoop.pitch = 1f;
            if (!_reelLoop.isPlaying)
                _reelLoop.Play();
        }

        /// <summary>Pitch the loop up a bit during anticipation, sells the extra speed.</summary>
        public void SetReelLoopPitch(float pitch) => _reelLoop.pitch = pitch;

        public void StopReelLoop(float fadeTime = 0.15f)
        {
            if (!_reelLoop.isPlaying) return;
            if (_reelFade != null) StopCoroutine(_reelFade);
            _reelFade = StartCoroutine(FadeOutReelLoop(fadeTime));
        }

        private IEnumerator FadeOutReelLoop(float fadeTime)
        {
            float from = _reelLoop.volume;
            yield return Ease.Tween(fadeTime, t => _reelLoop.volume = Mathf.Lerp(from, 0f, t));
            _reelLoop.Stop();
            _reelFade = null;
        }

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
            AudioListener.volume = IsMuted ? 0f : 1f;
            PlayerPrefs.SetInt(MutedPrefKey, IsMuted ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
