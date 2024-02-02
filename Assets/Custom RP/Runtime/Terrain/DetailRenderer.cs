using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Core;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine.UI;

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
        Bounds _mapBound;
        TData _headerData;
        int[] _patchDataOffsets;
        NativeArray<byte> _densityData;
        DetailPatch[] _patches;
        DetailQuadTreeNode _tree;
        int _patchX = 1;
        int _patchZ = 1;
        List<int> _buildingPatches = new List<int>();
        bool _receiveShadow = true;
        List<int> _drawablePathces = new List<int>();
        Array<int> _currentVisible;
        Array<int> _activePatches;
        Vector3 _patchParam = Vector3.zero;
        
        
        public DetailRenderer(TData data, Bounds bound, bool shadow)
        {
            _mapBound = bound;
            _headerData = data;
            _patchX = Mathf.CeilToInt((float)_headerData._detailWidth / _headerData._detailResolutionPerPatch);
            _patchZ = Mathf.CeilToInt((float)_headerData._detailHeight / _headerData._detailResolutionPerPatch);
            _patches = new DetailPatch[_patchX * _patchZ];
            _patchParam = new Vector3(_mapBound.min.x, _mapBound.min.z, Mathf.Max(_mapBound.size.x / _patchX, _mapBound.size.z / _patchZ));
            _receiveShadow = shadow;
            
            int treeDepth = Mathf.FloorToInt(Mathf.Log(Mathf.Max(_patchX, _patchZ), 2));
            _tree = new DetailQuadTreeNode(treeDepth, _mapBound, _mapBound);
            _currentVisible = new Array<int>(_patchX * _patchZ);
            _activePatches = new Array<int>(_patchX * _patchZ);
            _densityData = new NativeArray<byte>(_headerData._detailLayers.bytes, Allocator.Persistent);
            _patchDataOffsets = new int[_patchX * _patchZ * _headerData._detailPrototypes.Length];
            MemoryStream stream = new MemoryStream(_headerData._detailLayers.bytes);
            for (int i = 0; i < _patchDataOffsets.Length; ++i)
            {
                _patchDataOffsets[i] = FileUtility.ReadInt(stream);
            }
            stream.Close();

        }

        public void Cull(Plane[] plane)
        {
            _tree.CullQuadTree(plane, _currentVisible);
            for(int i = 0; i < _currentVisible.Length; ++i)
            {
                var pId = _currentVisible._data[i];
                if (!_activePatches.Contains(pId))
                {
                    ActivePatch(pId);
                }
            }

            for (int i = 0; i < _activePatches.Length; ++i)
            {
                var pid = _activePatches._data[i];
                if (!_currentVisible.Contains(pid))
                {
                    DeactivePatch(pid);
                }
            }

            var temp = _activePatches;
            _activePatches = _currentVisible;
            _currentVisible = temp;
            _currentVisible.Reset();
        }

        public void Tick(Camera camera)
        {
            for (int i = _buildingPatches.Count - 1; i >= 0; --i)
            {
                var pid = _buildingPatches[i];
                var p = _patches[pid];
                p.TickBuild();
                if (p.IsBuildDone)
                {
                    _buildingPatches.RemoveAt(i);
                    _drawablePathces.Add(pid);
                }
            }

            for (int i = _drawablePathces.Count - 1; i >= 0; --i)
            {
                var pid = _drawablePathces[i];
                var p = _patches[pid];
                bool invisible = false;
                p.Draw(camera, ref invisible);
                if (invisible)
                {
                    p.PushData();
                    _drawablePathces.RemoveAt(i);
                }
            }
        }

        public void Clear()
        {
            _drawablePathces.Clear();
            _buildingPatches.Clear();
            DetailPatchDrawParam.Clear();
            foreach (var patch in _patches)
            {
                if (patch != null)
                    patch.Clear();
            }
            _densityData.Dispose();
        }

        public void DrawDebug()
        {
            for (int i = 0; i < _activePatches.Length; ++i)
            {
                var p = _patches[_activePatches._data[i]];
                p.DrawDebug();
            }
        }

        void ActivePatch(int id)
        {
            if (_patches[id] == null)
            {
                int px = id % _patchX;
                int pz = id / _patchZ;
                _patches[id] = CreatePatch(px, pz);
            }
            var p = _patches[id];
            if (p != null)
            {
                p.Activate();
                if (!p.IsBuildDone)
                    _buildingPatches.Add(id);
            }
        }
        
        void DeactivePatch(int id)
        {
            var p = _patches[id];
            p.Deactivate();
            if (!p.IsBuildDone)
            {
                p.PushData();
                _buildingPatches.Remove(id);
            }
        }
        
        protected DetailPatch CreatePatch(int px, int pz)
        {
            return new DetailPatchJobs(px, pz, _patchX, _patchZ, _patchParam, _receiveShadow,
                _headerData, _patchDataOffsets, _densityData);
        }
    };
     
    

    
}
