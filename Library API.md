# Developer API

Справочник публичного API библиотеки `escontroller`. Описывает реализацию в ветке `main`.

Пространство имён основных типов: `Core`. Модуль проверки земли находится в `Core.CoreModules`.

> API экспериментальный и может изменяться. Примеры ниже показывают текущие имена типов и методов в исходном коде.

# Core

## `CoreController`

`CoreController` — центральный компонент ядра персонажа (`MonoBehaviour`). Он предоставляет общие данные и API для подключаемых модулей.

### Свойства

| API | Описание |
|---|---|
| `Rigidbody CoreRigidbody { get; }` | Ссылка на Rigidbody персонажа. |
| `IReadOnlyList<MonoBehaviour> CoreModules { get; }` | Список компонентов, назначенных в качестве кандидатов в Core Modules. |
| `IReadOnlyCollection<ActionId> CurrentActions { get; }` | Действия, присутствующие в реестре Action Registry. |

`CoreRigidbody` назначается через Inspector. Если поле не назначено, `CoreController` ищет `Rigidbody` на том же GameObject в `Awake()`. Если компонент не найден, ядро выводит ошибку и отключает себя.

`CoreModules` предоставляет список через интерфейс только для чтения. Фактический список сериализуется в поле `coreModules` и настраивается через Inspector или методы управления модулями.

# Core Modules

## `ICoreModule`

Контракт подключаемого модуля:

```csharp
namespace Core
{
    public interface ICoreModule
    {
        void Initialize(CoreController core);
    }
}
```

Каждый Core Module должен быть компонентом `MonoBehaviour` и реализовывать `ICoreModule`, чтобы его можно было назначить в список `CoreController`.

### Жизненный цикл

- Модули из Inspector проверяются в `OnValidate()`.
- Компоненты, не реализующие `ICoreModule`, и пустые ссылки удаляются из сериализованного списка при валидации.
- При изменении набора проверенных модулей `CoreController` синхронизирует инициализированный набор в `Update()`.
- `AddCoreModule(MonoBehaviour module)` добавляет компонент-кандидат, проверяет интерфейс и запускает синхронизацию.
- `RemoveCoreModule(ICoreModule module)` удаляет модуль из набора и запускает синхронизацию.
- При подключении вызывается `module.Initialize(this)`.
- При отключении вызывается `module.Initialize(null)`.

**Важно для реализации собственных модулей:** метод `Initialize` должен корректно обрабатывать `null`. Это сигнал об отключении модуля; в этот момент модуль должен освободить или сбросить ссылку на ядро и связанные с ним состояния.

### Методы управления

```csharp
public void AddCoreModule(MonoBehaviour module);
public void RemoveCoreModule(ICoreModule module);
```

| Метод | Поведение |
|---|---|
| `AddCoreModule` | Игнорирует `null` и компонент, уже присутствующий в исходном списке. Иначе добавляет его, проверяет реализацию `ICoreModule` и синхронизирует инициализацию. |
| `RemoveCoreModule` | Удаляет модуль из проверенного набора и исходного списка, затем синхронизирует инициализацию. |

В Inspector список `coreModules` является исходным набором кандидатов. Фактически подключёнными становятся только компоненты, реализующие `ICoreModule`.

# Built-in Core Modules

## `CheckGround`

Пространство имён: `Core.CoreModules`.

`CheckGround` — компонент `MonoBehaviour`, реализующий `ICoreModule`. Он определяет, касается ли область под персонажем объектов, входящих в заданную маску слоёв.

### Публичный API

```csharp
public bool IsGrounded { get; private set; }
public void Initialize(CoreController core);
```

- `IsGrounded == true` — сфера проверки пересекается с объектом из `groundCheckMask`.
- `IsGrounded == false` — пересечения нет.

Значение обновляется в `FixedUpdate()` с помощью `Physics.CheckSphere()`. Триггеры игнорируются через `QueryTriggerInteraction.Ignore`.

### Настройки Inspector

| Поле | Описание |
|---|---|
| `groundCheckRadius` | Радиус сферы проверки. |
| `groundCheckVerticalOffset` | Вертикальное смещение центра сферы относительно рассчитанной позиции. |
| `groundCheckMask` | Маска слоёв, участвующих в проверке. |

Центр сферы рассчитывается относительно позиции Rigidbody:

```csharp
var origin = _rigidbody.position
    + Vector3.up * groundCheckRadius
    + Vector3.up * groundCheckVerticalOffset;
```

Если `groundCheckMask` равен нулю, компонент выводит ошибку и отключается. Rigidbody назначается из `core.CoreRigidbody` во время `Initialize`.

# Action API

## `ActionId`

