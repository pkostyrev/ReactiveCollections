using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// Базовый тип для всех изменений наблюдаемой коллекции.
    /// </summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    /// <remarks>
    /// Изменения, адресующие конкретный occurrence, несут координату в
    /// коллекции-источнике. Это позволяет однозначно определить экземпляр
    /// даже при дубликатах по <c>Equals</c>.
    /// </remarks>
    public abstract class Change<T>
    {
    }

    /// <summary>Добавление элемента.</summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    public sealed class AddChange<T> : Change<T>
    {
        /// <summary>Добавленный элемент.</summary>
        public T Item { get; }

        /// <summary>Позиция, на которую встал элемент.</summary>
        public int Index { get; }

        internal AddChange(T item, int index)
        {
            Item = item;
            Index = index;
        }

        public override string ToString() => $"Add: {Item} @{Index}";
    }

    /// <summary>Удаление элемента.</summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    public sealed class RemoveChange<T> : Change<T>
    {
        /// <summary>Удалённый элемент.</summary>
        public T Item { get; }

        /// <summary>Позиция элемента до удаления.</summary>
        public int Index { get; }

        internal RemoveChange(T item, int index)
        {
            Item = item;
            Index = index;
        }

        public override string ToString() => $"Remove: {Item} @{Index}";
    }

    /// <summary>
    /// Обновление элемента без смены позиции (mutable-модель).
    /// </summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    public sealed class UpdateChange<T> : Change<T>
    {
        /// <summary>Текущее значение элемента после обновления.</summary>
        public T Item { get; }

        /// <summary>Позиция элемента в источнике.</summary>
        public int Index { get; }

        internal UpdateChange(T item, int index)
        {
            Item = item;
            Index = index;
        }

        public override string ToString() => $"Update: {Item} @{Index}";
    }

    /// <summary>Замена одного элемента другим в той же позиции.</summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    public sealed class ReplaceChange<T> : Change<T>
    {
        /// <summary>Заменяемый элемент.</summary>
        public T OldItem { get; }

        /// <summary>Новый элемент.</summary>
        public T NewItem { get; }

        /// <summary>Позиция замены.</summary>
        public int Index { get; }

        internal ReplaceChange(T oldItem, T newItem, int index)
        {
            OldItem = oldItem;
            NewItem = newItem;
            Index = index;
        }

        public override string ToString()
            => $"Replace: {OldItem} -> {NewItem} @{Index}";
    }

    /// <summary>Перемещение элемента внутри коллекции.</summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    public sealed class MoveChange<T> : Change<T>
    {
        /// <summary>Перемещённый элемент.</summary>
        public T Item { get; }

        /// <summary>Исходная позиция.</summary>
        public int FromIndex { get; }

        /// <summary>Конечная позиция.</summary>
        public int ToIndex { get; }

        internal MoveChange(T item, int fromIndex, int toIndex)
        {
            Item = item;
            FromIndex = fromIndex;
            ToIndex = toIndex;
        }

        public override string ToString()
            => $"Move: {Item} {FromIndex} -> {ToIndex}";
    }

    /// <summary>Полный сброс коллекции.</summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    public sealed class ResetChange<T> : Change<T>
    {
        internal ResetChange()
        {
        }

        public override string ToString() => "Reset";
    }

    /// <summary>
    /// Группа изменений, выполненных как одна операция.
    /// </summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    /// <remarks>
    /// Индексы вложенных изменений относятся к состоянию коллекции на
    /// момент соответствующей операции, а не к финальному состоянию.
    /// </remarks>
    public sealed class BatchChange<T> : Change<T>
    {
        /// <summary>Вложенные изменения.</summary>
        public IReadOnlyList<Change<T>> Changes { get; }

        internal BatchChange(IReadOnlyList<Change<T>> changes)
        {
            Changes = changes;
        }

        public override string ToString() => $"Batch ({Changes.Count})";
    }
}