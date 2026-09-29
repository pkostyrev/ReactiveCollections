using System;

namespace ReactiveCollections
{
    /// <summary>
    /// Корневой изменяемый список — единственный узел, который пользователь
    /// создаёт вручную как источник данных.
    /// </summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    /// <remarks>
    /// <para>
    /// Все остальные узлы (<see cref="FilterNode{T}"/>,
    /// <see cref="SelectNode{TSource, TResult}"/>, <see cref="GroupNode{TKey, TSource}"/>,
    /// <see cref="MergeNode{T}"/>) строятся поверх <see cref="IObservableList{T}"/>
    /// и не могут быть созданы пользователем напрямую как источники.
    /// </para>
    /// <para>
    /// Коллекция не является потокобезопасной — см. README, раздел 4.7.
    /// </para>
    /// </remarks>
    public class ObservableList<T> : ObservableNode<T>
    {
        /// <summary>
        /// Добавляет элемент в конец коллекции и публикует <see cref="ChangeType.Add"/>.
        /// </summary>
        /// <returns>Всегда <c>true</c>.</returns>
        public bool Add(T item)
        {
            return AddInternal(item);
        }

        /// <summary>
        /// Удаляет первый элемент, равный <paramref name="item"/> по
        /// <see cref="EqualityComparer{T}.Default"/>, и публикует <see cref="ChangeType.Remove"/>.
        /// </summary>
        /// <returns><c>true</c>, если элемент найден и удалён; иначе <c>false</c>.</returns>
        /// <remarks>
        /// При наличии дубликатов по <see cref="object.Equals(object)"/> неопределённо,
        /// какой именно экземпляр уйдёт — см. README, раздел 4.3.
        /// </remarks>
        public bool Remove(T item)
        {
            return RemoveInternal(item);
        }

        /// <summary>
        /// Заменяет первый элемент, равный <paramref name="oldItem"/>, на <paramref name="newItem"/>.
        /// </summary>
        /// <returns><c>true</c>, если <paramref name="oldItem"/> найден; иначе <c>false</c>.</returns>
        /// <remarks>
        /// Публикует <see cref="ChangeType.Replace"/>. Используйте, когда в коллекции
        /// появляется <b>другой</b> объект вместо существующего. Для изменения полей
        /// того же объекта — <see cref="Update"/>.
        /// </remarks>
        public bool Replace(T oldItem, T newItem)
        {
            return ReplaceInternal(oldItem, newItem);
        }

        /// <summary>
        /// Публикует <see cref="ChangeType.Update"/> для элемента, если он присутствует в коллекции.
        /// </summary>
        /// <returns><c>true</c>, если элемент найден; иначе <c>false</c>.</returns>
        /// <remarks>
        /// <para>
        /// Ничего не мутирует — предполагается, что объект уже изменён пользователем
        /// (mutable-модель). Метод служит только для оповещения подписчиков.
        /// </para>
        /// <para>
        /// Старое значение не сохраняется — подписчики видят уже изменённый объект.
        /// Для immutable <typeparamref name="T"/> используйте <see cref="Replace"/> —
        /// см. README, раздел 4.2.
        /// </para>
        /// </remarks>
        public bool Update(T item)
        {
            return UpdateInternal(item);
        }

        /// <summary>
        /// Полностью очищает коллекцию и публикует <see cref="ChangeType.Reset"/>.
        /// </summary>
        /// <returns><c>true</c>, если коллекция была непустой; иначе <c>false</c>.</returns>
        /// <remarks>
        /// <para>
        /// Событие не райзится, если коллекция уже пуста — <c>Reset</c> на пустой
        /// коллекции неотличим от «ничего не произошло».
        /// </para>
        /// <para>
        /// В текущей реализации <c>Reset</c> означает полную очистку, а не
        /// WPF-семантику «содержимое могло измениться целиком» — см. README,
        /// раздел 4.3.
        /// </para>
        /// </remarks>
        public bool Reset()
        {
            return ResetInternal();
        }

        /// <summary>
        /// Источник не «умирает» от <see cref="Dispose"/> — это no-op.
        /// </summary>
        /// <remarks>
        /// <see cref="ObservableList{T}"/> — корневой узел, он не подписан ни на
        /// что и не удерживает другие узлы. <c>Dispose</c> на нём не имеет эффекта,
        /// но реализован для единообразия с <see cref="IObservableList{T}"/>.
        /// Чтобы удалить цепочку проекций, вызови <c>Dispose</c> на каждом узле
        /// цепочки — сам источник при этом останется живым.
        /// </remarks>
        protected override void DisposeCore()
        {
            // Намеренно пусто — источник ничего не удерживает.
            base.DisposeCore();
        }
    }
}