using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace CustomURP
{
    public class MaterialUtils
    {
        static Texture2D ExportAlphaMap(string path, string dataName, UnityEngine.Terrain terrain, int matIndex)
        {
#if UNITY_EDITOR
            if (matIndex >= terrain.terrainData.alphamapTextureCount)
                return null;

            byte[] alphaMapData = terrain.terrainData.alphamapTextures[matIndex].EncodeToTGA();
            string alphaMapSavePath = string.Format("{0}/{1}_alpha{2}.tga", path, dataName, matIndex);

            if (File.Exists(alphaMapSavePath))
            {
                File.Delete(alphaMapSavePath);
            }

            FileStream stream = File.Open(alphaMapSavePath, FileMode.Create);
            stream.Write(alphaMapData, 0, alphaMapData.Length);
            stream.Close();
            AssetDatabase.Refresh();
            string alphaMapPath = string.Format("{0}/{1}_alpha{2}.tga", path, dataName, matIndex);
            TextureImporter importer = AssetImporter.GetAtPath(alphaMapPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("Export terrain alpha map failed");
                return null;
            }

            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Clamp;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(alphaMapPath);
#else
            return null;
#endif
        }

        private static void SaveMixMaterail(string path, string dataName, UnityEngine.Terrain terrain, int matIndex, int layerStart, string shaderName, List<string> assetPath)
        {
#if UNITY_EDITOR
            Texture2D alphaMap = ExportAlphaMap(path, dataName, terrain, matIndex);
            if (alphaMap == null)
                return;

            string matPath = string.Format("{0}/{1}_{2}.mat", path, dataName, matIndex);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat != null)
            {
                AssetDatabase.DeleteAsset(matPath);
            }

            Material tmat = new Material(Shader.Find(shaderName));
            tmat.SetTexture("_Control", alphaMap);
            if (tmat == null)
            {
                Debug.Log("Export terrain material failed.");
                return;
            }

            for (int l = layerStart; l < layerStart + 4 && l < terrain.terrainData.terrainLayers.Length; ++l)
            {
                int idx = l - layerStart;
                TerrainLayer layer = terrain.terrainData.terrainLayers[l];
                Vector2 tiling = new Vector2(terrain.terrainData.size.x / layer.tileSize.x, terrain.terrainData.size.z / layer.tileSize.y);
                tmat.SetTexture(string.Format("_Splat{0}", idx), layer.diffuseTexture);
                tmat.SetTextureOffset(string.Format("_Splat{0}", idx), layer.tileOffset);
                tmat.SetTextureScale(string.Format("_Splat{0}", idx), tiling);
                tmat.SetTexture(string.Format("_Normal{0}", idx), layer.normalMapTexture);
                tmat.SetFloat(string.Format("_NormalScale{0}", idx), layer.normalScale);
                tmat.SetFloat(string.Format("_Metallic{0}", idx), layer.metallic);
                tmat.SetFloat(string.Format("_Smoothness{0}", idx), layer.smoothness);
                tmat.EnableKeyword("_NORMALMAP");
                
                if (layer.maskMapTexture != null)
                {
                    tmat.EnableKeyword("_MASKMAP");
                    tmat.SetFloat(string.Format("_LayerHasMask{0}", idx), 1f);
                    tmat.SetTexture(string.Format("_Mask{0}", idx), layer.maskMapTexture);
                }
                else
                {
                    tmat.SetFloat(string.Format("_LayerHasMask{0}", idx), 0f);
                }
            }
            
            AssetDatabase.CreateAsset(tmat, matPath);
            if(assetPath != null)
                assetPath.Add(matPath);
#endif
        }
        
        public static void SaveMixMaterials(string path, string dataName, UnityEngine.Terrain terrain, List<string> asssetPath)
        {
#if UNITY_EDITOR
            if (terrain.terrainData == null)
            {
                Debug.Log("Terrain data doesn't exist");
                return;
            }

            int matCount = terrain.terrainData.alphamapTextureCount;
            if (matCount <= 0)
                return;
            
            SaveMixMaterail(path, dataName, terrain, 0, 0, "Custom RP/TerrainLit", asssetPath);
            for (int i = 1; i < matCount; ++i)
            {
                SaveMixMaterail(path, dataName, terrain, i, i * 4, "Custom RP/TerrainLitAdd", asssetPath);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }

        public static void GetBakeMaterials(UnityEngine.Terrain terrain, Material[] albedos, Material[] bumps)
        {
#if UNITY_EDITOR
            if (terrain.terrainData == null)
            {
                Debug.LogError("Terrain data doesn't exist.");
                return;
            }

            int matCount = terrain.terrainData.alphamapTextureCount;
            if (matCount <= 0 || albedos == null || albedos.Length < 1 || bumps == null || bumps.Length < 1)
                return;

            albedos[0] = GetBakeAlbedo(terrain, 0, 0, "Custom RP/VTDiffuse");
            for(int i = 0; i < matCount && i < albedos.Length; ++i)
            {
                albedos[i] = GetBakeAlbedo(terrain, i, i * 4, "Custom RP/VTDiffuseAdd");
            }
            
            bumps[0] = GetBakeNormal(terrain, 0, 0, "Custom RP/VTBump");
            for (int i = 0; i < matCount && i < albedos.Length; ++i)
            {
                bumps[i] = GetBakeNormal(terrain, i, i * 4, "Custom RP/VTBumpAdd");
            }
#endif
        }

        static Material GetBakeAlbedo(UnityEngine.Terrain terrain, int matIdx, int layerStart, string shaderName)
        {
#if UNITY_EDITOR
            Material tMat = new Material(Shader.Find(shaderName));
            if (matIdx < terrain.terrainData.alphamapTextureCount)
            {
                var alphaMap = terrain.terrainData.alphamapTextures[matIdx];
                tMat.SetTexture("_Control", alphaMap);
            }

            for (int l = layerStart; l < layerStart + 4 && l < terrain.terrainData.terrainLayers.Length; ++l)
            {
                int idx = l - layerStart;
                TerrainLayer layer = terrain.terrainData.terrainLayers[l];
                Vector2 tiling = new Vector2(terrain.terrainData.size.x / layer.tileSize.x, terrain.terrainData.size.z / layer.tileSize.y);
                tMat.SetTexture(string.Format("_Splat{0}", idx), layer.diffuseTexture);
                tMat.SetTextureOffset(string.Format("_Splat{0}", idx), layer.tileOffset);
                tMat.SetTextureScale(string.Format("_Splat{0}", idx), tiling);
                if (layer.maskMapTexture != null)
                {
                    tMat.SetFloat(string.Format("_HasMask{0}", idx), 1f);
                    tMat.SetTexture(string.Format("_Mask{0}", idx), layer.maskMapTexture);
                }
                else
                {
                    tMat.SetFloat(string.Format("_HasMask{0}", idx), 0f);
                }
                tMat.SetFloat(string.Format("_Smoothness{0}", idx), layer.smoothness);
            }
            
            return tMat;
#else            
            return null;
#endif
        }

        static Material GetBakeNormal(UnityEngine.Terrain terrain, int matIdx, int layerStart, string shaderName)
        {
#if UNITY_EDITOR
            Material tMat = new Material(Shader.Find(shaderName));
            if (matIdx < terrain.terrainData.alphamapTextureCount)
            {
                var alphaMap = terrain.terrainData.alphamapTextures[matIdx];
                tMat.SetTexture("_Control", alphaMap);
            }
            
            for (int l = layerStart; l < layerStart + 4 && l < terrain.terrainData.terrainLayers.Length; ++l)
            {
                int idx = l - layerStart;
                TerrainLayer layer = terrain.terrainData.terrainLayers[l];
                Vector2 tiling = new Vector2(terrain.terrainData.size.x / layer.tileSize.x, terrain.terrainData.size.z / layer.tileSize.y);
                tMat.SetTexture(string.Format("_Normal{0}", idx), layer.normalMapTexture);
                tMat.SetFloat(string.Format("_NormalScale{0}", idx), layer.normalScale);
                tMat.SetTextureOffset(string.Format("_Normal{0}", idx), layer.tileOffset);
                tMat.SetTextureScale(string.Format("_Normal{0}", idx), tiling); 
                
                if (layer.maskMapTexture != null)
                {
                    tMat.SetFloat(string.Format("_HasMask{0}", idx), 1f);
                    tMat.SetTexture(string.Format("_Mask{0}", idx), layer.maskMapTexture);
                }
                else
                {
                    tMat.SetFloat(string.Format("_HasMask{0}", idx), 0f);
                }
                tMat.SetFloat(string.Format("_Metallic{0}", idx), layer.metallic);
            }
            return tMat;
#else
            return null;
#endif
        }
        
        public static void SaveVTMaterials(string path, string dataName, UnityEngine.Terrain t,
            List<string> albetoPath, List<string> bumpPath)
        {
#if UNITY_EDITOR
            if (t.terrainData == null)
            {
                Debug.LogError("terrain data doesn't exist");
                return;
            }
            int matCount = t.terrainData.alphamapTextureCount;
            if (matCount <= 0)
                return;
            //base pass
            SaveVTMaterail(path, dataName, t, 0, 0, "", albetoPath, bumpPath);
            for (int i = 1; i < matCount; ++i)
            {
                SaveVTMaterail(path, dataName, t, i, i * 4, "Add", albetoPath, bumpPath);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }
        
          private static void SaveVTMaterail(string path, string dataName, UnityEngine.Terrain t, int matIdx, int layerStart, string shaderPostfix,
            List<string> albetoPath, List<string> bumpPath)
        {
#if UNITY_EDITOR
            Texture2D alphaMap = ExportAlphaMap(path, dataName, t, matIdx);
            if (alphaMap == null)
                return;
            //
            string mathPath = string.Format("{0}/VTDiffuse_{1}.mat", path, matIdx);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(mathPath);
            if (mat != null)
                AssetDatabase.DeleteAsset(mathPath);
            Material tMat = new Material(Shader.Find("MT/VTDiffuse" + shaderPostfix));
            tMat.SetTexture("_Control", alphaMap);
            if (tMat == null)
            {
                Debug.LogError("export terrain vt diffuse material failed");
                return;
            }
            string bumpMatPath = string.Format("{0}/VTBump_{1}.mat", path, matIdx);
            Material bmat = AssetDatabase.LoadAssetAtPath<Material>(bumpMatPath);
            if (bmat != null)
                AssetDatabase.DeleteAsset(bumpMatPath);
            Material bumpmat = new Material(Shader.Find("MT/VTBump" + shaderPostfix));
            bumpmat.SetTexture("_Control", alphaMap);
            if (bumpmat == null)
            {
                Debug.LogError("export terrain vt bump material failed");
                return;
            }
            for (int l = layerStart; l < layerStart + 4 && l < t.terrainData.terrainLayers.Length; ++l)
            {
                int idx = l - layerStart;
                TerrainLayer layer = t.terrainData.terrainLayers[l];
                Vector2 tiling = new Vector2(t.terrainData.size.x / layer.tileSize.x,
                    t.terrainData.size.z / layer.tileSize.y);
                tMat.SetTexture(string.Format("_Splat{0}", idx), layer.diffuseTexture);
                tMat.SetTextureOffset(string.Format("_Splat{0}", idx), layer.tileOffset);
                tMat.SetTextureScale(string.Format("_Splat{0}", idx), tiling);
                var diffuseRemapScale = layer.diffuseRemapMax - layer.diffuseRemapMin;
                if (diffuseRemapScale.magnitude > 0)
                    tMat.SetColor(string.Format("_DiffuseRemapScale{0}", idx), diffuseRemapScale);
                else
                    tMat.SetColor(string.Format("_DiffuseRemapScale{0}", idx), Color.white);
                if (layer.maskMapTexture != null)
                {
                    tMat.SetFloat(string.Format("_HasMask{0}", idx), 1f);
                    tMat.SetTexture(string.Format("_Mask{0}", idx), layer.maskMapTexture);
                }
                else
                {
                    tMat.SetFloat(string.Format("_HasMask{0}", idx), 0f);
                }
                tMat.SetFloat(string.Format("_Smoothness{0}", idx), layer.smoothness);

                bumpmat.SetTexture(string.Format("_Normal{0}", idx), layer.normalMapTexture);
                bumpmat.SetFloat(string.Format("_NormalScale{0}", idx), layer.normalScale);
                bumpmat.SetTextureOffset(string.Format("_Normal{0}", idx), layer.tileOffset);
                bumpmat.SetTextureScale(string.Format("_Normal{0}", idx), tiling);
                if (layer.maskMapTexture != null)
                {
                    bumpmat.SetFloat(string.Format("_HasMask{0}", idx), 1f);
                    bumpmat.SetTexture(string.Format("_Mask{0}", idx), layer.maskMapTexture);
                }
                else
                {
                    bumpmat.SetFloat(string.Format("_HasMask{0}", idx), 0f);
                }
                bumpmat.SetFloat(string.Format("_Metallic{0}", idx), layer.metallic);
            }
            AssetDatabase.CreateAsset(tMat, mathPath);
            if (albetoPath != null)
                albetoPath.Add(mathPath);
            AssetDatabase.CreateAsset(bumpmat, bumpMatPath);
            if (bumpPath != null)
                bumpPath.Add(bumpMatPath);
#endif
        }
    };
};
