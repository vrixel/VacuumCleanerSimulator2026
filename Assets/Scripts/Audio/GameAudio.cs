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

        AudioSource hum, sfx, ui;
        AudioClip pop, gulp, boing, whoosh, ding, levelUp, clunk, bagFull, start, fanfare;
        float humVolTarget;
        float humPitchTarget = 0.85f;

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
            hum = gameObject.AddComponent<AudioSource>();
            hum.clip = MakeHum();
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
        }

        public void PlayPop(int sizeClass)
        {
            bool big = sizeClass >= 3;
            sfx.pitch = Random.Range(0.92f, 1.18f) - sizeClass * 0.06f;
            sfx.PlayOneShot(big ? gulp : pop, big ? 0.9f : 0.55f);
        }

        public void PlayBoing() { sfx.pitch = Random.Range(0.95f, 1.1f); sfx.PlayOneShot(boing, 0.5f); }
        public void PlayWhoosh() { sfx.pitch = 1f; sfx.PlayOneShot(whoosh, 0.6f); }
        public void PlayClunk() { sfx.pitch = 1f; sfx.PlayOneShot(clunk, 0.8f); }
        public void PlayDing() { ui.pitch = 1f; ui.PlayOneShot(ding, 0.7f); }
        public void PlayLevelUp() { ui.pitch = 1f; ui.PlayOneShot(levelUp, 0.8f); }
        public void PlayBagFull() { ui.pitch = 1f; ui.PlayOneShot(bagFull, 0.7f); }
        public void PlayStart() { ui.pitch = 1f; ui.PlayOneShot(start, 0.7f); }
        public void PlayFanfare() { ui.pitch = 1f; ui.PlayOneShot(fanfare, 0.8f); }
    }
}
