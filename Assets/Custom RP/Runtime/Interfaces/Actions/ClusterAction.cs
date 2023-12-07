using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

namespace CustomURP
{
    [CreateAssetMenu(menuName = "Custom URP/Cluster")]
    public sealed unsafe class ClusterAction : CoreAction
    {
        struct ScreenToView
        {
            float4x4 inverseProjection;
            uint tileSizeX;
            uint tileSizeY;
            uint tileSizeZ;
            uint padding1;
            float2 tileSizePx;
            float2 viewPxSize;
            float scale;
            float bias;
            uint padding2;
            uint padding3;
        };
        
        struct AdditionalLightData
        {
            public Vector4 minPoint;
            public Vector4 maxPoint; 
            public Vector4 dirAndMask;
            public Vector4 ShadowData;
            public Vector3 PosWS;
            public float AttenuationCoef;
            public Vector3 Color;
            public uint renderingLayerMask;
            public Vector3 SpotDir;
            public Vector2 SpotAngle;

        };

        
        struct VolumeTileAABB
        {
            public Vector4 minPoint;
            public Vector4 maxPoint;
        };
        
        struct TestBox
        {
            public float3 p0, p1, p2, p3, p4, p5, p6, p7;
        };
        
        public static ClusterAction _cluster { get; private set; }
        public int _maxClusterCount = 100000;
        public int _maxMaterialCount = 1;
        public int _materialPoolSize = 500;
        public bool _isInited = false;
        
        int2 _gridXY;
        int _clusterGridBuildKernel;
        int _gridLightBuildKernel;
        AdditionalLightData[] _lightListArray;
        
        [NonSerialized]
        public ComputeShader _clusterShading;
        ComputeBuffer _lightListBuffer;
        ComputeBuffer _gridBuffer;
        ComputeBuffer _lightIndexBuffer;
        ComputeBuffer _gridLightIndexBuffer;

        public const int CLUSTER_GRID_BUILD_NUMTHREADS_X = 8;
        public const int CLUSTER_GRID_BUILD_NUMTHREADS_Y = 4;
        public const int LIGHT_CLUSTER_Z_SLICE = 16;
        public const int CLUSTER_MAX_LIGHTS_COUNT = 255;
        public const int CLUSTER_MAX_DIR_LIGHTS_COUNT = 4;

        int3 _clusterSize = new int3(CLUSTER_GRID_BUILD_NUMTHREADS_X, CLUSTER_GRID_BUILD_NUMTHREADS_Y, LIGHT_CLUSTER_Z_SLICE);
        float _zFar;
        float _zNear;
        Vector4 _clusterData;
        
        private                 CullingResults _cullingResults;
        private                 Shadows        _shadows;
        private static readonly Vector4[]      _dirLightColors               = new Vector4[CLUSTER_MAX_DIR_LIGHTS_COUNT];
        private static readonly Vector4[]      _dirLightDirectionsAndMasks   = new Vector4[CLUSTER_MAX_DIR_LIGHTS_COUNT];
        private static readonly Vector4[]      _dirLightShadowData           = new Vector4[CLUSTER_MAX_DIR_LIGHTS_COUNT];
        private static readonly string UseClusterLightlist = "USE_CLUSTER_LIGHT";
        private static readonly string LIGHTS_PER_OBJECT_KEYWORD = "_LIGHTS_PER_OBJECT";
        
        
        protected internal override void Initialization(CustomRenderPipelineAsset asset)
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                throw new PlatformNotSupportedException("Compute Shader NOt Supported.");
            }

            _asset = asset;
            _clusterShading = asset._pipelineShaders._ClusterRenderShader;
            _cmd = new Command("ClusterAction");
            _clusterGridBuildKernel = _clusterShading.FindKernel("ClusterGridBuild");
            _gridLightBuildKernel = _clusterShading.FindKernel("GridLightBuild");

            int total = _clusterSize.x * _clusterSize.y * _clusterSize.z;
            _lightListArray = new AdditionalLightData[CLUSTER_MAX_LIGHTS_COUNT];

