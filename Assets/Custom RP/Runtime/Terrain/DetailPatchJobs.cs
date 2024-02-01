using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;


namespace CustomURP
{

    
    public class DetailPatchDrawParam
    {
        static Queue<DetailPatchDrawParam> _pool = new Queue<DetailPatchDrawParam>();
        public int _used = 0;
        public Matrix4x4[] _matrixs;
        public Vector4[] _colors;
        public MaterialPropertyBlock _matBlock;

        public static DetailPatchDrawParam Pop()
        {
            if (_pool.Count > 0)
            {
                return _pool.Dequeue();
            }
            return new DetailPatchDrawParam();
        }

        public static void Push(DetailPatchDrawParam p)
        {
            p._used = 0;
            _pool.Enqueue(p);
        }

        public static void Clear()
        {
            _pool.Clear();
        }

        public DetailPatchDrawParam()
        {
            
        }

        public void Reset(int size)
        {
            if (_matrixs == null || size > _matrixs.Length)
            {
                size = Mathf.Min(size, 1023);
                _matrixs = new Matrix4x4[size];
                _colors = new Vector4[size];
            }
            if (_matBlock == null)
            {
                _matBlock = new MaterialPropertyBlock();
            }
            _matBlock.Clear();
            _used = 0;
        }
    };

    internal class PatchMaterialCutoffAnim
    {
        const float _maxCutoffVal = 1.01f;
        const float _cutoffAnimDuration = 0.3f;
        public const int _playing = 0;
        public const int _playDone = 1;
        
        public int State { get; private set; }
        protected float _cutoffAnimStartTime = 0;
        protected float _cutoffVal = 0.5f;
        protected float _animCutoffVal = 0.5f;
        protected Material _target;
        
        public bool Reversed { get; private set; }

        public bool MatInvisible
        {
            get
            {
                return State == _playDone && Reversed;
            }
        }

        public PatchMaterialCutoffAnim(Material mat)
        {
            Reversed = false;
            _target = mat;
            _cutoffVal = _target.GetFloat("_Cutoff");
            State = _playDone;
        }

        public void Replay(bool isReverse)
        {
            if (State == _playing && Reversed == isReverse)
            {
                return;
            }

            Reversed = isReverse;
            if (State == _playing)
            {
                float timeSkiped = _cutoffAnimDuration - (Time.time - _cutoffAnimStartTime);
                _cutoffAnimStartTime = Time.time - timeSkiped;
                InterpolateValue(timeSkiped);
            }
            else
            {
                _cutoffAnimStartTime = Time.time;
                _animCutoffVal = Reversed ? _cutoffVal : _maxCutoffVal;
            }
            State = _playing;
        }

        void InterpolateValue(float timePast)
        {
            float rate = timePast / _cutoffAnimDuration;
            if (Reversed)
            {
                _animCutoffVal = Mathf.Lerp(_cutoffVal, _maxCutoffVal, rate);
            }
            else
            {
                _animCutoffVal = Mathf.Lerp(_maxCutoffVal, _cutoffVal, rate);
            }
        }

        public void Tick()
        {
            if (State == _playDone) return;
            float timePast = Time.time - _cutoffAnimStartTime;
            if (timePast >= _cutoffAnimDuration)
            {
                State = _playDone;
                timePast = _cutoffAnimDuration;
            }
            InterpolateValue(timePast);
            _target.SetFloat("_Cutoff", _animCutoffVal);
        }

    };

    public abstract class DetailPatchLayer
    {
        public abstract bool _isSpawnDone { get; }
        protected Vector3 _localScale = Vector3.one;
        protected Mesh _mesh;
        protected Material _materialLod0;
        protected Material _materialLod1;
        protected DetailLayerData _layerData;
        protected Array<DetailPatchDrawParam> _drawParam;
        protected int _totalPrototypeCount;
        protected bool _isReceiveShadow = false;

        PatchMaterialCutoffAnim _cutoffAnim;

