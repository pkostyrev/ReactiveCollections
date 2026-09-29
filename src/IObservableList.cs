using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    public interface IObservableList<T> : IReadOnlyList<T>
    {
        event Action<Change<T>>? Changed;
    }
}