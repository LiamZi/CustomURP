using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace CustomURP
{
    [System.Serializable]
    public class PerlinNoiseGenerator : Noise
    {
        public int _octaves = 4;
        
        public override float Generator(Vector3 pos)
        {
            return OctaveNoise(pos, _period, _octaves);
        }

        public static float OctaveNoise(Vector3 pos, int period, int octaves, float persistence = 0.5f)
        {
            float total = 0.0f;
            float result = 0.0f;
            float amp = 0.5f;
            float freq = 1.0f;

            for (int i = 0; i < octaves; ++i)
            {
                total += amp;
                result += (Generator(pos, Mathf.RoundToInt(freq * period)) * 2.0f - 1.0f) * amp;
                amp *= persistence;
                freq *= 2.0f;
            }

            if (octaves == 0) return 0.0f;

            return (result / total + 1.0f) / 2.0f;
        }

        public static float Generator(Vector3 pos, int period)
        {
            pos *= period;
            var x = pos.x;
            var y = pos.y;
            var z = pos.z;
            var X = Mathf.FloorToInt(x) & 0xff;
            var Y = Mathf.FloorToInt(y) & 0xff;
            var Z = Mathf.FloorToInt(z) & 0xff;
            x -= Mathf.Floor(x);
            y -= Mathf.Floor(y);
            z -= Mathf.Floor(z);


            var u = NoiseUtils.Fade(x);
            var v = NoiseUtils.Fade(y);
            var w = NoiseUtils.Fade(z);
            var A = (NoiseUtils.Perm[NoiseUtils.Wrap(X, period)] + Y) & 0xff;
            var B = (NoiseUtils.Perm[NoiseUtils.Wrap(X + 1, period)] + Y) & 0xff;
            var AA = (NoiseUtils.Perm[NoiseUtils.Wrap(A, period)] + Z) & 0xff;
            var BA = (NoiseUtils.Perm[NoiseUtils.Wrap(B, period)] + Z) & 0xff;
            var AB = (NoiseUtils.Perm[NoiseUtils.Wrap(A + 1, period)] + Z) & 0xff;
            var BB = (NoiseUtils.Perm[NoiseUtils.Wrap(B + 1, period)] + Z) & 0xff;
            var result = Mathf.LerpUnclamped(w, Mathf.LerpUnclamped(v, Mathf.LerpUnclamped(u, NoiseUtils.Grad(NoiseUtils.Perm[AA], x, y, z), NoiseUtils.Grad(NoiseUtils.Perm[BA], x - 1, y, z)),
                    Mathf.LerpUnclamped(u, NoiseUtils.Grad(NoiseUtils.Perm[AB], x, y - 1, z), NoiseUtils.Grad(NoiseUtils.Perm[BB], x - 1, y - 1, z))),
                Mathf.LerpUnclamped(v, Mathf.LerpUnclamped(u, NoiseUtils.Grad(NoiseUtils.Perm[AA + 1], x, y, z - 1), NoiseUtils.Grad(NoiseUtils.Perm[BA + 1], x - 1, y, z - 1)),
                    Mathf.LerpUnclamped(u, NoiseUtils.Grad(NoiseUtils.Perm[AB + 1], x, y - 1, z - 1), NoiseUtils.Grad(NoiseUtils.Perm[BB + 1], x - 1, y - 1, z - 1))));
            
            return (result + 1.0f) / 2.0f;
        }
    }
}
