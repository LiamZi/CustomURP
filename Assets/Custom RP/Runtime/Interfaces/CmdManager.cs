using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Rendering;


namespace CustomURP
{
    public class CmdManager
    {
        private static CmdManager _sharedInstance = null;

        private List<Command> _list = new List<Command>();

        private CmdManager()
        {
            
        }

        public static CmdManager Singleton
        {
            get { return _sharedInstance ??= new CmdManager(); }
        }

        public void Add(Command cb)
        {
            _list.Add(cb);
        }

        public void Remove(Command cb)
        {
            _list.Remove(cb);
        }

        public void Clear()
        {
            foreach (var cmd in _list)
            {
                cmd.Destroy();
            }
            
            _list.Clear();
        }

        public List<Command> GetAll()
        {
            return _list;
        }

        public bool Has(Command cb)
        {
            return _list.Contains(cb);
        }

        public Command GetTemporaryCmd(string name = "")
        {
            var cmd = new Command(name);
            _list.Add(cmd);
            return cmd;
        }

        public Command Get(string name)
        {
            return _list.Find(cmd => { return cmd.Name.Equals(name); });
        }

        public Command First()
        {
            return _list.First();
        }

        public Command Last()
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
