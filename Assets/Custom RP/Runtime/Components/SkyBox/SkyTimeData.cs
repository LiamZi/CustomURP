using UnityEngine;


namespace CustomURP
{
    
    public class SkyTimeData : ScriptableObject
    {
        public Gradient _skyColorGradient;
        public float _sunIntensity;
        public float _scatteringIntensity;
        public float _starIntensity;
        public float _milkyWayIntensity;

        [HideInInspector]
        public Texture2D _skyColorGraidentTex;
    };
}
