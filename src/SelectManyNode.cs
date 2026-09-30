using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// Узел flattening: разворачивает каждый элемент источника в его
    /// собственную <see cref="IObservableList{TResult}"/>, объединяя все
    /// элементы в один плоский список.
    /// </summary>
    /// <typeparam name="TSource">Тип элемента источника.</typeparam>
    /// <typeparam name="TResult">Тип элемента вложенной коллекции.</typeparam>
    /// <remarks>
    /// <para>
    /// Для каждого элемента <c>source</c> вызывается селектор, возвращающий
    /// <see cref="IObservableList{TResult}"/>. Узел подписывается на изменения
    /// каждой вложенной коллекции и отражает их в плоском результате.
    /// </para>
    /// <para>
    /// Одинаковые по <see cref="object.Equals(object)"/> элементы из разных
    /// вложенных коллекций различаются: каждая пара «элемент + его источник»
    /// хранится как отдельная <see cref="Entry"/>. Это позволяет корректно
    /// удалять вклад конкретной коллекции при <c>Remove</c> или <c>Reset</c>.
    /// </para>
    /// <para>
    /// При <c>Update</c> источника селектор вызывается повторно. Если он
    /// вернул **ту же самую** ссылку на внутреннюю коллекцию — ничего не
    /// происходит. Если вернул новую — старая отписывается, её вклад
    /// удаляется, новая подписывается. Сравнение — по <see cref="object.ReferenceEquals"/>.
    /// </para>
    /// </remarks>
    public sealed class SelectManyNode<TSource, TResult> : ObservableNode<TResult>
    {
        /// <summary>
        /// Соответствие одного элемента источника его вложенной коллекции.
        /// </summary>
        /// <remarks>
        /// Хранит ссылку на источник, на вложенную коллекцию, обработчик
        /// события и элементы, добавленные этой коллекцией в плоский результат.
        /// </remarks>
        private sealed class Subscription
        {
            public TSource Source { get; }
            public IObservableList<TResult> Inner { get; }
            public Action<Change<TResult>> Handler { get; }

            public Subscription(
                TSource source,
                IObservableList<TResult> inner,
                SelectManyNode<TSource, TResult> owner)
            {
                Source = source;
                Inner = inner;
                Handler = change => owner.OnInnerChanged(this, change);
            }
        }

        /// <summary>
        /// Одно вхождение в плоском результате: ссылка на <see cref="Subscription"/>
        /// и элемент.
        /// </summary>
        /// <remarks>
        /// Порядок <c>_entries</c> совпадает с порядком элементов в <see cref="ObservableNode{T}.Items"/>.
        /// </remarks>
        private sealed class Entry
        {
            public Subscription Subscription { get; }
            public TResult Item { get; }

            public Entry(Subscription subscription, TResult item)
            {
                Subscription = subscription;
                Item = item;
            }
        }

        private readonly IObservableList<TSource> _source;
        private readonly Func<TSource, IObservableList<TResult>> _selector;

        private readonly List<Subscription> _subscriptions = new();
        private readonly List<Entry> _entries = new();

        /// <summary>
        /// Создаёт узел flattening над указанным источником.
        /// </summary>
        /// <param name="source">Источник. Не может быть <c>null</c>.</param>
        /// <param name="selector">
        /// Селектор вложенной коллекции. Не может быть <c>null</c>.
        /// Не должен возвращать <c>null</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Если <paramref name="source"/> или <paramref name="selector"/> — <c>null</c>.
        /// </exception>
        public SelectManyNode(
            IObservableList<TSource> source,
            Func<TSource, IObservableList<TResult>> selector)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _selector = selector ?? throw new ArgumentNullException(nameof(selector));

            Initialize();
        }

        /// <summary>
        /// Считывает текущее содержимое источника и подписывается на его изменения.
        /// </summary>
        private void Initialize()
        {
            foreach (var sourceItem in _source)
                AddSourceItem(sourceItem);

            _source.Changed += OnSourceChanged;
        }

        /// <summary>
        /// Отписывается от источника и от всех вложенных коллекций.
        /// </summary>
        /// <remarks>
        /// В отличие от других узлов, <see cref="SelectManyNode{TSource, TResult}"/>
        /// держит подписки не только на корневой <c>Source</c>, но и на каждую
        /// вложенную коллекцию. Все они отписываются при <c>Dispose</c>.
        /// Сами вложенные коллекции не удаляются — они могут использоваться
        /// вне этого узла.
        /// </remarks>
        protected override void DisposeCore()
        {
            _source.Changed -= OnSourceChanged;

            foreach (var subscription in _subscriptions)
                subscription.Inner.Changed -= subscription.Handler;

            _subscriptions.Clear();
            _entries.Clear();

            base.DisposeCore();
        }

        // -------------------------------------------------------------------
        // Source
        // -------------------------------------------------------------------

        private void OnSourceChanged(Change<TSource> change)
        {
            if (change.Type == ChangeType.Batch)
            {
                foreach (var inner in change.Changes!)
                    OnSourceChanged(inner);
                return;
            }

            switch (change.Type)
            {
                case ChangeType.Add:
                    AddSourceItem(change.Item);
                    break;

                case ChangeType.Remove:
                    RemoveSourceItem(change.Item);
                    break;

                case ChangeType.Update:
                    UpdateSourceItem(change.Item);
                    break;

                case ChangeType.Replace:
                    if (change.OldItem is null)
                        throw new InvalidOperationException(
                            "Change.Replace was raised without OldItem.");

                    RemoveSourceItem(change.OldItem);
                    AddSourceItem(change.Item);
                    break;

                case ChangeType.Reset:
                    ResetSource();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AddSourceItem(TSource sourceItem)
        {
            var inner = GetInner(sourceItem);
            var subscription = new Subscription(sourceItem, inner, this);

            _subscriptions.Add(subscription);
            inner.Changed += subscription.Handler;

            foreach (var item in inner)
            {
                AddInternal(item);
                _entries.Add(new Entry(subscription, item));
            }
        }

        private void RemoveSourceItem(TSource sourceItem)
        {
            var subscription = FindSubscription(sourceItem);
            if (subscription is null)
                return;

            RemoveSubscription(subscription);
        }

        private void UpdateSourceItem(TSource sourceItem)
        {
            var subscription = FindSubscription(sourceItem);
            if (subscription is null)
                return;

            var newInner = GetInner(sourceItem);

            // Та же самая ссылка — ничего не делаем, подписка уже актуальна.
            if (ReferenceEquals(subscription.Inner, newInner))
                return;

            // Ссылка изменилась — переключаем подписку.
            RemoveSubscription(subscription);
            AddSourceItem(sourceItem);
        }

        private void ResetSource()
        {
            foreach (var subscription in _subscriptions)
                subscription.Inner.Changed -= subscription.Handler;

            _subscriptions.Clear();

            ResetInternal();

            _entries.Clear();

            foreach (var sourceItem in _source)
                AddSourceItem(sourceItem);
        }

        // -------------------------------------------------------------------
        // Inner collection
        // -------------------------------------------------------------------

        private void OnInnerChanged(Subscription subscription, Change<TResult> change)
        {
            if (change.Type == ChangeType.Batch)
            {
                foreach (var inner in change.Changes!)
                    OnInnerChanged(subscription, inner);
                return;
            }

            switch (change.Type)
            {
                case ChangeType.Add:
                    AddInnerItem(subscription, change.Item);
                    break;

                case ChangeType.Remove:
                    RemoveInnerItem(subscription, change.Item);
                    break;

                case ChangeType.Update:
                    UpdateInnerItem(subscription, change.Item);
                    break;

                case ChangeType.Replace:
                    if (change.OldItem is null)
                        throw new InvalidOperationException(
                            "Change.Replace was raised without OldItem.");

                    ReplaceInnerItem(subscription, change.OldItem, change.Item);
                    break;

                case ChangeType.Reset:
                    ResetInnerItems(subscription);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AddInnerItem(Subscription subscription, TResult item)
        {
            AddInternal(item);
            _entries.Add(new Entry(subscription, item));
        }

        private void RemoveInnerItem(Subscription subscription, TResult item)
        {
            int index = FindEntryIndex(subscription, item);
            if (index < 0)
                return;

            RemoveAtInternal(index);
            _entries.RemoveAt(index);
        }

        private void UpdateInnerItem(Subscription subscription, TResult item)
        {
            int index = FindEntryIndex(subscription, item);
            if (index < 0)
                return;

            UpdateAtInternal(index);
        }

        private void ReplaceInnerItem(Subscription subscription, TResult oldItem, TResult newItem)
        {
            int index = FindEntryIndex(subscription, oldItem);
            if (index < 0)
                return;

            ReplaceAtInternal(index, newItem);
            _entries[index] = new Entry(subscription, newItem);
        }

        private void ResetInnerItems(Subscription subscription)
        {
            // Убираем вклад этой subscription из результата.
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_entries[i].Subscription, subscription))
                {
                    RemoveAtInternal(i);
                    _entries.RemoveAt(i);
                }
            }

            // Добавляем актуальное содержимое inner заново.
            foreach (var item in subscription.Inner)
            {
                AddInternal(item);
                _entries.Add(new Entry(subscription, item));
            }
        }

        // -------------------------------------------------------------------
        // Subscription management
        // -------------------------------------------------------------------

        private void RemoveSubscription(Subscription subscription)
        {
            subscription.Inner.Changed -= subscription.Handler;

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_entries[i].Subscription, subscription))
                {
                    RemoveAtInternal(i);
                    _entries.RemoveAt(i);
                }
            }

            _subscriptions.Remove(subscription);
        }

        private Subscription? FindSubscription(TSource sourceItem)
        {
            var comparer = EqualityComparer<TSource>.Default;

            foreach (var subscription in _subscriptions)
            {
                if (comparer.Equals(subscription.Source, sourceItem))
                    return subscription;
            }

            return null;
        }

        private int FindEntryIndex(Subscription subscription, TResult item)
        {
            var comparer = EqualityComparer<TResult>.Default;

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];

                if (!ReferenceEquals(entry.Subscription, subscription))
                    continue;

                if (comparer.Equals(entry.Item, item))
                    return i;
            }

            return -1;
        }

        private IObservableList<TResult> GetInner(TSource sourceItem)
        {
            return _selector(sourceItem)
                ?? throw new InvalidOperationException(
                    "SelectMany selector returned null.");
        }
    }
}