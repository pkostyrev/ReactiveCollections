using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// Тип изменения коллекции.
    /// </summary>
    public enum ChangeType
    {
        /// <summary>Элемент добавлен в коллекцию.</summary>
        Add,

        /// <summary>Элемент удалён из коллекции.</summary>
        Remove,

        /// <summary>Один экземпляр заменён другим.</summary>
        Replace,

        /// <summary>Элемент изменился, но ссылка на него осталась той же.</summary>
        Update,

        /// <summary>Содержимое коллекции могло измениться целиком.</summary>
        Reset,

        /// <summary>
        /// Группа изменений, накопленных в режиме батчинга.
        /// Используется при <c>BeginUpdate</c>/<c>EndUpdate</c> и <c>Batch()</c>.
        /// </summary>
        /// <remarks>
        /// <see cref="Change{T}.Changes"/> содержит вложенные изменения.
        /// Батч не содержит других батчей (вложенные запрещены).
        /// </remarks>
        Batch
    }

    /// <summary>
    /// Иммутабельный DTO одного перехода состояния коллекции.
    /// </summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    /// <remarks>
    /// <para>
    /// Экземпляр создаётся только через статические фабрики
    /// (<see cref="Add"/>, <see cref="Remove"/>, <see cref="Update"/>,
    /// <see cref="Replace"/>, <see cref="Reset"/>, <see cref="Batch(IReadOnlyList{Change{T}})"/>).
    /// </para>
    /// <para>
    /// <see cref="OldItem"/> заполняется только для <see cref="ChangeType.Replace"/>.
    /// <see cref="Changes"/> — только для <see cref="ChangeType.Batch"/>.
    /// </para>
    /// </remarks>
    public sealed class Change<T>
    {
        /// <summary>Тип изменения.</summary>
        public ChangeType Type { get; }

        /// <summary>Актуальное значение.</summary>
        /// <remarks>
        /// Не используется для <see cref="ChangeType.Reset"/> и <see cref="ChangeType.Batch"/>.
        /// </remarks>
        public T Item { get; }

        /// <summary>Предыдущее значение. Заполняется только для <see cref="ChangeType.Replace"/>.</summary>
        public T? OldItem { get; }

        /// <summary>
        /// Вложенные изменения. Заполняется только для <see cref="ChangeType.Batch"/>.
        /// Список не пустой и не содержит других батчей.
        /// </summary>
        public IReadOnlyList<Change<T>>? Changes { get; }

        private Change(
            ChangeType type,
            T item,
            T? oldItem = default,
            IReadOnlyList<Change<T>>? changes = null)
        {
            Type = type;
            Item = item;
            OldItem = oldItem;
            Changes = changes;
        }

        /// <summary>Создаёт изменение типа <see cref="ChangeType.Add"/>.</summary>
        public static Change<T> Add(T item) => new Change<T>(ChangeType.Add, item);

        /// <summary>Создаёт изменение типа <see cref="ChangeType.Remove"/>.</summary>
        public static Change<T> Remove(T item) => new Change<T>(ChangeType.Remove, item);

        /// <summary>Создаёт изменение типа <see cref="ChangeType.Update"/>.</summary>
        public static Change<T> Update(T item, T? oldItem = default)
            => new Change<T>(ChangeType.Update, item, oldItem);

        /// <summary>Создаёт изменение типа <see cref="ChangeType.Replace"/>.</summary>
        public static Change<T> Replace(T oldItem, T newItem)
            => new Change<T>(ChangeType.Replace, newItem, oldItem);

        /// <summary>Создаёт изменение типа <see cref="ChangeType.Reset"/>.</summary>
        public static Change<T> Reset() => new Change<T>(ChangeType.Reset, default!);

        /// <summary>
        /// Создаёт изменение типа <see cref="ChangeType.Batch"/>, объединяющее
        /// список вложенных изменений.
        /// </summary>
        /// <param name="changes">
        /// Список вложенных изменений. Не может быть <c>null</c>, пустым
        /// или содержать другие батчи.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Если <paramref name="changes"/> — <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Если <paramref name="changes"/> пуст или содержит вложенный батч.
        /// </exception>
        public static Change<T> Batch(IReadOnlyList<Change<T>> changes)
        {
            if (changes is null)
                throw new ArgumentNullException(nameof(changes));

            if (changes.Count == 0)
                throw new ArgumentException(
                    "Batch must contain at least one change.", nameof(changes));

            for (int i = 0; i < changes.Count; i++)
            {
                if (changes[i].Type == ChangeType.Batch)
                    throw new ArgumentException(
                        "Nested batches are not allowed.", nameof(changes));
            }

            return new Change<T>(ChangeType.Batch, default!, default, changes);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Type switch
            {
                ChangeType.Replace => $"{Type}: {OldItem} -> {Item}",

                ChangeType.Reset => $"{Type}",

                ChangeType.Batch => $"{Type} ({Changes!.Count})",

                _ => $"{Type}: {Item}"
            };
        }
    }
}