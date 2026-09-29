using System;

namespace ReactiveCollections
{
    /// <summary>
    /// Проекция, пропускающая только те элементы источника, которые удовлетворяют
    /// предикату. Порядок элементов сохраняется как в источнике.
    /// </summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    /// <remarks>
    /// <para>
    /// Предикат вызывается синхронно, в момент прихода изменения от источника.
    /// Он должен быть чистым — без побочных эффектов, кроме, возможно, ленивых
    /// вычислений. Исключение в предикате прервёт обработку события.
    /// </para>
    /// <para>
    /// Семантика <see cref="OnRemove"/> совпадает с source: если источник удалил
    /// первый равный по <see cref="EqualityComparer{T}.Default"/> элемент,
    /// фильтр удалит первый равный из своей выдачи — см. README, раздел 4.3.
    /// </para>
    /// </remarks>
    public class FilterNode<T> : ProjectionNode<T, T>
    {
        /// <summary>
        /// Предикат: <c>true</c> — элемент остаётся в результате,
        /// <c>false</c> — исключается.
        /// </summary>
        private readonly Func<T, bool> _predicate;

        /// <summary>
        /// Создаёт фильтр над указанным источником.
        /// </summary>
        /// <param name="source">Источник изменений. Не может быть <c>null</c>.</param>
        /// <param name="predicate">
        /// Предикат фильтрации. Не может быть <c>null</c>.
        /// Вызывается синхронно при каждом изменении источника.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Если <paramref name="source"/> или <paramref name="predicate"/> — <c>null</c>.
        /// </exception>
        /// <remarks>
        /// Конструктор вызывает <see cref="ProjectionNode{TSource, TResult}.Initialize"/>
        /// в конце — после инициализации поля <c>_predicate</c>. Это важно:
        /// <c>Initialize</c> вызывает виртуальные <c>On*</c>-методы, которые
        /// читают <c>_predicate</c>.
        /// </remarks>
        public FilterNode(IObservableList<T> source, Func<T, bool> predicate) : base(source)
        {
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

            Initialize();
        }

        /// <summary>
        /// Добавляет элемент в результат, если он проходит предикат.
        /// </summary>
        protected override void OnAdd(Change<T> change)
        {
            var item = change.Item;

            if (_predicate(item))
                AddInternal(item);
        }

        /// <summary>
        /// Удаляет элемент из результата. Если элемент не был в результате
        /// (например, не прошёл предикат), <see cref="ObservableNode{T}.RemoveInternal"/>
        /// вернёт <c>false</c> и событие не райзится.
        /// </summary>
        protected override void OnRemove(Change<T> change)
        {
            RemoveInternal(change.Item);
        }

        /// <summary>
        /// Обрабатывает обновление элемента источника. Возможны три случая:
        /// <list type="bullet">
        /// <item>Элемент был в результате и остался валидным → <c>Update</c>.</item>
        /// <item>Элемент был в результате и стал невалидным → <c>Remove</c>.</item>
        /// <item>Элемента не было, и он стал валидным → <c>Add</c>.</item>
        /// </list>
        /// Если элемент не был в результате и остался невалидным — ничего не происходит.
        /// </summary>
        /// <remarks>
        /// Индекс ищется один раз через <see cref="List{T}.IndexOf"/>,
        /// затем используются индексные операции (<c>UpdateAtInternal</c>,
        /// <c>RemoveAtInternal</c>) — это позволяет избежать двойного поиска
        /// и гарантировать, что манипуляция применяется именно к найденному элементу.
        /// </remarks>
        protected override void OnUpdate(Change<T> change)
        {
            var item = change.Item;

            int index = Items.IndexOf(item);
            bool exists = index >= 0;
            bool valid = _predicate(item);

            if (valid && !exists)
                AddInternal(item);
            else if (!valid && exists)
                RemoveAtInternal(index);
            else if (valid && exists)
                UpdateAtInternal(index);
        }

        /// <summary>
        /// Обрабатывает замену элемента источника. Логика строится на факте наличия
        /// <see cref="Change{T}.OldItem"/> в текущем результате, а не на предикате
        /// старого значения — иначе при рассинхроне (старый элемент не в результате,
        /// но проходит предикат) можно потерять изменение.
        /// </summary>
        /// <remarks>
        /// Возможны четыре случая:
        /// <list type="bullet">
        /// <item>Старый в результате, новый валиден → заменить по индексу.</item>
        /// <item>Старый в результате, новый невалиден → удалить по индексу.</item>
        /// <item>Старого нет, новый валиден → добавить.</item>
        /// <item>Старого нет, новый невалиден → ничего.</item>
        /// </list>
        /// Все операции индексные — это гарантирует, что мы не заденем другой
        /// элемент при дубликатах по <see cref="EqualityComparer{T}.Default"/>.
        /// </remarks>
        protected override void OnReplace(Change<T> change)
        {
            var oldItem = change.OldItem!;
            var newItem = change.Item;

            int index = Items.IndexOf(oldItem);
            bool newValid = _predicate(newItem);

            if (index >= 0)
            {
                if (newValid)
                    ReplaceAtInternal(index, newItem);
                else
                    RemoveAtInternal(index);
            }
            else
            {
                if (newValid)
                    AddInternal(newItem);
            }
        }
    }
}