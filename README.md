# ReactiveCollections

Реактивная библиотека коллекций для .NET: живые проекции, группы,
объединения и одиночные значения.

## Содержание

1. [Цель](#1-цель)
2. [Архитектура](#2-архитектура)
3. [Публичный API](#3-публичный-api)
4. [Ограничения и допущения](#4-ограничения-и-допущения)
5. [Сценарии использования](#5-сценарии-использования)
6. [История решений](#6-история-решений)
7. [Дорожная карта](#7-дорожная-карта)
8. [Изменения](#8-изменения)

---

## 1. Цель

ReactiveCollections — не копия `ObservableCollection<T>` и не аналог LINQ.
Основная идея — **живой граф данных**: один узел получает изменения
другого узла, преобразует их и автоматически поддерживает своё текущее
состояние.

Ключевой принцип:

> Коллекция хранит **текущее состояние**, а `Change<T>` описывает **переход
> состояния**. Проекции — это живые узлы и могут быть источниками для
> следующих проекций.

Центральная идея новой архитектуры — **сопоставление по occurrence, а не
по значению**:

    Source occurrence
          │
          │ Change.Index / FromIndex / ToIndex
          ▼
    Projection mapping
          │
          ▼
    Result occurrence

Изменение получает индекс непосредственно из `Change<T>`. `Equals`
не используется для определения того, **какой** occurrence изменился.
Это позволяет корректно работать с элементами, равными по `Equals`.

Целевые сценарии:

- фильтрация сущностей по признаку (HP, активности, статусу и т.п.);
- преобразование доменной модели в модель представления
  (`Player` → `PlayerView`);
- группировка по ключу (`Player` → `Group<TeamId, Player>`);
- объединение двух живых источников;
- flattening вложенных реактивных коллекций.

---

## 2. Архитектура

### Уровни

| Уровень | Ответственность | Пример |
|---|---|---|
| `Change<T>` | Описание изменения (абстрактный базовый тип) | AddChange, RemoveChange, ... |
| `IObservableList<T>` | Контракт чтения + событие + IDisposable | Источник или проекция |
| `ObservableNode<T>` | Хранение элементов, публикация событий, жизненный цикл | `Items` + `Changed` |
| `ObservableList<T>` | Корневой изменяемый источник | Add / AddAt / Remove / RemoveAt / Move / ... |
| `ProjectionNode<TSource, TResult>` | База для одноисточниковых проекций | Filter / Select |
| `KeyedProjectionNode<TKey, TSource, TResult>` | База для группирующих проекций | GroupBy |
| `MergeNode<T>` | Объединение двух источников (Concat) | `first.Merge(second)` |
| `SelectManyNode<TSource, TResult>` | Flattening вложенных коллекций (Concat) | `source.SelectMany(x => x.Items)` |
| `AggregateNode<TSource, TResult>` | Свёртка списка в одиночное значение | `ObserveCount` |
| `ValueChange<T>` | Описание изменения одиночного значения | `OldValue → NewValue` |
| `IObservableValue<T>` | Контракт реактивного значения | `Value` + `Changed` |
| `ObservableValue<T>` | Корневое реактивное значение | `Set` / `Update` |

### Поток изменений

    ObservableList<TSource>
            │
            ▼
    ProjectionNode<TSource, TResult>  ─── Filter / Select
            │
            ▼
    ObservableNode<TResult>           ─── сам является IObservableList<TResult>
            │
            ▼
       следующий узел / UI / ...

Агрегаты:

    IObservableList<T>
            │
            ▼
    AggregateNode<T, TResult>
            │
            ▼
    IObservableValue<TResult>

---

## 3. Публичный API

### 3.1 Иерархия `Change<T>`

Изменение — абстрактный базовый тип с конкретными подклассами. Каждый
подкласс несёт только те поля, которые для него осмысленны.

| Тип | Поля | Смысл |
|---|---|---|
| `AddChange<T>` | `Item`, `int Index` | Элемент добавлен на позицию |
| `RemoveChange<T>` | `Item`, `int Index` | Элемент удалён с позиции |
| `UpdateChange<T>` | `Item`, `int Index` | Поля объекта изменились, позиция та же |
| `ReplaceChange<T>` | `OldItem`, `NewItem`, `int Index` | Замена в той же позиции |
| `MoveChange<T>` | `Item`, `int FromIndex`, `int ToIndex` | Перемещение |
| `ResetChange<T>` | — | Содержимое изменилось целиком |
| `BatchChange<T>` | `IReadOnlyList<Change<T>> Changes` | Группа изменений |

**Индексы не nullable.** Изменения, адресующие конкретный occurrence,
несут координату в коллекции-источнике. Это факт о произошедшей операции,
а не подсказка для проекций. Проекция сама решает, как перевести эти
координаты в свои.

Семантика индексов:

- `AddChange.Index` — позиция, на которую встал элемент.
- `RemoveChange.Index` — позиция элемента **до удаления**.
- `UpdateChange.Index` — позиция элемента в состоянии на момент операции.
- `ReplaceChange.Index` — позиция замены.
- `MoveChange.FromIndex` — исходная позиция, `ToIndex` — конечная.

**Индексы в `BatchChange` относятся к состоянию коллекции на момент
соответствующей операции**, а не к финальному состоянию после всего
batch:

    // начальное состояние: [A, B, C]
    using (list.Batch())
    {
        list.RemoveAt(0);      // состояние: [B, C],      Index = 0
        list.AddAt(1, item);   // состояние: [B, item, C], Index = 1
        list.Move(1, 0);       // состояние: [item, B, C], From = 1, To = 0
    }

Пользовательский код не создаёт `Change` вручную — изменения создаёт
сама библиотека.

    public abstract class Change<T> { }

    public sealed class AddChange<T> : Change<T>
    {
        public T Item { get; }
        public int Index { get; }
    }

    public sealed class MoveChange<T> : Change<T>
    {
        public T Item { get; }
        public int FromIndex { get; }
        public int ToIndex { get; }
    }

    // ... и т.д.

**Использование — pattern matching:**

    node.Changed += change =>
    {
        switch (change)
        {
            case AddChange<Player> add:
                InsertRowAt(add.Index, add.Item);
                break;
            case MoveChange<Player> move:
                MoveRow(move.FromIndex, move.ToIndex);
                break;
            // ...
        }
    };

### 3.2 `IObservableList<T>`

    public interface IObservableList<T> : IReadOnlyList<T>, IDisposable
    {
        event Action<Change<T>>? Changed;
    }

### 3.3 `ObservableList<T>`

    var list = new ObservableList<Player>();

    // Без индекса
    list.Add(p);
    list.Remove(p);
    list.Update(p);          // для mutable-объектов: p изменился внутри
    list.Replace(p, q);
    list.Reset();

    // С индексом
    list.AddAt(1, p);
    list.RemoveAt(3);
    list.Move(0, 5);

    // Батчинг
    using (list.Batch())
    {
        list.Add(p1);
        list.Add(p2);
        list.AddAt(0, p0);
    }
    // одно событие: BatchChange([Add p1, Add p2, Add p0@0])

    // Или вручную:
    list.BeginUpdate();
    list.Add(p);
    list.EndUpdate();

### 3.4 Extension-методы

    var filtered = source.Filter(p => p.Level >= 10);

    var views = source.Select(
        p => new PlayerView(p),
        (p, view) => view.Refresh(p));

    var groups = source.GroupBy(p => p.TeamId);

    var merged = first.Merge(second);

    var flat = players.SelectMany(p => p.Items);

### 3.5 `ObservableValue<T>`

    var hp = new ObservableValue<int>(100);

    hp.Changed += c => Console.WriteLine($"HP: {c.OldValue} -> {c.NewValue}");

    hp.Set(80);
    hp.Set(80);      // Set райзит всегда

    // mutable-модель:
    var player = new ObservableValue<Player>(p);
    p.Hp = 50;
    player.Update();

### 3.6 Агрегаты

    var count = players.ObserveCount();

Имена начинаются с `Observe*`, чтобы явно отличать **живые реактивные
агрегаты** от одноразовых LINQ-агрегаций (`Count`, `Any`, `Sum` и т.д.).
Это делает намерение вызова очевидным.

| Живой метод | Аналог LINQ | Тип результата |
|---|---|---|
| `ObserveCount()` | `Count()` | `IObservableValue<int>` |
| `ObserveAny(pred)` | `Any(pred)` | `IObservableValue<bool>` |
| `ObserveAll(pred)` | `All(pred)` | `IObservableValue<bool>` |
| `ObserveSum(sel)` | `Sum(sel)` | `IObservableValue<TNumber>` |
| `ObserveMin(sel)` | `Min(sel)` | `IObservableValue<T?>` |
| `ObserveMax(sel)` | `Max(sel)` | `IObservableValue<T?>` |
| `ObserveAverage(sel)` | `Average(sel)` | `IObservableValue<TNumber?>` |

---

## 4. Ограничения и допущения

### 4.1 Индексная семантика в проекциях

Каждый узел-проекция транслирует индексы из `Change<T>` в свои координаты:

- **`SelectNode`** — 1:1. Индексы в результате совпадают с индексами
  источника.
- **`FilterNode`** — пересчёт через предикат: `_sourceToFilter` хранит
  для каждого source-индекса позицию в filter (или −1).
- **`MergeNode`** — Concat: индекс = индекс в источнике + смещение.
  Для второго источника смещение = количество элементов первого.
- **`KeyedProjectionNode`** — индекс внутри группы вычисляется по
  количеству элементов той же группы перед данным occurrence в source.
- **`SelectManyNode`** — индекс в `flat` вычисляется как смещение блока,
  равное сумме размеров предыдущих inner-коллекций, плюс индекс внутри
  текущей inner-коллекции.

`Equals` не используется для определения того, какой occurrence изменился.
Только `Change.Index` / `FromIndex` / `ToIndex`.

### 4.2 Сложность операций

Текущая реализация основана на `List<T>` и линейных индексных маппингах.

Операции без изменения структуры результата часто выполняются за O(1).
Вставка, удаление, перемещение и пересчёт результирующего индекса
могут иметь O(N) сложность.

Для блочных проекций дополнительная стоимость зависит от количества
элементов или блоков перед изменяемой позицией.

Это осознанный компромисс: ясность кода важнее микрооптимизации.
Оптимизация — в дорожной карте.

### 4.3 `Remove` удаляет первый равный

`ObservableList.Remove(item)` делегирует в `List<T>.Remove(item)`, который
**детерминированно** удаляет **первый** элемент, равный `item` по `Equals`.
Если в списке несколько таких элементов — уйдёт именно первый. Для
удаления конкретного occurrence используйте `RemoveAt(index)`.

В проекциях `RemoveChange` от источника несёт индекс, и проекция удаляет
элемент по этому индексу. Это делает удаление детерминированным даже
при дубликатах по `Equals`.

### 4.4 `Update` не несёт старое значение

`UpdateChange<T>.Item` — текущая ссылка. Для mutable-моделей это
нормально: пользователь меняет поле и вызывает `Update(p)` — подписчики
видят тот же экземпляр.

Для immutable `T` `Update` бесполезен — старое значение потеряно.
Используйте `Replace`.

### 4.5 `IDisposable` и жизненный цикл узлов

`IObservableList<T>` реализует `IDisposable`. Модель — **Dispose только себя**:
узел отписывается от своих источников, но не трогает их.

- `Dispose` идемпотентен.
- После `Dispose` мутации (`Add`, `Remove`, `Update`, `Move`, `Reset`,
  `BeginUpdate` и т.д.) бросают `ObjectDisposedException`.
- Чтение (`Count`, индексатор, `GetEnumerator`) после `Dispose` разрешено.
- `Dispose` на `ObservableList<T>` помечает его как освобождённый
  и сбрасывает буфер активного батча (если он был открыт).
- Узлы **не** вызывают `Dispose` на своих источниках. Удаление цепочки —
  ответственность вызывающего.

Пример удаления цепочки:

    var source = new ObservableList<Player>();
    var filter = source.Filter(p => p.Level >= 10);
    var select = filter.Select(p => new PlayerView(p), (p, v) => v.Refresh(p));

    select.Dispose();
    filter.Dispose();
    // source остаётся живым

### 4.6 Батчинг не каскадируется

`BeginUpdate` / `EndUpdate` / `Batch()` накапливают изменения на **источнике**
и райзят один `BatchChange`. Узлы (Filter, Select, GroupBy, Merge,
SelectMany) **разворачивают** `BatchChange` в отдельные события для своих
подписчиков — батч не пробрасывается насквозь.

Вложенные батчи запрещены: `BeginUpdate` внутри открытого батча бросает
`InvalidOperationException`.

### 4.7 `Merge` — Concat, не Union

`first.Merge(second)` даёт список: сначала все элементы `first`, затем
все элементы `second`. Дубликаты сохраняются. Порядок элементов внутри
каждого источника сохраняется.

Из этого следует: изменения `first` меняют только первый блок,
изменения `second` — только второй блок. Произвольного перемешивания
элементов двух источников нет.

### 4.8 `SelectMany` — Concat вложенных коллекций

`flat = inner[0] + inner[1] + ... + inner[N-1]`. Каждая вложенная
коллекция образует непрерывный блок.

**Требование:** каждый элемент источника должен возвращать собственную
внутреннюю коллекцию. Shared inner-коллекции не входят в поддерживаемый
контракт текущей реализации: одна физическая inner-коллекция будет иметь
несколько подписок, а одно её изменение будет обрабатываться несколькими
`Subscription`. Такое поведение сейчас не специфицировано.

### 4.9 Поток выполнения

Библиотека **не потокобезопасна**. Все изменения — в одном потоке.

### 4.10 Исключения в подписчиках

`Raise` вызывает всех подписчиков, собирая исключения. Одиночное
исключение пробрасывается как есть, несколько — как `AggregateException`.
Это отличается от «сырого» multicast delegate, где первое исключение
прерывает обход.

### 4.11 Синхронная доставка событий

`Raise` доставляет события **синхронно и вложенно**. Если узел подписан
на другой узел, обработка downstream-узлов происходит непосредственно
во время вызова обработчика upstream. Поэтому подписчик конечного узла
может получить событие раньше, чем последующие подписчики
промежуточного узла.

Пример вложенной доставки:

    source.Add(x)
      → filter.Changed
        → select.Changed
          → groups.Changed      ← сработает первым
        → (остальные подписчики filter.Changed)

Порядок **между подписчиками одного и того же события** определяется
порядком их подписки.

### 4.12 Особенности отдельных узлов

**`KeyedProjectionNode`: переезд между группами**

При `Update`/`Replace` со сменой ключа элемент сначала удаляется из
старой группы, затем добавляется в новую. Между этими двумя операциями
элемент **отсутствует в обеих группах**. Подписчики `group.Items.Changed`
могут это заметить.

**`SelectManyNode`: `Reset` вложенной коллекции**

Для текущей реализации `SelectManyNode` не транслирует inner
`ResetChange` напрямую. Он заменяет соответствующий блок серией
`RemoveChange` и `AddChange`. Это решение самого `SelectManyNode`,
а не общий закон для `Reset`.

**`ResetInternal` на пустой коллекции не райзит событие**

`ObservableNode.ResetInternal` возвращает `false` и не публикует
`ResetChange`, если коллекция уже пуста. Это публичная семантика:
`Reset` на пустой коллекции неотличим от «ничего не произошло» — ни для
источника, ни для узлов.

### 4.13 Целевые платформы

`netstandard2.0`. Поддерживаются платформы, совместимые с .NET Standard
2.0, включая .NET Framework 4.6.2+, .NET Core, .NET 5+ и Unity
с поддержкой .NET Standard 2.0.

---

## 5. Сценарии использования

### 5.1 Player → PlayerView → Group

    var views = players
        .Filter(p => p.Level >= 10)
        .Select(
            p => new PlayerView(p),
            (p, view) => view.Refresh(p))
        .GroupBy(view => view.TeamId);

### 5.2 Живая группа с ссылкой на вложенный список

    var groups = results
        .Filter(r => r.SessionVM.SchemeIndex >= 0)
        .GroupBy(r => r.SessionVM.SchemeIndex);

    var dynamics = groups.Select(
        g => new Dynamics(g.Items),
        (_, _) => { });

    // Dynamics получает ту же живую коллекцию, что и g.Items.
    // Изменения g.Items видны Dynamics без пересоздания объекта.

### 5.3 Наследование через `Select` без потери ссылки

    public class DerivedResult : ResultBase { }

    var bases = results.Select(
        r => (ResultBase)r,
        (_, _) => { });

    // bases[0] и results[0] — один и тот же объект.

### 5.4 Flattening вложенных коллекций

    var source = new ObservableList<Result>();
    // у каждого Result есть свой Items — ObservableList<Item>

    var flat = source.SelectMany(r => r.Items);
    // flat = [все элементы всех Results в порядке source]

    source.Add(newResult);        // блок нового Result добавляется
    result1.Items.Add(item);       // элемент добавляется в блок result1
    source.Remove(result2);        // блок result2 удаляется

### 5.5 Concat двух источников

    var a = new ObservableList<Player>();
    var b = new ObservableList<Player>();

    var merged = a.Merge(b);
    // merged = [элементы a, затем элементы b]

    a.Add(p);   // p встаёт в конец блока a, перед элементами b

### 5.6 Живое количество элементов

    var count = players.ObserveCount();

    count.Changed += c => label.text = $"Игроков: {c.NewValue}";

    players.Add(newPlayer);              // событие: 0 -> 1
    players.Replace(newPlayer, other);   // событий count нет — количество не изменилось
    players.Update(other);               // событий count нет — количество не изменилось

### 5.7 Батчинг

    using (players.Batch())
    {
        foreach (var p in loaded)
            players.Add(p);
    }
    // 1000 добавлений — одно событие BatchChange

---

## 6. История решений

### Почему иерархия `Change<T>`, а не плоский DTO

Плоская модель с `Type`, `Item`, `OldItem`, `Changes` перегружена:
поля осмысленны только для части типов. С индексами и новыми
изменениями (`Move`, `Batch`) количество nullable-полей росло бы дальше.

Иерархия делает явным: `AddChange` не имеет `OldItem`, `ResetChange`
не имеет индексов, `MoveChange` имеет `FromIndex` и `ToIndex`. Pattern
matching в C# даёт типобезопасную диспетчеризацию. Побочный эффект —
проблема `OldItem` для value-types (`default(T)` vs «не заполнено»)
исчезла: `ReplaceChange<T>.OldItem` теперь просто `T`.

### Почему индекс non-nullable

`Add/Remove/Update/Replace/Move` несут координату в источнике.
Это **факт о произошедшей операции**, а не подсказка для проекций.
Источник знает индекс, проекция может его проигнорировать или
транслировать. Если бы индекс был nullable, информация терялась бы
на уровне контракта.

### Почему `Move` — отдельный тип

`Remove + Add` представляет операцию двумя независимыми изменениями.
Downstream-узел видит промежуточное состояние, а проекция обычно
удаляет существующий результат и создаёт новый.

`Move` — один атомарный факт: элемент изменил позицию, оставаясь тем же.
`SelectNode` сохраняет существующий `TResult` — не пересоздаёт
`PlayerView`. `FilterNode` пересчитывает индексы в своём пространстве.

### Почему Concat для `Merge`

Единственная модель, в которой индексы транслируются предсказуемо:
позиция в merged = позиция в источнике + смещение. Старая модель
«добавляем в конец по мере прихода» давала корректное состояние,
но индексы из источника не имели смысла в merged.

### Почему Concat блоков для `SelectMany`

`flat = inner[0] + inner[1] + ...`. Позволяет единообразно вычислять
позицию блока: сумма Count предыдущих inner. При произвольном
перемешивании пришлось бы хранить явный маппинг каждого элемента.

### Почему `UpdateChange.Index` появился

Без индекса при дубликатах по `Equals` невозможно определить,
какой именно экземпляр обновился. `Items.IndexOf(item)` находит
первый равный, что в проекциях даёт неверное сопоставление
source occurrence → projection entry.

### Почему `List<Entry>` вместо `Dictionary` для source → result

Изначально `SelectNode` и отображение элементов в `KeyedProjectionNode`
использовали словари для сопоставления **элемента источника с результатом**.
Это ломается на дубликатах по `Equals`: второй элемент перезатирает первый,
`Remove` удаляет не тот `TResult`.

Параллельный `List<Entry>` решает задачу иначе: **Entry с индексом `i`
соответствует occurrence источника с индексом `i`**. Изменение получает
индекс непосредственно из `Change<T>`. `Equals` не используется для
определения того, какой occurrence изменился.

При этом `KeyedProjectionNode` сохраняет `Dictionary<TKey, TResult>`
для другого отношения: **`key → group`**. Оно не заменяет positional
mapping `source occurrence → group`, а дополняет его.

### Почему `Group.Items` публичный

`Group<TKey, T>` — самостоятельный реактивный источник. Пользователь
может подписаться на `group.Items.Changed` и не тянуть всё через
один канал.

### Почему `Raise` не останавливается на первом исключении

Multicast delegate по умолчанию прерывает обход при первом исключении.
В реактивной цепочке это означало бы, что упавший подписчик отрезает
остальных. `Raise` собирает исключения и вызывает всех.

### Почему `Observe*` для агрегатов

Живой реактивный агрегат (`ObserveCount`) и одноразовая LINQ-агрегация
(`Count`) семантически разные операции. Префикс `Observe*` делает
намерение вызова очевидным, не связываясь с правилами разрешения
extension-методов или потенциальными конфликтами имён.

### Почему батчинг не каскадируется

Каскадный батчинг требует координации между узлами и меняет их
внутреннюю модель: каждый узел должен уметь «накопить и разом
райзнуть». Простая модель — батч только на источнике — позволяет
получить существенную часть выгоды при значительно меньшей сложности
реализации и не ломает существующую архитектуру.

---

## 7. Дорожная карта

Текущий этап — рефакторинг на иерархию `Change<T>` и индексную
семантику завершён.

Дальше — по приоритету:

1. **Оптимизация индексных операций в проекциях** — если появится
   реальная потребность в частых `Move`/`AddAt`. Конкретный выбор
   структуры данных (дерево префиксных сумм, segment tree и т.п.)
   будет сделан под нагрузку.
2. **`OrderBy`** — построение сортированного реактивного узла на базе
   новой индексной модели.
3. **Каскадный батчинг** — если станет ясно, что простой модели
   не хватает.
4. **Дополнительные агрегаты и комбинации значений** — `ObservableValue`
   готов к расширению.

---

## 8. Изменения

История изменений ведётся через git-коммиты. Раздел будет заменён
на `CHANGELOG.md` при выходе за пределы активной разработки.