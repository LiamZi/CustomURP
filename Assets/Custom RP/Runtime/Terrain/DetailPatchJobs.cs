using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;


namespace CustomURP
{

    
    
    public class DetailPatchJobs : DetailPatch
    {
        bool _buildDone = false;
        public override bool IsBuildDone { get { return _buildDone; } }
        
        public DetailPatchJobs(int dx, int dz, int patchX, int patchZ, Vector3 posParam, 
                            bool receiveShadow, TData header, int[] patchDataOffsets, 
                            NativeArray<byte> ddata) : base(dx, dz, posParam, header)
        {
            Dictionary<int, List<int>> combineLayers = new Dictionary<int, List<int>>();
            for (int i = 0; i < _headerData._detailPrototypes.Length; i++)
            {
                var _dataOffset = patchDataOffsets[i * patchX * patchZ + _denZ * patchX + _denX];
                if(_dataOffset < 0) continue;

                DetailLayerData layerData = _headerData._detailPrototypes[i];
                var uid = layerData._prototype.GetInstanceID();
                if (!combineLayers.ContainsKey(uid))
                {
                    combineLayers.Add(uid, new List<int>());
                }
                combineLayers[uid].Add(i);
            }

            _layers = new DetailPatchLayer[combineLayers.Count];
            var combineLayerIter = combineLayers.GetEnumerator();
            var combined = 0;
            while (combineLayerIter.MoveNext())
            {
                var layerIds = combineLayerIter.Current.Value;
                var combineCount = layerIds.Count;
                var job = new DetailLayerCreateJob();
                job._densityData = ddata;
                job._detailHeight = _headerData._detailHeight;
                job._denX = _denX;
                job._denZ = _denZ;
                job._posParam = posParam;
                job._detailResolutionPerPatch = _headerData._detailResolutionPerPatch;
                job._localScale = Vector3.one;
                job._detailMaxDensity = 0;
                job._dataOffset = new NativeArray<int>(combineCount, Allocator.Persistent);
                job._noiseSeed = new NativeArray<float>(combineCount, Allocator.Persistent);
                job._minWidth = new NativeArray<float>(combineCount, Allocator.Persistent);
                job._maxWidth = new NativeArray<float>(combineCount, Allocator.Persistent);
                job._minHeight = new NativeArray<float>(combineCount, Allocator.Persistent);
                job._maxHeight = new NativeArray<float>(combineCount, Allocator.Persistent);
                job._noiseSpread = new NativeArray<float>(combineCount, Allocator.Persistent);
                job._healthyColor = new NativeArray<float4>(combineCount, Allocator.Persistent);
                job._dryColor = new NativeArray<float4>(combineCount, Allocator.Persistent);
                
                DetailLayerData layerData = null;
                for (int i = 0; i < layerIds.Count; ++i)
                {
                    var l = layerIds[i];
                    layerData = _headerData._detailPrototypes[l];
                    job._localScale = layerData._prototype.transform.localScale;
                    job._detailMaxDensity = Mathf.Min(byte.MaxValue, job._detailMaxDensity + layerData._maxDensity);
                    job._dataOffset[i] = patchDataOffsets[l * patchX * patchZ + _denZ * patchX + _denX];
                    job._noiseSeed[i] = (float)l / _headerData._detailPrototypes.Length;
                    //prototype define
                    job._minWidth[i] = layerData._minWidth;
                    job._maxWidth[i] = layerData._maxWidth;
                    job._minHeight[i] = layerData._minHeight;
                    job._maxHeight[i] = layerData._maxHeight;
                    job._noiseSpread[i] = layerData._noiseSpread;
                    job._healthyColor[i] = new float4(layerData._healthyColor.r, layerData._healthyColor.g, layerData._healthyColor.b, layerData._healthyColor.a);
                    job._dryColor[i] = new float4(layerData._dryColor.r, layerData._dryColor.g, layerData._dryColor.b, layerData._dryColor.a);
                }
                _layers[combined] = new DetailPatchLayerJob(layerData, job, receiveShadow);
                ++combined;
            }
        }
        
        public override void Activate()
        {
            bool rebuild = !_buildDone;
            for (int l = 0; l < _layers.Length; ++l)
            {
                _layers[l].OnActivate(rebuild);
            }
        }
        
        public override void PushData()
        {
            base.PushData();
            _buildDone = false;
        }

        public override void Clear()
        {
            _buildDone = false;
            foreach (var l in _layers)
            {
                l.Clear();
            }
        }

        public override void TickBuild()
        {
            if (_buildDone) return;

            _buildDone = true;
            
            foreach(var l in _layers)
            {
                l.TickBuild();
                if (!l._isSpawnDone)
                {
                    _buildDone = false;
                    break;
                }
            }
        }
    };
    
