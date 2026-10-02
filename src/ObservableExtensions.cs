using System;

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
        public static FilterNode<T> Filter<T>(
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
        public static SelectNode<TSource, TResult> Select<TSource, TResult>(
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
        public static GroupNode<TKey, T> GroupBy<TKey, T>(
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
        public static MergeNode<T> Merge<T>(
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
        public static SelectManyNode<TSource, TResult> SelectMany<TSource, TResult>(
            this IObservableList<TSource> source,
            Func<TSource, IObservableList<TResult>> selector)
            => new SelectManyNode<TSource, TResult>(source, selector);
    }
}