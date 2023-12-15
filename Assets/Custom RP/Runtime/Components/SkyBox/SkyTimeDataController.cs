using System;
using UnityEngine;

namespace CustomURP
{
    [ExecuteInEditMode]
    public class SkyTimeDataController : MonoBehaviour
    {
        [System.Serializable]
        public class SkyTimeDataCollection
        {
            public SkyTimeData _time0;
            public SkyTimeData _time3;
            public SkyTimeData _time6;
            public SkyTimeData _time9;
            public SkyTimeData _time12;
            public SkyTimeData _time15;
            public SkyTimeData _time18;
            public SkyTimeData _time21;
        };
        
        // private SkyTimeData _newData = ScriptableObject.CreateInstance("CustomURP.SkyTimeData") as SkyTimeData;
        SkyTimeData _newData = null;
        public SkyTimeDataCollection _skyTimeDataCollection = new SkyTimeDataCollection();

        void OnEnable()
        {
            _newData = ScriptableObject.CreateInstance("CustomURP.SkyTimeData") as SkyTimeData;
        }

        public SkyTimeData GetSkyTimeData(float time)
        {
            SkyTimeData start = _skyTimeDataCollection._time0;
            SkyTimeData end = _skyTimeDataCollection._time0;

            if(time >= 0 && time < 3)
            {
                start = _skyTimeDataCollection._time0;
                end = _skyTimeDataCollection._time3;
            }
            else if(time >= 3 && time < 6)
            {
                start = _skyTimeDataCollection._time3;
                end = _skyTimeDataCollection._time6;
            }
            else if(time >= 6 && time < 9)
            {
                start = _skyTimeDataCollection._time6;
                end = _skyTimeDataCollection._time9;
            }
            else if(time >= 9 && time < 12)
            {
                start = _skyTimeDataCollection._time9;
                end = _skyTimeDataCollection._time12;
            }
            else if(time >= 12 && time < 15)
            {
                start = _skyTimeDataCollection._time12;
                end = _skyTimeDataCollection._time15;
            }
            else if(time >= 15 && time < 18)
            {
                start = _skyTimeDataCollection._time15;
                end = _skyTimeDataCollection._time18;
            }
            else if(time >= 18 && time < 21)
            {
                start = _skyTimeDataCollection._time18;
                end = _skyTimeDataCollection._time21;
            }
            else if(time >= 21 && time < 24)
            {
                start = _skyTimeDataCollection._time21;
                end = _skyTimeDataCollection._time0;
            }

            float lerpVal = (time % 3 / 3f);
            _newData._skyColorGraidentTex = GenerateSkyGradientColorTexture(start._skyColorGradient, end._skyColorGradient, 128, lerpVal);
            _newData._starIntensity = Mathf.Lerp(start._starIntensity, end._starIntensity, lerpVal);
            _newData._milkyWayIntensity = Mathf.Lerp(start._milkyWayIntensity, end._milkyWayIntensity, lerpVal);
            _newData._sunIntensity = Mathf.Lerp(start._sunIntensity, end._sunIntensity, lerpVal);
            _newData._scatteringIntensity = Mathf.Lerp(start._scatteringIntensity, end._scatteringIntensity, lerpVal);

            
            return _newData;
        }

        public Texture2D GenerateSkyGradientColorTexture(Gradient startGradient, Gradient endGradient, int resolution, float lerpValue)
        {
            Texture2D tex = new Texture2D(resolution, 1, TextureFormat.RGBAFloat, false, true);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int i = 0; i < resolution; ++i)
            {
                Color start = startGradient.Evaluate(i * 1.0f / resolution).linear;
                Color end = endGradient.Evaluate(i * 1.0f / resolution).linear;

                Color fin = Color.Lerp(start, end, lerpValue);

                tex.SetPixel(i, 0, fin);
            }
            
            tex.Apply(false, false);
            return tex;
        }
    };
}
