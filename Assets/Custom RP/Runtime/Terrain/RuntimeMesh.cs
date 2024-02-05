using System.Collections.Generic;
using UnityEngine;
namespace CustomURP
{
    public class RenderMesh
    {
        public Mesh _mesh;
        public Vector2 _uvMin;
        public Vector2 _uvMax;

        public void Clear()
        {
            MonoBehaviour.Destroy(_mesh);
            _mesh = null;
        }
    };

    public interface ITVirtualTextureReceiver
    {
        long WaitCmdId { get; }
        void OnTextureReady(long cmd, ITVirtualTexture[] textures);
    };

    public class PooledRenderMesh : ITVirtualTextureReceiver
    {
        static Queue<PooledRenderMesh> _pool = new Queue<PooledRenderMesh>();
        TData _dataHeader;
        RenderMesh _rm;
        GameObject _go;
        MeshFilter _mesh;
        MeshRenderer _renderer;
        Material[] _materials;
        IVTCreator _vtCreator;
        float _diameter = 0;
        Vector3 _center = Vector3.zero;
        int _textureSize = -1;
        ITVirtualTexture[] _textures;
        long _waitBackCmdId = 0;
        TVTCreateCmd _lastPendingCreateCmd;

        public PooledRenderMesh()
        {
            _go = new GameObject("TerrainPatch");
            _mesh = _go.AddComponent<MeshFilter>();
            _renderer = _go.AddComponent<MeshRenderer>();
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public void Reset(TData header, IVTCreator vtCreator, RenderMesh mesh, Vector3 offset)
        {
            _dataHeader = header;
            _vtCreator = vtCreator;
            _rm = mesh;
            _go.SetActive(true);
            _go.transform.position = offset;
            _mesh.mesh = _rm._mesh;
            if (_materials == null)
            {
                _materials = new Material[1];
                _materials[0] = GameObject.Instantiate(_dataHeader._bakedMat);
            }
            ClearRendererMaterial();
            _renderer.materials = _dataHeader._detailMats;
            _diameter = _rm._mesh.bounds.size.magnitude;
            _center = _rm._mesh.bounds.center + offset;
            _textureSize = -1;
            _waitBackCmdId = 0;
        }

        public static PooledRenderMesh Pop()
        {
            if (_pool.Count > 0)
            {
                return _pool.Dequeue();
            }

            return new PooledRenderMesh();
        }

        public static void Push(PooledRenderMesh mesh)
        {
            mesh.OnPushBackPool();
            _pool.Enqueue(mesh);
        }

        public static void Clear()
        {
            while (_pool.Count > 0)
            {
                _pool.Dequeue().Destory();
            }
        }

        void Destory()
        {
            ClearRendererMaterial();
            if (_materials != null)
            {
                foreach (var m in _materials)
                {
                    GameObject.Destroy(m);
                }
            }

            _materials = null;

            if (_go != null)
            {
                MonoBehaviour.Destroy(_go);
            }
            _go = null;
            _mesh = null;
        }

        void ClearRendererMaterial()
        {
            if (_renderer != null && _renderer.materials != null)
            {
                for (int i = 0; i < _renderer.materials.Length; ++i)
                {
                    var mat = _renderer.materials[i];
                    GameObject.Destroy(mat);
                }
            }
        }

        public void OnPushBackPool()
        {
            _materials[0].SetTexture("_Diffuse", null);
            _materials[0].SetTexture("_Normal", null);
            if (_go != null) _go.SetActive(false);

            if (_textures != null)
            {
                _vtCreator.DiposeTextures(_textures);
                _textures = null;
            }

            _waitBackCmdId = 0;
            if (_lastPendingCreateCmd != null)
            {
                TVTCreateCmd.Push(_lastPendingCreateCmd);
                _lastPendingCreateCmd = null;
            }
            _textureSize = -1;
            _rm = null;
        }

        int CalculateTextureSize(Vector3 viewCenter, float fov, float screenH)
        {
            float distance = Vector3.Distance(viewCenter, _center);
            float pixelSize = (_diameter * Mathf.Rad2Deg * screenH) / (distance * fov);
            return Mathf.NextPowerOfTwo(Mathf.FloorToInt(pixelSize));
        }

        void RequestTexture(int size)
        {
            size = Mathf.Clamp(size, 128, 2048);
            // if (size != _textureSize)
            {
                _textureSize = size;
                var cmd = TVTCreateCmd.Pop();
                cmd._cmdId = TVTCreateCmd.GenerateId();
                cmd._size = size;
                cmd._uvMin = _rm._uvMin;
                cmd._uvMax = _rm._uvMax;
                cmd._bakeDiffuse = _dataHeader._bakeDiffuseMats;
                cmd._bakeNormal = _dataHeader._bakeNormalMats;
                cmd._receiver = this;
                if (_waitBackCmdId > 0)
                {
                    if (_lastPendingCreateCmd != null)
                    {
                        TVTCreateCmd.Push(_lastPendingCreateCmd);
                    }
                    _lastPendingCreateCmd = cmd;
                }
                else
                {
                    _waitBackCmdId = cmd._cmdId;
                    _vtCreator.AppendCmd(cmd);
                }
            }
        }

        void ApplyTextures()
        {
            Vector2 size = _rm._uvMax - _rm._uvMin;
            var scale = new Vector2(1f / size.x, 1f / size.y);
            var offset = -new Vector2(scale.x * _rm._uvMin.x, scale.y * _rm._uvMin.y);
            _materials[0].SetTexture("_Diffuse", _textures[0].tex);
            _materials[0].SetTextureScale("_Diffuse", scale);
            _materials[0].SetTextureOffset("_Diffuse", offset);
            _materials[0].SetTexture("_Normal", _textures[1].tex);
            _materials[0].SetTextureScale("_Normal", scale);
            _materials[0].SetTextureOffset("_Normal", offset);

        }

        long ITVirtualTextureReceiver.WaitCmdId
        {
            get
            {
                return _waitBackCmdId;
            }
        }
        public void OnTextureReady(long cmd, ITVirtualTexture[] textures)
        {
            if (_rm == null || cmd != _waitBackCmdId)
            {
                _vtCreator.DiposeTextures(textures);
                return;
            }

            if (_textures != null)
            {
                _vtCreator.DiposeTextures(_textures);
                _textures = null;
            }

            _textures = textures;
            ApplyTextures();
            ClearRendererMaterial();
            _renderer.materials = _materials;
            _waitBackCmdId = 0;
            if (_lastPendingCreateCmd != null)
            {
                _waitBackCmdId = _lastPendingCreateCmd._cmdId;
                _vtCreator.AppendCmd(_lastPendingCreateCmd);
                _lastPendingCreateCmd = null;
            }
        }

        public void UpdatePatch(Vector3 viewCenter, float fov, float screenH, float screenW)
        {
            int curTexSize = CalculateTextureSize(viewCenter, fov, screenH);
            if (curTexSize != _textureSize)
            {
                RequestTexture(curTexSize);
            }
        }

    };
}
