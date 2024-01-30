using UnityEngine;

namespace CustomURP
{
    public class LodPolicy : ScriptableObject
    {
        public float[] _screenCover;

        public int GetLodLevel(float screenSize, float screenW)
        {
            if (_screenCover != null)
            {
                float rate = screenSize / screenW;
                for (int i = 0; i < _screenCover.Length; ++i)
                {
                    if (rate >= _screenCover[i])
                        return i;
                }
            }
            return 0;
        }
    };
};
