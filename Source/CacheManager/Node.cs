namespace CacheManager
{
    public class Node<TK, TV>
    {
        public TV Value;
        public TK Key;
        [Newtonsoft.Json.JsonIgnore]
        public Node<TK, TV> Next;
        [Newtonsoft.Json.JsonIgnore]
        public Node<TK, TV> Previous;

        public Node(TK key, TV value)
        {
            Key = key;
            Value = value;
            Next = null;
            Previous = null;
        }
    }
}