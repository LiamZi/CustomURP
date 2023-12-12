using System;
using System.Collections;
using Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    [System.Serializable]
    public unsafe abstract class CoreAction : ScriptableObject
    {
        [SerializeField]
        protected bool _isEnabled;
        protected bool _isInitialized;
        protected UnsafeList* _dependedActions;
        protected UnsafeList* _dependingActions;
        protected CustomRenderPipelineCamera _camera;
        protected Command _cmd;
        protected CustomRenderPipelineAsset _asset;
        protected bool _isUseHDR;
        protected bool _isUseColorTexture;
        protected bool _isUseDepthTexture;
        protected bool _isUseIntermediateBuffer;
        protected bool _isUseScaledRendering;

        public bool Enabled
        {
            get { return _isEnabled; }
            set
            {
                if (_isEnabled == value) return;
                InspectDependActions();
            }
        }
        
        public void Prepare()
        {
            _dependedActions = UnsafeList.Allocate<UIntPtr>(10);
            _dependingActions = UnsafeList.Allocate<UIntPtr>(10);
        }

        public void InspectDependActions()
        {
            if (_isInitialized) return;
            
            if (_isEnabled)
            {
                if (_dependingActions == null) return;
                
                var iter = UnsafeList.GetIterator<UIntPtr>(_dependingActions);
                foreach (var i in iter)
                {
                    var action = CustomPipeline.UnsafeUtility.GetObject<CoreAction>(i.ToPointer());
                    if (!action._isEnabled)
                    {
                        Enabled = false;
                        return;
                    }
                }
            }
            else
            {
                if (_dependedActions == null) return;

                var iter = UnsafeList.GetIterator<UIntPtr>(_dependedActions);
                foreach (var i in iter)
                {
                    var action = CustomPipeline.UnsafeUtility.GetObject<CoreAction>(i.ToPointer());
                    action.Enabled = false;
                }
            }
            
            if(Enabled) 
                OnEnable();
            else
                OnDisable();
        }

        protected internal abstract void Initialization(CustomRenderPipelineAsset asset);
        
        public virtual void Tick(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            _cmd = cmd;
            _camera = camera;
        }

        public virtual void BeginRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            _cmd = cmd;
            _camera = camera;
        }

        public virtual void EndRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            
        }

        protected virtual void OnEnable()
        {
            
        }

        protected virtual void OnDisable()
        {
           
        }

        public virtual void Dispose()
        {
            if (_dependedActions != null)
            {
                UnsafeList.Free(_dependedActions);
                _dependedActions = null;    
            }

            if (_dependingActions != null)
            {
                UnsafeList.Free(_dependingActions);
                _dependingActions = null;
            }
            
            _isInitialized = false;
        }

        public abstract bool InspectProperty();
    };

    public unsafe static class Actions
    {
        [RenderingType(CustomRenderPipelineAsset.CameraRenderType.Forward)]
        public static readonly Type[] _forwardRendering = 
        {
            typeof(LightPass),
            typeof(GeometryPass),
            typeof(PostProcessPass),
            typeof(CombinePass),
        };
    };
    

}