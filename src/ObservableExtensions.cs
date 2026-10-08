using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// Fluent-фасад для построения цепочек реактивных узлов.
    /// </summary>
    /// <remarks>
    /// Каждый метод возвращает конкретный узел, который сам реализует
    /// <see cref="IObservableList{T}"/> и может быть источником для
    /// следующего вызова.
    /// </remarks>
    public static class ObservableExtensions
    {
        /// <summary>
        /// Создаёт фильтр: оставляет только элементы, удовлетворяющие предикату.
        /// </summary>
        /// <param name="predicate">Предикат фильтрации.</param>
        public static FilterNode<T> ObserveWhere<T>(
            this IObservableList<T> source,
            Func<T, bool> predicate)
            => new FilterNode<T>(source, predicate);

        /// <summary>
        /// Создаёт проекцию: превращает каждый элемент источника в результат.
        /// </summary>
        /// <param name="factory">Фабрика нового результата.</param>
        /// <param name="updater">
        /// Обновлятор существующего результата. Для mutable-модели. Если
        /// обновление не требуется — передавай <c>(_, _) =&gt; { }</c>.
        /// </param>
        /// <remarks>
        /// При <c>Update</c> существующий результат сохраняется и обновляется
        /// через <paramref name="updater"/>, без создания нового экземпляра.
        /// </remarks>
        public static SelectNode<TSource, TResult> ObserveSelect<TSource, TResult>(
            this IObservableList<TSource> source,
            Func<TSource, TResult> factory,
            Action<TSource, TResult> updater)
            => new SelectNode<TSource, TResult>(source, factory, updater);

        /// <summary>
        /// Разбивает элементы источника на группы по ключу.
        /// </summary>
        /// <param name="selector">Селектор ключа группы.</param>
        /// <remarks>
        /// Каждая группа — самостоятельный реактивный источник. Селектор
        /// должен возвращать допустимый ключ. Для ссылочного
        /// <typeparamref name="TKey"/> значение <c>null</c> не поддерживается.
        /// </remarks>
        public static GroupNode<TKey, T> ObserveGroupBy<TKey, T>(
            this IObservableList<T> source,
            Func<T, TKey> selector)
            => new GroupNode<TKey, T>(source, selector);

        /// <summary>
        /// Объединяет два источника в один живой список по семантике Concat.
        /// </summary>
        /// <remarks>
        /// Порядок: сначала все элементы <paramref name="first"/>, затем
        /// все элементы <paramref name="second"/>. Дубликаты сохраняются.
        /// </remarks>
        public static MergeNode<T> ObserveMerge<T>(
            this IObservableList<T> first,
            IObservableList<T> second)
            => new MergeNode<T>(first, second);

        /// <summary>
        /// Разворачивает каждый элемент источника в его вложенную коллекцию,
        /// объединяя элементы всех коллекций в один плоский список.
        /// </summary>
        /// <param name="selector">
        /// Селектор вложенной коллекции. Не должен возвращать <c>null</c>.
        /// </param>
        /// <remarks>
        /// При <c>Update</c> источника селектор вызывается повторно: если он
        /// вернул ту же ссылку на вложенную коллекцию — ничего не происходит;
        /// если новую — подписка переключается.
        /// </remarks>
        public static SelectManyNode<TSource, TResult> ObserveSelectMany<TSource, TResult>(
            this IObservableList<TSource> source,
            Func<TSource, IObservableList<TResult>> selector)
            => new SelectManyNode<TSource, TResult>(source, selector);

        /// <summary>
        /// Сортирует элементы источника по ключу.
        /// </summary>
        /// <param name="selector">Селектор ключа сортировки.</param>
        /// <remarks>Порядок элементов с равными ключами не определён.</remarks>
        public static OrderByNode<TSource, TKey> ObserveOrderBy<TSource, TKey>(
            this IObservableList<TSource> source,
            Func<TSource, TKey> selector)
            => new OrderByNode<TSource, TKey>(source, selector);

        /// <inheritdoc cref="ObserveOrderBy{TSource, TKey}(IObservableList{TSource}, Func{TSource, TKey})"/>
        public static OrderByNode<TSource, TKey> ObserveOrderBy<TSource, TKey>(
            this IObservableList<TSource> source,
            Func<TSource, TKey> selector,
            IComparer<TKey>? comparer)
            => new OrderByNode<TSource, TKey>(source, selector, comparer);

        /// <summary>
        /// Сортирует элементы источника по ключу в обратном порядке.
        /// </summary>
        /// <param name="selector">Селектор ключа сортировки.</param>
        /// <remarks>Порядок элементов с равными ключами не определён.</remarks>
        public static OrderByNode<TSource, TKey> ObserveOrderByDescending<TSource, TKey>(
            this IObservableList<TSource> source,
            Func<TSource, TKey> selector)
            => new OrderByNode<TSource, TKey>(source, selector, descending: true);

        /// <inheritdoc cref="ObserveOrderByDescending{TSource, TKey}(IObservableList{TSource}, Func{TSource, TKey})"/>
        public static OrderByNode<TSource, TKey> ObserveOrderByDescending<TSource, TKey>(
            this IObservableList<TSource> source,
            Func<TSource, TKey> selector,
            IComparer<TKey>? comparer)
            => new OrderByNode<TSource, TKey>(source, selector, comparer, descending: true);
    }
}