
using System.Collections.Generic;
using UnityEngine;
namespace CustomURP
{

    public interface IWaterHeightProvider
    {
        bool Contains(Vector3 worldPos);
        float GetHeight(Vector3 worldPos);
    };
    
    public class WaterHeight
    {
        static List<IWaterHeightProvider> _providers = new List<IWaterHeightProvider>();

        public static void RegProvider(IWaterHeightProvider provider)
        {
            _providers.Add(provider);
        }

        public static void UnregProvider(IWaterHeightProvider provider)
        {
            _providers.Remove(provider);
        }

        public static float GetWaterHeight(Vector3 groundPos)
        {
            float h = groundPos.y;
            for (int i = 0; i < _providers.Count; ++i)
            {
                var water = _providers[i];
                if (water.Contains(groundPos))
                {
                    float wh = water.GetHeight(groundPos);
                    if (wh > h)
                        return wh;
                }
            }

            return h;
        }
    }
}
