using System.Collections.Concurrent;

namespace GameServer.Core
{
    public class Pool<T>
    {
        private readonly ConcurrentStack<int> _indices;
        private readonly int _capacity;

        public Pool(int capacity)
        {
            _capacity = capacity;
            _indices = new ConcurrentStack<int>();

            for (int i = capacity - 1; i >= 0; i--)
            {
                _indices.Push(i + 1);
            }
        }

        public int Get() => _indices.TryPop(out int index) ? index : 0;

        public void Return(int index)
        {
            if (index > 0 && index <= _capacity)
            {
                _indices.Push(index);
            }
        }
    }
}