            _gridBuffer = new ComputeBuffer(total, sizeof(VolumeTileAABB));
            _lightListBuffer = new ComputeBuffer(CLUSTER_MAX_LIGHTS_COUNT, sizeof(AdditionalLightData));
            _lightIndexBuffer = new ComputeBuffer(total * CLUSTER_MAX_LIGHTS_COUNT, sizeof(uint));
            _gridLightIndexBuffer = new ComputeBuffer(total, sizeof(uint));
            // _shadows = new Shadows();

        }
        
        public override void BeginRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            if (_isInited) return;
            _cmd.Context = cmd.Context;
            _camera = camera;

            _zFar = camera._camera.farClipPlane;
            _zNear = camera._camera.nearClipPlane;
            var scale = _clusterSize.z / Mathf.Log(_zFar / _zNear);
            var bias = -_clusterSize.z * Mathf.Log(_zNear) / Mathf.Log(_zFar / _zNear);
            _clusterData = new Vector4(_clusterSize.x, _clusterSize.y, scale, bias);
            // _shadows.Setup(cmd.Context, _cullingResults, _asset.Shadows);
            BuildGrid();

            _isInited = true;
            // DebugCluster(camera._camera);
        }

        public override void Tick(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            _cmd.Context = cmd.Context;
            _camera = camera;
            
            var cameraSettings = camera ? camera.Setting : new CameraSettings();
            // _shadows.Render();
            BuildLightList(_asset.LightsPerObject, cameraSettings._maskLights ? cameraSettings._renderingLayerMask : -1);
            BuildGridLight();
            BindShaderConstant();
        }

        public override void EndRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            
        }

        public void SetCullResult(ref CullingResults result)
        {
            _cullingResults = result;
        }

        private void BuildGrid()
        {
            _cmd.SetComputeVectorParam(_clusterShading, ShaderParams._clusterDataId, _clusterData);
            _cmd.SetComputeFloatParam(_clusterShading, ShaderParams._clusterZFarId, _zFar);
            _cmd.SetComputeFloatParam(_clusterShading, ShaderParams._clusterZNearId, _zNear);
            _cmd.SetComputeBufferParam(_clusterShading, _clusterGridBuildKernel, ShaderParams._clusterGridRWId, _gridBuffer);
            _cmd.DispatchCompute(_clusterShading, _clusterGridBuildKernel, 1, 1, 1);
            _cmd.Execute();
        }

        private void BuildLightList(bool useLightsPerObject, int renderingLayerMask)
        {
            var w2v = _camera._camera.worldToCameraMatrix;
            int dirLightCount = 0;
            int otherLightCount = 0;
            NativeArray<int> indexMap = useLightsPerObject ? _cullingResults.GetLightIndexMap(Allocator.Temp) : default;
            NativeArray<VisibleLight> visibleLights = _cullingResults.visibleLights;
            
            var i = 0;
            for (i = 0; i < visibleLights.Length; ++i)
            {
                if(otherLightCount == CLUSTER_MAX_LIGHTS_COUNT) continue;
                
                var newIndex = -1;
                VisibleLight visibleLight = visibleLights[i];
                Light light = visibleLight.light;
                
                if ((light.renderingLayerMask & renderingLayerMask) != 0)
                {
                    switch (visibleLight.lightType)
                    {
                        case LightType.Directional:
                        {
                            if (dirLightCount < CLUSTER_MAX_DIR_LIGHTS_COUNT)
                                SetupDirectionalLight(dirLightCount++, i, ref visibleLight, light);
                        }
                            break;
                        case LightType.Point:
                        {
                            SetupPointLight(otherLightCount++, ref visibleLight, light, w2v, i);
                        }
                            break;
                        case LightType.Spot:
                        {
                            SetupSpotLight(otherLightCount++, ref visibleLight, light, w2v, i );
                        }
                            break;
                    }
                }
            }
            
            Shader.DisableKeyword(LIGHTS_PER_OBJECT_KEYWORD);
            
            _cmd.SetGlobalInt(ShaderParams._clusterDirectionalLightCountId, dirLightCount);
            
            if (dirLightCount > 0)
            {
                _cmd.SetGlobalVectorArray(ShaderParams._clusterDirectionalLightColorId,      _dirLightColors);
                _cmd.SetGlobalVectorArray(ShaderParams._clusterDirectionalLightDirAndMasksId,  _dirLightDirectionsAndMasks);
                _cmd.SetGlobalVectorArray(ShaderParams._clusterDirectionalLightShadowDataId, _dirLightShadowData);
            }
            
            if (otherLightCount > 0)
            {
                _lightListBuffer.SetData(_lightListArray, 0, 0, otherLightCount);
                _cmd.SetComputeIntParam(_clusterShading, ShaderParams._clusterLightCountId, otherLightCount);
            }
            
             _cmd.BeginSample();
             _cmd.Execute();
             _cmd.EndSampler();
        }
        
        private void SetupDirectionalLight(int index, int visibleIndex, ref VisibleLight visibleLight, Light light)
        {
            _dirLightColors[index] = visibleLight.finalColor;
            var dirAndMask = -visibleLight.localToWorldMatrix.GetColumn(2);
            dirAndMask.w = light.renderingLayerMask.ReinterpretAsFloat();

            _dirLightDirectionsAndMasks[index] = dirAndMask;
            // _dirLightShadowData[index]         = _shadows.ReserveDirectinalShadows(light, visibleIndex);
            _dirLightShadowData[index] = Vector4.zero;
        }

        void SetupPointLight(int arrayIndex, ref VisibleLight visibleLight, Light light, Matrix4x4 worldToView, int visibleIndex)
        {
            var rect = visibleLight.screenRect;
            Vector4 minPoint = new Vector4();
            Vector4 maxPoint = new Vector4();
            Vector4 pos = visibleLight.localToWorldMatrix.GetColumn(3);
            minPoint.x = rect.x;
            minPoint.y = rect.y;
            maxPoint.x = rect.x + rect.width;
            maxPoint.y = rect.y + rect.height;
            var lightPos = light.transform.position;
            var z = Vector4.Dot(worldToView.GetRow(2), new Vector4(lightPos.x, lightPos.y, lightPos.z, 1.0f));
            minPoint.z = z + visibleLight.range;
            maxPoint.z = z - visibleLight.range;

            _lightListArray[arrayIndex].minPoint = minPoint;
            _lightListArray[arrayIndex].maxPoint = maxPoint;
            _lightListArray[arrayIndex].PosWS = pos;
            _lightListArray[arrayIndex].Color = (Vector4)visibleLight.finalColor;
            _lightListArray[arrayIndex].AttenuationCoef = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.0001f);
            _lightListArray[arrayIndex].SpotAngle = new Vector2(0f, 1f);
            _lightListArray[arrayIndex].SpotDir = Vector4.zero;
            _lightListArray[arrayIndex].dirAndMask = new Vector4(0, 0, 0, light.renderingLayerMask.ReinterpretAsFloat());
            // _lightListArray[arrayIndex].ShadowData = _shadows.ReserveOtherShadows(light, visibleIndex);
        }

        void SetupSpotLight(int arrayIndex, ref VisibleLight visibleLight, Light light, Matrix4x4 worldToView, int visibleIndex)
        {
            var rect = visibleLight.screenRect;
            Vector4 minPoint = new Vector4();
            Vector4 maxPoint = new Vector4();

            minPoint.x = rect.x;
            minPoint.y = rect.y;
            maxPoint.x = rect.x + rect.width;
            maxPoint.y = rect.y + rect.height;
            var lightPos = light.transform.position;
            var posZ = Vector4.Dot(worldToView.GetRow(2), new Vector4(lightPos.x, lightPos.y, lightPos.z, 1.0f));
            var dir = light.transform.forward;
            var dirV = worldToView * new Vector4(dir.x, dir.y, dir.z, 0.0f);
            var zDir = Mathf.Sign(dirV.z);

            var costheta = Mathf.Max(0.0001f, zDir * dirV.z);
            var vertical = new Vector3(-dirV.x, -dirV.y, zDir / costheta - dirV.z);
            vertical.Normalize();
            var radius = Mathf.Tan(visibleLight.spotAngle / 2 * Mathf.Deg2Rad) * visibleLight.range;
            var deltaZ = vertical.z * radius * zDir;
            var dirVZ = dirV.z * visibleLight.range;

            var z1 = posZ + dirVZ - deltaZ;
            z1 = Mathf.Min(z1, posZ);
            maxPoint.z = z1;
            var z2 = posZ + dirVZ + deltaZ;
            z2 = Mathf.Max(z2, posZ);
            minPoint.z = z2;

            Vector4 pos = visibleLight.localToWorldMatrix.GetColumn(3);
            _lightListArray[arrayIndex].minPoint = minPoint;
            _lightListArray[arrayIndex].maxPoint = maxPoint;
            _lightListArray[arrayIndex].PosWS = pos;
            _lightListArray[arrayIndex].Color = (Vector4)visibleLight.finalColor;
            _lightListArray[arrayIndex].AttenuationCoef = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.0001f);

            float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
            float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.spotAngle);
            float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
            _lightListArray[arrayIndex].SpotAngle = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
            _lightListArray[arrayIndex].SpotDir = -visibleLight.localToWorldMatrix.GetColumn(2);
            var dirAndMask = -visibleLight.localToWorldMatrix.GetColumn(2);
            dirAndMask.w = light.renderingLayerMask.ReinterpretAsFloat();
            _lightListArray[arrayIndex].dirAndMask = dirAndMask;
            // _lightListArray[arrayIndex].ShadowData = _shadows.ReserveOtherShadows(light, visibleIndex);
        }

        private void BuildGridLight()
        {
            _cmd.SetComputeFloatParam(_clusterShading, ShaderParams._clusterZFarId, _zFar);
            _cmd.SetComputeFloatParam(_clusterShading, ShaderParams._clusterZNearId, _zNear);
            _cmd.SetComputeBufferParam(_clusterShading, _gridLightBuildKernel, ShaderParams._clusterGridRWId, _gridBuffer);
            _cmd.SetComputeBufferParam(_clusterShading, _gridLightBuildKernel, ShaderParams._clusterLightListId, _lightListBuffer);
            _cmd.SetComputeBufferParam(_clusterShading, _gridLightBuildKernel, ShaderParams._clusterLightIndexRWId, _lightIndexBuffer);
            _cmd.SetComputeBufferParam(_clusterShading, _gridLightBuildKernel, ShaderParams._clusterGrirdLightRWId, _gridLightIndexBuffer);
            _cmd.BeginSample();
            _cmd.DispatchCompute(_clusterShading, _gridLightBuildKernel, 1, 1, 1);
            _cmd.EndSampler();
            
            _cmd.Execute();
        }

        public void BindShaderConstant()
        {
            _cmd.EnableShaderKeyword(UseClusterLightlist);
            _cmd.SetGlobalVector(ShaderParams._clusterDataId, _clusterData);
            _cmd.SetGlobalBuffer(ShaderParams._clusterLightListId, _lightListBuffer);
            _cmd.SetGlobalBuffer(ShaderParams._clusterLightIndexId, _lightIndexBuffer);
            _cmd.SetGlobalBuffer(ShaderParams._clusterGridLightId, _gridLightIndexBuffer);
            _cmd.BeginSample();
            _cmd.Execute();
            _cmd.EndSampler();
        }

        public override bool InspectProperty()
        {
            return true;
        }
        
        public void DebugCluster(Camera camera)
        {
            var size = _clusterSize.x * _clusterSize.y * _clusterSize.z;
            VolumeTileAABB[] boxes = new VolumeTileAABB[size];
            _gridBuffer.GetData(boxes, 0, 0, size);
            
            
            Matrix4x4 viewMatrix = Camera.main.worldToCameraMatrix;
            Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            Matrix4x4 vpMatrix = projMatrix * viewMatrix;
            Matrix4x4 vpMatrixInv = vpMatrix.inverse;
            
            foreach (var i in boxes)
            {
                var xmin = i.minPoint.x;
                var ymin = i.minPoint.y;
                var zmin = i.minPoint.z;
                var wmin = i.minPoint.w;
                var xmax = i.maxPoint.x;
                var ymax = i.maxPoint.y;
                var zmax = i.maxPoint.z;
                
                float3 p0 = MatTransformProj(vpMatrixInv, new float3(xmin, ymin, 0));
                float3 p1 = MatTransformProj(vpMatrixInv, new float3(xmin, ymin, 1));
                float3 p2 = MatTransformProj(vpMatrixInv, new float3(xmin, ymax, 0));
                float3 p3 = MatTransformProj(vpMatrixInv, new float3(xmin, ymax, 1));
                float3 p4 = MatTransformProj(vpMatrixInv, new float3(xmax, ymin, 0));
                float3 p5 = MatTransformProj(vpMatrixInv, new float3(xmax, ymin, 1));
                float3 p6 = MatTransformProj(vpMatrixInv, new float3(xmax, ymax, 0));
                float3 p7 = MatTransformProj(vpMatrixInv, new float3(xmax, ymax, 1));
                
                TestBox box = new TestBox();
                
                box.p0 = p0 + zmin * (p1 - p0);
                box.p1 = p0 + zmax * (p1 - p0);
                box.p2 = p2 + zmin * (p3 - p2);
                box.p3 = p2 + zmax * (p3 - p2);
                box.p4 = p4 + zmin * (p5 - p4);
                box.p5 = p4 + zmax * (p5 - p4);
                box.p6 = p6 + zmin * (p7 - p6);
                box.p7 = p6 + zmax * (p7 - p6);
                
                DrawBox(box, Color.gray);
            }
               
        }
        
        float3 MatTransformProj(Matrix4x4 mat, float3 v3)
        {
            float4 v4 = new float4(v3, 1);
            // v4 = mul(mat, v4);
            v4 = mat * v4;
            v4 /= v4.w;
            return v4.xyz;
        }
        
        void DrawBox(TestBox box, Color color)
        {
            Debug.DrawLine(box.p0, box.p1, color);
            Debug.DrawLine(box.p0, box.p2, color);
            Debug.DrawLine(box.p0, box.p4, color);
        
            Debug.DrawLine(box.p6, box.p2, color);
            Debug.DrawLine(box.p6, box.p7, color);
            Debug.DrawLine(box.p6, box.p4, color);

            Debug.DrawLine(box.p5, box.p1, color);
            Debug.DrawLine(box.p5, box.p7, color);
            Debug.DrawLine(box.p5, box.p4, color);

            Debug.DrawLine(box.p3, box.p1, color);
            Debug.DrawLine(box.p3, box.p2, color);
            Debug.DrawLine(box.p3, box.p7, color);
        }
        
        private bool Cull(float maxShadowDistance)
        {
            if (_camera._camera.TryGetCullingParameters(out var p))
            {
                p.shadowDistance = Mathf.Min(maxShadowDistance, _camera._camera.farClipPlane);
                _cullingResults  = _cmd.Context.Cull(ref p);
                return true;
            }

            return false;
        }
        
        unsafe T[] GetData<T>(ComputeBuffer buffer)
        {
            var count = buffer.count;
            var data = new T[count];
            buffer.GetData(data);
            return data;
        }

        public void Dispose()
        {
            _isInited = false;
            if(_lightListBuffer != null) _lightListBuffer.Release();
            if(_lightIndexBuffer != null) _lightIndexBuffer.Release();
            if(_gridBuffer != null) _gridBuffer.Release();
            if(_gridBuffer != null) _gridLightIndexBuffer.Release();
            _cmd.Release();
            
        }
        
    }
}
