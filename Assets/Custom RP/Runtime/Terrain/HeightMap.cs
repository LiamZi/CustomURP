using System.Collections.Generic;
using UnityEngine;

namespace CustomURP
{
    public class HeightMap
    {
        static Dictionary<uint, HeightMap> _dictMaps = new Dictionary<uint, HeightMap>();
        static int _mapWidth = 512;
        static int _mapHeight = 512;
        static float _halfRange = 0.0f;

        public Bounds Bound { get; private set; }
        int _heightResolusion = 513;
        byte[] _heights;
        Vector3 _heightScale;
        
        public HeightMap(Bounds bound, int resolution, Vector3 scale, byte[] data)
        {
            this.Bound = bound;
            _heightScale = scale;
            _heightResolusion = resolution;
            _heights = data;
            Registermap(this);
        }
        
        public static uint FormatId(Vector3 pos)
        {
            int x = Mathf.CeilToInt(pos.x + _halfRange) / _mapWidth;
            int y = Mathf.CeilToInt(pos.z + _halfRange) / _mapHeight;
            uint id = (uint)x;
            id = (id << 16) | (uint)y;
            return id;
        }
    
        
        public static void Registermap(HeightMap map)
        {
            var width = Mathf.FloorToInt(map.Bound.size.x);
            var height = Mathf.FloorToInt(map.Bound.size.z);

            if (_dictMaps.Count == 0)
            {
                _mapWidth = width;
                _mapHeight = height;
                _halfRange = Mathf.Max(_mapWidth, _mapHeight) * short.MaxValue;
            }

            if (_mapWidth != width || _mapHeight != height)
            {
                Debug.LogError(string.Format("height map size is not vaild : {0}, {1}", width, height));
                return;
            }

            uint id = FormatId(map.Bound.min);
            if (_dictMaps.ContainsKey(id))
            {
                Debug.LogError(string.Format("height map id overlapped : {0}, {1}", map.Bound.min.x, map.Bound.min.z));
                return;
            }
            _dictMaps.Add(id, map);
        }

        public static void UnregisterMap(HeightMap map)
        {
            uint id = FormatId(map.Bound.min);
            if (!_dictMaps.ContainsKey(id))
            {
                Debug.LogError(string.Format("height map not exist : {0}, {1}", map.Bound.center.x, map.Bound.center.z));
                return;
            }
            _dictMaps.Remove(id);
        }

        public static bool GetHeightInterpolated(Vector3 pos, ref float h)
        {
            uint id = FormatId(pos);
            if (_dictMaps.ContainsKey(id))
            {
                return _dictMaps[id].GetInterpolatedHeight(pos, ref h);
            }
            return false;
        }


        public bool GetInterpolatedHeight(Vector3 pos, ref float h)
        {
            var checkPos = pos;
            checkPos.y = Bound.center.y;
            if (!Bound.Contains(checkPos))
            {
                return false;
            }
            
            float val = GetInterpolatedHeightVal(pos);
            h = val * _heightScale.y / 255f + Bound.min.y;
            return true;
        }
        
        float GetInterpolatedHeightVal(Vector3 pos)
        {
            var local_x = Mathf.Clamp01((pos.x - Bound.min.x) / Bound.size.x) * (_heightResolusion - 1);
            var local_y = Mathf.Clamp01((pos.z - Bound.min.z) / Bound.size.z) * (_heightResolusion - 1);
            int x = Mathf.FloorToInt(local_x);
            int y = Mathf.FloorToInt(local_y);
            float tx = local_x - x;
            float ty = local_y - y;
            float y00 = SampleHeightMapData(x, y);
            float y10 = SampleHeightMapData(x + 1, y);
            float y01 = SampleHeightMapData(x, y + 1);
            float y11 = SampleHeightMapData(x + 1, y + 1);
            return Mathf.Lerp(Mathf.Lerp(y00, y10, tx), Mathf.Lerp(y01, y11, tx), ty);
        }
        
        float SampleHeightMapData(int x, int y)
        {
            int idx = y * _heightResolusion * 2 + x * 2;
            byte h = _heights[idx];
            byte l = _heights[idx + 1];
            return h + l / 255f;
        }

        public bool GetHeight(Vector3 pos, ref float h)
        {
            var localX = pos.x - Bound.min.x;
            var localY = pos.z - Bound.min.z;
            int x = Mathf.FloorToInt(localX);
            int y = Mathf.FloorToInt(localY);
            if (x >= 0 && x < _heightResolusion && y >= 0 && y < _heightResolusion)
            {
                float val = SampleHeightMapData(x, y) * _heightScale.y / 255.0f;
                h = val + Bound.min.y;
                return true;
            }

            return false;
        }
    }
}
