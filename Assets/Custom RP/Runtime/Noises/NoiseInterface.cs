
using UnityEngine;

namespace CustomURP
{
    public interface NoiseInterface
    {
        float Generator(Vector3 pos);
    }

    public interface TextureGeneratorInterface
    {
        Color Sample(Vector3 pos);
    }

    public class NoiseTextureAdapter : TextureGeneratorInterface
    {
        NoiseInterface _noiseIF;

        public NoiseTextureAdapter(NoiseInterface noise)
        {
            this._noiseIF = noise;
        }

        public Color Sample(Vector3 pos)
        {
            return _noiseIF.Generator(pos) * Color.white;
        }
    }
}
