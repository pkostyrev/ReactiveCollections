using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    public class GroupNode<TKey, TSource> : KeyedProjectionNode<TKey, TSource, Group<TKey, TSource>>
    {
        public GroupNode(IObservableList<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TSource>? comparer = null) 
            : base(source, keySelector, comparer)
        {
            Initialize();
        }

        protected override Group<TKey, TSource> CreateResult(TKey key) => new Group<TKey, TSource>(key);

        protected override void AddItem(Group<TKey, TSource> group, TSource item) => group.Items.Add(item);

        protected override void RemoveItem(Group<TKey, TSource> group, TSource item) => group.Items.Remove(item);

        protected override void UpdateItem(Group<TKey, TSource> group, TSource item) => group.Items.Update(item);

        protected override bool IsEmpty(Group<TKey, TSource> group) => group.Items.Count == 0;

        protected override TKey GetKey(Group<TKey, TSource> group) => group.Key;
    }
}
