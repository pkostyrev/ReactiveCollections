using System;

namespace ReactiveCollections
{
    public static class ObservableExtensions
    {
        public static FilterNode<T> Filter<T>(this IObservableList<T> source, Func<T, bool> predicate)
        {
            return new FilterNode<T>(source, predicate);
        }

        public static SelectNode<TSource, TResult> Select<TSource, TResult>(this IObservableList<TSource> source, Func<TSource, TResult> factory, Action<TSource, TResult> updater)
        {
            return new SelectNode<TSource, TResult>(source, factory, updater);
        }

        public static GroupNode<TKey, T> GroupBy<TKey, T>(this IObservableList<T> source, Func<T, TKey> selector)
        {
            return new GroupNode<TKey, T>(source, selector);
        }

        public static MergeNode<T> Merge<T>(this IObservableList<T> first, IObservableList<T> second)
        {
            return new MergeNode<T>(first, second);
        }
    }
}