        public DetailPatchLayer(DetailLayerData data, bool receiveShadow)
        {
            _layerData = data;
            _localScale = data._prototype.transform.localScale;
            _mesh = data._prototype.GetComponent<MeshFilter>().sharedMesh;
            var matSrc = data._prototype.GetComponent<MeshRenderer>().sharedMaterial;
            _materialLod0 = new Material(matSrc);
            _isReceiveShadow = receiveShadow;
            if (_isReceiveShadow)
            {
                _materialLod0.EnableKeyword("_MAIN_LIGHT_SHADOWS");
            }
            _materialLod1 = new Material(_materialLod0);
            _materialLod1.DisableKeyword("_NORMALMAP");
            _materialLod1.EnableKeyword("FORCE_UP_NORMAL");
            _cutoffAnim = new PatchMaterialCutoffAnim(_materialLod1);
        }

        public virtual void OnActivate(bool rebuild)
        {
            if (_cutoffAnim.State != PatchMaterialCutoffAnim._playDone)
            {
                _cutoffAnim.Replay(false);
            }
            
            if (rebuild)
            {
                _totalPrototypeCount = 0;
            }
        }

        public virtual void OnDrawParamReady()
        {
            _cutoffAnim.Replay(false);
        }

        public virtual void OnDeactive()
        {
            _cutoffAnim.Replay(true);
        }

        public virtual void PushData()
        {
            if (_drawParam == null) return;

            for (int i = 0; i < _drawParam.Length; ++i)
            {
                DetailPatchDrawParam.Push(_drawParam._data[i]);
            }
            _drawParam.Reset();
        }

        public abstract void TickBuild();

        public virtual void OnDraw(Camera camera, int lod, ref bool matInvisible)
        {
            if (_drawParam == null) return;
            
            _cutoffAnim.Tick();
            if (_cutoffAnim.MatInvisible)
            {
                matInvisible = true;
                return;
            }

            matInvisible = false;
            for (int i = 0; i < _drawParam.Length; ++i)
            {
                if(_drawParam._data[i]._used <= 0)
                    continue;

                var mat = _materialLod0;
                if(lod > 0)
                    mat = _materialLod1;
                _drawParam._data[i]._matBlock.SetVectorArray("_PerInstanceColor", _drawParam._data[i]._colors);
                Graphics.DrawMeshInstanced(_mesh, 0, mat, _drawParam._data[i]._matrixs, 
                                _drawParam._data[i]._used, _drawParam._data[i]._matBlock, ShadowCastingMode.Off, 
                                _isReceiveShadow, LayerMask.NameToLayer("Default"), camera);
            }
        }

        public virtual void Clear()
        {
            PushData();
            _cutoffAnim = null;
            if (_materialLod0 != null)
            {
                Object.Destroy(_materialLod0);
                _materialLod0 = null;
            }
            
            if (_materialLod1 != null)
            {
                Object.Destroy(_materialLod1);
                _materialLod1 = null;
            }
        }
    };

    public abstract class DetailPatch
    {
        public abstract bool IsBuildDone { get; }
        protected int _denX;
        protected int _denZ;
        protected Vector3 _posParam;
        protected TData _headerData;
        protected DetailPatchLayer[] _layers;
        protected Vector2 _center;
        float _lod0Range;

        public DetailPatch(int dx, int dz, Vector3 posParam, TData header)
        {
            _denX = dx;
            _denZ = dz;
            _posParam = posParam;
            _headerData = header;
            _center = new Vector2(posParam.x + (_denX + 0.5f) * posParam.z, posParam.y + (_denZ + 0.5f) * posParam.z);
            _lod0Range = posParam.z * 1.5f;
        }

        public virtual void Activate()
        {
            for (int i = 0; i < _layers.Length; ++i)
            {
                _layers[i].OnActivate(true);
            }
        }

        public virtual void Deactivate()
        {
            for (int i = 0; i < _layers.Length; ++i)
            {
                _layers[i].OnDeactive();
            }
        }

        public virtual void PushData()
        {
            for (int i = 0; i < _layers.Length; ++i)
            {
                _layers[i].PushData();
            }
        }

        public virtual void Clear()
        {
            for (int i = 0; i < _layers.Length; ++i)
            {
                _layers[i].Clear();
            }
        }

        public abstract void TickBuild();

