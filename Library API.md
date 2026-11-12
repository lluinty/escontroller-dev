# Library API

Справочник публичного API библиотеки `escontroller`. Документ сверён с исходным кодом ветки `main` на коммите [`64a6589`](https://github.com/lluinty/escontroller-dev/commit/64a6589363e6de6fe545bb61c33dc21e3e7dd617) от 12 ноября 2026 года.

> API находится в активной разработке. При расхождении документации и реализации источником истины для фактических сигнатур и поведения являются исходники.

## Содержание

- [Core](#core)
  - [CoreController](#corecontroller)
  - [ICoreModule](#icoremodule)
  - [CoreModule](#coremodule)
  - [ActionId](#actionid)
  - [MovementActions](#movementactions)
- [CoreModules](#coremodules)
  - [CheckGround](#checkground)
  - [ActionsController](#actionscontroller)
  - [ActionsPermissionsController](#actionspermissionscontroller)
- [GameModules](#gamemodules)
- [Общие замечания](#общие-замечания)

---

## Core

Фундаментальные типы библиотеки. `Core` предоставляет общий компонент персонажа, контракт и базовый класс подключаемых модулей, а также общий тип идентификатора действия.

### CoreController

`CoreController` — центральный компонент ядра персонажа. Он предоставляет общие зависимости и управляет жизненным циклом подключённых `CoreModule`. Реализации Action Registry и Action Permission находятся в отдельных модулях раздела [CoreModules](#coremodules).

**Исходный файл:** [`Assets/Scripts/Core/CoreController.cs`](Assets/Scripts/Core/CoreController.cs)  
**Namespace:** `Core`  
**Тип:** `public class CoreController : MonoBehaviour`  
**Зависимости:** компонент `Rigidbody` на том же GameObject либо назначенная ссылка на него в Inspector.

#### Назначение и ответственность

- Предоставляет ссылку на физическое тело через `CoreRigidbody`.
- Хранит список подключённых `CoreModule`.
- Отдельно учитывает модули, успешно прошедшие инициализацию.
- Вызывает `Initialize(this)` при подключении модуля и `Deinitialize()` при отключении.
- Обрабатывает ошибки инициализации и деинициализации модулей, не прерывая обработку остальных модулей.
- Откладывает запросы на удаление модуля, если они поступили во время прохода инициализации.

#### Жизненный цикл

1. В `Awake()` вызывается `GetComponents()`.
2. Если поле `rigidbody` не назначено, ядро ищет `Rigidbody` на том же GameObject через `GetComponent<Rigidbody>()`.
3. Если Rigidbody не найден, выводится ошибка, `CoreController` отключается (`enabled = false`), а модули не инициализируются.
4. После успешного получения Rigidbody вызывается `InitializeCoreModules()`.
5. При успешной инициализации модуль добавляется в `InitializedCoreModules`. Если `Initialize()` вернул `false` или выбросил исключение, модуль не считается инициализированным: ядро выводит сообщение и запрашивает его деинициализацию.
6. `AddCoreModule()` и `RemoveCoreModule()` запускают повторную синхронизацию после изменения списка.
7. Если модуль запрашивает собственное удаление во время инициализации, запрос помещается в очередь и обрабатывается после текущего прохода.

`InitializeCoreModules()` не вызывается автоматически из `Update()` или `OnValidate()` в текущей реализации.

#### Public API

##### `CoreRigidbody`

**Тип:** `Rigidbody`  
**Доступ:** только чтение.

Ссылка на физическое тело персонажа. Может быть назначена через Inspector. Если ссылка не назначена, ядро ищет компонент `Rigidbody` на том же GameObject в `Awake()`.

**Пример:**

```csharp
Rigidbody body = core.CoreRigidbody;
body.AddForce(Vector3.up * 5f, ForceMode.Impulse);
```

**Ограничения:** если Rigidbody отсутствует, `CoreController` отключается и не выполняет инициализацию модулей. Примеры используют API `Rigidbody` Unity 6.

##### `CoreModules`

**Тип:** `IReadOnlyCollection<CoreModule>`  
**Доступ:** только чтение через объявленный интерфейс.

Коллекция модулей, присутствующих в текущем списке `coreModules`. Она не означает, что каждый элемент уже успешно инициализирован: для этого предназначено свойство `InitializedCoreModules`.

Свойство возвращает read-only представление внутреннего списка. Это не отдельная копия: изменения списка ядром отражаются в доступной коллекции.

**Пример:**

```csharp
foreach (CoreModule module in core.CoreModules)
{
    Debug.Log(module.name);
}
```

##### `InitializedCoreModules`

**Тип:** `IReadOnlyCollection<CoreModule>`  
**Доступ:** только чтение через объявленный интерфейс.

Коллекция модулей, для которых `Initialize(this)` завершился успешно и которые были добавлены во внутренний список `_initializedCoreModules`.

Модуль, чья инициализация вернула `false` или завершилась исключением, в эту коллекцию не попадает.

**Пример:**

```csharp
foreach (CoreModule module in core.InitializedCoreModules)
{
    Debug.Log($"Initialized: {module.name}");
}
```

##### `AddCoreModule(CoreModule module)`

**Сигнатура:**

```csharp
public void AddCoreModule(CoreModule module)
```

Добавляет модуль в список `coreModules` и запускает синхронизацию инициализации.

**Параметры:**

| Параметр | Тип | Описание и ограничения |
|---|---|---|
| `module` | `CoreModule` | Компонент-модуль. `null` и уже присутствующий в списке экземпляр отклоняются. |

**Возвращает:** ничего (`void`).

**Пример:**

```csharp
[SerializeField] private CoreController core;
[SerializeField] private CheckGround checkGround;

private void Start()
{
    core.AddCoreModule(checkGround);
}
```

**Поведение и ограничения:**

- Метод принимает `CoreModule`, а не произвольный `MonoBehaviour` или объект, который только реализует `ICoreModule`.
- Если передан `null` или модуль уже находится в `coreModules`, выводится предупреждение.
- Если вызов происходит во время инициализации модулей (`_isInitializing == true`), добавление отклоняется и выводится предупреждение.
- При успешном добавлении вызывается `InitializeCoreModules()`. Если `Initialize()` модуля не удаётся, ядро пытается вызвать `Deinitialize()` и удаляет модуль из списка.
- Метод не возвращает признак успешного добавления. При необходимости результат можно проверить через `CoreModules` и `InitializedCoreModules`.

##### `RemoveCoreModule(CoreModule module)`

**Сигнатура:**

```csharp
public bool RemoveCoreModule(CoreModule module)
```

Удаляет модуль из списка `coreModules` и синхронизирует состояние инициализированных модулей.

**Параметры:**

| Параметр | Тип | Описание и ограничения |
|---|---|---|
| `module` | `CoreModule` | Модуль, который требуется отключить. |

**Возвращает:**

- `true` — запрос на удаление принят;
- `false` — передан `null` либо модуль не найден в `coreModules`.

**Пример:**

```csharp
bool removed = core.RemoveCoreModule(checkGround);
```

**Поведение и ограничения:**

- Если модуль равен `null` или отсутствует в `coreModules`, метод выводит предупреждение и возвращает `false`.
- Если метод вызван вне процесса инициализации, модуль сразу удаляется из `coreModules`, после чего вызывается `InitializeCoreModules()`. Инициализированный модуль при синхронизации получает вызов `Deinitialize()`.
- Если метод вызван во время инициализации, модуль добавляется в очередь `_modulesToRemoveAfterInitialization`. Метод возвращает `true`, но фактическое удаление выполняется после текущего прохода.
- Повторный запрос на удаление того же модуля во время инициализации не создаёт дубликат в очереди.
- Метод не уничтожает компонент Unity и не вызывает `Destroy()`.

#### Обработка ошибок и реентерабельность

- Исключение из `Initialize()` перехватывается ядром. Оно логируется, после чего модуль считается не прошедшим инициализацию.
- Если `Initialize()` возвращает `false` или выбрасывает исключение, ядро дополнительно пытается вызвать `Deinitialize()` для очистки частично установленного состояния.
- Исключение из `Deinitialize()` перехватывается и логируется.
- Повторный вход в `InitializeCoreModules()` во время уже выполняющейся синхронизации игнорируется.
- Вызов `AddCoreModule()` в процессе инициализации отклоняется; `RemoveCoreModule()` в этот момент ставит запрос в очередь.

**Практическое требование к модулю:** `Deinitialize()` должен быть безопасен и в случае частично выполненной инициализации, поскольку ядро вызывает его после неудачного `Initialize()`.

**Tags:** `#core #controller #module-lifecycle`

### ICoreModule

`ICoreModule` — контракт подключаемого модуля ядра.

**Исходный файл:** [`Assets/Scripts/Core/ICoreModule.cs`](Assets/Scripts/Core/ICoreModule.cs)  
**Namespace:** `Core`  
**Тип:** `public interface ICoreModule`

В текущей реализации `CoreController` хранит модули как `CoreModule` и принимает их через `AddCoreModule(CoreModule)`. Поэтому для подключения через штатный API недостаточно реализовать один `ICoreModule`: компонент должен наследоваться от `CoreModule`.

#### Public API

##### `Initialize(CoreController core)`

**Сигнатура:**

```csharp
bool Initialize(CoreController core);
```

Получает ссылку на ядро и выполняет подготовку модуля к работе.

**Параметры:**

| Параметр | Тип | Описание |
|---|---|---|
| `core` | `CoreController` | Ядро, подключающее модуль. При штатном вызове со стороны `CoreController` передаётся `this`. |

**Возвращает:**

- `true` — модуль успешно инициализирован;
- `false` — модуль не смог инициализироваться.

**Пример:**

```csharp
public override bool Initialize(CoreController core)
{
    if (!core)
        return false;

    _core = core;
    return true;
}
```

Если метод вернул `false`, `CoreController` не добавляет модуль в `InitializedCoreModules`, пытается вызвать `Deinitialize()` и удаляет его из `CoreModules`.

##### `Deinitialize()`

**Сигнатура:**

```csharp
void Deinitialize();
```

Отключает модуль и освобождает ссылки и состояние, установленные при инициализации.

**Параметры:** отсутствуют.  
**Возвращает:** ничего (`void`).

**Пример:**

```csharp
public override void Deinitialize()
{
    _core = null;
    _isInitialized = false;
}
```

Реализация должна допускать повторный и безопасный вызов, в том числе после неудачного или частично завершённого `Initialize()`.

**Tags:** `#core #module-contract #initialization`

### CoreModule

`CoreModule` — абстрактный базовый класс компонентов, подключаемых к `CoreController`.

**Исходный файл:** [`Assets/Scripts/Core/CoreModule.cs`](Assets/Scripts/Core/CoreModule.cs)  
**Namespace:** `Core`  
**Тип:** `public abstract class CoreModule : MonoBehaviour, ICoreModule`

Наследование от `CoreModule` одновременно делает компонент `MonoBehaviour` и реализует контракт `ICoreModule`. Именно этот базовый тип используется в `coreModules`, `InitializedCoreModules`, `AddCoreModule()` и `RemoveCoreModule()`.

#### Public API

Класс объявляет два абстрактных метода, которые обязан реализовать каждый конкретный модуль:

```csharp
public abstract bool Initialize(CoreController core);
public abstract void Deinitialize();
```

**Пример реализации:**

```csharp
using UnityEngine;

namespace Core.CoreModules
{
    public sealed class ExampleModule : CoreModule
    {
        private CoreController _core;

        public override bool Initialize(CoreController core)
        {
            if (!core)
                return false;

            _core = core;
            return true;
        }

        public override void Deinitialize()
        {
            _core = null;
        }
    }
}
```

**Примечания:**

- `CoreModule` не реализует конкретную игровую логику.
- Модуль самостоятельно проверяет зависимости, необходимые для его работы.
- Возвращаемое значение `Initialize()` сообщает ядру, удалось ли подключить модуль.

**Tags:** `#core #module-base`

### ActionId

`ActionId` — типизированный идентификатор действия. Используется в модулях регистрации действий и управления разрешениями вместо строковых ключей.

**Исходный файл:** [`Assets/Scripts/Core/ActionId.cs`](Assets/Scripts/Core/ActionId.cs)  
**Namespace:** `Core`  
**Тип:** `public readonly struct ActionId : IEquatable<ActionId>`

#### Public API

##### `ActionId(string value)`

**Сигнатура:**

```csharp
public ActionId(string value)
```

Создаёт идентификатор действия из строки.

| Параметр | Тип | Описание и ограничения |
|---|---|---|
| `value` | `string` | Не может быть `null` или пустой строкой. |

**Возвращает:** новый `ActionId`.

**Примеры:**

```csharp
var interactAction = new ActionId("Interaction.Use");
```

```csharp
private static readonly ActionId OpenInventory =
    new ActionId("Inventory.Open");
```

**Исключения:** `ArgumentException`, если `value` равен `null` или пустой строке.

Строки, состоящие только из пробелов, конструктор принимает.

##### `Value`

**Тип:** `string`  
**Доступ:** только чтение.

Исходное строковое значение идентификатора.

```csharp
var action = new ActionId("Inventory.Open");
Debug.Log(action.Value);
```

##### `IsValid`

**Тип:** `bool`  
**Доступ:** только чтение.

Возвращает `true`, если `Value` не равен `null` и не является пустой строкой. Строки из одних пробелов считаются валидными.

```csharp
ActionId uninitialized = default;
bool isValid = uninitialized.IsValid; // false
```

Поскольку `ActionId` является структурой, значение `default(ActionId)` возможно, несмотря на проверки в конструкторе. Перед передачей идентификатора в API регистрации или блокировки действий проверяйте `IsValid`.

##### `Equals(ActionId other)`

**Сигнатура:** `public bool Equals(ActionId other)`

Сравнивает два идентификатора по строковому значению `Value`.

```csharp
var first = new ActionId("Movement.Jump");
var second = new ActionId("Movement.Jump");
bool equal = first.Equals(second); // true
```

##### `Equals(object obj)`

**Сигнатура:** `public override bool Equals(object obj)`

Возвращает `true`, если объект является `ActionId` с тем же значением. Для объекта другого типа возвращает `false`.

##### `GetHashCode()`

**Сигнатура:** `public override int GetHashCode()`

Возвращает хеш-код строкового значения `Value`. Согласован с `Equals()` и позволяет использовать `ActionId` в `Dictionary` и `HashSet`.

##### Операторы `==` и `!=`

**Сигнатуры:**

```csharp
public static bool operator ==(ActionId left, ActionId right);
public static bool operator !=(ActionId left, ActionId right);
```

Сравнивают идентификаторы по `Value` и согласованы с `Equals()`.

**Общие примечания:** сравнение строк регистрозависимое. `Movement.Move` и `movement.move` являются разными идентификаторами.

**Tags:** `#core #identifier #actions`

### MovementActions

`MovementActions` — статический каталог встроенных идентификаторов действий перемещения.

**Исходный файл:** [`Assets/Scripts/Core/ActionContracts.cs`](Assets/Scripts/Core/ActionContracts.cs)  
**Namespace:** `Core`  
**Тип:** `public static class MovementActions`

Все поля имеют тип `ActionId` и объявлены как `public static readonly`.

| Поле | Значение `ActionId.Value` | Назначение |
|---|---|---|
| `Move` | `Movement.Move` | Перемещение. |
| `Run` | `Movement.Run` | Бег. |
| `Jump` | `Movement.Jump` | Прыжок. |

**Примеры:**

```csharp
permissions.BlockAction(MovementActions.Run, "Stamina");
```

```csharp
actionsController.AddAction(MovementActions.Jump, "PlayerMovement");
```

Каталог содержит только идентификаторы. Он не реализует перемещение, бег или прыжок и не задаёт игровую логику этих действий.

**Tags:** `#core #action-catalog #movement`

---

## CoreModules

Общие подключаемые компоненты, наследующиеся от `CoreModule` и предоставляющие функциональность, которую могут использовать разные игровые системы.

### CheckGround

`CheckGround` — модуль проверки пересечения сферы с объектами выбранных слоёв.

**Исходный файл:** [`Assets/Scripts/Core/CoreModules/CheckGround.cs`](Assets/Scripts/Core/CoreModules/CheckGround.cs)  
**Namespace:** `Core.CoreModules`  
**Тип:** `public class CheckGround : CoreModule`  
**Зависимости:** `CoreController`, доступный через него `Rigidbody` и ненулевая `groundCheckMask`.

#### Назначение и работа

Компонент выполняет физическую проверку через `Physics.CheckSphere()` в `FixedUpdate()` и предоставляет результат в свойстве `IsGrounded`.

Центр сферы вычисляется относительно позиции Rigidbody:

```csharp
_rigidbody.position +
    Vector3.up * (groundCheckRadius + groundCheckVerticalOffset)
```

Триггеры игнорируются через `QueryTriggerInteraction.Ignore`.

#### Настройки Inspector

| Поле | Назначение |
|---|---|
| `groundCheckRadius` | Радиус сферы проверки. |
| `groundCheckVerticalOffset` | Дополнительное вертикальное смещение центра сферы. |
| `groundCheckMask` | Маска слоёв, с которыми выполняется проверка. |

Поля сериализованы, но не входят в публичный API: они доступны для настройки через Inspector.

#### Public API

##### `IsGrounded`

**Тип:** `bool`  
**Доступ:** публичное чтение, приватная запись.

Результат последней проверки. `true` означает, что сфера пересекается с объектом из `groundCheckMask`; `false` — что пересечения нет.

```csharp
if (checkGround.IsGrounded)
{
    Debug.Log("Character is grounded");
}
```

##### `Initialize(CoreController core)`

**Сигнатура:** `public override bool Initialize(CoreController core)`

Проверяет маску слоёв и ссылку на ядро, получает `core.CoreRigidbody` и включает обновление проверки.

Возвращает `true`, если все обязательные зависимости доступны. При отсутствии маски, ядра или Rigidbody возвращает `false`.

В штатном сценарии метод вызывается `CoreController`, когда модуль добавляется через `AddCoreModule()`.

##### `Deinitialize()`

**Сигнатура:** `public override void Deinitialize()`

Сбрасывает ссылки на `CoreController` и `Rigidbody`, а также флаг инициализации.

Метод не уничтожает компонент и не меняет настройки Inspector.

#### Примечания по жизненному циклу

- Если после инициализации модуль теряет ссылку на Rigidbody, он выводит ошибку и вызывает `RemoveCoreModule(this)`.
- В `OnDestroy()` модуль пытается удалить себя из `CoreController`.
- Текущий `OnDestroy()` обращается к `_coreController` без предварительной проверки на `null`. Поэтому уничтожение экземпляра, который ещё не был успешно инициализирован и не получил ссылку на ядро, может привести к `NullReferenceException`. Это ограничение текущей реализации, а не гарантия безопасного автоматического отключения.

**Tags:** `#core-modules #ground-check #physics`

### ActionsController

`ActionsController` — модуль Action Registry. Он хранит действия, зарегистрированные одним или несколькими авторами.

**Исходный файл:** [`Assets/Scripts/Core/CoreModules/ActionsController.cs`](Assets/Scripts/Core/CoreModules/ActionsController.cs)  
**Namespace:** `Core.CoreModules`  
**Тип:** `public class ActionsController : CoreModule`

#### Назначение и ответственность

- Регистрирует `ActionId` от имени автора `authorId`.
- Поддерживает несколько авторов для одного идентификатора.
- Удаляет действие из реестра только после удаления последнего автора.
- Не управляет разрешением на выполнение действия. Для этого предназначен `ActionsPermissionsController`.

`ActionsController` является отдельным компонентом. `CoreController` не предоставляет методы `AddAction()` или `RemoveAction()` и не проксирует `CurrentActions`. Вызывайте API непосредственно через ссылку на `ActionsController`.

#### Жизненный цикл

`Initialize(CoreController core)` проверяет ссылку на ядро, сохраняет её и разрешает операции регистрации и удаления. Метод возвращает `false`, если `core` равен `null`.

`Deinitialize()` очищает ссылку на ядро и отключает операции регистрации и удаления. Содержимое реестра при этом не очищается.

#### Public API

##### `CurrentActions`

**Тип:** `IReadOnlyCollection<ActionId>`  
**Доступ:** только чтение.

Коллекция идентификаторов, зарегистрированных хотя бы одним автором. Возвращается представление ключей внутреннего словаря, а не отдельная копия; оно отражает изменения реестра.

```csharp
bool moveRegistered = actionsController.CurrentActions
    .Contains(MovementActions.Move);
```

Для LINQ-метода `Contains()` может потребоваться `using System.Linq;`.

##### `AddAction(ActionId actionId, string authorId)`

**Сигнатура:**

```csharp
public void AddAction(ActionId actionId, string authorId)
```

Регистрирует действие от имени указанного автора.

| Параметр | Тип | Описание и ограничения |
|---|---|---|
| `actionId` | `ActionId` | Должен иметь `IsValid == true`. |
| `authorId` | `string` | Не может быть `null`, пустым или состоять только из пробелов. |

**Возвращает:** ничего (`void`).

**Пример — несколько авторов одного действия:**

```csharp
actionsController.AddAction(MovementActions.Move, "KeyboardInput");
actionsController.AddAction(MovementActions.Move, "GamepadInput");
```

**Поведение и ограничения:**

- Метод работает только после успешной инициализации.
- При невалидном `ActionId` или пустом `authorId` выводится предупреждение, состояние реестра не меняется.
- Повторное добавление того же `authorId` для того же `ActionId` не создаёт дубликат.
- Строковые идентификаторы авторов сравниваются с учётом регистра.

##### `RemoveAction(ActionId actionId, string authorId)`

**Сигнатура:**

```csharp
public void RemoveAction(ActionId actionId, string authorId)
```

Снимает регистрацию действия от имени конкретного автора.

| Параметр | Тип | Описание и ограничения |
|---|---|---|
| `actionId` | `ActionId` | Должен иметь `IsValid == true` и присутствовать в реестре. |
| `authorId` | `string` | Непустой идентификатор автора, ранее зарегистрировавшего действие. |

**Возвращает:** ничего (`void`).

**Пример:**

```csharp
actionsController.AddAction(MovementActions.Move, "KeyboardInput");
actionsController.AddAction(MovementActions.Move, "GamepadInput");

actionsController.RemoveAction(MovementActions.Move, "KeyboardInput");
// Move всё ещё зарегистрирован GamepadInput.

actionsController.RemoveAction(MovementActions.Move, "GamepadInput");
// Если других авторов нет, Move удаляется из CurrentActions.
```

**Поведение и ограничения:**

- Метод работает только после успешной инициализации.
- При невалидном `ActionId`, пустом `authorId`, отсутствии действия или отсутствии регистрации у указанного автора операция не выполняется; в соответствующих случаях выводится предупреждение.
- Удаление одного автора не затрагивает регистрации других авторов.

##### `Initialize(CoreController core)` и `Deinitialize()`

Реализации методов контракта `ICoreModule`:

```csharp
public override bool Initialize(CoreController core);
public override void Deinitialize();
```

`Initialize()` возвращает `false`, если `core` равен `null`. В противном случае сохраняет ссылку на ядро и включает модуль. Сам `ActionsController` не использует переданную ссылку для операций реестра.

`Deinitialize()` очищает ссылку на ядро и флаг инициализации, но сохраняет зарегистрированные действия.

#### Примечания по жизненному циклу

В `OnDestroy()` модуль пытается удалить себя из `CoreController`. Как и в других текущих Core-модулях, `_coreController` здесь не проверяется на `null` до вызова `RemoveCoreModule()`.

**Tags:** `#core-modules #actions #registry`

### ActionsPermissionsController

`ActionsPermissionsController` — модуль Action Permission. Он позволяет нескольким системам независимо блокировать и разблокировать одно действие.

**Исходный файл:** [`Assets/Scripts/Core/CoreModules/ActionsPermissionsController.cs`](Assets/Scripts/Core/CoreModules/ActionsPermissionsController.cs)  
**Namespace:** `Core.CoreModules`  
**Тип:** `public class ActionsPermissionsController : CoreModule`

#### Назначение и ответственность

Каждый `blockerId` представляет источник блокировки. Действие считается доступным, если для него не зарегистрировано ни одной блокировки.

Модуль хранит состояние разрешений, но сам не запускает и не останавливает игровое действие. Код, который выполняет действие, должен самостоятельно проверять `CanPerform()`.

#### Жизненный цикл

`Initialize(CoreController core)` возвращает `false`, если `core` равен `null`. В противном случае сохраняет ссылку на ядро и разрешает операции `BlockAction()` и `UnblockAction()`.

`Deinitialize()` очищает ссылку на ядро и отключает операции изменения блокировок. Набор блокировок при деинициализации не очищается.

#### Public API

##### `CanPerform(ActionId action)`

**Сигнатура:**

```csharp
public bool CanPerform(ActionId action)
```

Проверяет, зарегистрированы ли блокировки для действия.

**Параметр:** `action` — идентификатор действия.

**Возвращает:** `false`, если для действия есть хотя бы одна блокировка; иначе `true`.

**Пример:**

```csharp
if (permissions.CanPerform(MovementActions.Run))
{
    StartRunning();
}
```

**Особенности текущей реализации:**

- Метод не проверяет состояние инициализации модуля.
- Метод не проверяет `action.IsValid`.
- Для действия, отсутствующего в словаре блокировок, возвращается `true`. Поэтому до инициализации модуля или при передаче `default(ActionId)` результат также может быть `true`.
- Метод только читает состояние блокировок: он не выполняет действие и не меняет блокировки.

##### `BlockAction(ActionId action, string blockerId)`

**Сигнатура:**

```csharp
public void BlockAction(ActionId action, string blockerId)
```

Добавляет блокировку действия от имени указанного источника.

| Параметр | Тип | Описание и ограничения |
|---|---|---|
| `action` | `ActionId` | Должен иметь `IsValid == true`. |
| `blockerId` | `string` | Не может быть `null`, пустым или состоять только из пробелов. |

**Возвращает:** ничего (`void`).

**Пример — независимые причины блокировки:**

```csharp
permissions.BlockAction(MovementActions.Run, "Stamina");
permissions.BlockAction(MovementActions.Run, "Injury");
```

**Поведение и ограничения:**

- Метод работает только после успешной инициализации.
- Невалидный `ActionId` и пустой `blockerId` отклоняются с предупреждением.
- Один и тот же `blockerId` не дублируется для одного действия.
- Добавление блокировки само по себе не останавливает уже выполняющееся действие. Код действия должен отдельно проверять `CanPerform()` в подходящий момент.

##### `UnblockAction(ActionId action, string blockerId)`

**Сигнатура:**

```csharp
public void UnblockAction(ActionId action, string blockerId)
```

Снимает блокировку, принадлежащую указанному источнику.

| Параметр | Тип | Описание и ограничения |
|---|---|---|
| `action` | `ActionId` | Должен иметь `IsValid == true`. |
| `blockerId` | `string` | Идентификатор источника блокировки. Для изменения состояния должен быть непустым. |

**Возвращает:** ничего (`void`).

**Пример:**

```csharp
permissions.BlockAction(MovementActions.Run, "Stamina");
permissions.BlockAction(MovementActions.Run, "Injury");

permissions.UnblockAction(MovementActions.Run, "Stamina");
// Действие всё ещё заблокировано источником Injury.

permissions.UnblockAction(MovementActions.Run, "Injury");
// Run снова разрешён, если других блокировок нет.
```

**Поведение и ограничения:**

- Метод работает только после успешной инициализации.
- Невалидный `ActionId` отклоняется с предупреждением.
- Если `blockerId` равен `null`, пустой строке или состоит из пробелов, метод не меняет состояние.
- Если действие или указанный источник блокировки не найдены, выводится предупреждение.
- После снятия последней блокировки ключ действия удаляется из внутреннего словаря.

##### `Initialize(CoreController core)` и `Deinitialize()`

Реализации методов контракта `ICoreModule`:

```csharp
public override bool Initialize(CoreController core);
public override void Deinitialize();
```

`Initialize()` возвращает `false`, если `core` равен `null`. В противном случае сохраняет ссылку на ядро и включает модуль. `Deinitialize()` сбрасывает ссылку и флаг инициализации, но не удаляет существующие блокировки.

#### Примечания по жизненному циклу

В `OnDestroy()` модуль пытается удалить себя из `CoreController`. Текущий код не проверяет `_coreController` на `null` перед вызовом `RemoveCoreModule()`.

**Tags:** `#core-modules #actions #permissions`

---

## GameModules

В проверенном состоянии ветки `main` публичные компоненты игровых модулей в справочник пока не включены. Этот раздел зарезервирован для отдельных игровых систем и их подмодулей.

При добавлении игровых систем описывайте здесь их фактическую иерархию. Общие механизмы, предназначенные для повторного использования разными игровыми системами, следует документировать в `Core` или `CoreModules`.

**Tags:** `#game-modules`

## Общие замечания

- Для подключения через `CoreController` модуль должен наследоваться от `CoreModule`. Произвольный `MonoBehaviour`, который только реализует `ICoreModule`, штатными методами `AddCoreModule()` и `RemoveCoreModule()` не принимается.
- `CoreModules` показывает компоненты из списка ядра, а `InitializedCoreModules` — только те, чья инициализация завершилась успешно.
- `Initialize()` должен возвращать `true` только после успешной подготовки модуля. При `false` ядро вызывает `Deinitialize()` и удаляет модуль из своего списка.
- `Deinitialize()` должен корректно обрабатывать частично инициализированное и уже очищенное состояние.
- `ActionsController` отвечает за регистрацию действий и авторов. `ActionsPermissionsController` отвечает за блокировки выполнения. Это независимые модули.
- `ActionId` можно создать через `default`, несмотря на проверки в конструкторе. Перед регистрацией и блокировкой проверяйте `IsValid`.
- `CheckGround.IsGrounded` обновляется в `FixedUpdate()`, поэтому в других методах Unity читается результат последней выполненной физической проверки.
- Текущие `OnDestroy()` у `CheckGround`, `ActionsController` и `ActionsPermissionsController` обращаются к `_coreController` без проверки на `null`. Это стоит учитывать при уничтожении модулей, которые не прошли успешную инициализацию.

---

*Документ описывает публичные типы и члены, обнаруженные в текущем состоянии репозитория. После изменения публичного контракта сверяйте и обновляйте соответствующие разделы по исходникам.*