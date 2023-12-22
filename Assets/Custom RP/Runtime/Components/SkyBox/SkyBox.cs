using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace CustomURP
{
    [ExecuteInEditMode]
    public class SkyBox : MonoBehaviour
    {
        [Range(0f, 24f)]
        public float _time = 9f;
        public bool _timeTick;
        public SkyTimeDataController _skyTimeDataController;

        public Texture2D _starTex;
        public Texture2D _moonTex;
        public Light _mainLightSun;
        public Light _mainLightMoon;
        public Transform _sunTransform;
        public Transform _moonTransform;
        public Transform _milkyWayTransform;
        public Material _skyBoxMaterial;
        private float _updateGITime = 0f;
        bool _dayOrNightChanging = false;

        SkyTimeData _currentSkyTimeData;

        void OnEnable()
        {
            _skyTimeDataController = GetComponent<SkyTimeDataController>();
        }

        void Update()
        {
            if (_timeTick)
            {
                _time += Time.deltaTime;
                _updateGITime += Time.deltaTime;
            }

            _time %= 24f;
            
            if (!_dayOrNightChanging)
            {
                if (Mathf.Abs(_time - 6f) < 0.01f)
                {
                    StartCoroutine("ChangeToDay");
                }

                if (Mathf.Abs(_time - 18.0f) < 0.01f)
                {
                    StartCoroutine("ChangeToNight");
                }
            }

            if (_updateGITime > 0.5f)
            {
                // DynamicGI.UpdateEnvironment();
            }

            _currentSkyTimeData = _skyTimeDataController.GetSkyTimeData(_time);
            
            ControllerSunAndMoonTransform();
            SetProperties();
        }

        public void ControllerSunAndMoonTransform()
        {
            _mainLightSun.transform.eulerAngles = new Vector3((_time - 6) * 180 / 12, 180, 0);
            if (_time >= 18)
            {
                _mainLightMoon.transform.eulerAngles = new Vector3((_time - 18) * 180 / 12, 180, 0);
            }
            else if (_time >= 0)
            {
                _mainLightMoon.transform.eulerAngles = new Vector3((_time) * 180 / 12 + 90, 180, 0);
            }
                

            _sunTransform.eulerAngles = _mainLightSun.transform.eulerAngles;
            _moonTransform.eulerAngles = _mainLightMoon.transform.eulerAngles;
            
            _skyBoxMaterial.SetVector(ShaderParams._sunDirectionWS, _sunTransform.forward);
            _skyBoxMaterial.SetVector(ShaderParams._moonDreictinWS, _moonTransform.forward);
        }

        void SetProperties()
        {
            _skyBoxMaterial.SetTexture(ShaderParams._starTex, _starTex);
            _skyBoxMaterial.SetTexture(ShaderParams._moonTex, _moonTex);
            _skyBoxMaterial.SetTexture(ShaderParams._skyGradientTex, _currentSkyTimeData._skyColorGraidentTex);
            _skyBoxMaterial.SetFloat(ShaderParams._starIntensity, _currentSkyTimeData._starIntensity);
            _skyBoxMaterial.SetFloat(ShaderParams._milkyWayIntensity, _currentSkyTimeData._milkyWayIntensity);
            _skyBoxMaterial.SetFloat(ShaderParams._scatteringIntensity, _currentSkyTimeData._scatteringIntensity);
            _skyBoxMaterial.SetFloat(ShaderParams._sunIntensity, _currentSkyTimeData._sunIntensity);
            _skyBoxMaterial.SetMatrix(ShaderParams._moonWorld2Local, _moonTransform.worldToLocalMatrix);
            _skyBoxMaterial.SetMatrix(ShaderParams._milkyWayWorld2Local, _milkyWayTransform.worldToLocalMatrix);
        }

        IEnumerable ChangeToNight()
        {
            _dayOrNightChanging = true;
            Light moon = _mainLightSun;
            Light sun = _mainLightMoon;
            moon.enabled = true;
            float updateTime = 0f;
            while(updateTime <= 1)
            {
                updateTime += Time.deltaTime;
                moon.intensity = Mathf.Lerp(moon.intensity, 0.7f, updateTime);
                sun.intensity = Mathf.Lerp(sun.intensity, 0, updateTime);
            
                yield return 0;
            }
            sun.enabled = false;
            _dayOrNightChanging = false;
        }

        IEnumerable ChangeToDay()
        {
            _dayOrNightChanging = true;
            Light moon = _mainLightSun;
            Light sun = _mainLightMoon;
            sun.enabled = true;
            float updateTime = 0f;
            while(updateTime <= 1)
            {
                moon.intensity = Mathf.Lerp(moon.intensity, 0f, updateTime);
                sun.intensity = Mathf.Lerp(sun.intensity, 1f, updateTime);
                updateTime += Time.deltaTime;

                yield return 0;
            }
            moon.enabled = false;
            _dayOrNightChanging = false;
        }
        
    };
}
