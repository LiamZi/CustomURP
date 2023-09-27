using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class LoadingCommandQueue
    {
        public class Node
        {
            public IEnumerator _command;
            public Node _last;
            public Node _next;

            public Node(IEnumerator command)
            {
                this._command = command;
                _next = null;
                _last = null; 
            }
        };

        public Node _start = null;
        public Node _last = null;
        public List<Node> _pool;

        public LoadingCommandQueue()
        {
            _pool = new List<Node>(20);
            for(int i = 0; i < 20; ++i)
            {
                _pool.Add(new Node(default));
            }
        }
        public void Run()
        {
            if(_start == null) return;

            if(!_start._command.MoveNext())
            {
                Node last = _start;
                last._last = null;
                last._next = null;
                if(last != null)
                {
                    _pool.Add(last);
                }
                _start = _start._next;
                if(_start != null)
                {
                    _start._last = null;
                }
            }
        }

        public void Queue(IEnumerator command)
        {
            Node node;
            if(_pool.Count > 0)
            {
                node = _pool[_pool.Count - 1];
                _pool.RemoveAt(_pool.Count - 1);
                node._command = command;
            }
            else
            {
                node = new Node(command);
            }

            if(_start == null || _last == null)
            {
                _start = node;
                _last = node;
            }
            else
            {
                _last._next = node;
                node._last = _last;
                _last = node;
            }
        }

    };
};  