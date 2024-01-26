using System.Collections;
using System.Collections.Generic;

namespace KdTree
{
    public interface IKdTree<Key, Value> : IEnumerable<KdTreeNode<Key, Value>>
    {
        int Count { get; }
        
        bool Add(Key[] point, Value value);
        bool TryFindValueAt(Key[] point, out Value value);
        Value FindValueAt(Key[] point);
        bool TryFindValue(Value value, out Key[] point);
        KdTreeNode<Key, Value>[] RadialSearch(Key[] center, Key radius, int count);
        void RemoveAt(Key[] point);
        void Clear();
        KdTreeNode<Key, Value>[] GetNearestNeighbours(Key[] point, int count = int.MaxValue);
    };
};

