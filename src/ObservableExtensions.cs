using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// Fluent-фасад для построения цепочек реактивных узлов.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Методы — точки входа в библиотеку. Каждый возвращает конкретный узел,
    /// который сам реализует <see cref="IObservableList{T}"/> и может быть
    /// источником для следующего вызова.
    /// </para>
    /// <para>
    /// Пример цепочки:
    /// <code>
    /// var result = players
    ///     .Filter(p => p.Level >= 10)
    ///     .Select(p => new PlayerView(p), (p, v) => v.Refresh(p))
    ///     .GroupBy(v => v.TeamId);
    /// </code>
    /// </para>
    /// </remarks>
    public static class ObservableExtensions
    {
        /// <summary>
        /// Создаёт фильтр: оставляет только элементы, удовлетворяющие предикату.
        /// </summary>
        /// <typeparam name="T">Тип элемента коллекции.</typeparam>
        /// <param name="source">Источник изменений. Не может быть <c>null</c>.</param>
        /// <param name="predicate">
        /// Предикат фильтрации. Не может быть <c>null</c>.
        /// Вызывается синхронно, должен быть чистым.
        /// </param>
        /// <returns>Новый <see cref="FilterNode{T}"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Если <paramref name="source"/> или <paramref name="predicate"/> — <c>null</c>.
        /// </exception>
        public static FilterNode<T> Filter<T>(this IObservableList<T> source, Func<T, bool> predicate)
        {
            return new FilterNode<T>(source, predicate);
        }

        /// <summary>
        /// Создаёт проекцию: превращает каждый элемент источника в результат.
        /// </summary>
        /// <typeparam name="TSource">Тип элемента источника.</typeparam>
        /// <typeparam name="TResult">Тип элемента результата.</typeparam>
        /// <param name="source">Источник изменений. Не может быть <c>null</c>.</param>
        /// <param name="factory">
        /// Фабрика нового результата. Не может быть <c>null</c>.
        /// Вызывается один раз на каждый новый элемент источника.
        /// </param>
        /// <param name="updater">
        /// Обновлятор существующего результата. Не может быть <c>null</c>.
        /// Вызывается при <see cref="ChangeType.Update"/> источника.
        /// Если обновление не требуется, передавай пустой делегат: <c>(_, _) =&gt; { }</c>.
        /// </param>
        /// <returns>Новый <see cref="SelectNode{TSource, TResult}"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Если любой из аргументов — <c>null</c>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Результат сохраняет ссылочную идентичность при <see cref="ChangeType.Update"/>:
        /// <paramref name="updater"/> мутирует существующий экземпляр, а не
        /// пересоздаёт его. Это позволяет UI и следующим узлам цепочки держать
        /// ссылку на результат.
        /// </para>
        /// <para>
        /// Для immutable <typeparamref name="TResult"/> (records, structs)
        /// <paramref name="updater"/> не сможет изменить результат — используй
        /// <see cref="ChangeType.Replace"/>-семантику в вызывающем коде.
        /// </para>
        /// </remarks>
        public static SelectNode<TSource, TResult> Select<TSource, TResult>(
            this IObservableList<TSource> source,
            Func<TSource, TResult> factory,
            Action<TSource, TResult> updater)
        {
            return new SelectNode<TSource, TResult>(source, factory, updater);
        }

        /// <summary>
        /// Создаёт группировку: разбивает элементы источника на
        /// <see cref="Group{TKey, T}"/> по ключу.
        /// </summary>
        /// <typeparam name="TKey">Тип ключа группы.</typeparam>
        /// <typeparam name="T">Тип элемента источника.</typeparam>
        /// <param name="source">Источник изменений. Не может быть <c>null</c>.</param>
        /// <param name="selector">
        /// Функция вычисления ключа группы по элементу. Не может быть <c>null</c>.
        /// </param>
        /// <returns>Новый <see cref="GroupNode{TKey, T}"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Если <paramref name="source"/> или <paramref name="selector"/> — <c>null</c>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Каждая группа — самостоятельный реактивный источник. Подписка
        /// на <c>group.Items.Changed</c> даёт доступ к изменениям внутри
        /// группы без событий от узла группировки.
        /// </para>
        /// <para>
        /// Компаратор для <typeparamref name="TKey"/> не параметризован —
        /// используется <see cref="EqualityComparer{TKey}.Default"/>. Если
        /// нужен нестандартный компаратор, используй конструктор
        /// <see cref="GroupNode{TKey, TSource}"/> напрямую.
        /// </para>
        /// <para>
        /// Селектор не должен возвращать <c>null</c> для ссылочного
        /// <typeparamref name="TKey"/>: <c>Dictionary</c>, лежащий в основе,
        /// не принимает <c>null</c>-ключи.
        /// </para>
        /// </remarks>
        public static GroupNode<TKey, T> GroupBy<TKey, T>(
            this IObservableList<T> source,
            Func<T, TKey> selector)
        {
            return new GroupNode<TKey, T>(source, selector);
        }

        /// <summary>
        /// Объединяет два источника в один живой список.
        /// </summary>
        /// <typeparam name="T">Тип элемента коллекций.</typeparam>
        /// <param name="first">Первый источник. Не может быть <c>null</c>.</param>
        /// <param name="second">Второй источник. Не может быть <c>null</c>.</param>
        /// <returns>Новый <see cref="MergeNode{T}"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Если любой из источников — <c>null</c>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Семантика — <b>Concat</b>, не <b>Union</b>: дубликаты сохраняются.
        /// <c>[A] + [A] = [A, A]</c>.
        /// </para>
        /// <para>
        /// Оба источника равноправны — это «метод на первом», но не «владелец».
        /// Порядок элементов в результате: сначала все элементы
        /// <paramref name="first"/>, затем — <paramref name="second"/>.
        /// </para>
        /// </remarks>
        public static MergeNode<T> Merge<T>(
            this IObservableList<T> first,
            IObservableList<T> second)
        {
            return new MergeNode<T>(first, second);
        }

        /// <summary>
        /// Разворачивает каждый элемент источника в его вложенную коллекцию,
        /// объединяя элементы всех коллекций в один плоский живой список.
        /// </summary>
        /// <typeparam name="TSource">Тип элемента источника.</typeparam>
        /// <typeparam name="TResult">Тип элемента вложенной коллекции.</typeparam>
        /// <param name="source">Источник. Не может быть <c>null</c>.</param>
        /// <param name="selector">
        /// Селектор вложенной коллекции. Не может быть <c>null</c>.
        /// Не должен возвращать <c>null</c>.
        /// </param>
        /// <returns>Новый <see cref="SelectManyNode{TSource, TResult}"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Если <paramref name="source"/> или <paramref name="selector"/> — <c>null</c>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Каждый элемент источника разворачивается один раз, при добавлении.
        /// При <c>Update</c> источника селектор вызывается повторно: если он
        /// вернул ту же самую ссылку на внутреннюю коллекцию — ничего не
        /// происходит; если новую — подписка переключается.
        /// </para>
        /// <para>
        /// Одинаковые по <c>Equals</c> элементы из разных вложенных коллекций
        /// различаются — учёт ведётся по паре «элемент + его источник».
        /// </para>
        /// </remarks>
        public static SelectManyNode<TSource, TResult> SelectMany<TSource, TResult>(
            this IObservableList<TSource> source,
            Func<TSource, IObservableList<TResult>> selector)
        {
            return new SelectManyNode<TSource, TResult>(source, selector);
        }
    }
}