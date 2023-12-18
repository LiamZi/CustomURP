
using UnityEngine;

namespace CustomURP
{
    public abstract class Noise : NoiseInterface
    {
        public int _period = 16;

        public abstract float Generator(Vector3 pos);
    }
}
