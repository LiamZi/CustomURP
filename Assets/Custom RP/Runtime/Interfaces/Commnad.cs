using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    public class Command
    {
        private string _name;
        private UnityEngine.Rendering.CommandBuffer _cmd;
        private ScriptableRenderContext _context;
        private CustomRenderPipelineAsset _asset;

        public Command(string name)
        {
            _name = name;
            _cmd = new UnityEngine.Rendering.CommandBuffer() { name = _name };
        }

        public Command(UnityEngine.Rendering.CommandBuffer buffer, string name) 
        {
            _cmd = buffer;
            _name = name;
        }

        public void BeginSample()
        {
            _cmd.BeginSample(_name);
        }

        public void EndSampler()
        {
            _cmd.EndSample(_name);
        }

        public void Execute(ScriptableRenderContext context)
        {
            context.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }

        public void ExecuteAsync(ScriptableRenderContext context, ComputeQueueType type)
        {
            context.ExecuteCommandBufferAsync(_cmd, type);
            _cmd.Clear();
        }
        
        public string Name
        {
            get { return _name; }
            set { 
                _name = value;
                _cmd.name = value;
            }
        }

        public ScriptableRenderContext Context
        {
            get { return _context; }
            set { _context = value; }
        }

        public CustomRenderPipelineAsset Asset
        {
            get { return _asset; }
            set { _asset = value; }
        }
        
        public UnityEngine.Rendering.CommandBuffer Cmd => _cmd;

    };

};
