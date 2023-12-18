using UnityEngine;

namespace CustomURP
{
    [System.Serializable]
    public class WorleyNoiseGenerator : Noise
    {
        public float _brightness = 1.0f;
        public float _contrast = 1.0f;
        public int _octaves = 4;
        
        public override float Generator(Vector3 pos)
        {
            return Mathf.Clamp01(((OctaveNoise(pos, _period, _octaves)) + (_brightness - 1.0f)) * _contrast);
        }
        
        public static float OctaveNoise(Vector3 pos, int period, int octaves, float persistence = 0.5f)
        {
            float result = 0.0f;
            float amp = 0.5f;
            float freq = 1.0f;
            float totalAmp = 0.0f;
            for (int i = 0; i < octaves; ++i)
            {
                totalAmp += amp;
                result += Generator(pos, Mathf.RoundToInt(freq * period)) * amp;
                amp *= persistence;
                freq /= persistence;
            }

            if (octaves == 0) return 0.0f;
            return result / totalAmp;
        }

        public static float Generator(Vector3 pos, int period)
        {
            pos *= period;
            var x = Mathf.FloorToInt(pos.x);
            var y = Mathf.FloorToInt(pos.y);
            var z = Mathf.FloorToInt(pos.z);
            
            Vector3Int boxPos = new Vector3Int(x, y, z);
            float minDistance = float.MaxValue;
            
            
            for (int xoffset = -1; xoffset <= 1; xoffset++) {
                for (int yoffset = -1; yoffset <= 1; yoffset++) {
                    for (int zoffset = -1; zoffset <= 1; zoffset++) {
                        var newboxPos = boxPos + new Vector3Int(xoffset, yoffset, zoffset);
                        var hashValue = (NoiseUtils.Wrap(newboxPos.x, period, true) + 
                                            NoiseUtils.Wrap(newboxPos.y, period, true) * 131 + 
                                            NoiseUtils.Wrap(newboxPos.z, period, true) * 17161) % int.MaxValue;
                        
                        UnityEngine.Random.InitState(hashValue);
                        var featurePoint = new Vector3(UnityEngine.Random.value + newboxPos.x, 
                                                        UnityEngine.Random.value + newboxPos.y, 
                                                        UnityEngine.Random.value + newboxPos.z);
                        
                        minDistance = Mathf.Min(minDistance, Vector3.Distance(pos, featurePoint));
                    }
                }
            }
            return 1.0f - minDistance;
        }
    }
}
