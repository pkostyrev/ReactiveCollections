using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// База для проекций, группирующих элементы источника по ключу.
    /// </summary>
    /// <typeparam name="TKey">Тип ключа группы.</typeparam>
    /// <typeparam name="TSource">Тип элемента источника.</typeparam>
    /// <typeparam name="TResult">Тип результата (обычно — группа).</typeparam>
    /// <remarks>
    /// <para>
    /// Поддерживает два индекса:
    /// <list type="bullet">
    /// <item><c>_results</c> — <typeparamref name="TKey"/> → <typeparamref name="TResult"/>.
    ///     Используется для проверки, существует ли уже группа с таким ключом.</item>
    /// <item><c>_itemEntries</c> — параллельный source список пар
    ///     «исходный элемент → результат». Позволяет найти, в какой группе
    ///     находится конкретный элемент, и корректно обработать переезд
    ///     между группами.</item>
    /// </list>
    /// </para>
    /// <para>
    /// Наследник реализует операции над группами через абстрактные методы
    /// <see cref="CreateResult"/>, <see cref="AddItem"/>, <see cref="RemoveItem"/>,
    /// <see cref="IsEmpty"/>, <see cref="GetKey"/>. Пример реализации —
    /// <see cref="GroupNode{TKey, TSource}"/>.
    /// </para>
    /// </remarks>
    public abstract class KeyedProjectionNode<TKey, TSource, TResult> : ProjectionNode<TSource, TResult>
    {
        /// <summary>
        /// Пара «исходный элемент → результат-группа».
        /// </summary>
        /// <remarks>
        /// Заменяет <c>Dictionary&lt;TSource, TResult&gt;</c> из ранней версии.
        /// Список допускает дубликаты по <see cref="object.Equals(object)"/>
        /// и сохраняет порядок source — см. README, раздел 6.
        /// </remarks>
        private sealed class Entry
        {
            /// <summary>Ссылка на исходный элемент.</summary>
            public readonly TSource Source;

            /// <summary>Группа, в которой находится этот элемент.</summary>
            public readonly TResult Result;

            public Entry(TSource source, TResult result)
            {
                Source = source;
                Result = result;
            }
        }

        /// <summary>
        /// Индекс групп по ключу. Уникальность ключа обязательна по смыслу.
        /// </summary>
        private readonly Dictionary<TKey, TResult> _results = new();

        /// <summary>
        /// Параллельный source список пар «элемент → группа».
        /// </summary>
        /// <remarks>
        /// Инвариант: порядок совпадает с порядком элементов источника.
        /// Используется в <see cref="FindItemEntryIndex"/> — поиск по первому равному.
        /// </remarks>
        private readonly List<Entry> _itemEntries = new();

        /// <summary>
        /// Селектор ключа группы по элементу источника.
        /// </summary>
        private readonly Func<TSource, TKey> _keySelector;

        /// <summary>
        /// Компаратор для сопоставления элементов источника.
        /// </summary>
        /// <remarks>
        /// Применяется в <see cref="FindItemEntryIndex"/>. По умолчанию —
        /// <see cref="EqualityComparer{T}.Default"/>. Для ссылочных типов без
        /// переопределённого <see cref="object.Equals(object)"/> сравнение
        /// происходит по ссылке.
        /// </remarks>
        private readonly IEqualityComparer<TSource> _itemComparer;

        /// <summary>
        /// Создаёт базовый узел группировки.
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
        /// Конструктор не вызывает <see cref="ProjectionNode{TSource, TResult}.Initialize"/>
        /// — это делает наследник, после инициализации своих полей.
        /// </remarks>
        protected KeyedProjectionNode(
            IObservableList<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TSource>? comparer = null) : base(source)
        {
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            _itemComparer = comparer ?? EqualityComparer<TSource>.Default;
        }

        /// <summary>
        /// Добавляет элемент источника в группу, создавая её при необходимости.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Порядок операций:
        /// <list type="number">
        /// <item>Вычислить ключ и, если группы нет, создать её и добавить
        ///     в <c>_results</c> и <c>Items</c>.</item>
        /// <item>Добавить элемент во внутреннее содержимое группы
        ///     через <see cref="AddItem"/>.</item>
        /// <item>Зарегистрировать пару в <c>_itemEntries</c>.</item>
        /// </list>
        /// </para>
        /// <para>
        /// Событие <c>Add(result)</c> в <c>Items</c> райзится до того, как
        /// в группе появится первый элемент. Это значит, что подписчики
        /// <c>Changed</c> могут увидеть пустую группу — см. README, раздел 4.3.
        /// </para>
        /// </remarks>
        protected override void OnAdd(Change<TSource> change)
        {
            var item = change.Item;
            var key = _keySelector(item);

            if (!_results.TryGetValue(key, out var result))
            {
                result = CreateResult(key);
                _results.Add(key, result);
                AddInternal(result);
            }

            AddItem(result, item);
            _itemEntries.Add(new Entry(item, result));
        }

        /// <summary>
        /// Удаляет элемент источника из его группы; если группа стала пустой —
        /// удаляет и саму группу.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Если <see cref="Change{T}.Item"/> не найден в <c>_itemEntries</c>,
        /// метод ничего не делает (это может означать рассинхрон с источником).
        /// </para>
        /// <para>
        /// Ключ группы берётся через <see cref="GetKey"/> <b>до</b> того,
        /// как элемент удалён из группы. Наследники должны возвращать
        /// стабильный ключ, не зависящий от содержимого группы.
        /// </para>
        /// </remarks>
        protected override void OnRemove(Change<TSource> change)
        {
            var item = change.Item;

            int index = FindItemEntryIndex(item);
            if (index < 0)
                return;

            var result = _itemEntries[index].Result;

            RemoveItem(result, item);
            _itemEntries.RemoveAt(index);

            if (!IsEmpty(result))
                return;

            _results.Remove(GetKey(result));
            RemoveInternal(result);
        }

        /// <summary>
        /// Обрабатывает обновление элемента источника. Если ключ не изменился —
        /// уведомляет группу через <see cref="UpdateItem"/>; если изменился —
        /// перемещает элемент между группами.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Сравнение ключей идёт по <see cref="EqualityComparer{TKey}.Default"/>,
        /// независимо от компаратора элементов.
        /// </para>
        /// <para>
        /// Порядок операций при переезде:
        /// <list type="number">
        /// <item>Удалить элемент из старой группы.</item>
        /// <item>Если старая группа опустела — удалить её из <c>_results</c>
        ///     и <c>Items</c>.</item>
        /// <item>Найти или создать новую группу.</item>
        /// <item>Добавить элемент в новую группу.</item>
        /// <item>Обновить запись в <c>_itemEntries</c>.</item>
        /// </list>
        /// Между шагами 1 и 4 элемент физически отсутствует в обеих группах —
        /// подписчики <c>group.Items.Changed</c> могут это заметить.
        /// </para>
        /// </remarks>
        protected override void OnUpdate(Change<TSource> change)
        {
            var item = change.Item;

            int index = FindItemEntryIndex(item);
            if (index < 0)
                return;

            var oldResult = _itemEntries[index].Result;
            var oldKey = GetKey(oldResult);
            var newKey = _keySelector(item);

            // элемент остался в той же группе
            if (EqualityComparer<TKey>.Default.Equals(oldKey, newKey))
            {
                UpdateItem(oldResult, item);
                return;
            }

            // элемент переехал в другую группу
            RemoveItem(oldResult, item);

            if (IsEmpty(oldResult))
            {
                _results.Remove(oldKey);
                RemoveInternal(oldResult);
            }

            if (!_results.TryGetValue(newKey, out var newResult))
            {
                newResult = CreateResult(newKey);
                _results.Add(newKey, newResult);
                AddInternal(newResult);
            }

            AddItem(newResult, item);
            _itemEntries[index] = new Entry(item, newResult);
        }

        /// <summary>
        /// Полный сброс: очищает оба индекса и базовую коллекцию.
        /// </summary>
        /// <remarks>
        /// Индексы чистятся <b>до</b> вызова базового <c>ResetInternal</c>,
        /// чтобы инвариант «если <c>Items</c> пуст, то и индексы пусты» соблюдался.
        /// </remarks>
        protected override void OnReset(Change<TSource> change)
        {
            _results.Clear();
            _itemEntries.Clear();
            base.OnReset(change);
        }

        /// <summary>
        /// Ищет запись для указанного элемента источника.
        /// </summary>
        /// <returns>Индекс записи или <c>-1</c>, если не найдено.</returns>
        /// <remarks>
        /// Возвращает <b>первый равный</b> по <c>_itemComparer</c>. Семантика
        /// совпадает с <see cref="List{T}.Remove"/> в источнике — см. README, раздел 4.3.
        /// </remarks>
        private int FindItemEntryIndex(TSource item)
        {
            for (int i = 0; i < _itemEntries.Count; i++)
            {
                if (_itemComparer.Equals(_itemEntries[i].Source, item))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Создаёт новый результат-группу для указанного ключа.
        /// Вызывается, когда элемент с этим ключом встретился впервые.
        /// </summary>
        /// <param name="key">Ключ новой группы.</param>
        protected abstract TResult CreateResult(TKey key);

        /// <summary>
        /// Добавляет элемент в содержимое группы.
        /// </summary>
        /// <param name="result">Целевая группа.</param>
        /// <param name="item">Добавляемый элемент.</param>
        protected abstract void AddItem(TResult result, TSource item);

        /// <summary>
        /// Удаляет элемент из содержимого группы.
        /// </summary>
        /// <param name="result">Целевая группа.</param>
        /// <param name="item">Удаляемый элемент.</param>
        protected abstract void RemoveItem(TResult result, TSource item);

        /// <summary>
        /// Уведомляет группу об изменении элемента, оставшегося в ней.
        /// По умолчанию — no-op.
        /// </summary>
        /// <param name="result">Группа, в которой находится элемент.</param>
        /// <param name="item">Изменившийся элемент.</param>
        protected virtual void UpdateItem(TResult result, TSource item)
        {
        }

        /// <summary>
        /// Проверяет, пуста ли группа.
        /// </summary>
        /// <param name="result">Проверяемая группа.</param>
        /// <returns><c>true</c>, если в группе нет элементов.</returns>
        protected abstract bool IsEmpty(TResult result);

        /// <summary>
        /// Возвращает ключ группы.
        /// </summary>
        /// <param name="result">Группа.</param>
        /// <remarks>
        /// Метод вызывается как до, так и после операций над содержимым группы.
        /// Наследники должны возвращать стабильный ключ, не зависящий от
        /// содержимого (например, сохранённый при создании группы).
        /// </remarks>
        protected abstract TKey GetKey(TResult result);
    }
}