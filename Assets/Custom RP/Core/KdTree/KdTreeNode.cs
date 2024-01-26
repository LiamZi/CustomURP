using System;
using System.Text;

namespace KdTree
{
    [Serializable]
    public class KdTreeNode<Key, Value>
    {
        public Key[] _point;
        public Value _value = default(Value);
        internal KdTreeNode<Key, Value> _leftChild = null;
        internal KdTreeNode<Key, Value> _rightChild = null;


        public KdTreeNode()
        {

        }

        public KdTreeNode(Key[] point, Value value)
        {
            _point = point;
            _value = value;
        }

        internal KdTreeNode<Key, Value> this[int compare]
        {
            get
            {
                if (compare <= 0)
                {
                    return _leftChild;
                }
                else
                {
                    return _rightChild;
                }
            }

            set
            {
                if (compare <= 0)
                {
                    _leftChild = value;
                }
                else
                {
                    _rightChild = value;
                }
            }
        }

        public bool IsLeaf
        {
            get
            {
                return (_leftChild == null) && (_rightChild == null);
            }
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            for (var dimension = 0; dimension < _point.Length; dimension++)
            {
                builder.Append(_point[dimension].ToString() + "\t");
            }

            if (_value == null)
            {
                builder.Append("null");
            }
            else
            {
                builder.Append(_value.ToString());
            }
            return builder.ToString();
        }

    };
};
