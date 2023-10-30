using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    public class Command
    {
        private Common.Pass _pass;
        private Common.RenderType _type;
        private string _name;
        private UnityEngine.Rendering.CommandBuffer _cmd = null;


        
        public Command(Common.Pass pass)
        {
            _pass = pass;
            _type = Common.RenderType.Normal;
            _cmd = new UnityEngine.Rendering.CommandBuffer() {};
        }
        

        public Command(UnityEngine.Rendering.CommandBuffer cmd, Common.Pass pass)
        {
            _pass = pass;
            _cmd = cmd;
            _type = Common.RenderType.Normal;
        }

        public Command(Common.Pass pass = Common.Pass.Normal, Common.RenderType type = Common.RenderType.Normal)
        {
            _pass = pass;
            _type = type;
            _cmd = new UnityEngine.Rendering.CommandBuffer() { };
        }
        
        public Command(Common.Pass pass = Common.Pass.Normal, Common.RenderType type = Common.RenderType.Normal, string name = "Render Camera Buffer")
        {
            _pass = pass;
            _type = type;
            _name = name;
            _cmd = new UnityEngine.Rendering.CommandBuffer() { name = _name };
        }

        public Command(UnityEngine.Rendering.CommandBuffer cmd, Common.Pass pass, Common.RenderType type, string name)
        {
            _pass = pass;
            _type = type;
            _cmd = cmd;
            _name = name;
        }

        public void BeginSample()
        {
            _cmd.BeginSample(_name);
        }

        public void EndSample()
        {
            _cmd.EndSample(_name);
        }

        public void Execute(ScriptableRenderContext context)
        {
            context.ExecuteCommandBuffer(_cmd);
            // _cmd.Clear();
            Clear();
        }

        public void Clear()
        {
            _cmd.Clear();
        }

        public void ExecuteAsync(ScriptableRenderContext context, ComputeQueueType type)
        {
            context.ExecuteCommandBufferAsync(_cmd, type);
            Clear();
        }
        
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public Common.Pass Pass
        {
            get { return _pass; }
            
            set { _pass = value; }
        }
        
        
        
        public UnityEngine.Rendering.CommandBuffer Cmd => _cmd;

        public void Destroy()
        {
            // _cmd.Clear();
            _cmd.Dispose();
        }

    };

};
