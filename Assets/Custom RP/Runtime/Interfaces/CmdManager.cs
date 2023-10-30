using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System.Threading;
using System;


namespace CustomURP
{
    public class CmdManager
    {
        private static CmdManager _sharedInstance = null;
        
        private Dictionary<Common.Pass, List<Command>> _cmdPool;
        // private LoadingThread _thread = null;
        private AutoResetEvent _restEvent;
        private Thread _thread;
        private bool _isReady = false;
        
        private CmdManager()
        {
            
            _thread = new Thread(Tick);
        }

        private void Initialization()
        {
            for (var i = Common.Pass.Default; i < Common.Pass.Max; ++i)
            {
                _cmdPool.Add((Common.Pass)i, new List<Command>());
            }
            
            _isReady = true;
            _thread.Start();
        }

        public static CmdManager Singleton
        {
            get { return _sharedInstance ??= new CmdManager(); }
        }

        public void Add(Command cb)
        {
            var renderList = _cmdPool[cb.Pass];
            if (renderList != null)
            {
                renderList.Add(cb);
            }
            else
            {
                renderList = new List<Command> { cb };
                _cmdPool[cb.Pass] = renderList;
            }
        }

        public void Remove(Command cb)
        {
            var renderList = _cmdPool[cb.Pass];
            if (renderList == null || renderList.Count <= 0) return;
            foreach (var cmd in renderList)
            {
                if(cmd != cb) continue;
                renderList.Remove(cb);
            }
        }

        public void Clear()
        {
            foreach (var pair in _cmdPool)
            {
                // cmd.Destroy();
            }
            
            _cmdPool.Clear();
        }

        public Dictionary<Common.Pass, List<Command>> GetAll()
        {
            return _cmdPool;
        }

        public bool Has(Command cb)
        {
            var renderList = _cmdPool[cb.Pass];
            if (renderList == null || renderList.Count <= 0) return false;
            return renderList.Contains(cb);
        }

        public Command GetTemporaryCmd(Common.Pass pass)
        {
            var cmd = new Command(pass);
            var renderList = _cmdPool[pass];
            if (renderList != null)
            {
                renderList.Add(cmd);
            }
            else
            {
                renderList = new List<Command>() { cmd };
                _cmdPool[pass] = renderList;
            }

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

        private void Tick()
        {
            if (!_isReady) return;
        }
    }

};
