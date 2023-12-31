namespace Crowmask.Merging
{
    public static class AsyncEnumerableExtensions
    {
        private class Worker<T>(IAsyncEnumerable<T> enumerable)
        {
            private readonly IAsyncEnumerator<T> _enumerator = enumerable.GetAsyncEnumerator();
            private readonly HashSet<T> _buffer = [];

            public IEnumerable<T> Buffer => _buffer;

            public async Task RefillAsync()
            {
                if (_buffer.Count == 0 && await _enumerator.MoveNextAsync())
                    _buffer.Add(_enumerator.Current);
            }

            public void Remove(T item)
            {
                _buffer.Remove(item);
            }
        }

        public static async IAsyncEnumerable<T> MergeNewest<T>(this IEnumerable<IAsyncEnumerable<T>> asyncEnumerables, Func<T, DateTimeOffset> dateSelector)
        {
            IReadOnlyList<Worker<T>> workers = asyncEnumerables
                .Select(e => new Worker<T>(e))
                .ToArray();

            while (true)
            {
                foreach (var worker in workers)
                {
                    await worker.RefillAsync();
                }

                var sorted = workers
                    .SelectMany(w => w.Buffer)
                    .OrderByDescending(dateSelector);

                if (!sorted.Any())
                    yield break;

                var newest = sorted.First();

                yield return newest;

                foreach (var worker in workers)
                {
                    worker.Remove(newest);
                }
            }
        }
    }
}
