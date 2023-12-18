using UnityEngine;
namespace CustomURP
{
    [System.Serializable]
    public class CurlNoiseGenerator : TextureGeneratorInterface
    {
        public Texture2D _perlinNoise;
        public float _brightness = 10.0f;
        public float _scale = 10.0f;
        public PerlinNoiseGenerator _perlin;

        private float Generator(float x, float y)
        {
            return _perlin.Generator(new Vector3(x, y));
        }

        public Color Sample(Vector3 pos)
        {
            return Sample(pos.x, pos.y);
        }

        public Color Sample(float x, float y)
        {
            float eps = 1.0f / 128.0f;
            float n1, n2, a, b;
            
            n1 = Generator(x, y + eps);
            n2 = Generator(x, y - eps);
            a = (n1 - n2) / 2;
            n1 = Generator(x + eps, y);
            n2 = Generator(x - eps, y);
            b = (n1 - n2) / 2;

            Vector2 tmp = new Vector2(a, -b);
            return new Color(tmp.x, tmp.y, 0);
        }
    }
}
