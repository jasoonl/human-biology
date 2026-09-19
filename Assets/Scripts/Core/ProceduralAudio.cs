using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Generates short sine-wave AudioClips at runtime so quiz feedback has sound
    /// cues without needing any imported audio assets.
    /// </summary>
    public static class ProceduralAudio
    {
        private const float SampleRate = 44100f;

        /// <summary>A quick two-note ascending chime (C5 -> E5) for a correct answer.</summary>
        public static AudioClip GenerateCorrectChime()
        {
            var first = GenerateToneSamples(523.25f, 0.12f, decay: 8f);
            var second = GenerateToneSamples(659.25f, 0.16f, decay: 6f);

            var combined = new float[first.Length + second.Length];
            first.CopyTo(combined, 0);
            second.CopyTo(combined, first.Length);

            return BuildClip("CorrectChime", combined);
        }

        /// <summary>A short, flat low buzz for a wrong answer.</summary>
        public static AudioClip GenerateWrongBuzz()
        {
            var samples = GenerateToneSamples(196f, 0.28f, decay: 3f, harshness: 0.35f);
            return BuildClip("WrongBuzz", samples);
        }

        private static float[] GenerateToneSamples(float frequency, float duration, float decay, float harshness = 0f)
        {
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
            var samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / SampleRate;
                float envelope = Mathf.Exp(-decay * t / duration);
                float wave = Mathf.Sin(2f * Mathf.PI * frequency * t);

                if (harshness > 0f)
                {
                    // Blend in a touch of square-wave clipping for a buzzier timbre.
                    float square = Mathf.Sign(wave);
                    wave = Mathf.Lerp(wave, square, harshness);
                }

                samples[i] = wave * envelope * 0.5f;
            }

            return samples;
        }

        private static AudioClip BuildClip(string name, float[] samples)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, (int)SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
