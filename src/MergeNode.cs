using System;
using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// Объединение двух источников одного типа в один живой список.
    /// </summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    /// <remarks>
    /// <para>
    /// Семантика — <b>Concat</b>, не <b>Union</b>: дубликаты сохраняются.
    /// <c>[A] + [A] = [A, A]</c>. Порядок — сначала элементы <c>first</c>,
    /// затем <c>second</c>, в порядке итерации источников.
    /// </para>
    /// <para>
    /// Узел не является <see cref="ProjectionNode{TSource, TResult}"/>,
    /// потому что у него два источника, и он не транслирует события одного
    /// в результат — он поддерживает собственное состояние.
    /// </para>
    /// <para>
    /// Изменения от обоих источников обрабатываются единым методом
    /// <see cref="ApplyChange"/>. Источник, из которого пришло изменение,
    /// идентифицируется через <c>sourceId</c> (<see cref="SourceFirst"/> или
    /// <see cref="SourceSecond"/>). Это необходимо, чтобы при дубликатах
    /// по <see cref="EqualityComparer{T}.Default"/> удалять/заменять именно
    /// тот экземпляр, который пришёл от нужного источника.
    /// </para>
    /// </remarks>
    public sealed class MergeNode<T> : ObservableNode<T>
    {
        /// <summary>
        /// Пара «элемент результата → идентификатор источника, из которого он пришёл».
        /// </summary>
        /// <remarks>
        /// Список параллелен <see cref="ObservableNode{T}.Items"/>:
        /// <c>_entries[i]</c> соответствует <c>Items[i]</c>. Инвариант должен
        /// соблюдаться во всех операциях над узлом.
        /// </remarks>
        private sealed class Entry
        {
            /// <summary>Элемент результата.</summary>
            public readonly T Item;

            /// <summary>Идентификатор источника: <see cref="SourceFirst"/> или <see cref="SourceSecond"/>.</summary>
            public readonly int SourceId;

            public Entry(T item, int sourceId)
            {
                Item = item;
                SourceId = sourceId;
            }
        }

        /// <summary>Идентификатор первого источника.</summary>
        private const int SourceFirst = 0;

        /// <summary>Идентификатор второго источника.</summary>
        private const int SourceSecond = 1;

        /// <summary>Первый источник.</summary>
        private readonly IObservableList<T> _first;

        /// <summary>Второй источник.</summary>
        private readonly IObservableList<T> _second;

        /// <summary>
        /// Список пар «элемент → источник», параллельный <c>Items</c>.
        /// </summary>
        /// <remarks>
        /// Инвариант: <c>_entries.Count == Count</c> и порядок совпадает.
        /// Используется для адресации элемента по <c>(item, sourceId)</c> —
        /// иначе при дубликатах по <see cref="EqualityComparer{T}.Default"/>
        /// невозможно определить, какой именно экземпляр удалять.
        /// </remarks>
        private readonly List<Entry> _entries = new();

        /// <summary>
        /// Создаёт объединение двух источников.
        /// </summary>
        /// <param name="first">Первый источник. Не может быть <c>null</c>.</param>
        /// <param name="second">Второй источник. Не может быть <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">
        /// Если любой из источников — <c>null</c>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Конструктор:
        /// <list type="number">
        /// <item>Считывает текущее содержимое обоих источников в <c>Items</c>
        ///     и <c>_entries</c>.</item>
        /// <item>Подписывается на <c>Changed</c> обоих источников.</item>
        /// </list>
        /// </para>
        /// <para>
        /// На этапе инициализации каждый элемент даёт событие <c>Add</c>
        /// (см. README, раздел 4.5). Порядок подписки — сначала <c>first</c>,
        /// затем <c>second</c>; события обрабатываются в этом же порядке.
        /// </para>
        /// </remarks>
        public MergeNode(
            IObservableList<T> first,
            IObservableList<T> second)
        {
            _first = first
                ?? throw new ArgumentNullException(nameof(first));

            _second = second
                ?? throw new ArgumentNullException(nameof(second));

            Initialize();

            _first.Changed += OnFirstChanged;
            _second.Changed += OnSecondChanged;
        }

        /// <summary>
        /// Считывает текущее содержимое обоих источников в результат.
        /// </summary>
        /// <remarks>
        /// Вызывается из конструктора. Порядок: сначала все элементы
        /// <c>_first</c>, затем все элементы <c>_second</c>.
        /// </remarks>
        private void Initialize()
        {
            foreach (var item in _first)
                AddFromSource(item, SourceFirst);

            foreach (var item in _second)
                AddFromSource(item, SourceSecond);
        }

        /// <summary>
        /// Отписывается от обоих источников при <see cref="ObservableNode{T}.Dispose"/>.
        /// </summary>
        /// <remarks>
        /// Источники <b>не</b> удаляются — это ответственность вызывающего.
        /// Отписка идемпотентна: если по какой-то причине конструктор бросил
        /// исключение до подписки, <c>-=</c> не найдёт делегата и не бросит ошибку.
        /// </remarks>
        protected override void DisposeCore()
        {
            _first.Changed -= OnFirstChanged;
            _second.Changed -= OnSecondChanged;

            base.DisposeCore();
        }

        /// <summary>
        /// Обработчик изменений первого источника.
        /// </summary>
        private void OnFirstChanged(Change<T> change) => ApplyChange(change, SourceFirst);

        /// <summary>
        /// Обработчик изменений второго источника.
        /// </summary>
        private void OnSecondChanged(Change<T> change) => ApplyChange(change, SourceSecond);

        /// <summary>
        /// Единый обработчик изменений от любого источника.
        /// </summary>
        /// <param name="change">Изменение от источника.</param>
        /// <param name="sourceId">
        /// Идентификатор источника: <see cref="SourceFirst"/> или <see cref="SourceSecond"/>.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Если <see cref="Change{T}.Type"/> содержит неизвестное значение.
        /// </exception>
        /// <remarks>
        /// <see cref="ChangeType.Reset"/> пересобирает <b>оба</b> источника,
        /// даже если сброс пришёл только от одного. Это компромисс: точнее
        /// и проще, но шумнее по событиям.
        /// </remarks>
        private void ApplyChange(Change<T> change, int sourceId)
        {
            switch (change.Type)
            {
                case ChangeType.Add:
                    AddFromSource(change.Item, sourceId);
                    break;

                case ChangeType.Remove:
                    RemoveFromSource(change.Item, sourceId);
                    break;

                case ChangeType.Update:
                    UpdateFromSource(change.Item, sourceId);
                    break;

                case ChangeType.Replace:
                    ReplaceFromSource(change.OldItem!, change.Item, sourceId);
                    break;

                case ChangeType.Reset:
                    Rebuild();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Добавляет элемент из указанного источника.
        /// </summary>
        /// <remarks>
        /// Порядок операций: сначала <c>AddInternal</c> (мутация <c>Items</c>
        /// и событие), затем регистрация в <c>_entries</c>. Инвариант
        /// <c>_entries.Count == Count</c> восстанавливается сразу после вызова.
        /// </remarks>
        private void AddFromSource(T item, int sourceId)
        {
            AddInternal(item);
            _entries.Add(new Entry(item, sourceId));
        }

        /// <summary>
        /// Удаляет элемент, пришедший из указанного источника.
        /// </summary>
        /// <remarks>
        /// Ищет пару <c>(item, sourceId)</c>, а не просто <c>item</c>. Это
        /// критично при дубликатах по <see cref="EqualityComparer{T}.Default"/>:
        /// без учёта <paramref name="sourceId"/> удалился бы первый равный
        /// в <c>Items</c>, независимо от того, из какого источника он пришёл.
        /// </remarks>
        private void RemoveFromSource(T item, int sourceId)
        {
            int index = FindEntryIndex(item, sourceId);

            if (index < 0)
                return;

            RemoveAtInternal(index);
            _entries.RemoveAt(index);
        }

        /// <summary>
        /// Уведомляет об обновлении элемента из указанного источника.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Использует <see cref="ObservableNode{T}.UpdateAtInternal"/> —
        /// райзит <see cref="ChangeType.Update"/> по индексу, найденному
        /// через <c>FindEntryIndex</c> с учётом <paramref name="sourceId"/>.
        /// </para>
        /// <para>
        /// <c>Entry</c> пересоздаётся с тем же значением <c>item</c>. Для
        /// ссылочных типов это тот же экземпляр; для значимых — обновлённая
        /// копия, чтобы <see cref="EqualityComparer{T}.Default"/> работал
        /// с актуальным значением.
        /// </para>
        /// </remarks>
        private void UpdateFromSource(T item, int sourceId)
        {
            int index = FindEntryIndex(item, sourceId);

            if (index < 0)
                return;

            UpdateAtInternal(index);

            _entries[index] = new Entry(item, sourceId);
        }

        /// <summary>
        /// Заменяет элемент из указанного источника.
        /// </summary>
        /// <param name="oldItem">Старое значение. Должно быть не <c>default</c>.</param>
        /// <param name="newItem">Новое значение.</param>
        /// <param name="sourceId">Идентификатор источника.</param>
        /// <remarks>
        /// Ищет пару <c>(oldItem, sourceId)</c>, заменяет элемент в <c>Items</c>
        /// через <see cref="ObservableNode{T}.ReplaceAtInternal"/> и обновляет
        /// запись в <c>_entries</c>. Если пара не найдена — ничего не делает.
        /// </remarks>
        private void ReplaceFromSource(T oldItem, T newItem, int sourceId)
        {
            int index = FindEntryIndex(oldItem, sourceId);

            if (index < 0)
                return;

            ReplaceAtInternal(index, newItem);
            _entries[index] = new Entry(newItem, sourceId);
        }

        /// <summary>
        /// Ищет запись по паре <c>(item, sourceId)</c>.
        /// </summary>
        /// <returns>Индекс записи или <c>-1</c>, если не найдено.</returns>
        /// <remarks>
        /// Возвращает <b>первый равный</b> по <see cref="EqualityComparer{T}.Default"/>
        /// среди записей с указанным <paramref name="sourceId"/>. Элементы
        /// из другого источника игнорируются — это ключевое отличие от
        /// <see cref="ObservableNode{T}.RemoveInternal"/>.
        /// </remarks>
        private int FindEntryIndex(T item, int sourceId)
        {
            var comparer = EqualityComparer<T>.Default;

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].SourceId != sourceId)
                    continue;

                if (comparer.Equals(_entries[i].Item, item))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Полностью пересобирает содержимое узла из обоих источников.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Вызывается при <see cref="ChangeType.Reset"/> от любого источника.
        /// Порядок: очистка <c>Items</c> и <c>_entries</c>, затем повторное
        /// считывание элементов из <c>_first</c> и <c>_second</c>.
        /// </para>
        /// <para>
        /// <see cref="ObservableNode{T}.ResetInternal"/> не райзит событие,
        /// если <c>Items</c> пуст. Это означает, что <c>Reset</c> от источника
        /// на пустом merge-узле не даст события — см. README, раздел 4.6.
        /// </para>
        /// </remarks>
        private void Rebuild()
        {
            ResetInternal();
            _entries.Clear();

            foreach (var item in _first)
                AddFromSource(item, SourceFirst);

            foreach (var item in _second)
                AddFromSource(item, SourceSecond);
        }
    }
}