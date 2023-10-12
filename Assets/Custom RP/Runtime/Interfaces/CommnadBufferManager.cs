using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Rendering;


namespace CustomURP
{
    public class CommandBufferManager
    {
        private static CommandBufferManager _sharedInstance = null;

        private List<CommandBuffer> _list = new List<CommandBuffer>();

        private CommandBufferManager()
        {
            
        }

        public static CommandBufferManager Singleton
        {
            get 
            {
                if (_sharedInstance == null)
                {
                    _sharedInstance = new CommandBufferManager();
                }
                return _sharedInstance;
            }
        }

        public void Add(CommandBuffer cb)
        {
            _list.Add(cb);
        }

        public void Remove(CommandBuffer cb)
        {
            _list.Remove(cb);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public List<CommandBuffer> GetAll()
        {
            return _list;
        }

        public bool Has(CommandBuffer cb)
        {
            return _list.Contains(cb);
        }

        public CommandBuffer GetTemporaryCB(string name = "")
        {
            var buffer = new CommandBuffer(name);
            _list.Add(buffer);
            return buffer;
        }

        public CommandBuffer Get(string name)
        {
            foreach(var cb in _list)
            {
                if(cb.Name == name) return cb;
            }

            return null;
        }

        public CommandBuffer First()
        {
            return _list.First();
        }

        public CommandBuffer Last()
        {
            return _list.Last();
        }

        public void BeginSample()
        {
            foreach (var cb in _list)
            {
                cb.BeginSample();
            }
        }

        public void BeginSample(string name)
        {
            foreach (var cb in _list)
            {
                if(cb.Name == name)
                {
                    cb.BeginSample();
                }
            }
        }

        public void EndSample()
        {
            foreach(var cb in _list)
            {
                cb.EndSampler();
            }
        }

        public void EndSample(string name)
        {
            foreach (var cb in _list)
            {
               if(cb.Name == name)
                {
                    cb.EndSampler();
                }
            }
        }
    }

};