    internal class DetailPatchLayerJob : DetailPatchLayer
    {
        enum JobState
        {
            Wait,
            Running,
            Done,
        };
        
        static int _jobRunningCount = 0;
        const int _maxConcurrentJobCount = 4;
        private DetailLayerCreateJob _job;
        private JobHandle _createJob;
        JobState _state = JobState.Wait;

        static bool AddSchedulJob()
        {
            if (_jobRunningCount >= _maxConcurrentJobCount)
                return false;
            ++_jobRunningCount;
            return true;
        }

        static void JobDone()
        {
            --_jobRunningCount;
        }
        
        public DetailPatchLayerJob(DetailLayerData data, DetailLayerCreateJob j, bool receiveShadow) 
            : base(data, receiveShadow)
        {
            _job = j;
        }

        public override void OnActivate(bool rebuild)
        {
            base.OnActivate(rebuild);
            if (rebuild)
            {
                if (_state != JobState.Wait)
                {
                    Debug.LogWarning("DetailPatchLayerJob OnActivate state should not be " + _state.ToString());
                    return;
                }
                TrySchedualJob();
            }
        }

        public override void OnDeactive()
        {
            base.OnDeactive();
            if (_state == JobState.Running)
            {
                JobDone();
            }
            _createJob.Complete();
            DisposeJob();
            _state = JobState.Wait;
        }

        public override bool _isSpawnDone
        {
            get
            {
                return _state == JobState.Done;
            }
        }
        public override void TickBuild()
        {
            if (_state == JobState.Wait)
            {
                TrySchedualJob();
            }
            if (_state == JobState.Running && _createJob.IsCompleted)
            {
                JobDone();
                _state = JobState.Done;
                _createJob.Complete();

                _totalPrototypeCount = _job._spawnedCount[0];
                if (_totalPrototypeCount > 0)
                {
                    int batchCount = _totalPrototypeCount / 1023 + 1;
                    if (_drawParam == null)
                    {
                        _drawParam = new Array<DetailPatchDrawParam>(batchCount);
                    }
                    
                    _drawParam.Reallocate(batchCount);
                    for (int batch = 0; batch < batchCount; ++batch)
                    {
                        var protoypeCount = Mathf.Min(1023, _totalPrototypeCount - batch * 1023);
                        var param = DetailPatchDrawParam.Pop();
                        param.Reset(protoypeCount);
                        param._used = protoypeCount;
                        for (int i = 0; i < protoypeCount; ++i)
                        {
                            var idxInJob = batch * 1023 + 1;
                            Vector3 pos = _job._positions[idxInJob];
                            HeightMap.GetHeightInterpolated(pos, ref pos.y);
                            if (this._layerData._waterFloating)
                            {
                                pos.y = WaterHeight.GetWaterHeight(pos);
                            }
                            Quaternion q = Quaternion.Euler(0, _job._orientations[idxInJob], 0);
                            param._matrixs[i] = Matrix4x4.Translate(pos) * Matrix4x4.Scale(_job._scales[idxInJob]) * Matrix4x4.Rotate(q);
                            param._colors[i] = _job._colors[idxInJob];
                        }
                        _drawParam.Add(param);
                    }
                    OnDrawParamReady();
                }
                else
                {
                    _drawParam.Reset();
                }
                DisposeJob();
            }
        }

        void TrySchedualJob()
        {
            if (AddSchedulJob())
            {
                int maxCount = _job._detailResolutionPerPatch * _job._detailResolutionPerPatch * _job._detailMaxDensity;
                _job._spawnedCount = new NativeArray<int>(1, Allocator.TempJob);
                _job._positions = new NativeArray<float3>(maxCount, Allocator.TempJob);
                _job._scales = new NativeArray<float3>(maxCount, Allocator.TempJob);
                _job._colors = new NativeArray<float4>(maxCount, Allocator.TempJob);
                _job._orientations = new NativeArray<float>(maxCount, Allocator.TempJob);
                _createJob = _job.Schedule();
                _state = JobState.Running;
            }
        }

        void DisposeJob()
        {
            if (_job._spawnedCount.IsCreated)
                _job._spawnedCount.Dispose();
            if (_job._positions.IsCreated)
                _job._positions.Dispose();
            if (_job._scales.IsCreated)
                _job._scales.Dispose();
            if (_job._colors.IsCreated)
                _job._colors.Dispose();
            if (_job._orientations.IsCreated)
                _job._orientations.Dispose();
        }

