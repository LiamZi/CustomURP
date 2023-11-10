﻿using System;
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
        protected ScriptableRenderContext _context;

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
        
        public virtual void Tick(CustomRenderPipelineCamera camera, ref ScriptableRenderContext context)
        {
            _context = context;
            _camera = camera;
        }

        public virtual void BeginRendering(CustomRenderPipelineCamera camera, ref ScriptableRenderContext context)
        {

        }

        public virtual void EndRendering(CustomRenderPipelineCamera camera, ref ScriptableRenderContext context)
        {
            
        }

        protected virtual void OnEnable()
        {
            
        }

        protected virtual void OnDisable()
        {
           
        }

        protected internal virtual void Dispose()
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
        }

        public abstract bool InspectProperty();
    };

    public unsafe static class Actions
    {
        [RenderingType(CustomRenderPipelineAsset.CameraRenderType.Forward)]
        public static readonly Type[] _forwardRendering = 
        {
            typeof(GeometryPass),
            typeof(LightPass),
            typeof(PostProcessPass),
        };
    };
    

}