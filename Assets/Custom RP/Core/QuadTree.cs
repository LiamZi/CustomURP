using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using System.Threading;
using UnityEngine.Rendering;

namespace Core
{
    public class QuadTree
    {
        public enum LocalPos
        {
            LeftDown,
            LeftUp,
            RightDown,
            RightUp
        };

        public QuadTree LeftDown { get; private set; }
        public QuadTree LeftUp { get; private set; }
        public QuadTree RightDown { get; private set; }
        public QuadTree RightUp { get; private set; }

        public int _lodLevel;
        public int2 _localPos;
        public int2 _renderingLocalPos;
        private double _distOffset;
        
        
    }
}