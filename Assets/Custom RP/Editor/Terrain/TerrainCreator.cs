

using System;
using UnityEditor;
using UnityEngine;

namespace CustomURP
{

    [CreateAssetMenu(menuName = "Custom URP/Terrain/TerrainDataEditorConfig")]
    public class TerrainDataEditorConfig : ScriptableObject
    {
        public TerrainMeshData _terrainMeshData;
    }
    
    public class TerrainCreator : EditorWindow
    {
        int _quadTreeDepth = 2;
        // public UnityEngine.Terrain _terrainData;
        bool _createUV2 = false;
        int _lodCount = 1;
        bool _bakeMaterial = false;
        int _bakeTextureSize = 2048;
        Vector2 _scrollPos;
        
        SerializedObject _configSO;
        TerrainDataEditorConfig _terrainDataConfig;

        [MenuItem("SRP Tools/Terrain/TerrainMeshCreator")]
        static void Init()
        {
            TerrainCreator window = (TerrainCreator)EditorWindow.GetWindow(typeof(TerrainCreator));
            window.Show();
        }

        void OnGUI()
        {
            // var target = EditorGUILayout.ObjectField("Convert TerrianData", _terrainData, typeof(UnityEngine.Terrain), true) as UnityEngine.Terrain;
            
            // if (target != _terrainData)
            // {
            //     _terrainData = target;
            // }

            // int currentSliceSize = Mathf.FloorToInt(1 << _quadTreeDepth);
            // int sliceSize = EditorGUILayout.IntField("Slice Size (NxN)", currentSliceSize);
            // if (currentSliceSize != sliceSize)
            // {
            //     currentSliceSize = Mathf.NextPowerOfTwo(sliceSize);
            //     _quadTreeDepth = Mathf.FloorToInt(Mathf.Log(currentSliceSize, 2));
            // }
            //
            // Debug.LogError("11111111");
            
            EditorGUI.BeginChangeCheck();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height));
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical();
                {
                    // EditorGUILayout.PropertyField("")
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
            
            if (EditorGUI.EndChangeCheck())
            {
                // ConfigSO.ApplyModifiedProperties();
            }
        }


        TerrainDataEditorConfig Config
        {
            get
            {
                if (_terrainDataConfig == null)
                {
                    _terrainDataConfig = AssetDatabase.LoadAssetAtPath<TerrainDataEditorConfig>("Assets/Resources/Editor/Terrain/TerrainDataConfig.asset");
                    if (_terrainDataConfig == null)
                    {
                        var newConfig = ScriptableObject.CreateInstance<TerrainDataEditorConfig>();
                        AssetDatabase.CreateAsset(newConfig, "Assets/Resources/Editor/Terrain/TerrainDataConfig.asset");
                        _terrainDataConfig = newConfig;
                    }
                }
                return _terrainDataConfig;
            }
        }

    }
}
