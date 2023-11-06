using System.Collections.Generic;

namespace Core
{
    public interface IUnsafeIterator<T> : IEnumerator<T>, IEnumerable<T> where T : struct
    {

    };
}