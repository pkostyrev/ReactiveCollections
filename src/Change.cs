using System;

namespace ReactiveCollections
{
    /// <summary>
    /// Тип изменения коллекции.
    /// </summary>
    public enum ChangeType
    {
        /// <summary>
        /// Элемент добавлен в коллекцию.
        /// </summary>
        Add,

        /// <summary>
        /// Элемент удалён из коллекции.
        /// </summary>
        Remove,

        /// <summary>
        /// Один экземпляр заменён другим.
        /// </summary>
        /// <remarks>
        /// Используется, когда в коллекции появляется <b>другой</b> объект
        /// вместо существующего. Для изменения полей того же объекта —
        /// см. <see cref="Update"/>.
        /// </remarks>
        Replace,

        /// <summary>
        /// Элемент изменился, но ссылка на него осталась той же.
        /// </summary>
        /// <remarks>
        /// Используется для mutable-моделей: пользователь меняет поля объекта
        /// и уведомляет коллекцию. Старое значение не сохраняется — подписчик
        /// видит уже изменённый объект.
        /// </remarks>
        Update,

        /// <summary>
        /// Содержимое коллекции могло измениться целиком.
        /// </summary>
        /// <remarks>
        /// В текущей реализации возникает при <c>Reset()</c> корневой коллекции
        /// (полная очистка). Отдельного механизма «перечитать источник» пока нет.
        /// </remarks>
        Reset
    }

    /// <summary>
    /// Иммутабельный DTO одного перехода состояния коллекции.
    /// </summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    /// <remarks>
    /// <para>
    /// Экземпляр создаётся только через статические фабрики
    /// (<see cref="Add"/>, <see cref="Remove"/>, <see cref="Update"/>,
    /// <see cref="Replace"/>, <see cref="Reset"/>). Это гарантирует корректную
    /// комбинацию <see cref="Type"/>, <see cref="Item"/> и <see cref="OldItem"/>.
    /// </para>
    /// <para>
    /// <see cref="OldItem"/> заполняется только для <see cref="ChangeType.Replace"/>.
    /// Для остальных типов он содержит <c>default(T)</c>: <c>null</c> для ссылочных
    /// типов, <c>0</c>/<c>false</c> — для значимых. Отличить «не заполнено» от
    /// «заполнено значением по умолчанию» невозможно — см. README, раздел 4.1.
    /// </para>
    /// </remarks>
    public sealed class Change<T>
    {
        /// <summary>
        /// Тип изменения.
        /// </summary>
        public ChangeType Type { get; }

        /// <summary>
        /// Актуальное значение.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item><see cref="ChangeType.Add"/> — добавленный элемент.</item>
        /// <item><see cref="ChangeType.Remove"/> — удалённый элемент.</item>
        /// <item><see cref="ChangeType.Update"/> — изменившийся элемент (та же ссылка).</item>
        /// <item><see cref="ChangeType.Replace"/> — новый элемент.</item>
        /// <item><see cref="ChangeType.Reset"/> — не используется (<c>default(T)</c>).</item>
        /// </list>
        /// </remarks>
        public T Item { get; }

        /// <summary>
        /// Предыдущее значение. Заполняется только для <see cref="ChangeType.Replace"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Для ссылочных <typeparamref name="T"/> «не заполнено» означает <c>null</c>.
        /// Для значимых типов — <c>default(T)</c> (например, <c>0</c> для <see cref="int"/>).
        /// </para>
        /// <para>
        /// Если <typeparamref name="T"/> — значимый тип и <c>default(T)</c> является
        /// осмысленным значением (например, <c>0</c> как валидный ID), использовать
        /// <see cref="Replace"/> нельзя — см. README, раздел 4.1.
        /// </para>
        /// </remarks>
        public T? OldItem { get; }

        /// <summary>
        /// Приватный конструктор: экземпляры создаются только через фабрики.
        /// </summary>
        private Change(ChangeType type, T item, T? oldItem = default)
        {
            Type = type;
            Item = item;
            OldItem = oldItem;
        }

        /// <summary>
        /// Создаёт изменение типа <see cref="ChangeType.Add"/>.
        /// </summary>
        /// <param name="item">Добавленный элемент.</param>
        public static Change<T> Add(T item) => new Change<T>(ChangeType.Add, item);

        /// <summary>
        /// Создаёт изменение типа <see cref="ChangeType.Remove"/>.
        /// </summary>
        /// <param name="item">Удалённый элемент.</param>
        /// <remarks>
        /// При наличии дубликатов по <see cref="object.Equals(object)"/> неопределённо,
        /// какой именно экземпляр был удалён — см. README, раздел 4.3.
        /// </remarks>
        public static Change<T> Remove(T item) => new Change<T>(ChangeType.Remove, item);

        /// <summary>
        /// Создаёт изменение типа <see cref="ChangeType.Update"/>.
        /// </summary>
        /// <param name="item">Изменившийся элемент (та же ссылка).</param>
        /// <param name="oldItem">
        /// Не используется и оставлено для обратной совместимости; фактически
        /// <see cref="Change{T}.Update"/> не несёт старого значения.
        /// </param>
        /// <remarks>
        /// Для immutable <typeparamref name="T"/> этот тип события бесполезен —
        /// старое значение уже потеряно. Используйте <see cref="Replace"/>.
        /// См. README, раздел 4.2.
        /// </remarks>
        public static Change<T> Update(T item, T? oldItem = default)
            => new Change<T>(ChangeType.Update, item, oldItem);

        /// <summary>
        /// Создаёт изменение типа <see cref="ChangeType.Replace"/>.
        /// </summary>
        /// <param name="oldItem">
        /// Заменяемый элемент. Не должен быть <c>default</c>: для ссылочных типов
        /// не <c>null</c>, для значимых — не <c>default(T)</c>.
        /// </param>
        /// <param name="newItem">Новый элемент.</param>
        /// <remarks>
        /// Передача <c>default</c> не проверяется при создании, но приводит
        /// к <see cref="InvalidOperationException"/> в момент обработки события
        /// в узле проекции.
        /// </remarks>
        public static Change<T> Replace(T oldItem, T newItem)
            => new Change<T>(ChangeType.Replace, newItem, oldItem);

        /// <summary>
        /// Создаёт изменение типа <see cref="ChangeType.Reset"/>.
        /// </summary>
        public static Change<T> Reset() => new Change<T>(ChangeType.Reset, default!);

        /// <summary>
        /// Возвращает строковое представление изменения для отладки и логов.
        /// </summary>
        public override string ToString()
        {
            return Type switch
            {
                ChangeType.Replace => $"{Type}: {OldItem} -> {Item}",

                ChangeType.Reset => $"{Type}",

                _ => $"{Type}: {Item}"
            };
        }
    }
}