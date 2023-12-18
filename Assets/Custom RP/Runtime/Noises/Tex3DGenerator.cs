using UnityEngine;

namespace CustomURP
{
    [System.Serializable]
    public class Tex3DGenerator : TextureGeneratorInterface
    {
        public int _texResolution = 32;
        public int _perlinOctaves = 4;
        public int _channel1PerlinPeriod = 16;
        public int _channel2WorleyPeriod = 16;
        public int _channel3WorleyPeriod = 32;
        public int _channel4WorleyPeriod = 64;

        public Color Sample(Vector3 pos)
        {
            Color res = new Color();
            res.r = PerlinNoiseGenerator.OctaveNoise(pos, _channel1PerlinPeriod, _perlinOctaves);
            res.g = WorleyNoiseGenerator.OctaveNoise(pos, _channel2WorleyPeriod, 3);
            res.b = WorleyNoiseGenerator.OctaveNoise(pos, _channel3WorleyPeriod, 3);
            res.a = WorleyNoiseGenerator.OctaveNoise(pos, _channel4WorleyPeriod, 3);
            
            return res;
        }
    }

    [System.Serializable]
    public class Tex3DGeneratorSecond : TextureGeneratorInterface
    {
        public int _texResolution = 32;
        public int channel1WorleyFreq = 16;
        public int _channel2WorleyFreq = 32;
        public int _channel3WorleyFreq = 64;

        public Color Generator(Vector3 pos)
        {
            Color res = new Color();
            res.r = WorleyNoiseGenerator.OctaveNoise(pos, channel1WorleyFreq, 3);
            res.g = WorleyNoiseGenerator.OctaveNoise(pos, _channel2WorleyFreq, 3);
            res.b = WorleyNoiseGenerator.OctaveNoise(pos, _channel3WorleyFreq, 3);
            res.a = 1.0f;
            
            return res;
        }

        public Color Sample(Vector3 pos)
        {
            return Generator(pos);
        }
        
    }
}
