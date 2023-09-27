using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;
using UnityEngine.SocialPlatforms;

namespace Core
{
    public sealed class LoadingThread
    {
        private struct Command
        {
            public object _src;
            public Action<object> _func;
        };

        public static LoadingCommandQueue CommandQueue { get; private set; }
        public static LoadingThread _Instance { get; private set; }
        private static List<Command> _Commands = new List<Command>();
        private List<Command> _localCommands =new List<Command>();
        private AutoResetEvent _restEvent;
        private Thread _thread;
        private bool _isRunning;

        public LoadingThread GetInstance()
        {
            if(_Instance == null)
            {
                _Instance = new LoadingThread();
                _Instance.Init();
            }

            return _Instance;
        }

        private void Init()
        {
            _isRunning = true;
            _restEvent = new AutoResetEvent(false);
            _thread = new Thread(Run);
            _thread.Start();
            CommandQueue = new LoadingCommandQueue(); 
        }

        public static void AddCommand(Action<object> func, object src)
        {
            lock(_Commands)
            {
                _Commands.Add(new Command{ _src = src, _func = func });
                _Instance._restEvent.Set();
            }
        }

        private void Run()
        {
            while(_isRunning)
            {
                _restEvent.WaitOne();
                lock(_Commands)
                {
                    _localCommands.AddRange(_Commands);
                    _Commands.Clear();
                }

                foreach(var c in _localCommands)
                {
                    c._func(c._src);
                }
                _localCommands.Clear();
            }
        }

        public void Update()
        {
            lock(CommandQueue)
            {
                CommandQueue.Run();
            }
        }

        public void Dispose()
        {
            _Instance = null;
            _isRunning = false;
            _restEvent.Set();
            _thread.Join();
            _restEvent.Dispose();
        }
    };
};