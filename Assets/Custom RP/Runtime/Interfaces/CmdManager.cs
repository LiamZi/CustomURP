using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Rendering;
using UnityEngine.UI;


namespace CustomURP
{
    public class CmdManager
    {
        private static CmdManager _sharedInstance = null;
        
        private Dictionary<Common.Pass, List<Command>> _cmdPool;
        private LoadingThread _thread = null;
        
        
        private CmdManager()
        {
            _thread = LoadingThread.Singleton();
        }

        public static CmdManager Singleton
        {
            get { return _sharedInstance ??= new CmdManager(); }
        }

        public void Add(Command cb)
        {
            _cmdPool.Add(cb.Pass, cb);
        }

        public void Remove(Command cb)
        {
            _cmdPool.Remove(cb);
        }

        public void Clear()
        {
            foreach (var pair in _cmdPool)
            {
                // cmd.Destroy();
                
            }
            
            
            _cmdPool.Clear();
        }

        public List<Command> GetAll()
        {
            // return _cmdPool;
            
        }

        public bool Has(Command cb)
        {
            // return _cmdPool.Contains(cb);
        }

        public Command GetTemporaryCmd(Common.Pass pass)
        {
            var cmd = new Command(pass);
            _cmdPool.Add(cmd);
            return cmd;
        }

        public Command GetTemporaryCmd(Common.Pass pass, Common.RenderType type)
        {
            var cmd = new Command(pass, type);
            _cmdPool.Add(cmd);
            return cmd;
        }

        public Command GetTemporaryCmd(Common.Pass pass, Common.RenderType type, string name)
        {
            var cmd = new Command(pass, type, name);
            _cmdPool.Add(cmd);
            return cmd;
        }
       
        public Command Get(string name)
        {
            return _cmdPool.Find(cmd => { return cmd.Name.Equals(name); });
        }

        public Command First()
        {
            return _cmdPool.First();
        }

        public Command Last()
        {
            return _cmdPool.Last();
        }

        public void BeginSample()
        {
            foreach (var cb in _cmdPool)
            {
                cb.BeginSample();
            }
        }

        public void BeginSample(string name)
        {
            var cmd = _list.Find(cmd => { return cmd.Name.Equals(name); });
            if(cmd != null) cmd.BeginSample();
        }

        public void EndSample()
        {
            foreach(var cb in _list)
            {
                cb.EndSample();
            }
        }

        public void EndSample(string name)
        {
            var cmd = _list.Find(cmd => { return cmd.Name.Equals(name); });
            if(cmd != null) cmd.EndSample();
        }

        public Command Find(string name)
        {
            return _list.Find(cmd => { return cmd.Name.Equals(name); });
        }

        public bool Exists(string name)
        {
            return _list.Exists(cmd => { return cmd.Name.Equals(name); });
        }
    }

};
