using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Diagnostics;

namespace CustomURP
{
    public class VolumeCloudEditor : EditorWindow
    {
        private VolumeCloudEditorConfig _config;
        private SerializedObject _configSO;
        Texture2D _testTexPreview;
        Texture2D _firstTexPreview;
        Texture2D _secondTexPreview;
        Vector2 _scrollPos;
        
        private VolumeCloudEditorConfig Config
        {
            get
            {
                if (_config == null)
                {
                    _config = AssetDatabase.LoadAssetAtPath<VolumeCloudEditorConfig>("Assets/Resources/Editor/VolumeCloudConfig.asset");
                    if (_config == null)
                    {
                        var newConfig = ScriptableObject.CreateInstance<VolumeCloudEditorConfig>();
                        AssetDatabase.CreateAsset(newConfig, "Assets/Resources/Editor/VolumeCloudConfig.asset");
                        this._config = newConfig;
                    }
                }
                return _config;
            }
        }

        private SerializedObject ConfigSO
        {
            get
            {
                return _configSO ?? (_configSO = new SerializedObject(Config));
            }
        }

        [MenuItem("SRP Tools/VolumeCloud")]
        static void Init()
        {
            VolumeCloudEditor window = (VolumeCloudEditor)EditorWindow.GetWindow(typeof(VolumeCloudEditor));
            window.Show();
        }

        void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height));

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.PropertyField(ConfigSO.FindProperty("_testGenerator"), true);
                    if (GUILayout.Button("Save Texture"))
                    {
                        var texture = NoiseUtils.Get3DTexture(Config._testGenerator, 128, TextureFormat.RGB24);
                        AssetDatabase.CreateAsset(texture, "Assets/Resources/Textures/SkyBox/VolumeCloud/" + "TestTex" + ".asset");
                        AssetDatabase.SaveAssets();
                    }
                }
                
                EditorGUILayout.EndVertical();
            
                if (ConfigSO.FindProperty("_testGenerator").isExpanded)
                {
                    EditorGUILayout.BeginVertical(GUILayout.Width(256.0f));
                    {
                        if (GUILayout.Button("Preview"))
                        {
                            _testTexPreview = NoiseUtils.GetPreviewTexture(Config._testGenerator, 128, TextureFormat.RGFloat);
                        }
            
                        if (GUILayout.Button("Save Preview"))
                        {
                            AssetDatabase.CreateAsset(_testTexPreview, "Assets/Resources/Textures/SkyBox/VolumeCloud/" + "curlNoise" + ".asset");
                            AssetDatabase.SaveAssets();
                        }
            
                        if (_testTexPreview != null)
                        {
                            GUI.DrawTexture(EditorGUILayout.GetControlRect(false, GUILayout.Width(256.0f), GUILayout.Height(256.0f)), _testTexPreview);
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            //3d texture
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.PropertyField(ConfigSO.FindProperty("_tex3DGenerator"), true);
                    if (ConfigSO.FindProperty("_tex3DGenerator").isExpanded)
                    {
                        EditorGUILayout.PropertyField(ConfigSO.FindProperty("_tex3DGeneratorSaveName"));
                        if (GUILayout.Button("Save Texture"))
                        {
                            var texture = NoiseUtils.Get3DTexture(Config._tex3DGenerator, Config._tex3DGenerator._texResolution);
                            AssetDatabase.CreateAsset(texture, "Assets/Resources/Textures/SkyBox/VolumeCloud/" + Config._tex3DGeneratorSaveName + ".asset");
                            AssetDatabase.SaveAssets();
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            
                EditorGUILayout.BeginVertical(GUILayout.Width(256.0f));
                {
                    if (GUILayout.Button("Preview"))
                    {
                        _firstTexPreview = NoiseUtils.GetPreviewTexture(Config._tex3DGenerator, Config._tex3DGenerator._texResolution);
                    }
                    
                    if (_firstTexPreview != null)
                    {
                        GUI.DrawTexture(EditorGUILayout.GetControlRect(false, GUILayout.Width(256.0f), GUILayout.Height(256.0f)), _firstTexPreview);
                    }
                    
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
            
            //second 3d texture
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.PropertyField(ConfigSO.FindProperty("_tex3DGeneratorSecond"), true);
                    if (ConfigSO.FindProperty("_tex3DGeneratorSecond").isExpanded)
                    {
                        EditorGUILayout.PropertyField(ConfigSO.FindProperty("_tex3DSecondSaveName"));
                        if (GUILayout.Button("Save Texture"))
                        {
                            var texture = NoiseUtils.Get3DTexture(Config._tex3DGeneratorSecond, Config._tex3DGeneratorSecond._texResolution, TextureFormat.RGB24);
                            AssetDatabase.CreateAsset(texture, "Assets/Resources/Textures/SkyBox/VolumeCloud/" + Config._tex3DSecondSaveName + ".asset");
                            AssetDatabase.SaveAssets();
                        }
                    }
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(GUILayout.Width(256.0f));
                {
                    if (GUILayout.Button("Preview"))
                    {
                        _secondTexPreview = NoiseUtils.GetPreviewTexture(Config._tex3DGeneratorSecond, Config._tex3DGeneratorSecond._texResolution);
                    }
                    
                    if (_secondTexPreview != null)
                    {
                        GUI.DrawTexture(EditorGUILayout.GetControlRect(false, GUILayout.Width(256.0f), GUILayout.Height(256.0f)), _secondTexPreview);
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
            
            
            EditorGUILayout.EndScrollView();
            if (EditorGUI.EndChangeCheck())
            {
                ConfigSO.ApplyModifiedProperties();
            }
        }
    }
}
