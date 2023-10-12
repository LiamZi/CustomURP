using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    public class CommandBuffer
    {
        private string _name;
        private UnityEngine.Rendering.CommandBuffer _buffer;

        public CommandBuffer(string name)
        {
            _name = name;
            _buffer = new UnityEngine.Rendering.CommandBuffer() { name = _name };
        }

        public CommandBuffer(UnityEngine.Rendering.CommandBuffer buffer, string name) 
        {
            _buffer = buffer;
            _name = name;
        }

        public void BeginSample()
        {
            _buffer.BeginSample(_name);
        }

        public void EndSampler()
        {
            _buffer.EndSample(_name);
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        
        public UnityEngine.Rendering.CommandBuffer Buffer => _buffer;

    };

};
