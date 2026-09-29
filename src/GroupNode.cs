using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// Конкретная реализация <see cref="KeyedProjectionNode{TKey, TSource, TResult}"/>
    /// для группировки элементов источника по ключу.
    /// </summary>
    /// <typeparam name="TKey">Тип ключа группы.</typeparam>
    /// <typeparam name="TSource">Тип элемента источника.</typeparam>
    /// <remarks>
    /// <para>
    /// Результат — <see cref="Group{TKey, T}"/>: обёртка над
    /// <see cref="ObservableList{T}"/> с фиксированным ключом. Каждая группа
    /// является самостоятельным реактивным источником.
    /// </para>
    /// <para>
    /// Пример использования:
    /// <code>
    /// var groups = players.GroupBy(p => p.TeamId);
    /// // groups[0].Key     — TeamId
    /// // groups[0].Items   — живой ObservableList&lt;Player&gt; этой команды
    /// </code>
    /// </para>
    /// </remarks>
    public class GroupNode<TKey, TSource> : KeyedProjectionNode<TKey, TSource, Group<TKey, TSource>>
    {
        /// <summary>
        /// Создаёт узел группировки над указанным источником.
        /// </summary>
        /// <param name="source">Источник изменений. Не может быть <c>null</c>.</param>
        /// <param name="keySelector">
        /// Функция вычисления ключа группы по элементу. Не может быть <c>null</c>.
        /// </param>
        /// <param name="comparer">
        /// Компаратор элементов источника. Если <c>null</c> —
        /// используется <see cref="EqualityComparer{T}.Default"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Если <paramref name="source"/> или <paramref name="keySelector"/> — <c>null</c>.
        /// </exception>
        /// <remarks>
        /// Вызывает <see cref="ProjectionNode{TSource, TResult}.Initialize"/>
        /// в конце конструктора — после инициализации полей базового класса.
        /// </remarks>
        public GroupNode(
            IObservableList<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TSource>? comparer = null)
            : base(source, keySelector, comparer)
        {
            Initialize();
        }

        /// <summary>
        /// Создаёт новую пустую группу с указанным ключом.
        /// </summary>
        protected override Group<TKey, TSource> CreateResult(TKey key)
            => new Group<TKey, TSource>(key);

        /// <summary>
        /// Добавляет элемент в содержимое группы через
        /// <see cref="Group{TKey, T}.Items"/>.
        /// </summary>
        /// <remarks>
        /// Делегирует в публичный <see cref="ObservableList{T}.Add"/>,
        /// который райзит <see cref="ChangeType.Add"/> в <c>group.Items.Changed</c>.
        /// </remarks>
        protected override void AddItem(Group<TKey, TSource> group, TSource item)
            => group.Items.Add(item);

        /// <summary>
        /// Удаляет элемент из содержимого группы.
        /// </summary>
        /// <remarks>
        /// Делегирует в <see cref="ObservableList{T}.Remove"/>, который удаляет
        /// первый равный по <see cref="EqualityComparer{T}.Default"/>.
        /// </remarks>
        protected override void RemoveItem(Group<TKey, TSource> group, TSource item)
            => group.Items.Remove(item);

        /// <summary>
        /// Уведомляет группу об изменении элемента, оставшегося в ней.
        /// </summary>
        /// <remarks>
        /// Делегирует в <see cref="ObservableList{T}.Update"/>, который проверяет
        /// наличие элемента в группе и райзит <see cref="ChangeType.Update"/>.
        /// Элемент <b>не пересоздаётся</b> — подписчики видят ту же ссылку,
        /// но с изменёнными полями (mutable-модель).
        /// </remarks>
        protected override void UpdateItem(Group<TKey, TSource> group, TSource item)
            => group.Items.Update(item);

        /// <summary>
        /// Проверяет, пуста ли группа.
        /// </summary>
        protected override bool IsEmpty(Group<TKey, TSource> group)
            => group.Items.Count == 0;

        /// <summary>
        /// Возвращает ключ группы.
        /// </summary>
        /// <remarks>
        /// Возвращает <see cref="Group{TKey, T}.Key"/> — фиксированное значение,
        /// не зависящее от содержимого группы. Это соответствует требованию
        /// <see cref="KeyedProjectionNode{TKey, TSource, TResult}.GetKey"/>
        /// о стабильности ключа.
        /// </remarks>
        protected override TKey GetKey(Group<TKey, TSource> group)
            => group.Key;
    }
}