        public override void Clear()
        {
            base.Clear();
            if (_job._dataOffset.IsCreated)
                _job._dataOffset.Dispose();
            if (_job._noiseSeed.IsCreated)
                _job._noiseSeed.Dispose();
            if (_job._minWidth.IsCreated)
                _job._minWidth.Dispose();
            if (_job._maxWidth.IsCreated)
                _job._maxWidth.Dispose();
            if (_job._minHeight.IsCreated)
                _job._minHeight.Dispose();
            if (_job._maxHeight.IsCreated)
                _job._maxHeight.Dispose();
            if (_job._noiseSpread.IsCreated)
                _job._noiseSpread.Dispose();
            if (_job._healthyColor.IsCreated)
                _job._healthyColor.Dispose();
            if (_job._dryColor.IsCreated)
                _job._dryColor.Dispose();
        }
    };
    
    [BurstCompile]
    internal struct DetailLayerCreateJob : IJob
    {
        [ReadOnly]
        public NativeArray<byte> _densityData;
        public int _detailHeight;
        public int _denX;
        public int _denZ;
        public float3 _posParam; //x, offset x, y offset z, z patch size
        public float3 _localScale;
        public int _detailResolutionPerPatch;
        public int _detailMaxDensity;
        public NativeArray<float> _noiseSeed;
        public NativeArray<int> _dataOffset;
        //prototype define
        public NativeArray<float> _minWidth;
        public NativeArray<float> _maxWidth;
        public NativeArray<float> _minHeight;
        public NativeArray<float> _maxHeight;
        public NativeArray<float> _noiseSpread;
        public NativeArray<float4> _healthyColor;
        public NativeArray<float4> _dryColor;
        //output
        public NativeArray<float3> _positions;
        public NativeArray<float3> _scales;
        public NativeArray<float4> _colors;
        public NativeArray<float> _orientations;
        public NativeArray<int> _spawnedCount;
        public void Execute()
        {
            _spawnedCount[0] = 0;
            float stride = _posParam.z / _detailResolutionPerPatch;
            for (int i = 0; i < _dataOffset.Length; ++i)
            {
                GeneratePatch(i, stride);
            }
        }

        private void GeneratePatch(int i, float stride)
        {
            for (int z = 0; z < _detailResolutionPerPatch; ++z)
            {
                for (int x = 0; x < _detailResolutionPerPatch; ++x)
                {
                    var d_index = _dataOffset[i] + z * _detailResolutionPerPatch + x;
                    int density = _densityData[d_index];
                    if (density <= 0)
                        continue;
                    density = math.min(16, density);
                    float sx = _posParam.x + _denX * _posParam.z + x * stride;
                    float sz = _posParam.y + _denZ * _posParam.z + z * stride;
                    GenerateOnePixel(i, density, sx, sz, stride);
                }
            }
        }
        private void GenerateOnePixel(int i, int density, float sx, float sz, float stride)
        {
            int spread = (int)math.floor(math.sqrt(density) + 0.5f);
            float stride_x = 1f / spread;
            float stride_z = 1f / spread;
            for (int z = 0; z < spread; ++z)
            {
                for (int x = 0; x < spread; ++x)
                {
                    int idx = _spawnedCount[0];
                    float fx = sx + x * stride_x * stride;
                    float fz = sz + z * stride_z * stride;
                    float globalNoise = Noise(fx * _noiseSpread[i] + _noiseSeed[i], fz * _noiseSpread[i] + _noiseSeed[i]);
                    float localNoise = SNoise((fx + _noiseSeed[i]) * _detailResolutionPerPatch * _noiseSpread[i],
                        (fz + _noiseSeed[i]) * _detailResolutionPerPatch * _noiseSpread[i]);
                    float minW = math.min(_minWidth[i], _maxWidth[i]);
                    float maxW = math.max(_minWidth[i], _maxWidth[i]);
                    float width = math.lerp(minW, maxW, localNoise);
                    float minH = math.min(_minHeight[i], _maxHeight[i]);
                    float maxH = math.max(_minHeight[i], _maxHeight[i]);
                    float height = math.lerp(minH, maxH, localNoise);
                    _colors[idx] = math.lerp(_healthyColor[i], _dryColor[i], globalNoise);
                    _positions[idx] = new float3(fx + localNoise, 0, fz + localNoise);
                    _scales[idx] = new float3(width * _localScale.x, height * _localScale.y, height * _localScale.z);
                    _orientations[idx] = math.lerp(0, 360, localNoise);
                    ++_spawnedCount[0];
                }
            }
        }
        private float Noise(float x, float y)
        {
            float2 pos = math.float2(x, y);
            return noise.cnoise(pos);
        }
        private float SNoise(float x, float y)
        {
            float2 pos = math.float2(x, y);
            return noise.snoise(pos);
        }
    };


};
