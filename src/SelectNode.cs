using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// Проекция, преобразующая каждый элемент источника в результат типа
    /// <typeparamref name="TResult"/> с сохранением ссылочной идентичности.
    /// </summary>
    /// <typeparam name="TSource">Тип элемента источника.</typeparam>
    /// <typeparam name="TResult">Тип элемента результата.</typeparam>
    /// <remarks>
    /// <para>
    /// Отличие от LINQ <c>Select</c>: результат — <b>живой</b> узел. Изменения
    /// источника автоматически применяются к результату, а сам узел можно
    /// использовать как источник для следующих проекций.
    /// </para>
    /// <para>
    /// При <see cref="ChangeType.Update"/> элемент <typeparamref name="TResult"/>
    /// <b>не пересоздаётся</b> — вызывается <c>_updater</c>, который должен
    /// обновить существующий экземпляр. Это позволяет UI и последующим узлам
    /// сохранять ссылки на результат.
    /// </para>
    /// </remarks>
    public class SelectNode<TSource, TResult> : ProjectionNode<TSource, TResult>
    {
        /// <summary>
        /// Пара «исходный элемент → результат».
        /// </summary>
        /// <remarks>
        /// Заменяет <c>Dictionary&lt;TSource, TResult&gt;</c> из ранней версии,
        /// которая ломалась на дубликатах по <see cref="object.Equals(object)"/>:
        /// второй элемент перезатирал первый, и <c>Remove</c> удалял не тот
        /// <typeparamref name="TResult"/>. Список сохраняет порядок source
        /// и допускает дубликаты — см. README, раздел 6.
        /// </remarks>
        private sealed class Entry
        {
            /// <summary>Ссылка на исходный элемент.</summary>
            public readonly TSource Source;

            /// <summary>Соответствующий ему результат.</summary>
            public readonly TResult Result;

            public Entry(TSource source, TResult result)
            {
                Source = source;
                Result = result;
            }
        }

        /// <summary>
        /// Фабрика результата. Вызывается один раз на каждый добавленный
        /// элемент источника.
        /// </summary>
        private readonly Func<TSource, TResult> _factory;

        /// <summary>
        /// Обновлятор результата. Вызывается при <see cref="ChangeType.Update"/>
        /// источника — должен мутировать <typeparamref name="TResult"/> in-place.
        /// </summary>
        /// <remarks>
        /// Работает только для mutable <typeparamref name="TResult"/>.
        /// Для immutable-типов (records, structs) <c>_updater</c> не сможет
        /// изменить результат — в этом случае используйте <see cref="Replace"/>
        /// семантику через отдельный узел.
        /// </remarks>
        private readonly Action<TSource, TResult> _updater;

        /// <summary>
        /// Параллельный source список пар «источник → результат».
        /// </summary>
        /// <remarks>
        /// Инвариант: порядок <c>_entries</c> совпадает с порядком элементов
        /// источника. Используется в <see cref="FindEntryIndex"/> для поиска
        /// по первому равному — та же семантика, что <see cref="List{T}.Remove"/>.
        /// </remarks>
        private readonly List<Entry> _entries = new();

        /// <summary>
        /// Создаёт проекцию над указанным источником.
        /// </summary>
        /// <param name="source">Источник изменений. Не может быть <c>null</c>.</param>
        /// <param name="factory">
        /// Фабрика нового результата. Не может быть <c>null</c>.
        /// Вызывается один раз на каждый новый элемент источника.
        /// </param>
        /// <param name="updater">
        /// Обновлятор существующего результата. Не может быть <c>null</c>.
        /// Вызывается при изменении элемента источника. Может быть пустым
        /// (<c>(_, _) =&gt; { }</c>), если обновление не требуется.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Если любой из аргументов — <c>null</c>.
        /// </exception>
        /// <remarks>
        /// Конструктор вызывает <see cref="ProjectionNode{TSource, TResult}.Initialize"/>
        /// в конце — после инициализации полей <c>_factory</c> и <c>_updater</c>.
        /// </remarks>
        public SelectNode(
            IObservableList<TSource> source,
            Func<TSource, TResult> factory,
            Action<TSource, TResult> updater) : base(source)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _updater = updater ?? throw new ArgumentNullException(nameof(updater));

            Initialize();
        }

        /// <summary>
        /// Создаёт результат для нового элемента источника, регистрирует пару
        /// в <c>_entries</c> и добавляет результат в коллекцию.
        /// </summary>
        /// <remarks>
        /// Порядок операций важен: <c>_entries.Add</c> выполняется <b>до</b>
        /// <see cref="ObservableNode{TResult}.AddInternal"/>, чтобы подписчики
        /// <c>Changed</c> не увидели рассинхрон между <c>Items</c> и <c>_entries</c>.
        /// </remarks>
        protected override void OnAdd(Change<TSource> change)
        {
            var sourceItem = change.Item;

            var result = _factory(sourceItem);
            _updater(sourceItem, result);

            _entries.Add(new Entry(sourceItem, result));

            AddInternal(result);
        }

        /// <summary>
        /// Находит запись по <paramref name="change"/>, удаляет её из <c>_entries</c>
        /// и удаляет соответствующий результат из коллекции.
        /// </summary>
        /// <remarks>
        /// Поиск идёт по первому равному через <see cref="FindEntryIndex"/> —
        /// та же семантика, что <see cref="List{T}.Remove"/> в source.
        /// Если запись не найдена (элемент не был в результате) — ничего не делает.
        /// </remarks>
        protected override void OnRemove(Change<TSource> change)
        {
            int index = FindEntryIndex(change.Item);

            if (index < 0)
                return;

            var result = _entries[index].Result;

            _entries.RemoveAt(index);

            RemoveInternal(result);
        }

        /// <summary>
        /// Обновляет существующий результат для изменившегося элемента источника.
        /// </summary>
        /// <remarks>
        /// Результат <b>не пересоздаётся</b> — <c>_updater</c> мутирует его
        /// in-place, чтобы потребители сохранили ссылку. Если элемент не найден —
        /// ничего не делает.
        /// </remarks>
        protected override void OnUpdate(Change<TSource> change)
        {
            int index = FindEntryIndex(change.Item);

            if (index < 0)
                return;

            var result = _entries[index].Result;

            _updater(change.Item, result);

            UpdateInternal(result);
        }

        /// <summary>
        /// Ищет запись для указанного элемента источника.
        /// </summary>
        /// <returns>Индекс записи или <c>-1</c>, если не найдено.</returns>
        /// <remarks>
        /// Возвращает <b>первый равный</b> по <see cref="EqualityComparer{T}.Default"/>,
        /// что совпадает с семантикой <see cref="List{T}.Remove"/> в источнике.
        /// При дубликатах возможна путаница — см. README, раздел 4.3.
        /// </remarks>
        private int FindEntryIndex(TSource sourceItem)
        {
            var comparer = EqualityComparer<TSource>.Default;

            for (int i = 0; i < _entries.Count; i++)
            {
                if (comparer.Equals(_entries[i].Source, sourceItem))
                    return i;
            }

            return -1;
        }
    }
}