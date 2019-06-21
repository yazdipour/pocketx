﻿namespace CacheManager
{
    internal interface ILru<K, V>
    {
        V Get(K key);
        void Put(K key, V value);
        void InsertAtHead(Node<K, V> node);
        void MoveToHead(Node<K, V> node);
    }
}