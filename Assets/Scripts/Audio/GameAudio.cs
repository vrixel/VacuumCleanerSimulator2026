using UnityEngine;

namespace VCS.Audio
{
    /// <summary>
    /// All sound is synthesised at startup (no audio assets): a looping vacuum hum whose pitch follows the action,
    /// plus short one-shots for pops, hops, blowing, achievements and menus.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        const int Rate = 44100;

        AudioSource hum, sfx, ui, music;
        AudioClip pop, gulp, boing, whoosh, ding, levelUp, clunk, bagFull, start, fanfare, meow;
        AudioClip popReal, bagAlarmReal;
        float humVolTarget;
        float humPitchTarget = 0.85f;
        float musicVolTarget;
        const float MusicVolume = 0.32f;
        string currentMusic;
        bool ducked;

        public static GameAudio Create(Transform parent)
        {
            var go = new GameObject("Audio");
            go.transform.SetParent(parent, false);
            var a = go.AddComponent<GameAudio>();
            a.Init();
            return a;
        }

        void Init()
        {
            // a real motor recording (generated with kie.ai) replaces the synthesised hum when present
            var motor = Resources.Load<AudioClip>("Audio/Sfx/motor_loop");
            popReal = Resources.Load<AudioClip>("Audio/Sfx/pop_real");
            bagAlarmReal = Resources.Load<AudioClip>("Audio/Sfx/bag_alarm");

            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.spatialBlend = 0f;
            music.volume = 0f;
            music.ignoreListenerPause = true;

            hum = gameObject.AddComponent<AudioSource>();
            hum.clip = motor != null ? motor : MakeHum();
            hum.loop = true;
            hum.volume = 0f;
            hum.pitch = 0.85f;
            hum.playOnAwake = false;
            hum.spatialBlend = 0f;
            hum.Play();

            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;

            ui = gameObject.AddComponent<AudioSource>();
            ui.playOnAwake = false;
            ui.spatialBlend = 0f;
            ui.ignoreListenerPause = true;

            pop = Sweep("pop", 0.14f, 900f, 180f, 26f, 0.6f, 0.05f);
            gulp = Sweep("gulp", 0.35f, 320f, 90f, 9f, 0.7f, 0.1f);
            boing = Wobble("boing", 0.4f, 260f, 520f, 9f, 0.45f);
            whoosh = Noise("whoosh", 0.5f, 0.5f, 0.08f);
            ding = Chord("ding", new[] { 880f, 1318.5f, 1760f }, 0.7f, 5f, 0.35f);
            levelUp = Arpeggio("levelup", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.11f, 0.5f, 8f, 0.4f);
            clunk = Sweep("clunk", 0.25f, 140f, 60f, 14f, 0.8f, 0.3f);
            bagFull = Arpeggio("bagfull", new[] { 392f, 349.23f, 311.13f }, 0.16f, 0.4f, 7f, 0.4f);
            start = Arpeggio("start", new[] { 392f, 523.25f, 659.25f, 783.99f }, 0.09f, 0.5f, 8f, 0.4f);
            fanfare = Arpeggio("fanfare", new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 783.99f, 1046.5f, 1318.5f }, 0.12f, 0.9f, 4f, 0.45f);
            meow = Meow("meow", 0.5f, 520f, 880f, 440f, 0.5f);
        }

        static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // One second loop: 85 Hz saw (85 whole cycles, so it loops cleanly) + octave + low-passed noise.
        static AudioClip MakeHum()
        {
            int n = Rate;
            var d = new float[n];
            var rng = new System.Random(7);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float saw = 2f * (t * 85f - Mathf.Floor(t * 85f)) - 1f;
                float s2 = Mathf.Sin(2f * Mathf.PI * 170f * t);
                float noise = (float)rng.NextDouble() * 2f - 1f;
                lp += 0.12f * (noise - lp);
                d[i] = saw * 0.16f + s2 * 0.08f + lp * 0.35f;
            }
            return Make("hum", d);
        }

        // A cat: pitch rises then falls, 7 Hz vibrato, a few harmonics so it is not a sine, soft attack.
        static AudioClip Meow(string name, float dur, float f0, float f1, float f2, float amp)
        {
            int n = (int)(dur * Rate);
            var d = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = t / dur;
                float f = u < 0.4f ? Mathf.Lerp(f0, f1, u / 0.4f) : Mathf.Lerp(f1, f2, (u - 0.4f) / 0.6f);
                f *= 1f + 0.03f * Mathf.Sin(2f * Mathf.PI * 7f * t);
                phase += 2.0 * System.Math.PI * f / Rate;
                float s = (float)(System.Math.Sin(phase) + 0.5 * System.Math.Sin(2 * phase) + 0.25 * System.Math.Sin(3 * phase) + 0.12 * System.Math.Sin(4 * phase));
                float env = Mathf.Min(1f, t * 30f) * (u < 0.65f ? 1f : Mathf.Clamp01((1f - u) / 0.35f));
                d[i] = s * env * amp * 0.45f;
            }
            return Make(name, d);
        }

        static AudioClip Sweep(string name, float dur, float f0, float f1, float decay, float amp, float noise)
        {
            int n = (int)(dur * Rate);
            var d = new float[n];
            double phase = 0;
            var rng = new System.Random(3);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float f = Mathf.Lerp(f0, f1, t / dur);
                phase += 2.0 * System.Math.PI * f / Rate;
                float env = Mathf.Exp(-t * decay) * Mathf.Min(1f, t * 600f);
                float s = (float)System.Math.Sin(phase);
                if (noise > 0f) s = s * (1f - noise) + ((float)rng.NextDouble() * 2f - 1f) * noise;
                d[i] = s * env * amp;
            }
            return Make(name, d);
        }

        static AudioClip Wobble(string name, float dur, float f0, float f1, float decay, float amp)
        {
            int n = (int)(dur * Rate);
            var d = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float f = Mathf.Lerp(f0, f1, Mathf.Clamp01(t / (dur * 0.5f))) * (1f + 0.06f * Mathf.Sin(t * 60f));
                phase += 2.0 * System.Math.PI * f / Rate;
                float env = Mathf.Exp(-t * decay) * Mathf.Min(1f, t * 600f);
                d[i] = (float)System.Math.Sin(phase) * env * amp;
            }
            return Make(name, d);
        }

        static AudioClip Noise(string name, float dur, float amp, float lowpass)
        {
            int n = (int)(dur * Rate);
            var d = new float[n];
            var rng = new System.Random(5);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float noise = (float)rng.NextDouble() * 2f - 1f;
                lp += lowpass * (noise - lp);
                float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                d[i] = lp * env * amp;
            }
            return Make(name, d);
        }

        static AudioClip Chord(string name, float[] freqs, float dur, float decay, float amp)
        {
            int n = (int)(dur * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float s = 0f;
                foreach (var f in freqs) s += Mathf.Sin(2f * Mathf.PI * f * t);
                float env = Mathf.Exp(-t * decay) * Mathf.Min(1f, t * 600f);
                d[i] = s / freqs.Length * env * amp;
            }
            return Make(name, d);
        }

        static AudioClip Arpeggio(string name, float[] notes, float noteDur, float tail, float decay, float amp)
        {
            float dur = notes.Length * noteDur + tail;
            int n = (int)(dur * Rate);
            var d = new float[n];
            for (int k = 0; k < notes.Length; k++)
            {
                int startIdx = (int)(k * noteDur * Rate);
                float f = notes[k];
                for (int i = startIdx; i < n; i++)
                {
                    float t = (i - startIdx) / (float)Rate;
                    float env = Mathf.Exp(-t * decay) * Mathf.Min(1f, t * 600f);
                    d[i] += (Mathf.Sin(2f * Mathf.PI * f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * f * 2f * t)) * env * amp * 0.6f;
                }
            }
            for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(d[i], -1f, 1f);
            return Make(name, d);
        }

        public void SetHum(float intensity, bool blowing)
        {
            intensity = Mathf.Clamp01(intensity);
            humVolTarget = intensity <= 0f ? 0f : 0.10f + 0.30f * intensity;
            humPitchTarget = blowing ? 1.5f : 0.8f + 0.7f * intensity;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            hum.volume = Mathf.Lerp(hum.volume, humVolTarget, 1f - Mathf.Exp(-dt * 6f));
            hum.pitch = Mathf.Lerp(hum.pitch, humPitchTarget, 1f - Mathf.Exp(-dt * 5f));
            float mv = ducked ? musicVolTarget * 0.35f : musicVolTarget;
            music.volume = Mathf.Lerp(music.volume, mv, 1f - Mathf.Exp(-dt * 3f));
        }

        /// <summary>Plays Resources/Audio/Music/&lt;name&gt; on a loop, fading from whatever played before. Silent if missing.</summary>
        public void PlayMusic(string name)
        {
            if (name == currentMusic) return;
            var clip = Resources.Load<AudioClip>("Audio/Music/" + name);
            currentMusic = name;
            if (clip == null) { musicVolTarget = 0f; return; }
            StopAllCoroutines();
            StartCoroutine(SwapMusic(clip));
        }

        System.Collections.IEnumerator SwapMusic(AudioClip clip)
        {
            musicVolTarget = 0f;
            float t = 0f;
            while (t < 0.5f && music.isPlaying && music.volume > 0.02f) { t += Time.unscaledDeltaTime; yield return null; }
            music.clip = clip;
            music.volume = 0f;
            music.Play();
            musicVolTarget = MusicVolume;
        }

        public void DuckMusic(bool on) { ducked = on; }

        public void PlayPop(int sizeClass)
        {
            bool big = sizeClass >= 3;
            sfx.pitch = Random.Range(0.92f, 1.18f) - sizeClass * 0.06f;
            if (!big && popReal != null && Random.value < 0.5f) { sfx.PlayOneShot(popReal, 0.6f); return; }
            sfx.PlayOneShot(big ? gulp : pop, big ? 0.9f : 0.55f);
        }

        public void PlayBoing() { sfx.pitch = Random.Range(0.95f, 1.1f); sfx.PlayOneShot(boing, 0.5f); }
        public void PlayMeow() { sfx.pitch = Random.Range(0.9f, 1.15f); sfx.PlayOneShot(meow, 0.6f); }
        public void PlayYowl() { sfx.pitch = 0.72f; sfx.PlayOneShot(meow, 0.9f); }
        public void PlayClick() { ui.pitch = 1.35f; ui.PlayOneShot(pop, 0.35f); }
        /// <summary>One tooth of the cord reel: a tiny click, pitch jittered so the run sounds mechanical.</summary>
        public void PlayRatchet() { sfx.pitch = Random.Range(1.6f, 2.1f); sfx.PlayOneShot(clunk, 0.18f); }
        public void PlayThunk() { sfx.pitch = 0.75f; sfx.PlayOneShot(clunk, 0.9f); }
        public void PlayWhoosh() { sfx.pitch = 1f; sfx.PlayOneShot(whoosh, 0.6f); }
        public void PlayClunk() { sfx.pitch = 1f; sfx.PlayOneShot(clunk, 0.8f); }
        public void PlayDing() { ui.pitch = 1f; ui.PlayOneShot(ding, 0.7f); }
        public void PlayLevelUp() { ui.pitch = 1f; ui.PlayOneShot(levelUp, 0.8f); }
        public void PlayBagFull() { ui.pitch = 1f; ui.PlayOneShot(bagAlarmReal != null ? bagAlarmReal : bagFull, 0.7f); }
        public void PlayStart() { ui.pitch = 1f; ui.PlayOneShot(start, 0.7f); }
        public void PlayFanfare() { ui.pitch = 1f; ui.PlayOneShot(fanfare, 0.8f); }
    }
}