```csharp
public readonly struct ActionId : IEquatable<ActionId>
{
    public string Value { get; }
    public ActionId(string value);
}
```

Типизированный идентификатор действия. Используется в Action Permission и Action Registry вместо передачи строк в качестве ключей.

Конструктор выбрасывает `ArgumentException`, если значение равно `null` или пустой строке. Сравнение идентификаторов регистрозависимое. Реализованы `Equals`, `GetHashCode()`, операторы `==` и `!=`.

## `MovementActions`

Статический каталог встроенных идентификаторов перемещения:

| Поле | Значение |
|---|---|
| `MovementActions.Move` | `Movement.Move` |
| `MovementActions.Run` | `Movement.Run` |
| `MovementActions.Jump` | `Movement.Jump` |

Файл: `Assets/Scripts/Core/ActionContracts.cs`.

# Action Permission

Система блокировок отвечает на вопрос: разрешено ли сейчас выполнять действие? Каждое действие может иметь несколько независимых блокировок, принадлежащих разным системам.

### Методы

```csharp
public bool CanPerform(ActionId action);
public void BlockAction(ActionId action, string blockerId);
public void UnblockAction(ActionId action, string blockerId);
```

| Метод | Поведение |
|---|---|
| `CanPerform(action)` | Возвращает `true`, если для действия нет блокировок; иначе `false`. |
| `BlockAction(action, blockerId)` | Добавляет блокировку от указанного источника. |
| `UnblockAction(action, blockerId)` | Снимает блокировку, принадлежащую указанному источнику. |

`blockerId` не должен быть `null`, пустым или состоять из пробелов. Такие значения не добавляются и вызывают предупреждение в Console.

Блокировки хранятся отдельно для каждого действия. Один и тот же `blockerId` не дублируется. Снятие блокировки одного источника не затрагивает блокировки других источников. Действие снова становится доступным после снятия последней блокировки.

### Пример

```csharp
core.BlockAction(MovementActions.Run, "Stamina");
core.BlockAction(MovementActions.Run, "Injury");

// Убираем только ограничение выносливости.
// Run всё ещё заблокирован источником Injury.
core.UnblockAction(MovementActions.Run, "Stamina");

bool canRun = core.CanPerform(MovementActions.Run); // false

core.UnblockAction(MovementActions.Run, "Injury");
canRun = core.CanPerform(MovementActions.Run); // true
```

Система хранит только состояние разрешений. Она не останавливает уже выполняющееся действие и не запускает его автоматически. Код, который выполняет действие, должен проверять `CanPerform()` в подходящий момент.

# Action Registry

Action Registry хранит действия, зарегистрированные одним или несколькими авторами. Для одного `ActionId` может быть несколько `authorId`.

### Методы

```csharp
public IReadOnlyCollection<ActionId> CurrentActions { get; }

public void AddAction(ActionId actionId, string authorId);
public void RemoveAction(ActionId actionId, string authorId);
```

| Метод | Поведение |
|---|---|
| `AddAction(actionId, authorId)` | Регистрирует действие от имени автора. |
| `RemoveAction(actionId, authorId)` | Удаляет регистрацию указанного автора. |

`authorId` при добавлении не должен быть `null`, пустым или состоять из пробелов. Повторная регистрация того же автора для того же действия не создаёт дубликат.

Если несколько авторов зарегистрировали один `ActionId`, удаление одного автора не удаляет само действие. Ключ действия удаляется из реестра, когда список его авторов становится пустым. `CurrentActions` возвращает ключи, которые в данный момент присутствуют в реестре.

### Пример

```csharp
core.AddAction(MovementActions.Move, "KeyboardInput");
core.AddAction(MovementActions.Move, "GamepadInput");

// Действие Move остаётся зарегистрированным GamepadInput.
core.RemoveAction(MovementActions.Move, "KeyboardInput");

bool isRegistered = core.CurrentActions.Contains(MovementActions.Move); // true

core.RemoveAction(MovementActions.Move, "GamepadInput");
// Теперь Move отсутствует в CurrentActions.
```

# Примечания по текущей реализации

- `CoreController` требует `Rigidbody` на том же GameObject, если ссылка не назначена в Inspector.
- `CheckGround` — отдельный Core Module, а не часть `CoreController`.
- Action Permission и Action Registry — независимые подсистемы: первая управляет доступностью действия, вторая — наличием регистраций действия.
- Core Modules, назначенные через Inspector, проходят валидацию в редакторе. Runtime-добавление выполняется через `AddCoreModule`.
- При отключении модуля ядро вызывает `Initialize(null)`. Реализация модуля обязана учитывать этот вызов и не обращаться к свойствам `core` без проверки на `null`.
