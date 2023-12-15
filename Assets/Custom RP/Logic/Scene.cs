
using System.Collections;
using System.Collections.Generic;
using CustomURP;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;

namespace CustomPipeline
{
    public unsafe class Scene
    {
        private CustomRenderPipelineAsset _asset;
        private int _clusterCount = 0;
        
        public bool _gpuDriven { get; private set; } = false;
        ClusterAction _cluster = null;
        public ClusterAction Cluster { get => _cluster; set { _cluster = value; } }
        public Scene(CustomRenderPipelineAsset asset)
        {
            _asset = asset;
        }
        public void Awake()
        {
            int maxClusterCount = 0;
            _cluster = _asset.ClusterShading._clusterAction;
            // if (Application.isPlaying && _cluster != null)
            if(_cluster != null)
            {
                //todo: init cluster
                _cluster.Initialization(_asset);
            }
        }

        public void SetClusterCullResult(ref CullingResults results)
        {
            // if (Application.isPlaying && _cluster != null && !_cluster._isInited)
            if(_cluster != null)
            {
                _cluster.SetCullResult(ref results);
            }
        }
        public void BeginRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            // if (Application.isPlaying && _cluster != null && !_cluster._isInited)
            if (_cluster != null && _cluster.Enabled)
            // if(_cluster != null)
            {
                _cluster.BeginRendering(camera, ref cmd);
            }
        }


        public void Tick(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            // if (Application.isPlaying && _cluster != null && _cluster._isInited)
            // if (Application.isPlaying && _cluster != null )
            if (_cluster != null && _cluster.Enabled)
            // if(_cluster != null)
            {
                // _cluster.DebugCluster(camera._camera);
                _cluster.Tick(camera, ref cmd); 
            }

            CleanClusterShadow();

        }

        public void EndRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            
        }

        public void CleanClusterShadow()
        {
            if (_cluster && _cluster.Enabled)
            {
                _cluster.CleanShadows();
            }
        }

        public void Dispose()
        {
            if (_cluster)
            {
                _cluster.Dispose();
                // _cluster = null;
            }
        }

        public void SetState()
        {
            _gpuDriven = _clusterCount > 0;
        }
    }
}