        public void Draw(Camera camera, ref bool isInvisble)
        {
            int lod = 1;
            if (camera != null)
            {
                var transform = camera.transform;
                Vector2 distance = new Vector2(_center.x - transform.position.x, _center.y - transform.position.z);
                if (distance.magnitude < _lod0Range)
                    lod = 0;
            }

            isInvisble = true;
            for (int i = 0; i < _layers.Length; ++i)
            {
                bool matInvisible = true;
                _layers[i].OnDraw(camera, lod, ref matInvisible);
                if (!matInvisible)
                    matInvisible = false;
            }
        }

        public void DrawDebug()
        {
#if UNITY_EDITOR
            Gizmos.color = Color.yellow;
            var min = new Vector3(_posParam.x + _denX * _posParam.z, 0, _posParam.y + _denZ * _posParam.z);
            var size = new Vector3(_posParam.z, 100, _posParam.z);
            Gizmos.DrawWireCube(min + 0.5f * size, size); 
#endif
        }
    };

    internal class DetailQuadTreeNode
    {
        public Bounds _bound;
        public DetailQuadTreeNode[] _children;
        public int _patchId = -1;
        int _depth = 0;

        public DetailQuadTreeNode(int top, Bounds nodeBound, Bounds worldBounds)
        {
            _bound = nodeBound;
            _depth = top;
            if (_depth < 1)
            {
                var localCenter = _bound.center - worldBounds.min;
                int width = Mathf.FloorToInt(worldBounds.size.x / nodeBound.size.x);
                int px = Mathf.FloorToInt(localCenter.x / nodeBound.size.x);
                int pz = Mathf.FloorToInt(localCenter.z / nodeBound.size.z);
                _patchId = pz * width + px;
                return;
            }

            _children = new DetailQuadTreeNode[4];
            var subSize = nodeBound.size;
            subSize.x *= 0.5f;
            subSize.z *= 0.5f;
            var subCenter = nodeBound.center;
            subCenter.x -= 0.5f * subSize.x;
            subCenter.z -= 0.5f * subSize.z;
            _children[0] = new DetailQuadTreeNode(top - 1, new Bounds(subCenter, subSize), worldBounds);
            subCenter = nodeBound.center;
            subCenter.x += 0.5f * subSize.x;
            subCenter.z -= 0.5f * subSize.z;
            _children[1] = new DetailQuadTreeNode(top - 1, new Bounds(subCenter, subSize), worldBounds);
            subCenter = nodeBound.center;
            subCenter.x += 0.5f * subSize.x;
            subCenter.z += 0.5f * subSize.z;
            _children[2] = new DetailQuadTreeNode(top - 1, new Bounds(subCenter, subSize), worldBounds);
            subCenter = nodeBound.center;
            subCenter.x -= 0.5f * subSize.x;
            subCenter.z += 0.5f * subSize.z;
            _children[3] = new DetailQuadTreeNode(top - 1, new Bounds(subCenter, subSize), worldBounds);
        }

        public void CullQuadTree(Plane[] planes, Array<int> visible)
        {
            if (GeometryUtility.TestPlanesAABB(planes, _bound))
            {
                if (_children == null)
                {
                    visible.Add(_patchId);
                }
                else
                {
                    foreach (var c in _children)
                    {
                        c.CullQuadTree(planes, visible);
                    }
                }
            }
        }

    };

    public class DetailRenderer
    {

    };

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
        static int _jobRunningCount = 0;
        const int _maxConcurrentJobCount = 4;
        private DetailLayerCreateJob _job;
        private JobHandle _createJob;
        
        public DetailPatchLayerJob(DetailLayerData data, DetailLayerCreateJob j, bool receiveShadow) 
            : base(data, receiveShadow)
        {
            _job = j;
        }
        
        public override bool _isSpawnDone
        {
            get;
        }
        public override void TickBuild()
        {
            throw new System.NotImplementedException();
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
                    float min_w = math.min(_minWidth[i], _maxWidth[i]);
                    float max_w = math.max(_minWidth[i], _maxWidth[i]);
                    float width = math.lerp(min_w, max_w, localNoise);
                    float min_h = math.min(_minHeight[i], _maxHeight[i]);
                    float max_h = math.max(_minHeight[i], _maxHeight[i]);
                    float height = math.lerp(min_h, max_h, localNoise);
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
