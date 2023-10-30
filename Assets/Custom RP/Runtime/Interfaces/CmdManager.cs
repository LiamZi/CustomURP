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
using System.Data;

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
        private ScriptableRenderContext _context;
        
        private CmdManager()
        {
            
            _thread = new Thread(Tick);
            //Initialization();
        }

        private void Initialization(ref ScriptableRenderContext context)
        {
            if (_context == null)
            {
                Debug.Log("<color=#FF0000>CmdManger Initialization failed. Becuase the render context should be not null. </color>");
                return;
            }

            _context = context;

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
            _Add(ref cb);
            //var renderList = _cmdPool[cb.Pass];
            //if (renderList != null)
            //{
            //    renderList.Add(cb);
            //}
            //else
            //{
            //    renderList = new List<Command> { cb };
            //    _cmdPool[cb.Pass] = renderList;
            //}
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
                foreach (var cmd in pair.Value)
                {
                    cmd.Clear();
                }
                pair.Value.Clear();
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
            _Add(ref cmd);

            return cmd;
        }

        public Command GetTemporaryCmd(Common.Pass pass, Common.RenderType type)
        {
            var cmd = new Command(pass, type);
            //_cmdPool.Add(cmd);
            _Add(ref cmd);
            return cmd;
        }

        public Command GetTemporaryCmd(Common.Pass pass, Common.RenderType type, string name)
        {
            var cmd = new Command(pass, type, name);
            _Add(ref cmd);
            return cmd;
        }

        private void _Add(ref Command cmd)
        {
            var renderList = _cmdPool[cmd.Pass];
            if (renderList != null)
            {
                renderList.Add(cmd);
                //Sort.QuickSort(renderList, 0, renderList.Count - 1);
                renderList.Sort((x, y) => x.Type.CompareTo(y.Type));
            }
            else
            {
                renderList = new List<Command>() { cmd };
                renderList.Sort((x, y) => x.Type.CompareTo(y.Type));
                _cmdPool[cmd.Pass] = renderList;
            }
        }
       
        public Command Get(Common.Pass pass, string name)
        {
            var renderList = _cmdPool[pass];
            if (pass >= Common.Pass.Max && (renderList == null || renderList.Count <= 0)) return null;
            return renderList.Find(cmd  => cmd.Name == name);
            //return _cmdPool.Find(cmd => { return cmd.Name.Equals(name); });
        }

        public Command First(Common.Pass pass)
        {
            //return _cmdPool.First();
            var renderList = _cmdPool[pass];
            if (pass < Common.Pass.Max && (renderList == null || renderList.Count <= 0))
            {
                var cmd = new Command(pass);
                Add(cmd);
                return cmd;
            }

            return renderList.First();
        }

        public Command Last(Common.Pass pass)
        {
            var renderList = _cmdPool[pass];
            if (pass < Common.Pass.Max && (renderList == null || renderList.Count <= 0))
            {
                var cmd = new Command(pass);
                Add(cmd);
                return cmd;
            }

            return renderList.Last();
        }

        //public void BeginSample()
        //{
        //    foreach (var cb in _cmdPool)
        //    {
        //        cb.BeginSample();
        //    }
        //}

        //public void BeginSample(string name)
        //{
        //    var cmd = _list.Find(cmd => { return cmd.Name.Equals(name); });
        //    if(cmd != null) cmd.BeginSample();
        //}

        //public void EndSample()
        //{
        //    foreach(var cb in _list)
        //    {
        //        cb.EndSample();
        //    }
        //}

        //public void EndSample(string name)
        //{
        //    var cmd = _list.Find(cmd => { return cmd.Name.Equals(name); });
        //    if(cmd != null) cmd.EndSample();
        //}

        public Command Find(Common.Pass pass, string name)
        {
            //return _list.Find(cmd => { return cmd.Name.Equals(name); });
            var renderList = _cmdPool[pass];
            if (pass >= Common.Pass.Max && (renderList == null || renderList.Count <= 0)) return null;
            return renderList.Find(cmd => cmd.Name == name);
        }

        public bool Exists(Common.Pass pass, string name)
        {
            var renderList = _cmdPool[pass];
            if (pass >= Common.Pass.Max && (renderList == null || renderList.Count <= 0)) return false;
            return renderList.Exists(cmd => cmd.Name == name);
            //return _list.Exists(cmd => { return cmd.Name.Equals(name); });
        }

        private void Tick()
        {
            if (!_isReady) return;

            _restEvent.WaitOne();
            foreach (var cmdList in _cmdPool)
            {
                foreach (var cmd in cmdList.Value)
                {
                    if (cmd.Async)
                    {
                        cmd.ExecuteAsync(_context);
                    }
                    else
                    {
                        cmd.Execute(_context);
                    }
                }
            }
        }
    }

};
