
using System.Collections.Generic;
using System.IO;
using Core;
using UnityEditor;
using UnityEngine;

namespace CustomURP
{
    public class TerrainDataExport : EditorWindow
    {
        int _quadTreeDepht = 2;
        LodSetting[] _lodSettings = new LodSetting[0];
        UnityEngine.Terrain _terrain;
        bool _genUV2;
        int _lodCount = 1;
        int _dataPack = 0;

        DataCreateJob _dataCreateJob;
        TessellationJob _tessellationJob;
        

        [MenuItem("Assets/Create/Custom URP/Terrain/Lod Policy")]
        static void CreateLodPolicy()
        {
            Object folder = Selection.activeObject;
            if (folder)
            {
                var path = AssetDatabase.GetAssetPath(folder);
                path = string.Format("{0}/{1}", path, "LodPolicy.asset");
                var asset = ScriptableObject.CreateInstance<LodPolicy>();
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        
        [MenuItem("SRP Tools/Terrain/TerrainDataExport")]
        static void Init()
        {
            var window = (TerrainDataExport)EditorWindow.GetWindow(typeof(TerrainDataExport));
            window.Show();
        }

        void OnGUI()
        {
            var currentTarget = EditorGUILayout.ObjectField("Convert Target", _terrain, typeof(UnityEngine.Terrain), true) as UnityEngine.Terrain;
            if (currentTarget != _terrain)
            {
                _terrain = currentTarget;
            }

            int curSliceSize = Mathf.FloorToInt(1 << _quadTreeDepht);
            int sliceSize = EditorGUILayout.IntField("Slice Size(n * n)", curSliceSize);
            if (sliceSize != curSliceSize)
            {
                curSliceSize = Mathf.NextPowerOfTwo(sliceSize);
                _quadTreeDepht = Mathf.FloorToInt(Mathf.Log(curSliceSize, 2));
            }

            if (_lodCount != _lodSettings.Length)
            {
                var old = _lodSettings;
                _lodSettings = new LodSetting[_lodCount];

                for (int i = 0; i < Mathf.Min(_lodCount, old.Length); ++i)
                {
                    _lodSettings[i] = old[i];
                }

                for (int i = old.Length; i < _lodCount; ++i)
                {
                    _lodSettings[i] = new LodSetting();
                    _lodSettings[i]._subdivision = 4;
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

            _dataPack = EditorGUILayout.IntField("Data Pack", _dataPack);
            _genUV2 = EditorGUILayout.ToggleLeft("Generate UV2", _genUV2);
            if (GUILayout.Button("Generate"))
            {
                if (_lodSettings == null || _lodSettings.Length == 0)
                {
                    Debug.Log("no lod setting");
                    return;
                }

                if (_terrain == null)
                {
                    Debug.Log("no lod terrain");
                    return;
                }

                int maxSub = 1;
                if (_lodSettings.Length > 0)
                {
                    maxSub = _lodSettings[0]._subdivision;
                }

                float maxSubGrids = sliceSize * (1 << maxSub);
                float minEdgeLen = Mathf.Max(_terrain.terrainData.bounds.size.x, _terrain.terrainData.bounds.size.z) / maxSubGrids;
                float minArea = minEdgeLen * minEdgeLen / 8f;

                var bounds = new Bounds(_terrain.transform.TransformPoint(_terrain.terrainData.bounds.center), _terrain.terrainData.bounds.size);
                _dataCreateJob = new DataCreateJob(_terrain, bounds, _quadTreeDepht, _lodSettings, 0.5f * minEdgeLen);

                for (int i = 0; i < int.MaxValue; ++i)
                {
                    _dataCreateJob.Tick();
                    EditorUtility.DisplayProgressBar("Create data", "Scanning volume", _dataCreateJob.Progress);
                    if(_dataCreateJob.Done) break;
                }
                
                _dataCreateJob.End();

                _tessellationJob = new TessellationDataJob(_dataCreateJob._lods, minArea);
                for (int i = 0; i < int.MaxValue; ++i)
                {
                    _tessellationJob.Tick();
                    EditorUtility.DisplayProgressBar("creating data", "tessellation", _tessellationJob.Progress);
                    if(_tessellationJob.Done) break;
                }

                var treeRoot = new QuadTreeBuildNode(_quadTreeDepht, bounds.min, bounds.max, Vector2.zero, Vector2.one);

                var folder0 = AssetDatabase.CreateFolder("Assets", string.Format("{0}", _terrain.name));
                folder0 = AssetDatabase.GUIDToAssetPath(folder0);
                var topFullPath = Application.dataPath + folder0.Substring(folder0.IndexOf("/"));
                var folder1 = "Assets/MeshData";
                if (!AssetDatabase.IsValidFolder(folder1))
                {
                    folder1 = AssetDatabase.CreateFolder("Assets", "MeshData");
                    folder1 = AssetDatabase.GUIDToAssetPath(folder1);
                }

                var meshFullPath = Application.dataPath + folder1.Substring(folder1.IndexOf("/"));

                TData dataHeader = ScriptableObject.CreateInstance<TData>();
                dataHeader._meshDataPack = _dataPack;
                dataHeader._meshPrefix = _terrain.name;
                {
                    int packed = 0;
                    int startMeshId = 0;
                    MemoryStream stream = new MemoryStream();
                    for (int i = 0; i < _tessellationJob._mesh.Length; ++i)
                    {
                        MeshData data = _tessellationJob._mesh[i];
                        if (!treeRoot.AddMesh(data))
                        {
                            Debug.LogError("Mesh can't insert into tree : " + data._meshId);
                        }

                        if (startMeshId < 0)
                            startMeshId = data._meshId;
                        
                        EditorUtility.DisplayProgressBar("Saving mesh data", "processing", (float)i / _tessellationJob._mesh.Length);
                        if (packed % _dataPack == 0)
                        {
                            if (stream.Length > 0)
                            {
                                File.WriteAllBytes(string.Format("{0}/{1}_{2}.bytes", meshFullPath, _terrain.name, startMeshId), stream.ToArray());
                                stream.Close();
                                startMeshId = data._meshId;
                                stream = new MemoryStream();
                            }
                            packed = 0;

                            for (int o = 0; o < _dataPack; ++o)
                            {
                                FileUtility.WriteInt(stream, 0);
                            }
                        }

                        int reserve = (int)stream.Position;
                        stream.Position = packed * sizeof(int);
                        FileUtility.WriteInt(stream, reserve);
                        stream.Position = reserve;
                        MeshUtils.Serialize(stream, data._lods[0]);
                        ++packed;
                    }

                    if (stream.Length > 0 && startMeshId >= 0)
                    {
                        File.WriteAllBytes(string.Format("{0}/{1}_{2}.bytes", meshFullPath, _terrain.name, startMeshId), stream.ToArray());
                        stream.Close();
                    }

                    AssetDatabase.Refresh();
                }

                {
                    List<QuadTreeNode> nodes = new List<QuadTreeNode>();
                    QuadTreeNode rootNode = new QuadTreeNode(0);
                    nodes.Add(rootNode);
                    ExportTree(treeRoot, rootNode, nodes);
                    EditorUtility.DisplayProgressBar("Saving tree data", "processing", 0);
                    MemoryStream stream = new MemoryStream();
                    SerializeTrees(stream, nodes);
                    File.WriteAllBytes(string.Format("{0}/treeData.bytes", topFullPath), stream.ToArray());
                    stream.Close();
                    AssetDatabase.Refresh();
                    dataHeader._treeData = AssetDatabase.LoadAssetAtPath(string.Format("{0}/treeData.bytes", folder0), typeof(TextAsset)) as TextAsset;
                }

                List<string> detailMats = new List<string>();
                List<string> bakeAlbedoMats = new List<string>();
                List<string> bakeBumpMats = new List<string>();
                
                MaterialUtils.SaveMixMaterials(folder0, _terrain.name, _terrain, detailMats);
                MaterialUtils.SaveVTMaterials(folder0, _terrain.name, _terrain, bakeAlbedoMats, bakeBumpMats);
                
                dataHeader._detailMats = new Material[detailMats.Count];
                for (int p = 0; p < detailMats.Count; ++p)
                {
                    dataHeader._detailMats[p] = AssetDatabase.LoadAssetAtPath(detailMats[p], typeof(Material)) as Material;
                }
                
                dataHeader._bakeDiffuseMats = new Material[bakeAlbedoMats.Count];
                for (int p = 0; p < bakeAlbedoMats.Count; ++p)
                {
                    dataHeader._bakeDiffuseMats[p] = AssetDatabase.LoadAssetAtPath(bakeAlbedoMats[p], typeof(Material)) as Material;
                }
                
                dataHeader._bakeNormalMats = new Material[bakeBumpMats.Count];
                for (int p = 0; p < bakeBumpMats.Count; ++p)
                {
                    dataHeader._bakeNormalMats[p] = AssetDatabase.LoadAssetAtPath(bakeBumpMats[p], typeof(Material)) as Material;
                }
                
                Material bakedMat = new Material(Shader.Find("Custom RP/TerrainVTLit"));
                bakedMat.EnableKeyword("_NORMAL_MAP");
                var bakedMatPath = string.Format("{0}/BakedMat.mat", folder0);
                AssetDatabase.CreateAsset(bakedMat, bakedMatPath);
                dataHeader._bakedMat = AssetDatabase.LoadAssetAtPath(bakedMatPath, typeof(Material)) as Material;
               
                ExportHeightMap(dataHeader, currentTarget, topFullPath, folder0);
                ExportDetails(dataHeader, currentTarget, topFullPath, folder0);
          
                AssetDatabase.CreateAsset(dataHeader, string.Format("{0}/{1}.asset", folder0, _terrain.name));
                AssetDatabase.SaveAssets();
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();

            }
            
        }

        void ExportTree(QuadTreeBuildNode root, QuadTreeNode node, List<QuadTreeNode> nodes)
        {
            if (root == null) return;
            
            node._bound = root._bound;
            node._meshIndex = root._meshId;
            node._lodLv = (byte)root._lodLv;
            if (root._subNode != null)
            {
                node._children = new int[root._subNode.Length];
                for (int i = 0; i < root._subNode.Length; ++i)
                {
                    var child = new QuadTreeNode(nodes.Count);
                    nodes.Add(child);
                    node._children[i] = child._cellIndex;
                }
                for (int i = 0; i < root._subNode.Length; ++i)
                {
                    var childIdx = node._children[i];
                    ExportTree(root._subNode[i], nodes[childIdx], nodes);
                }
            }
            
        }

        void SerializeTrees(MemoryStream stream, List<QuadTreeNode> nodes)
        {
            FileUtility.WriteInt(stream, nodes.Count);
            foreach (var node in nodes)
            {
                node.Serialize(stream);
            }
        }
        
        private void ExportHeightMap(TData dataHeader, UnityEngine.Terrain curentTarget, string topFulllPath, string folder0)
        {
            EditorUtility.DisplayProgressBar("saving height map", "processing", 0);
            dataHeader._heightmapResolution = curentTarget.terrainData.heightmapResolution;
            dataHeader._heightmapScale = curentTarget.terrainData.heightmapScale;
            float[,] heightData = curentTarget.terrainData.GetHeights(0, 0, dataHeader._heightmapResolution, dataHeader._heightmapResolution);
            byte[] heightBytes = new byte[dataHeader._heightmapResolution * dataHeader._heightmapResolution * 2];
            for (int hy = 0; hy < dataHeader._heightmapResolution; ++hy)
            {
                for (int hx = 0; hx < dataHeader._heightmapResolution; ++hx)
                {
                    float val = heightData[hy, hx] * 255f;
                    byte h = (byte)Mathf.FloorToInt(val);
                    byte l = (byte)Mathf.FloorToInt((val - h) * 255f);
                    heightBytes[hy * dataHeader._heightmapResolution * 2 + hx * 2] = h;
                    heightBytes[hy * dataHeader._heightmapResolution * 2 + hx * 2 + 1] = l;
                }
            }
            File.WriteAllBytes(string.Format("{0}/heightMap.bytes", topFulllPath), heightBytes);
            AssetDatabase.Refresh();
            dataHeader._heightMap = AssetDatabase.LoadAssetAtPath(string.Format("{0}/heightMap.bytes", folder0), typeof(TextAsset)) as TextAsset;
        }
        
        private void ExportDetails(TData dataHeader, UnityEngine.Terrain curentTarget, string topFulllPath, string folder0)
        {
            EditorUtility.DisplayProgressBar("saving details", "processing", 0);
            var original = curentTarget.terrainData.detailPrototypes;
            //combine layers of the same mesh
            List<DetailLayerData> lLayers = new List<DetailLayerData>();
            int[][,] layerDatas = new int[original.Length][,];
            for (int l = 0; l < original.Length; ++l)
            {
                var originalLayer = original[l];
                if (originalLayer.prototype == null)
                    continue;
                var layerName = originalLayer.prototype.name;
                layerDatas[l] = curentTarget.terrainData.GetDetailLayer(0, 0, curentTarget.terrainData.detailWidth, curentTarget.terrainData.detailHeight, l);
                var layer = new DetailLayerData();
                layer._minWidth = originalLayer.minWidth;
                layer._maxWidth = originalLayer.maxWidth;
                layer._minHeight = originalLayer.minHeight;
                layer._maxHeight = originalLayer.maxHeight;
                layer._noiseSpread = originalLayer.noiseSpread;
                layer._dryColor = originalLayer.dryColor;
                layer._healthyColor = originalLayer.healthyColor;
                layer._prototype = originalLayer.prototype;
                lLayers.Add(layer);
            }
            dataHeader._detailPrototypes = new DetailLayerData[lLayers.Count];
            dataHeader._detailWidth = curentTarget.terrainData.detailWidth;
            dataHeader._detailHeight = curentTarget.terrainData.detailHeight;
            dataHeader._detailResolutionPerPatch = curentTarget.terrainData.detailResolutionPerPatch;
            if (dataHeader._detailHeight / dataHeader._detailResolutionPerPatch > byte.MaxValue)
            {
                Debug.LogError("导出Detail失败，_detailResolutionPerPatch 数值太小");
                return;
            }
            //split data to patches, drop empty patches
            //layer header : [patch data offsets] 4 bytes each, -1 means no density
            //patch data : [patch block] 1 byte each
            //
            MemoryStream detailStream = new MemoryStream();
            int patch_x = Mathf.CeilToInt((float)dataHeader._detailWidth / dataHeader._detailResolutionPerPatch);
            int patch_y = Mathf.CeilToInt((float)dataHeader._detailHeight / dataHeader._detailResolutionPerPatch);
            byte[] patch_block = new byte[dataHeader._detailResolutionPerPatch * dataHeader._detailResolutionPerPatch];
            int[] patchDataOffsets = new int[patch_x * patch_y * lLayers.Count];
            //header占位
            foreach (var d in patchDataOffsets)
                FileUtility.WriteInt(detailStream, -1);
            for (int l=0; l<lLayers.Count; ++l)
            {
                var layer = lLayers[l];
                dataHeader._detailPrototypes[l] = layer;
                layer._maxDensity = 0;
                //Texture2D debugTex = new Texture2D(dataHeader.DetailWidth, dataHeader.DetailHeight);
                for (int py = 0; py <patch_y; ++py)
                {
                    for (int px = 0; px < patch_x; ++px)
                    {
                        int max_density = 0;
                        for(int sub_py = 0; sub_py < dataHeader._detailResolutionPerPatch; ++sub_py)
                        {
                            for (int sub_px = 0; sub_px < dataHeader._detailResolutionPerPatch; ++sub_px)
                            {
                                int hy = py * dataHeader._detailResolutionPerPatch + sub_py;
                                int hx = px * dataHeader._detailResolutionPerPatch + sub_px;
                                hy = Mathf.Min(hy, dataHeader._detailHeight);
                                hx = Mathf.Min(hx, dataHeader._detailWidth);
                                int density = layerDatas[l][hy, hx];
                                if (density > max_density)
                                    max_density = density;
                                patch_block[sub_py * dataHeader._detailResolutionPerPatch + sub_px] = (byte)density;
                            }
                        }
                        var offsetDataIdx = l * patch_x * patch_y + py * patch_x + px;
                        if (max_density > 0)
                        {
                            patchDataOffsets[offsetDataIdx] = (int)detailStream.Position;
                            detailStream.Write(patch_block, 0, patch_block.Length);
                        }
                        else
                        {
                            patchDataOffsets[offsetDataIdx] = -1;
                        }
                        if (max_density > layer._maxDensity)
                            layer._maxDensity = max_density;
                    }
                }
                //var tgaBytes = debugTex.EncodeToTGA();
                //FileStream stream = File.Open(string.Format("{0}/debug_desity_texture.tga", topFulllPath), FileMode.Create);
                //stream.Write(tgaBytes, 0, tgaBytes.Length);
                //stream.Close();
                //AssetDatabase.Refresh();
            }
            detailStream.Position = 0;
            foreach (var d in patchDataOffsets)
                FileUtility.WriteInt(detailStream, d);
            File.WriteAllBytes(string.Format("{0}/details.bytes", topFulllPath), detailStream.ToArray());
            detailStream.Close();
            AssetDatabase.Refresh();
            dataHeader._detailLayers = AssetDatabase.LoadAssetAtPath(string.Format("{0}/details.bytes", folder0), typeof(TextAsset)) as TextAsset;
        }
    }
}
