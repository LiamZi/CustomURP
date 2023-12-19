using UnityEngine;

namespace CustomURP
{
    [CreateAssetMenu (menuName = "Custom URP/Weahters/Volume Cloud")]
    public class VolumeCloudConfig : ScriptableObject
    {
        [Header("Weather map")]
        [Tooltip("R for coverage, G for density, B for cloud type")]
        public Texture2D _weatherTex;
        public float _weatherTexSize = 40000;

        [Header("Shape")]
        public Vector2 _cloudHeightRange = new Vector2(1500.0f, 9000.0f);
        public Texture3D _baseTexture = null;

        [Range(0, 5)]
        public float _baseTile = 2.0f;
        public Texture2D _heightDensityMap = null;
        public float _overallSize = 50000;
        public float _topOffset = 0.0f;

        [Header("Shape Detail")]
        public Texture3D _detailTexture;
        [Range(0, 80)]
        public float _detailTile = 36.0f;
        [Range(0.01f, 0.5f)]
        public float _detailStrength = 0.2f;

        [Header("Shape  Curl")]
        public Texture2D _curlNoise = null;
        [Range(0.001f, 1.0f)]
        public float _curlTile = 0.01f;
        public float _curlStrength = 5.0f;

        [Header("Shape - Modifiers")]
        [Range(0, 1)]
        public float _overallDensity = 1.0f;
        [Range(0, 1)]
        public float _cloudTypeModifier = 1.0f;
        [Range(0, 1)]
        public float _cloudCoverageModifier = 1.0f;

        [Header("Shape - Wind")]
        public Vector2 _winDirection;
        public float _windSpeed;

        [Header("Lighting")]
        public Color _ambientColor = new Color(214, 37, 154);
        public const float COEFFICIENT_SCALE = 1e-2f;
        [Range(0.1f, 2.0f)]
        public float _scatteringCoefficient = 0.5f;
        [Range(0.1f, 2.0f)]
        public float _extinctionCoefficient = 0.52f;

        [Header("Lighting Multi Scattering Approximation")]
        [Range(0.01f, 1f)]
        [Tooltip("Value used in multi-scattering approximation, higher causes more light be scattered. Must be lower than multiScatteringExtinction.")]
        public float _multiScatteringScattering = 0.5f;
        [Range(0.01f, 1f)]
        [Tooltip("Value used in multi-scattering approximation, higher causes more light be extincted. Must be higher than multiScatteringScattering.")]
        public float _multiScatteringExtinction = 0.5f;
        [Range(0.01f, 1)]
        [Tooltip("Value used in multi-scattering approximation, phase function is p(g * pow(multiScatteringEC, octave), theta)")]
        public float _multiScatteringEC = 0.5f;

        [Header("Lighting Silver")]
        public float _silverSpread = 0.1f;

        [Header("Lighting Atmosphere")]
        public float _atmosphereSaturateDistance = 100000.0f;
        public Color _atomsphereColor = new Color(160.0f / 255.0f, 180.0f / 255.0f, 200.0f / 255.0f);

        



    }
}
