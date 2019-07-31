using System.Collections;
using System.Collections.Generic;

namespace CacheManager
{
    internal class LruCache<K, V> : ILru<K, V>
    {
        private readonly Dictionary<K, Node<K, V>> _memory;
        private readonly int _capacity;
        private int _currentMemoryInUse;
        private Node<K, V> _head;
        private Node<K, V> _tail;

        public LruCache(int capacity, IDictionary oldMemory = null)
        {
            _capacity = capacity;
            _memory = new Dictionary<K, Node<K, V>>();
            if (oldMemory != null)
                foreach (var k in oldMemory.Keys)
                    Put((K)k, ((Node<K, V>) oldMemory[k]).Value);
            _currentMemoryInUse = (int)_memory?.Count;
        }

        public Dictionary<K, Node<K, V>> GetAll() => _memory;

        public V TryGetValue(K key)
        {
            if (!_memory.ContainsKey(key)) return default;
            var result = _memory[key];
            //Move node to head
            MoveToHead(result);
            return result.Value;
        }

        public void Put(K key, V value)
        {
            Node<K, V> node;
            if (_memory.ContainsKey(key))
            {
                //Parameter key exists in hash-map
                node = _memory[key];
                node.Value = value;
                MoveToHead(node);
                return;
            }

            node = new Node<K, V>(key, value)
            {
                Key = key,
                Value = value
            };

            //Parameter key is new and there is capacity
            if (_currentMemoryInUse < _capacity)
            {
                if (_head == null)
                    _head = _tail = node;
                else
                    InsertAtHead(node);
                _memory[key] = node;
                _currentMemoryInUse++;
            }
            else //Parameter key is new and there is no capacity.
            {
                var keyToRemove = _tail.Key;

                if (_head != _tail)
                {
                    _tail.Previous.Next = null;
                    _tail = _tail.Previous;
                }
                _memory.Remove(keyToRemove);
                _currentMemoryInUse--;
                InsertAtHead(node);
                _memory[key] = node;
                _currentMemoryInUse++;
            }
        }

        public void InsertAtHead(Node<K, V> node)
        {
            node.Previous = null;
            node.Next = _head;
            _head.Previous = node;
            _head = node;
        }

        public void MoveToHead(Node<K, V> node)
        {
            if (node.Previous == null) return;
            if (node.Next == null)
                _tail = node.Previous;
            else
                node.Next.Previous = node.Previous;
            node.Previous.Next = node.Next;
            InsertAtHead(node);
        }

        public string Log()
        {
            var headReference = _head;
            var items = new List<string>();
            while (headReference != null)
            {
                items.Add($"[{headReference.Key}: {headReference.Value}]");
                headReference = headReference.Next;
            }
            return string.Join(",", items);
        }
    }
}