using UnityEngine;


namespace CustomURP
{
    [CreateAssetMenu(fileName = "VolumetricLightConfig", menuName = "Rendering/Volumetric Light Settings")]
    public class VolumetricLightConfig : ScriptableObject
    {
        public enum VolumtericRes
        {
            NONE,
            Full,
            Half,
            Quarter
        };

        [SerializeField]
        public VolumtericRes _res = VolumtericRes.Half;
    };
};
