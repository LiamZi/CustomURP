using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace KdTree
{
    public enum AddDuplicateBehavior
    {
        Skip,
        Error,
        Update
    };

    public class DuplicateNodeError : Exception
    {
        public DuplicateNodeError() : base("Cannot Add Node With Duplicate Coordinates")
        {
            
        }
    }

    [Serializable]
    public class KdTree<Key, Value> : IKdTree<Key, Value>
    {
        int _dimensions;
        ITypeMath<Key> _typeMath = null;
        KdTreeNode<Key, Value> _root = null;
        
        
        public KdTree(int dimensions, ITypeMath<Key> typeMath)
        {
            this._dimensions = dimensions;
            this._typeMath = typeMath;
            Count = 0;
        }
        
        public KdTree(int _dimensions, ITypeMath<Key> _typeMath, AddDuplicateBehavior addDuplicateBehavior)
            : this(_dimensions, _typeMath)
        {
            AddDuplicateBehavior = addDuplicateBehavior;
        }
        
        public bool Add(Key[] point, Value value)
        {
            var nodeToAdd = new KdTreeNode<Key, Value>(point, value);

            if (_root == null)
            {
                _root = new KdTreeNode<Key, Value>(point, value);
            }
            else
            {
                int dimension = -1;
                KdTreeNode<Key, Value> parent = _root;

                do
                {
                    // Increment the dimension we're searching in
                    dimension = (dimension + 1) % _dimensions;

                    // Does the node we're adding have the same hyperpoint as this node?
                    if (_typeMath.AreEqual(point, parent._point))
                    {
                        switch (AddDuplicateBehavior)
                        {
                            case AddDuplicateBehavior.Skip:
                                return false;

                            case AddDuplicateBehavior.Error:
                                throw new DuplicateNodeError();

                            case AddDuplicateBehavior.Update:
                                parent._value = value;
                                return true;

                            default:
                                // Should never happen
                                throw new Exception("Unexpected AddDuplicateBehavior");
                        }
                    }

                    // Which side does this node sit under in relation to it's parent at this level?
                    int compare = _typeMath.Compare(point[dimension], parent._point[dimension]);

                    if (parent[compare] == null)
                    {
                        parent[compare] = nodeToAdd;
                        break;
                    }
                    else
                    {
                        parent = parent[compare];
                    }
                }
                while (true);
            }

            Count++;
            return true;
        }
        
        private void ReaddChildNodes(KdTreeNode<Key, Value> removedNode)
        {
            if (removedNode.IsLeaf)
                return;

            // The folllowing code might seem a little redundant but we're using 
            // 2 queues so we can add the child nodes back in, in (more or less) 
            // the same order they were added in the first place
            var nodesToReadd = new Queue<KdTreeNode<Key, Value>>();

            var nodesToReaddQueue = new Queue<KdTreeNode<Key, Value>>();

            if (removedNode._leftChild != null)
                nodesToReaddQueue.Enqueue(removedNode._leftChild);

            if (removedNode._rightChild != null)
                nodesToReaddQueue.Enqueue(removedNode._rightChild);

            while (nodesToReaddQueue.Count > 0)
            {
                var nodeToReadd = nodesToReaddQueue.Dequeue();

                nodesToReadd.Enqueue(nodeToReadd);

                for (int side = -1; side <= 1; side += 2)
                {
                    if (nodeToReadd[side] != null)
                    {
                        nodesToReaddQueue.Enqueue(nodeToReadd[side]);

                        nodeToReadd[side] = null;
                    }
                }
            }

            while (nodesToReadd.Count > 0)
            {
                var nodeToReadd = nodesToReadd.Dequeue();

                Count--;
                Add(nodeToReadd._point, nodeToReadd._value);
            }
        }
        
        public void RemoveAt(Key[] point)
        {
            // Is tree empty?
            if (_root == null)
                return;

            KdTreeNode<Key, Value> node;

            if (_typeMath.AreEqual(point, _root._point))
            {
                node = _root;
                _root = null;
                Count--;
                ReaddChildNodes(node);
                return;
            }

            node = _root;

            int dimension = -1;
            do
            {
                dimension = (dimension + 1) % _dimensions;

                int compare = _typeMath.Compare(point[dimension], node._point[dimension]);

                if (node[compare] == null)
                    // Can't find node
                    return;

                if (_typeMath.AreEqual(point, node[compare]._point))
                {
                    var nodeToRemove = node[compare];
                    node[compare] = null;
                    Count--;

                    ReaddChildNodes(nodeToRemove);
                }
                else
                    node = node[compare];
            }
            while (node != null);
        }
        
        public KdTreeNode<Key, Value>[] GetNearestNeighbours(Key[] point, int count)
        {
            if (count > Count)
                count = Count;

            if (count < 0)
            {
                throw new ArgumentException("Number of neighbors cannot be negative");
            }

            if (count == 0)
                return new KdTreeNode<Key, Value>[0];

            var neighbours = new KdTreeNode<Key, Value>[count];

            var nearestNeighbours = new NearestNeighbourList<KdTreeNode<Key, Value>, Key>(count, _typeMath);

            var rect = HyperRect<Key>.Infinite(_dimensions, _typeMath);

            AddNearestNeighbours(_root, point, rect, 0, nearestNeighbours, _typeMath.MaxValue);

            count = nearestNeighbours.Count;

            var neighbourArray = new KdTreeNode<Key, Value>[count];

            for (var index = 0; index < count; index++)
                neighbourArray[count - index - 1] = nearestNeighbours.RemoveFurtherest();

            return neighbourArray;
        }
        
        /*
		 * 1. Search for the target
		 * 
		 *   1.1 Start by splitting the specified hyper rect
		 *       on the specified node's point along the current
		 *       dimension so that we end up with 2 sub hyper rects
		 *       (current dimension = depth % _dimensions)
		 *   
		 *	 1.2 Check what sub rectangle the the target point resides in
		 *	     under the current dimension
		 *	     
		 *   1.3 Set that rect to the nearer rect and also the corresponding 
		 *       child node to the nearest rect and node and the other rect 
		 *       and child node to the further rect and child node (for use later)
		 *       
		 *   1.4 Travel into the nearer rect and node by calling function
		 *       recursively with nearer rect and node and incrementing 
		 *       the depth
		 * 
		 * 2. Add leaf to list of nearest neighbours
		 * 
		 * 3. Walk back up tree and at each level:
		 * 
		 *    3.1 Add node to nearest neighbours if
		 *        we haven't filled our nearest neighbour
		 *        list yet or if it has a distance to target less
		 *        than any of the distances in our current nearest 
		 *        neighbours.
		 *        
		 *    3.2 If there is any point in the further rectangle that is closer to
		 *        the target than our furtherest nearest neighbour then travel into
		 *        that rect and node
		 * 
		 *  That's it, when it finally finishes traversing the branches 
		 *  it needs to we'll have our list!
		 */

		private void AddNearestNeighbours(
			KdTreeNode<Key, Value> node,
			Key[] target,
			HyperRect<Key> rect,
			int depth,
			NearestNeighbourList<KdTreeNode<Key, Value>, Key> nearestNeighbours,
			Key maxSearchRadiusSquared)
		{
			if (node == null)
				return;

			// Work out the current dimension
			int dimension = depth % _dimensions;

			// Split our hyper-rect into 2 sub rects along the current 
			// node's point on the current dimension
			var leftRect = rect.Clone();
			leftRect.MaxPoint[dimension] = node._point[dimension];

			var rightRect = rect.Clone();
			rightRect.MinPoint[dimension] = node._point[dimension];

			// Which side does the target reside in?
			int compare = _typeMath.Compare(target[dimension], node._point[dimension]);

			var nearerRect = compare <= 0 ? leftRect : rightRect;
			var furtherRect = compare <= 0 ? rightRect : leftRect;

			var nearerNode = compare <= 0 ? node._leftChild : node._rightChild;
			var furtherNode = compare <= 0 ? node._rightChild : node._leftChild;

			// Let's walk down into the nearer branch
			if (nearerNode != null)
			{
				AddNearestNeighbours(
					nearerNode,
					target,
					nearerRect,
					depth + 1,
					nearestNeighbours,
					maxSearchRadiusSquared);
			}

			Key distanceSquaredToTarget;

			// Walk down into the further branch but only if our capacity hasn't been reached 
			// OR if there's a region in the further rect that's closer to the target than our
			// current furtherest nearest neighbour
			Key[] closestPointInFurtherRect = furtherRect.GetClosestPoint(target, _typeMath);
			distanceSquaredToTarget = _typeMath.DistanceSquaredBetweenPoints(closestPointInFurtherRect, target);

			if (_typeMath.Compare(distanceSquaredToTarget, maxSearchRadiusSquared) <= 0)
			{
				if (nearestNeighbours.IsCapacityReached)
				{
					if (_typeMath.Compare(distanceSquaredToTarget, nearestNeighbours.GetFurtherestDistance()) < 0)
						AddNearestNeighbours(
							furtherNode,
							target,
							furtherRect,
							depth + 1,
							nearestNeighbours,
							maxSearchRadiusSquared);
				}
				else
				{
					AddNearestNeighbours(
						furtherNode,
						target,
						furtherRect,
						depth + 1,
						nearestNeighbours,
						maxSearchRadiusSquared);
				}
			}

			// Try to add the current node to our nearest neighbours list
			distanceSquaredToTarget = _typeMath.DistanceSquaredBetweenPoints(node._point, target);

			if (_typeMath.Compare(distanceSquaredToTarget, maxSearchRadiusSquared) <= 0)
				nearestNeighbours.Add(node, distanceSquaredToTarget);
		}
        
		public KdTreeNode<Key, Value>[] RadialSearch(Key[] center, Key radius)
		{
			var nearestNeighbours = new NearestNeighbourList<KdTreeNode<Key, Value>, Key>(_typeMath);
			return RadialSearch(center, radius, nearestNeighbours);
		}
		
		public KdTreeNode<Key, Value>[] RadialSearch(Key[] center, Key radius, int count)
		{
			var nearestNeighbours = new NearestNeighbourList<KdTreeNode<Key, Value>, Key>(count, _typeMath);
			return RadialSearch(center, radius, nearestNeighbours);
		}
		
		private KdTreeNode<Key, Value>[] RadialSearch(Key[] center, Key radius, NearestNeighbourList<KdTreeNode<Key, Value>, Key> nearestNeighbours)
		{
			AddNearestNeighbours(
				_root,
				center,
				HyperRect<Key>.Infinite(_dimensions, _typeMath),
				0,
				nearestNeighbours,
				_typeMath.Multiply(radius, radius));

			var count = nearestNeighbours.Count;

			var neighbourArray = new KdTreeNode<Key, Value>[count];

			for (var index = 0; index < count; index++)
				neighbourArray[count - index - 1] = nearestNeighbours.RemoveFurtherest();

			return neighbourArray;
		}
		
		public bool TryFindValueAt(Key[] point, out Value value)
		{
			var parent = _root;
			int dimension = -1;
			do
			{
				if (parent == null)
				{
					value = default(Value);
					return false;
				}
				else if (_typeMath.AreEqual(point, parent._point))
				{
					value = parent._value;
					return true;
				}

				// Keep searching
				dimension = (dimension + 1) % _dimensions;
				int compare = _typeMath.Compare(point[dimension], parent._point[dimension]);
				parent = parent[compare];
			}
			while (true);
		}
		
		public Value FindValueAt(Key[] point)
		{
			if (TryFindValueAt(point, out Value value))
				return value;
			else
				return default(Value);
		}

		public bool TryFindValue(Value value, out Key[] point)
		{
			if (_root == null)
			{
				point = null;
				return false;
			}

			// First-in, First-out list of nodes to search
			var nodesToSearch = new Queue<KdTreeNode<Key, Value>>();

			nodesToSearch.Enqueue(_root);

			while (nodesToSearch.Count > 0)
			{
				var nodeToSearch = nodesToSearch.Dequeue();

				if (nodeToSearch._value.Equals(value))
				{
					point = nodeToSearch._point;
					return true;
				}
				else
				{
					for (int side = -1; side <= 1; side += 2)
					{
						var childNode = nodeToSearch[side];

						if (childNode != null)
							nodesToSearch.Enqueue(childNode);
					}
				}
			}

			point = null;
			return false;
		}

		public Key[] FindValue(Value value)
		{
			if (TryFindValue(value, out Key[] point))
				return point;
			else
				return null;
		}

		private void AddNodeToStringBuilder(KdTreeNode<Key, Value> node, StringBuilder sb, int depth)
		{
			sb.AppendLine(node.ToString());

			for (var side = -1; side <= 1; side += 2)
			{
				for (var index = 0; index <= depth; index++)
					sb.Append("\t");

				sb.Append(side == -1 ? "L " : "R ");

				if (node[side] == null)
					sb.AppendLine("");
				else
					AddNodeToStringBuilder(node[side], sb, depth + 1);
			}
		}

		public override string ToString()
		{
			if (_root == null)
				return "";

			var sb = new StringBuilder();
			AddNodeToStringBuilder(_root, sb, 0);
			return sb.ToString();
		}

		private void AddNodesToList(KdTreeNode<Key, Value> node, List<KdTreeNode<Key, Value>> nodes)
		{
			if (node == null)
				return;

			nodes.Add(node);

			for (var side = -1; side <= 1; side += 2)
			{
				if (node[side] != null)
				{
					AddNodesToList(node[side], nodes);
					node[side] = null;
				}
			}
		}

		private void SortNodesArray(KdTreeNode<Key, Value>[] nodes, int byDimension, int fromIndex, int toIndex)
		{
			for (var index = fromIndex + 1; index <= toIndex; index++)
			{
				var newIndex = index;

				while (true)
				{
					var a = nodes[newIndex - 1];
					var b = nodes[newIndex];
					if (_typeMath.Compare(b._point[byDimension], a._point[byDimension]) < 0)
					{
						nodes[newIndex - 1] = b;
						nodes[newIndex] = a;
					}
					else
						break;
				}
			}
		}

		private void AddNodesBalanced(KdTreeNode<Key, Value>[] nodes, int byDimension, int fromIndex, int toIndex)
		{
			if (fromIndex == toIndex)
			{
				Add(nodes[fromIndex]._point, nodes[fromIndex]._value);
				nodes[fromIndex] = null;
				return;
			}

			// Sort the array from the fromIndex to the toIndex
			SortNodesArray(nodes, byDimension, fromIndex, toIndex);

			// Find the splitting point
			int midIndex = fromIndex + (int)System.Math.Round((toIndex + 1 - fromIndex) / 2f) - 1;

			// Add the splitting point
			Add(nodes[midIndex]._point, nodes[midIndex]._value);
			nodes[midIndex] = null;

			// Recurse
			int nextDimension = (byDimension + 1) % _dimensions;

			if (fromIndex < midIndex)
				AddNodesBalanced(nodes, nextDimension, fromIndex, midIndex - 1);

			if (toIndex > midIndex)
				AddNodesBalanced(nodes, nextDimension, midIndex + 1, toIndex);
		}

		public void Balance()
		{
			var nodeList = new List<KdTreeNode<Key, Value>>();
			AddNodesToList(_root, nodeList);

			Clear();

			AddNodesBalanced(nodeList.ToArray(), 0, 0, nodeList.Count - 1);
		}

		private void RemoveChildNodes(KdTreeNode<Key, Value> node)
		{
			for (var side = -1; side <= 1; side += 2)
			{
				if (node[side] != null)
				{
					RemoveChildNodes(node[side]);
					node[side] = null;
				}
			}
		}

		public void Clear()
		{
			if (_root != null)
				RemoveChildNodes(_root);
		}

		public void SaveToFile(string filename)
		{
			BinaryFormatter formatter = new BinaryFormatter();
			using (FileStream stream = File.Create(filename))
			{
				formatter.Serialize(stream, this);
				stream.Flush();
			}
		}

		public static KdTree<Key, Value> LoadFromFile(string filename)
		{
			BinaryFormatter formatter = new BinaryFormatter();
			using (FileStream stream = File.Open(filename, FileMode.Open))
			{
				return (KdTree<Key, Value>)formatter.Deserialize(stream);
			}

		}

		public IEnumerator<KdTreeNode<Key, Value>> GetEnumerator()
		{
			var left = new Stack<KdTreeNode<Key, Value>>();
			var right = new Stack<KdTreeNode<Key, Value>>();

			void addLeft(KdTreeNode<Key, Value> node)
			{
				if (node._leftChild != null)
				{
					left.Push(node._leftChild);
				}
			}

			void addRight(KdTreeNode<Key, Value> node)
			{
				if (node._rightChild != null)
				{
					right.Push(node._rightChild);
				}
			}

			if (_root != null)
			{
				yield return _root;

				addLeft(_root);
				addRight(_root);

				while (true)
				{
					if (left.Any())
					{
						var item = left.Pop();

						addLeft(item);
						addRight(item);

						yield return item;
					}
					else if (right.Any())
					{
						var item = right.Pop();

						addLeft(item);
						addRight(item);

						yield return item;
					}
					else
					{
						break;
					}
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		
        public int Count { get; private set; }
        public AddDuplicateBehavior AddDuplicateBehavior { get; private set; }

    };
};
