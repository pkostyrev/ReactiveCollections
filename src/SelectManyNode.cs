using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// Разворачивает каждый элемент источника в его вложенную коллекцию,
    /// объединяя элементы всех коллекций в один плоский живой список.
    /// </summary>
    /// <typeparam name="TSource">Тип элемента источника.</typeparam>
    /// <typeparam name="TResult">Тип элемента вложенной коллекции.</typeparam>
    /// <remarks>
    /// <para>
    /// Модель — Concat вложенных коллекций в порядке источника:
    /// <c>flat = inner[0] + inner[1] + ... + inner[N-1]</c>. Каждая вложенная
    /// коллекция образует непрерывный блок в результате.
    /// </para>
    /// <para>
    /// Требование к селектору: каждый элемент источника должен возвращать
    /// <b>собственную</b> внутреннюю коллекцию. Shared inner-коллекции
    /// не входят в поддерживаемый контракт текущей реализации: одна
    /// физическая коллекция получит несколько подписок, и одно её изменение
    /// будет обработано несколько раз. Требование не проверяется в рантайме —
    /// ответственность на вызывающем.
    /// </para>
    /// </remarks>
    public sealed class SelectManyNode<TSource, TResult> : ObservableNode<TResult>
    {
        /// <summary>
        /// Один источник в плоском результате: связывает элемент источника
        /// с его вложенной коллекцией и текущим размером блока.
        /// </summary>
        private sealed class Subscription
        {
            public TSource Source;
            public IObservableList<TResult> Inner;
            public Action<Change<TResult>> Handler;
            public int Count;
        }

        private readonly IObservableList<TSource> _source;
        private readonly Func<TSource, IObservableList<TResult>> _selector;

        /// <summary>
        /// Подписки в порядке элементов источника. Индекс подписки
        /// соответствует индексу occurrence в <c>_source</c>.
        /// </summary>
        private readonly List<Subscription> _subscriptions = new();

        /// <param name="selector">
        /// Селектор вложенной коллекции для каждого элемента источника.
        /// Не должен возвращать <c>null</c>.
        /// </param>
        public SelectManyNode(
            IObservableList<TSource> source,
            Func<TSource, IObservableList<TResult>> selector)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _selector = selector ?? throw new ArgumentNullException(nameof(selector));

            Initialize();
        }

        private void Initialize()
        {
            foreach (var sourceItem in _source)
                InsertSourceSubscription(_subscriptions.Count, sourceItem);

            _source.Changed += OnSourceChanged;
        }

        protected override void DisposeCore()
        {
            _source.Changed -= OnSourceChanged;

            foreach (var sub in _subscriptions)
                sub.Inner.Changed -= sub.Handler;

            _subscriptions.Clear();
            base.DisposeCore();
        }

        // -------------------------------------------------------------------
        // Source changes
        // -------------------------------------------------------------------

        private void OnSourceChanged(Change<TSource> change)
        {
            switch (change)
            {
                case BatchChange<TSource> batch:
                    foreach (var inner in batch.Changes)
                        OnSourceChanged(inner);
                    break;

                case AddChange<TSource> add:
                    InsertSourceSubscription(add.Index, add.Item);
                    break;

                case RemoveChange<TSource> remove:
                    RemoveSourceSubscription(remove.Index);
                    break;

                case UpdateChange<TSource> update:
                    UpdateSourceSubscription(update.Index, update.Item);
                    break;

                case ReplaceChange<TSource> replace:
                    ReplaceSourceSubscription(replace.Index, replace.NewItem);
                    break;

                case MoveChange<TSource> move:
                    MoveSourceSubscription(move.FromIndex, move.ToIndex);
                    break;

                case ResetChange<TSource>:
                    ResetSource();
                    break;

                default:
                    throw new NotSupportedException(
                        $"Unsupported change: {change.GetType().Name}");
            }
        }

        private void InsertSourceSubscription(int sourceIndex, TSource item)
        {
            var inner = GetInner(item);
            int startPos = BlockStartFor(sourceIndex);

            Subscription sub = null!;
            sub = new Subscription
            {
                Source = item,
                Inner = inner,
                Handler = c => OnInnerChanged(sub, c),
                Count = 0
            };

            _subscriptions.Insert(sourceIndex, sub);
            inner.Changed += sub.Handler;

            for (int i = 0; i < inner.Count; i++)
            {
                AddAtInternal(startPos + i, inner[i]);
                sub.Count++;
            }
        }

        private void RemoveSourceSubscription(int sourceIndex)
        {
            var sub = _subscriptions[sourceIndex];
            int startPos = BlockStartFor(sourceIndex);

            sub.Inner.Changed -= sub.Handler;

            for (int i = 0; i < sub.Count; i++)
                RemoveAtInternal(startPos);

            _subscriptions.RemoveAt(sourceIndex);
        }

        /// <summary>
        /// При той же ссылке на inner — только обновляет Source в subscription,
        /// блок не пересоздаётся.
        /// </summary>
        private void UpdateSourceSubscription(int sourceIndex, TSource item)
        {
            var sub = _subscriptions[sourceIndex];
            var newInner = GetInner(item);

            if (ReferenceEquals(sub.Inner, newInner))
            {
                sub.Source = item;
                return;
            }

            ReplaceBlock(sourceIndex, item, newInner);
        }

        /// <summary>
        /// При той же ссылке на inner — только обновляет Source, содержимое
        /// flat не меняется.
        /// </summary>
        private void ReplaceSourceSubscription(int sourceIndex, TSource newItem)
        {
            var oldSub = _subscriptions[sourceIndex];
            var newInner = GetInner(newItem);

            if (ReferenceEquals(oldSub.Inner, newInner))
            {
                oldSub.Source = newItem;
                return;
            }

            ReplaceBlock(sourceIndex, newItem, newInner);
        }

        private void ReplaceBlock(int sourceIndex, TSource item, IObservableList<TResult> newInner)
        {
            var oldSub = _subscriptions[sourceIndex];
            int startPos = BlockStartFor(sourceIndex);

            oldSub.Inner.Changed -= oldSub.Handler;

            for (int i = 0; i < oldSub.Count; i++)
                RemoveAtInternal(startPos);

            _subscriptions.RemoveAt(sourceIndex);

            Subscription newSub = null!;
            newSub = new Subscription
            {
                Source = item,
                Inner = newInner,
                Handler = c => OnInnerChanged(newSub, c),
                Count = 0
            };

            _subscriptions.Insert(sourceIndex, newSub);
            newInner.Changed += newSub.Handler;

            for (int i = 0; i < newInner.Count; i++)
            {
                AddAtInternal(startPos + i, newInner[i]);
                newSub.Count++;
            }
        }

        /// <summary>
        /// Переставляет целый блок inner-коллекции внутри flat. Наружу
        /// публикуется последовательностью удалений и добавлений, а не одним
        /// <see cref="MoveChange{TResult}"/>.
        /// </summary>
        private void MoveSourceSubscription(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex)
                return;

            var sub = _subscriptions[fromIndex];
            int blockSize = sub.Count;
            int oldStart = BlockStartFor(fromIndex);

            _subscriptions.RemoveAt(fromIndex);
            _subscriptions.Insert(toIndex, sub);

            int newStart = BlockStartFor(toIndex);

            if (oldStart == newStart)
                return;

            var blockItems = new TResult[blockSize];
            for (int i = 0; i < blockSize; i++)
                blockItems[i] = Items[oldStart + i];

            for (int i = 0; i < blockSize; i++)
                RemoveAtInternal(oldStart);

            for (int i = 0; i < blockSize; i++)
                AddAtInternal(newStart + i, blockItems[i]);
        }

        /// <summary>
        /// Полный rebuild: отписывается от всех inner, перечитывает source,
        /// наполняет flat напрямую, райзит один <see cref="ResetChange{TResult}"/>.
        /// </summary>
        private void ResetSource()
        {
            ThrowIfDisposed();

            foreach (var sub in _subscriptions)
                sub.Inner.Changed -= sub.Handler;
            _subscriptions.Clear();

            bool hadItems = Items.Count > 0;
            Items.Clear();

            foreach (var sourceItem in _source)
            {
                var inner = GetInner(sourceItem);

                Subscription sub = null!;
                sub = new Subscription
                {
                    Source = sourceItem,
                    Inner = inner,
                    Handler = c => OnInnerChanged(sub, c),
                    Count = 0
                };

                _subscriptions.Add(sub);
                inner.Changed += sub.Handler;

                for (int i = 0; i < inner.Count; i++)
                {
                    Items.Add(inner[i]);
                    sub.Count++;
                }
            }

            if (hadItems || Items.Count > 0)
                Raise(new ResetChange<TResult>());
        }

        // -------------------------------------------------------------------
        // Inner changes
        // -------------------------------------------------------------------

        private void OnInnerChanged(Subscription sub, Change<TResult> change)
        {
            int sourceIndex = _subscriptions.IndexOf(sub);
            if (sourceIndex < 0)
                return;

            int startPos = BlockStartFor(sourceIndex);

            switch (change)
            {
                case BatchChange<TResult> batch:
                    foreach (var inner in batch.Changes)
                        OnInnerChanged(sub, inner);
                    break;

                case AddChange<TResult> add:
                    AddAtInternal(startPos + add.Index, add.Item);
                    sub.Count++;
                    break;

                case RemoveChange<TResult> remove:
                    RemoveAtInternal(startPos + remove.Index);
                    sub.Count--;
                    break;

                case UpdateChange<TResult> update:
                    UpdateAtInternal(startPos + update.Index);
                    break;

                case ReplaceChange<TResult> replace:
                    ReplaceAtInternal(startPos + replace.Index, replace.NewItem);
                    break;

                case MoveChange<TResult> move:
                    MoveInternal(startPos + move.FromIndex, startPos + move.ToIndex);
                    break;

                case ResetChange<TResult>:
                    for (int i = 0; i < sub.Count; i++)
                        RemoveAtInternal(startPos);
                    sub.Count = 0;

                    for (int i = 0; i < sub.Inner.Count; i++)
                    {
                        AddAtInternal(startPos + i, sub.Inner[i]);
                        sub.Count++;
                    }
                    break;

                default:
                    throw new NotSupportedException(
                        $"Unsupported change: {change.GetType().Name}");
            }
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        /// <summary>
        /// Возвращает начало блока элемента с индексом <paramref name="sourceIndex"/>
        /// в flat как сумму размеров всех предыдущих блоков.
        /// </summary>
        private int BlockStartFor(int sourceIndex)
        {
            int start = 0;
            for (int i = 0; i < sourceIndex; i++)
                start += _subscriptions[i].Count;
            return start;
        }

        /// <summary>
        /// Возвращает внутреннюю коллекцию для элемента источника.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Если селектор вернул <c>null</c>.
        /// </exception>
        private IObservableList<TResult> GetInner(TSource item)
        {
            return _selector(item)
                ?? throw new InvalidOperationException(
                    "SelectMany selector returned null.");
        }
    }
}