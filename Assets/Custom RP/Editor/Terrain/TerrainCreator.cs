

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CustomURP
{

    // [CreateAssetMenu(menuName = "Custom URP/Terrain/TerrainDataEditorConfig")]
    // public class TerrainDataEditorConfig : ScriptableObject
    // {
    //     public TerrainMeshData _terrainMeshData;
    //     public int _slices = 0;
    //     public int _lodSize = 1;
    //
    // }


    public class LodSetting : LodDetail
    {
        public bool _editorUIFoldout = true;

        public void OnGUIDraw(int index)
        {
            _editorUIFoldout = EditorGUILayout.Foldout(_editorUIFoldout, string.Format("Lod {0}", index));
            if (!_editorUIFoldout)
            {
                EditorGUI.indentLevel++;
                int curRate = Mathf.FloorToInt(Mathf.Pow(2, _subdivision));
                int sampleRate = EditorGUILayout.IntField("Sample (N x N)", curRate);
                if (curRate != sampleRate)
                {
                    curRate = Mathf.NextPowerOfTwo(sampleRate);
                    _subdivision = Mathf.FloorToInt(Mathf.Log(curRate, 2));
                }

                var error = EditorGUILayout.FloatField("Slope Angle Error", _slopeAngleError);
                _slopeAngleError = Mathf.Max(0.01f, error);
                EditorGUI.indentLevel--;
            }
        }
    };

    internal class MeshPrefabBaker
    {
        public int _lod { get; private set; }
        public int _meshId { get; private set; }
        public Mesh _mesh { get; set; }
        public Vector4 scaleOffset { get; set; }

        public MeshPrefabBaker(int i, int meshId, Mesh mesh, Vector2 uvMin, Vector2 uvMax)
        {
            _lod = i;
            _meshId = meshId;
            _mesh = mesh;
            var v = new Vector4(1, 1, 0, 0);
            v.x = uvMax.x - uvMin.x;
            v.y = uvMax.y - uvMin.y;
            v.z = uvMin.x;
            v.w = uvMin.y;
            scaleOffset = v;
        }
    };

    public class TerrainCreator : EditorWindow
    {
        int _quadTreeDepth = 2;
        public UnityEngine.Terrain _terrainData;
        bool _createUV2 = false;
        int _lodCount = 1;
        bool _bakeMaterial = false;
        int _bakeTextureSize = 2048;
        Vector2 _scrollPos;
        LodSetting[] _lodSettings = new LodSetting[0];
        MeshCreatorJob _dataCreateJob;
        TessellationJob _tessellationJob;

        // SerializedObject _configSO;
        // TerrainDataEditorConfig _terrainDataConfig;

        [MenuItem("SRP Tools/Terrain/TerrainMeshCreator")]
        static void Init()
        {
            TerrainCreator window = (TerrainCreator)EditorWindow.GetWindow(typeof(TerrainCreator));
            window.Show();
        }

        void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height));

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(256.0f));
                {
                    // EditorGUILayout.PropertyField(ConfigSO.FindProperty("_terrainMeshData"), true);

                    UnityEngine.Terrain currentTarget = EditorGUILayout.ObjectField("Convert Target", _terrainData, typeof(UnityEngine.Terrain), true) as UnityEngine.Terrain;
                    if (currentTarget != _terrainData)
                    {
                        _terrainData = currentTarget;
                    }

                    int curSliceCount = Mathf.FloorToInt(Mathf.Pow(2, _quadTreeDepth));
                    // EditorGUILayout.PropertyField(ConfigSO.FindProperty("_slices"), new GUIContent("Slice Size(N x N)"), true);
                    int slice = EditorGUILayout.IntField("Slice Size (N x N)", curSliceCount);
                    if (slice != curSliceCount)
                    {
                        curSliceCount = Mathf.NextPowerOfTwo(slice);
                        _quadTreeDepth = Mathf.FloorToInt(Mathf.Log(curSliceCount, 2));
                        // Config._slices = _quadTreeDepth;
                    }

                    if (_lodCount != _lodSettings.Length)
                    {
                        LodSetting[] origin = _lodSettings;
                        _lodSettings = new LodSetting[_lodCount];
                        var size = Mathf.Min(_lodCount, origin.Length);
                        var maxSize = Mathf.Max(_lodCount, origin.Length);
                        for (int i = 0; i < size; ++i)
                        {
                            _lodSettings[i] = origin[i];
                        }

                        for (int i = size; i < maxSize; ++i)
                        {
                            _lodSettings[i] = new LodSetting();
                        }
                    }

                    _lodCount = EditorGUILayout.IntField("Lod Size", _lodSettings.Length);
                    if (_lodSettings.Length > 0)
                    {
                        EditorGUI.indentLevel++;
                        for (int i = 0; i < _lodSettings.Length; ++i)
                        {
                            _lodSettings[i].OnGUIDraw(i);
                        }
                        EditorGUI.indentLevel--;
                    }

                    _bakeMaterial = EditorGUILayout.ToggleLeft("Bake Material", _bakeMaterial);
                    if (_bakeMaterial)
                    {
                        _bakeTextureSize = EditorGUILayout.IntField("Bake Texture Size", _bakeTextureSize);
                        _bakeTextureSize = Mathf.NextPowerOfTwo(_bakeTextureSize);
                    }

                    _createUV2 = EditorGUILayout.ToggleLeft("Geerate UV2", _createUV2);
                    if (GUILayout.Button("Generate"))
                    {
                        if (_lodSettings == null || _lodSettings.Length == 0)
                        {
                            Debug.Log("There aren't lod setting");
                            return;
                        }

                        if (_terrainData == null)
                        {
                            Debug.LogError("There aren't terrain data.");
                            return;
                        }

                        int gridMax = 1 << _quadTreeDepth;
                        var center = _terrainData.transform.TransformPoint(_terrainData.terrainData.bounds.center);
                        var extent = _terrainData.terrainData.bounds.size;
                        var terrainBounds = new Bounds(center, extent);
                        _dataCreateJob = new MeshCreatorJob(_terrainData, terrainBounds, gridMax, gridMax, _lodSettings);

                        for (int i = 0; i < int.MaxValue; ++i)
                        {
                            _dataCreateJob.Tick();
                            EditorUtility.DisplayProgressBar("Creating Data", "Scanning Volume", _dataCreateJob.Progress);
                            if (_dataCreateJob.Done)
                            {
                                break;
                            }
                        }
                        _dataCreateJob.End();

                        int maxSub = 1;
                        foreach (var s in _lodSettings)
                        {
                            if (s._subdivision > maxSub)
                                maxSub = s._subdivision;
                        }

                        float maxSubGrids = gridMax * (1 << maxSub);
                        float minArea = Mathf.Max(_terrainData.terrainData.bounds.size.x, _terrainData.terrainData.bounds.size.z) / maxSubGrids;
                        minArea = minArea * minArea / 8.0f;

                        _tessellationJob = new TessellationJob(_dataCreateJob._lods, minArea);
                        for (int i = 0; i < int.MaxValue; ++i)
                        {
                            _tessellationJob.Tick();
                            EditorUtility.DisplayProgressBar("Creating Data", "Tessellation", _tessellationJob.Progress);
                            if (_tessellationJob.Done)
                                break;
                        }

                        string[] lodFolder = new string[_lodSettings.Length];
                        for (int i = 0; i < _lodSettings.Length; ++i)
                        {
                            string guid = AssetDatabase.CreateFolder("Assets", string.Format("{0}_LOD{1}", _terrainData.name, i));
                            lodFolder[i] = AssetDatabase.GUIDToAssetPath(guid);
                        }

                        List<MeshPrefabBaker> bakers = new List<MeshPrefabBaker>();
                        var meshDataSize = _tessellationJob._mesh.Length;
                        for (int i = 0; i < meshDataSize; ++i)
                        {
                            EditorUtility.DisplayProgressBar("Saving Data", "Processing", (float)i / meshDataSize);
                            MeshData data = _tessellationJob._mesh[i];
                            for (int lod = 0; lod < data._lods.Length; ++lod)
                            {
                                var folder = lodFolder[lod];
                                if (!AssetDatabase.IsValidFolder(Path.Combine(folder, "Meshes")))
                                {
                                    AssetDatabase.CreateFolder(folder, "Meshes");
                                    AssetDatabase.Refresh();
                                }

                                var mesh = SaveMesh(folder + "/Meshes", data._meshId, data._lods[lod], _createUV2);
                                var baker = new MeshPrefabBaker(lod, data._meshId, mesh, data._lods[lod]._uvMin, data._lods[lod]._uvMax);
                                bakers.Add(baker);
                            }
                        }

                        GameObject[] prefabRoot = new GameObject[_lodSettings.Length];
                        if (_bakeMaterial)
                        {
                            BakeMeshesWithMaterial(bakers, currentTarget, lodFolder, prefabRoot);
                        }
                        else
                        {
                            List<Material> mats = new List<Material>();
                            var folder = lodFolder[0];
                            List<string> matPath = new List<string>();
                            MaterialUtils.SaveMixMaterials(folder, currentTarget.name, currentTarget, matPath);
                            foreach (var p in matPath)
                            {
                                var m = AssetDatabase.LoadAssetAtPath<Material>(p);
                                mats.Add(m);
                            }

                            for (int i = 0; i < bakers.Count; ++i)
                            {
                                EditorUtility.DisplayProgressBar("Saving Data", "Processing", (float)i / bakers.Count);
                                var baker = bakers[i];

                                if (prefabRoot[baker._lod] == null)
                                {
                                    prefabRoot[baker._lod] = new GameObject(currentTarget.name);
                                }

                                GameObject meshGO = new GameObject(baker._meshId.ToString());
                                var filter = meshGO.AddComponent<MeshFilter>();
                                filter.mesh = baker._mesh;
                                var renderer = meshGO.AddComponent<MeshRenderer>();
                                renderer.sharedMaterials = mats.ToArray();
                                meshGO.transform.parent = prefabRoot[baker._lod].transform;
                            }
                        }

                        for (int i = prefabRoot.Length - 1; i >= 0; --i)
                        {
                            var folder = lodFolder[i];
                            PrefabUtility.SaveAsPrefabAsset(prefabRoot[i], folder + "/" + currentTarget.name + ".prefab");
                            DestroyImmediate(prefabRoot[i]);
                        }

                        EditorUtility.ClearProgressBar();
                        AssetDatabase.Refresh();
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
            if (EditorGUI.EndChangeCheck())
            {
                // ConfigSO.ApplyModifiedProperties();
            }
        }

        Mesh SaveMesh(string folder, int dataId, MeshData.Lod data, bool isCreateUV2)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = data._vertices;
            mesh.normals = data._normals;
            mesh.uv = data._uvs;
            if (isCreateUV2)
            {
                mesh.uv2 = data._uvs;
            }

            mesh.triangles = data._faces;
            AssetDatabase.CreateAsset(mesh, string.Format("{0}{1}.mesh", folder, dataId));
            return mesh;
        }

        void BakeMeshesWithMaterial(List<MeshPrefabBaker> bakers, UnityEngine.Terrain terrain, string[] lodFolder, GameObject[] prefabRoot)
        {
            var arrAlbedoMats = new Material[2];
            var arrNormalMats = new Material[2];

            MaterialUtils.GetBakeMaterials(terrain, arrAlbedoMats, arrNormalMats);
            var texture = new Texture2D(_bakeTextureSize, _bakeTextureSize, TextureFormat.RGBA32, false);
            RenderTexture rt = RenderTexture.GetTemporary(_bakeTextureSize, _bakeTextureSize);

            for (int i = 0; i < bakers.Count; ++i)
            {
                EditorUtility.DisplayProgressBar("Saving Data", "Processing", (float)i / bakers.Count);
                var baker = bakers[i];
                var folder = lodFolder[baker._lod];
                if (!AssetDatabase.IsValidFolder(Path.Combine(folder, "Textures")))
                {
                    AssetDatabase.CreateFolder(folder, "Textures");
                    AssetDatabase.Refresh();
                }

                var albedoPath = string.Format("{0}/Textures/albedo_{1}.png", folder, baker._meshId);
                SaveBakedTexture(albedoPath, rt, texture, arrAlbedoMats, baker.scaleOffset);

                var normalPath = string.Format("{0}/Textures/normal_{1}.png", folder, baker._meshId);
                SaveBakedTexture(normalPath, rt, texture, arrNormalMats, baker.scaleOffset);
                
                AssetDatabase.Refresh();

                var albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
                var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                if (!AssetDatabase.IsValidFolder(Path.Combine(folder, "Materials")))
                {
                    AssetDatabase.CreateFolder(folder, "Materials");
                    AssetDatabase.Refresh();
                }

                var matPath = string.Format("{0}/Materials/mat_{1}.mat", folder, baker._meshId);
                SaveBakedMaterial(matPath, albedoTex, normalTex, new Vector2(baker.scaleOffset.x, baker.scaleOffset.y));
                AssetDatabase.Refresh();

                if (prefabRoot[baker._lod] == null)
                {
                    prefabRoot[baker._lod] = new GameObject(terrain.name);
                }

                GameObject meshGO = new GameObject(baker._meshId.ToString());
                var filter = meshGO.AddComponent<MeshFilter>();
                filter.mesh = baker._mesh;
                var renderer = meshGO.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                meshGO.transform.parent = prefabRoot[baker._lod].transform;
            }
            
            RenderTexture.ReleaseTemporary(rt);
            foreach (var mat in arrAlbedoMats) 
            {
                DestroyImmediate(mat);
            }

            foreach (var mat in arrNormalMats)
            {
                DestroyImmediate(mat);
            }
            
            DestroyImmediate(texture);
        }

        void SaveBakedTexture(string path, RenderTexture rt, Texture2D texture, Material[] arrMats, Vector4 scaleOffset)
        {
            for (int i = 0; i < 2; ++i)
            {
                Graphics.Blit(null, rt, arrMats[0]);
                arrMats[0].SetVector("_BakeScaleOffset", scaleOffset);
                if (arrMats[1] != null)
                {
                    Graphics.Blit(null, rt, arrMats[1]);
                    arrMats[1].SetVector("_BakeScaleOffset", scaleOffset);
                }
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = rt;
                texture.ReadPixels(new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), 0, 0);
                texture.Apply();
                RenderTexture.active = previous;
            }

            byte[] tga = texture.EncodeToTGA();
            File.WriteAllBytes(path, tga);
        }

        void SaveBakedMaterial(string path, Texture2D albedo, Texture2D normal, Vector2 size)
        {
            var scale = new Vector2(1f / size.x, 1f / size.y);
            Material mat = new Material(Shader.Find("Custom RP/TerrainVTLit"));
            mat.SetTexture("_Diffuse", albedo);
            mat.SetTextureScale("_Diffuse", scale);
            mat.SetTexture("_Normal", normal);
            mat.SetTextureScale("_Normal", scale);
            mat.EnableKeyword("_NORMALMAP");
            AssetDatabase.CreateAsset(mat, path);
        }

    };
};
