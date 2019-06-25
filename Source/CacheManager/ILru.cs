namespace CacheManager
{
    internal interface ILru<K, V>
    {
        V TryGetValue(K key);
        void Put(K key, V value);
        void InsertAtHead(Node<K, V> node);
        void MoveToHead(Node<K, V> node);
    }
}