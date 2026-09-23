using UnityEngine;

namespace Game
{
    public enum Sfx
    {
        SpinStart,
        ReelStop,
        Anticipation,
        ScatterLand,
        Win,
    }

    /// <summary>
    /// Synthesizes the sound effects in code (game spec "Sound" sheet SE-01..05). A placeholder until real assets exist.
    /// </summary>
    public sealed class SynthAudio : MonoBehaviour
    {
        private const int Rate = 44100;
        private AudioSource _source;
        private AudioSource _loop;
        private AudioClip[] _clips;

        public bool Muted { get; set; }

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _loop = gameObject.AddComponent<AudioSource>();
            _loop.playOnAwake = false;
            _loop.loop = true;
            _clips = new[]
            {
                Make("spin", 0.15f, (t, d) => Sine(Lerp(300, 900, t / d), t) * Env(t, d, 0.01f) * 0.35f),
                Make("stop", 0.07f, (t, d) => (Noise() * 0.5f + Sine(120, t)) * Mathf.Exp(-t * 60f) * 0.6f),
                Make("anticipation", 0.9f, (t, d) => Sine(Lerp(400, 1200, t / d), t) * (0.6f + 0.4f * Mathf.Sin(t * 60f)) * Env(t, d, 0.05f) * 0.18f),
                Make("scatter", 0.5f, (t, d) => (Sine(1318.5f, t) + Sine(1975.5f, t) * 0.6f) * Mathf.Exp(-t * 7f) * 0.25f),
                Make("win", 0.36f, (t, d) =>
                {
                    var notes = new[] { 523.25f, 659.25f, 783.99f, 1046.5f };
                    var i = Mathf.Min(notes.Length - 1, (int)(t / 0.07f));
                    var local = t - i * 0.07f;
                    return Sine(notes[i], t) * Mathf.Exp(-local * 18f) * 0.3f;
                }),
            };
        }

        public void Play(Sfx sfx)
        {
            if (Muted || _clips == null) return;
            if (sfx == Sfx.Anticipation)
            {
                _loop.clip = _clips[(int)sfx];
                if (!_loop.isPlaying) _loop.Play();
                return;
            }
            _source.PlayOneShot(_clips[(int)sfx]);
        }

        public void StopLoop()
        {
            if (_loop != null) _loop.Stop();
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        private static float Sine(float hz, float t) => Mathf.Sin(2f * Mathf.PI * hz * t);

        private static float Noise() => Random.value * 2f - 1f;

        private static float Env(float t, float d, float fade) => Mathf.Clamp01(t / fade) * Mathf.Clamp01((d - t) / fade);

        private static AudioClip Make(string name, float seconds, System.Func<float, float, float> wave)
        {
            var n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (var i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate, seconds), -1f, 1f);
            var clip = AudioClip.Create("sfx_" + name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
