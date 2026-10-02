using System;

namespace ReactiveCollections
{
    /// <summary>
    /// Группировка элементов источника по ключу.
    /// </summary>
    /// <typeparam name="TKey">Тип ключа группы.</typeparam>
    /// <typeparam name="TSource">Тип элемента источника.</typeparam>
    public class GroupNode<TKey, TSource> : KeyedProjectionNode<TKey, TSource, Group<TKey, TSource>>
    {
        /// <param name="keySelector">Селектор ключа группы.</param>
        public GroupNode(
            IObservableList<TSource> source,
            Func<TSource, TKey> keySelector) : base(source, keySelector)
        {
            Initialize();
        }

        protected override Group<TKey, TSource> CreateResult(TKey key)
            => new Group<TKey, TSource>(key);

        protected override void AddItemAt(Group<TKey, TSource> group, int index, TSource item)
            => group.Items.AddAt(index, item);

        protected override void RemoveItemAt(Group<TKey, TSource> group, int index)
            => group.Items.RemoveAt(index);

        protected override void UpdateItem(Group<TKey, TSource> group, int index, TSource item)
            => group.Items.UpdateAt(index);

        protected override void ReplaceItemAt(Group<TKey, TSource> group, int index, TSource newItem)
            => group.Items.ReplaceAt(index, newItem);

        protected override void MoveItem(Group<TKey, TSource> group, int fromIndex, int toIndex)
            => group.Items.Move(fromIndex, toIndex);

        protected override bool IsEmpty(Group<TKey, TSource> group)
            => group.Items.Count == 0;

        protected override TKey GetKey(Group<TKey, TSource> group)
            => group.Key;
    }
}