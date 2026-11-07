# Library API

Справочник публичного API библиотеки `escontroller`. Документ отражает исходный код ветки `main` на момент последней проверки.

> API находится в активной разработке. При расхождении документации и реализации источником истины для фактических сигнатур и поведения являются исходники.

## Содержание

- [Core](#core)
  - [CoreController](#corecontroller)
  - [ICoreModule](#icoremodule)
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

Фундаментальные типы и механизмы библиотеки. `Core` не содержит конкретные реализации игровых систем.

### CoreController

`CoreController` — центральный компонент ядра персонажа. Он предоставляет базовые данные и управляет жизненным циклом подключённых `ICoreModule`. Реализация Action Registry и Action Permission находится в отдельных модулях раздела [CoreModules](#coremodules), а не в самом `CoreController`.

**Исходный файл:** [`Assets/Scripts/Core/CoreController.cs`](Assets/Scripts/Core/CoreController.cs)  
**Namespace:** `Core`  
**Тип:** `public class CoreController : MonoBehaviour`  
**Зависимости:** компонент `Rigidbody`; подключаемые компоненты должны реализовывать [`ICoreModule`](#icoremodule).

#### Назначение и ответственность

- Предоставляет Rigidbody персонажа через `CoreRigidbody`.
- Хранит список компонентов-кандидатов в Core Modules и предоставляет его для чтения.
- Проверяет соответствие подключаемых компонентов интерфейсу `ICoreModule`.
- Вызывает `Initialize(this)` при подключении модуля и `Deinitialize()` при отключении.
- Синхронизирует набор проверенных и инициализированных модулей.

#### Жизненный цикл

1. В `Awake()` ядро проверяет ссылку на Rigidbody. Если ссылка не назначена в Inspector, выполняется поиск `Rigidbody` на том же GameObject.
2. Если Rigidbody не найден, `CoreController` выводит ошибку, отключает себя и не инициализирует модули.
3. После успешной проверки Rigidbody ядро инициализирует проверенные модули.
4. В `Update()` ядро обнаруживает изменение набора проверенных модулей и синхронизирует инициализацию.
5. `AddCoreModule()` и `RemoveCoreModule()` запускают синхронизацию сразу после изменения списка.
6. `OnValidate()` в редакторе повторно проверяет список и удаляет из него пустые ссылки и компоненты, не реализующие `ICoreModule`.

#### Public API

##### `CoreRigidbody`

**Тип:** `Rigidbody`  
**Доступ:** только чтение.

Ссылка на физическое тело персонажа. Может быть назначена через Inspector. Если поле не назначено, `CoreController` ищет компонент `Rigidbody` на том же GameObject в `Awake()`.

**Примеры:**

```csharp
Rigidbody body = core.CoreRigidbody;
body.AddForce(Vector3.up * 5f, ForceMode.Impulse);
```

```csharp
if (core.CoreRigidbody != null)
{
    Vector3 velocity = core.CoreRigidbody.linearVelocity;
}
```

**Ограничения:** при отсутствии назначенного или найденного Rigidbody компонент `CoreController` отключается. Примеры используют API `Rigidbody` Unity 6.

##### `CoreModules`

**Тип:** `IReadOnlyList<MonoBehaviour>`  
**Доступ:** только чтение.

Список компонентов, назначенных в качестве Core Modules. Свойство возвращает read-only представление внутреннего списка: вызывающий код не может изменять его через этот интерфейс. Изменения самого списка, выполненные `CoreController`, отражаются в представлении.

В список должны попадать компоненты, реализующие `ICoreModule`. В runtime добавление следует выполнять через `AddCoreModule()`, а удаление — через `RemoveCoreModule()`.

**Примеры:**

```csharp
foreach (MonoBehaviour module in core.CoreModules)
{
    Debug.Log(module.name);
}
```

```csharp
bool hasGroundCheck = core.CoreModules
    .Any(module => module is CheckGround);
```

Для второго примера требуется `using System.Linq;`.

##### `AddCoreModule(MonoBehaviour module)`

**Назначение:** добавить компонент в список Core Modules и подключить его, если он реализует `ICoreModule`.

**Сигнатура:**

```csharp
public void AddCoreModule(MonoBehaviour module)
```

**Параметры:**

| Параметр | Тип | Описание и ограничения |
|---|---|---|
| `module` | `MonoBehaviour` | Компонент-кандидат. `null`, уже добавленный компонент и компонент без `ICoreModule` игнорируются. |

**Возвращает:** ничего (`void`).

**Пример 1 — подключение заранее полученной ссылки:**

```csharp
[SerializeField] private CoreController core;
[SerializeField] private CheckGround checkGround;

private void Start()
{
    core.AddCoreModule(checkGround);
}
```

**Пример 2 — подключение компонента с того же GameObject:**

```csharp
private void Start()
{
    CheckGround groundModule = GetComponent<CheckGround>();
    core.AddCoreModule(groundModule);
}
```

**Ограничения и примечания:**

- Метод принимает `MonoBehaviour`, а не произвольную реализацию `ICoreModule`. Это позволяет проверить, что модуль является компонентом Unity.
- При `null`, дубликате или отсутствии `ICoreModule` метод завершает работу без изменения списка.
- При успешном добавлении модуль проверяется и синхронизация инициализации запускается немедленно.
- Метод не возвращает признак успешного добавления. При необходимости вызывающий код может проверить `CoreModules`.
- Не следует добавлять модули до готовности `CoreController` в `Awake()`, если их `Initialize()` зависит от уже разрешённых зависимостей ядра, включая `CoreRigidbody`.

##### `RemoveCoreModule(ICoreModule module)`

**Назначение:** удалить Core Module из набора проверенных модулей и исходного списка, после чего синхронизировать инициализацию.

**Сигнатура:**

```csharp
public void RemoveCoreModule(ICoreModule module)
```

**Параметры:**

| Параметр | Тип | Описание и ограничения |
|---|---|---|
| `module` | `ICoreModule` | Модуль, который требуется отключить. Передача `null` игнорируется. |

**Возвращает:** ничего (`void`).

**Пример 1 — удаление модуля проверки земли:**

```csharp
core.RemoveCoreModule(checkGround);
```

**Пример 2 — удаление модуля реестра действий:**

```csharp
core.RemoveCoreModule(actionsController);
```

**Ограничения и примечания:**

- Если переданный модуль отсутствует в наборе проверенных модулей, выводится предупреждение.
- Если модуль является существующим компонентом `MonoBehaviour`, он также удаляется из исходного списка `coreModules`.
- Для отключённого инициализированного модуля вызывается `Deinitialize()`.
- Метод не уничтожает сам компонент Unity и не вызывает `Destroy()`.

**Tags:** `#core #controller #module-lifecycle`

### ICoreModule

`ICoreModule` — контракт подключаемого модуля ядра. Реализация интерфейса должна находиться на компоненте `MonoBehaviour`, поскольку `CoreController` хранит модули как `MonoBehaviour`.

**Исходный файл:** [`Assets/Scripts/Core/ICoreModule.cs`](Assets/Scripts/Core/ICoreModule.cs)  
**Namespace:** `Core`  
**Тип:** `public interface ICoreModule`

#### Жизненный цикл

- При подключении `CoreController` вызывает `Initialize(CoreController core)` и передаёт ссылку на ядро.
- При отключении `CoreController` вызывает `Deinitialize()` без параметров.
- `Deinitialize()` — отдельный метод интерфейса. Ядро не передаёт `null` в `Initialize()` при отключении.
- Реализация должна освобождать собственные ссылки на ядро и связанные ресурсы в `Deinitialize()`.

#### Public API

##### `Initialize(CoreController core)`

**Сигнатура:**

```csharp
void Initialize(CoreController core);
```

**Назначение:** принять ссылку на `CoreController` и подготовить модуль к работе.

**Параметры:**

| Параметр | Тип | Описание и ограничения |
|---|---|---|
| `core` | `CoreController` | Экземпляр ядра, который подключает модуль. При штатном вызове со стороны `CoreController` передаётся `this`. |

**Возвращает:** ничего (`void`).

**Пример 1 — сохранение ссылки на ядро:**

```csharp
private CoreController _core;

public void Initialize(CoreController core)
{
    _core = core;
}
```

**Пример 2 — получение общей зависимости:**

```csharp
private Rigidbody _body;

public void Initialize(CoreController core)
{
    _body = core.CoreRigidbody;
}
```

**Ограничения:** интерфейс не определяет механизм обработки ошибок инициализации. Проверку необходимых зависимостей выполняет сам модуль.

##### `Deinitialize()`

**Сигнатура:**

```csharp
void Deinitialize();
```

**Назначение:** отключить модуль и освободить ссылки и состояние, установленные во время инициализации.

**Параметры:** отсутствуют.  
**Возвращает:** ничего (`void`).

**Пример 1 — сброс ссылки на ядро:**

```csharp
public void Deinitialize()
{
    _core = null;
}
```

**Пример 2 — сброс нескольких зависимостей и состояния:**

```csharp
public void Deinitialize()
{
    _body = null;
    _core = null;
    _isInitialized = false;
}
```

**Примечания:** `CoreController` вызывает этот метод при удалении модуля из набора. Сам интерфейс не предписывает уничтожать компонент Unity.

**Tags:** `#core #module-contract #initialization`

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

**Назначение:** создать идентификатор действия из строки.

**Параметры:**

| Параметр | Тип | Описание и ограничения |
|---|---|---|
| `value` | `string` | Значение идентификатора. Не может быть `null` или пустой строкой. |

**Возвращает:** новый `ActionId`.

**Пример 1 — пользовательский идентификатор:**

```csharp
var interactAction = new ActionId("Interaction.Use");
```

**Пример 2 — использование идентификатора в локальном каталоге:**

```csharp
private static readonly ActionId OpenInventory =
    new ActionId("Inventory.Open");
```

**Исключения:** `ArgumentException`, если `value` равен `null` или `string.Empty`.

**Примечания:** строки, состоящие только из пробелов, конструктор принимает. Рекомендуется использовать непустые идентификаторы с устойчивым пространством имён, например `Inventory.Open`.

##### `Value`

**Тип:** `string`  
**Доступ:** только чтение.

Исходное строковое значение идентификатора.

**Примеры:**

```csharp
var action = new ActionId("Inventory.Open");
Debug.Log(action.Value);
```

```csharp
string actionName = MovementActions.Jump.Value;
```

##### `IsValid`

**Тип:** `bool`  
**Доступ:** только чтение.

Возвращает `true`, если `Value` не равен `null` и не является пустой строкой. Проверка не отклоняет строки из одних пробелов.

**Примеры:**

```csharp
var action = new ActionId("Inventory.Open");
if (action.IsValid)
{
    Debug.Log(action.Value);
}
```

```csharp
ActionId uninitialized = default;
bool isValid = uninitialized.IsValid; // false
```

**Ограничения:** поскольку `ActionId` является структурой, `default(ActionId)` создать возможно. Такой идентификатор невалиден; перед передачей в API регистрации или блокировки действий проверяйте `IsValid`.

##### `Equals(ActionId other)`

**Сигнатура:** `public bool Equals(ActionId other)`

Сравнивает два идентификатора по строковому значению `Value`.

**Примеры:**

```csharp
var first = new ActionId("Movement.Jump");
var second = new ActionId("Movement.Jump");
bool equal = first.Equals(second); // true
```

```csharp
var move = new ActionId("Movement.Move");
var jump = new ActionId("Movement.Jump");
bool equal = move.Equals(jump); // false
```

##### `Equals(object obj)`

**Сигнатура:** `public override bool Equals(object obj)`

Проверяет, является ли объект `ActionId` с тем же значением. Для объекта другого типа возвращает `false`.

**Примеры:**

```csharp
object candidate = new ActionId("Movement.Run");
bool equal = new ActionId("Movement.Run").Equals(candidate); // true
```

```csharp
bool equal = new ActionId("Movement.Run").Equals("Movement.Run"); // false
```

##### `GetHashCode()`

**Сигнатура:** `public override int GetHashCode()`

Возвращает хеш-код строкового значения `Value`. Метод согласован с `Equals()` и позволяет использовать `ActionId` в `Dictionary` и `HashSet`.

**Примеры:**

```csharp
var actions = new HashSet<ActionId>();
actions.Add(MovementActions.Jump);
bool containsJump = actions.Contains(MovementActions.Jump);
```

```csharp
var owners = new Dictionary<ActionId, string>();
owners[MovementActions.Move] = "KeyboardInput";
```

##### Операторы `==` и `!=`

**Сигнатуры:**

```csharp
public static bool operator ==(ActionId left, ActionId right);
public static bool operator !=(ActionId left, ActionId right);
```

Операторы сравнивают идентификаторы по `Value` и согласованы с `Equals()`.

**Примеры:**

```csharp
bool sameAction = MovementActions.Move == new ActionId("Movement.Move");
```

```csharp
bool differentActions = MovementActions.Move != MovementActions.Run;
```

**Общие примечания:** сравнение строк регистрозависимое. `Movement.Move` и `movement.move` являются разными идентификаторами.

**Tags:** `#core #identifier #actions`

### MovementActions

`MovementActions` — статический каталог встроенных идентификаторов действий перемещения.

**Исходный файл:** [`Assets/Scripts/Core/ActionContracts.cs`](Assets/Scripts/Core/ActionContracts.cs)  
**Namespace:** `Core`  
**Тип:** `public static class MovementActions`

#### Public API

Публичные поля имеют тип `ActionId` и объявлены как `static readonly`.

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

**Примечания:** каталог содержит только три идентификатора. Он не реализует само перемещение, бег или прыжок и не задаёт игровую логику этих действий.

**Tags:** `#core #action-catalog #movement`

---

## CoreModules

Общие подключаемые компоненты, реализующие `ICoreModule` и предоставляющие функциональность, которую могут использовать разные игровые системы.

### CheckGround

`CheckGround` — модуль проверки контакта области под персонажем с объектами выбранных слоёв.

**Исходный файл:** [`Assets/Scripts/Core/CoreModules/CheckGround.cs`](Assets/Scripts/Core/CoreModules/CheckGround.cs)  
**Namespace:** `Core.CoreModules`  
**Тип:** `public class CheckGround : MonoBehaviour, ICoreModule`  
**Зависимости:** `CoreController` и доступный через него `Rigidbody`; ненулевая `groundCheckMask`.

#### Назначение и ответственность

Компонент выполняет сферическую физическую проверку через `Physics.CheckSphere()` и предоставляет результат в `IsGrounded`. Проверка обновляется в `FixedUpdate()`.

#### Жизненный цикл

- Добавляется в `CoreController` как Core Module.
- В `Initialize(CoreController core)` получает ссылку на ядро и `core.CoreRigidbody`.
- При отсутствии ядра, Rigidbody или корректной маски компонент выводит ошибку и запрашивает удаление из Core Modules.
- В `Deinitialize()` сбрасывает ссылки на `CoreController` и `Rigidbody`, а также флаг инициализации.

#### Настройки Inspector

| Поле | Назначение |
|---|---|
| `groundCheckRadius` | Радиус сферы проверки. |
| `groundCheckVerticalOffset` | Дополнительное вертикальное смещение центра сферы. |
| `groundCheckMask` | Маска слоёв, с которыми выполняется проверка. |

Центр сферы расположен на высоте `groundCheckRadius + groundCheckVerticalOffset` относительно позиции Rigidbody по оси `Vector3.up`. Триггеры игнорируются через `QueryTriggerInteraction.Ignore`.

#### Public API

##### `IsGrounded`

**Тип:** `bool`  
**Доступ:** публичное чтение, приватная запись.

Результат последней проверки земли. Значение `true` означает, что сфера пересекается с объектом из `groundCheckMask`; `false` означает, что пересечения нет.

**Примеры:**

```csharp
private void FixedUpdate()
{
    if (checkGround.IsGrounded)
    {
        Debug.Log("Character is grounded");
    }
}
```

```csharp
bool canStartJump = checkGround.IsGrounded
                    && permissions.CanPerform(MovementActions.Jump);
```

##### `Initialize(CoreController core)`

**Сигнатура:** `public void Initialize(CoreController core)`

Получает ядро и его Rigidbody, проверяет обязательные зависимости и включает обновление проверки. Это реализация метода [`ICoreModule.Initialize`](#icoremodule). В обычном сценарии вызывается `CoreController`, а не пользовательским кодом напрямую.

**Параметр:** `core` — ядро, подключающее модуль.

**Примеры вызова жизненного цикла:**

```csharp
core.AddCoreModule(checkGround);
// CoreController вызовет checkGround.Initialize(this).
```

```csharp
core.RemoveCoreModule(checkGround);
// При отключении CoreController вызовет checkGround.Deinitialize().
```

**Ограничения:** при отсутствии `core`, `CoreRigidbody` или маски слоёв модуль не сможет выполнять проверку и запросит удаление из ядра.

##### `Deinitialize()`

**Сигнатура:** `public void Deinitialize()`

Сбрасывает ссылки на ядро и Rigidbody и отключает обновление проверки. Реализация контракта [`ICoreModule.Deinitialize`](#icoremodule).

**Примеры:** см. примеры жизненного цикла в описании [`Initialize(CoreController core)`](#initializecorecontroller-core). В штатном сценарии метод вызывает `CoreController` при отключении модуля.

**Примечания:** метод не уничтожает компонент и не меняет настройки Inspector.

**Tags:** `#core-modules #ground-check #physics`

### ActionsController

`ActionsController` — модуль Action Registry. Он хранит действия, зарегистрированные одним или несколькими авторами. Название класса приведено по исходному коду; в текущей версии используется именно `ActionsController`, а не `ActionController`.

**Исходный файл:** [`Assets/Scripts/Core/CoreModules/ActionsController.cs`](Assets/Scripts/Core/CoreModules/ActionsController.cs)  
**Namespace:** `Core.CoreModules`  
**Тип:** `public class ActionsController : MonoBehaviour, ICoreModule`  
**Зависимости:** подключение к `CoreController` через `ICoreModule`.

#### Назначение и ответственность

- Регистрирует `ActionId` от имени автора `authorId`.
- Поддерживает несколько авторов для одного идентификатора.
- Удаляет действие из реестра только после удаления последнего автора.
- Не управляет разрешением на выполнение действия. Для этого предназначен [`ActionsPermissionsController`](#actionspermissionscontroller).

#### Жизненный цикл

`Initialize(CoreController core)` разрешает операции регистрации и удаления. `Deinitialize()` запрещает эти операции. При деинициализации содержимое реестра не очищается.

**Важно:** `ActionsController` является отдельным компонентом. `CoreController` не предоставляет методы `AddAction()` или `RemoveAction()` и не проксирует `CurrentActions`. Вызывайте API непосредственно через ссылку на `ActionsController`.

#### Public API

##### `CurrentActions`

**Тип:** `IReadOnlyCollection<ActionId>`  
**Доступ:** только чтение.

Коллекция идентификаторов, зарегистрированных хотя бы одним автором. Возвращается read-only представление ключей внутреннего реестра; это представление отражает последующие изменения реестра, а не отдельная копия.

**Примеры:**

```csharp
bool moveRegistered = actionsController.CurrentActions
    .Contains(MovementActions.Move);
```

```csharp
foreach (ActionId action in actionsController.CurrentActions)
{
    Debug.Log(action.Value);
}
```

##### `AddAction(ActionId actionId, string authorId)`

**Сигнатура:**

```csharp
public void AddAction(ActionId actionId, string authorId)
```

**Назначение:** зарегистрировать действие от имени указанного автора.

**Параметры:**

| Параметр | Тип | Описание и ограничения |
|---|---|---|
| `actionId` | `ActionId` | Валидный идентификатор. Если `IsValid == false`, операция игнорируется с предупреждением. |
| `authorId` | `string` | Непустой идентификатор автора. `null`, пустая строка и строка из пробелов отклоняются с предупреждением. |

**Возвращает:** ничего (`void`).

**Пример 1 — регистрация действия одним автором:**

```csharp
actionsController.AddAction(MovementActions.Move, "KeyboardInput");
```

**Пример 2 — регистрация того же действия несколькими авторами:**

```csharp
actionsController.AddAction(MovementActions.Move, "KeyboardInput");
actionsController.AddAction(MovementActions.Move, "GamepadInput");
```

**Ограничения и примечания:**

- Метод работает только после `Initialize()`. До инициализации выводится предупреждение, регистрация не выполняется.
- Повторное добавление того же `authorId` для того же `ActionId` не создаёт дубликат.
- Идентификаторы автора сравниваются как строки с учётом регистра.

##### `RemoveAction(ActionId actionId, string authorId)`

**Сигнатура:**

```csharp
public void RemoveAction(ActionId actionId, string authorId)
```

**Назначение:** снять регистрацию действия от имени конкретного автора.

**Параметры:**

| Параметр | Тип | Описание и ограничения |
|---|---|---|
| `actionId` | `ActionId` | Валидный идентификатор действия. |
| `authorId` | `string` | Непустой идентификатор автора, ранее зарегистрировавшего действие. |

**Возвращает:** ничего (`void`).

**Пример 1 — убрать регистрацию одного автора:**

```csharp
actionsController.AddAction(MovementActions.Move, "KeyboardInput");
actionsController.AddAction(MovementActions.Move, "GamepadInput");
actionsController.RemoveAction(MovementActions.Move, "KeyboardInput");
// Move остаётся зарегистрированным GamepadInput.
```

**Пример 2 — удалить последнюю регистрацию:**

```csharp
actionsController.RemoveAction(MovementActions.Move, "GamepadInput");
// Если других авторов нет, Move удаляется из CurrentActions.
```

**Ограничения и примечания:**

- Метод работает только после `Initialize()`.
- При невалидном `ActionId`, пустом `authorId`, отсутствии действия или отсутствии регистрации у указанного автора операция не выполняется; в соответствующих случаях выводится предупреждение.
- Удаление одного автора не затрагивает регистрации других авторов.

##### `Initialize(CoreController core)` и `Deinitialize()`

Публичные методы контракта `ICoreModule`. Первый включает операции реестра, второй отключает их. Параметр `core` не используется самим `ActionsController`. При `Deinitialize()` регистрации сохраняются.

**Примеры:**

```csharp
core.AddCoreModule(actionsController);
// CoreController вызывает Initialize(this).
```

```csharp
core.RemoveCoreModule(actionsController);
// CoreController вызывает Deinitialize().
```

**Tags:** `#core-modules #actions #registry`

### ActionsPermissionsController

`ActionsPermissionsController` — модуль Action Permission. Он позволяет нескольким системам независимо блокировать и разблокировать одно действие.

**Исходный файл:** [`Assets/Scripts/Core/CoreModules/ActionsPermissionsController.cs`](Assets/Scripts/Core/CoreModules/ActionsPermissionsController.cs)  
**Namespace:** `Core.CoreModules`  
**Тип:** `public class ActionsPermissionsController : MonoBehaviour, ICoreModule`  
**Зависимости:** подключение к `CoreController` через `ICoreModule`.

#### Назначение и ответственность

Каждый `blockerId` представляет источник блокировки. Действие считается доступным, когда в системе не зарегистрировано ни одной блокировки для его `ActionId`. Система хранит состояние разрешений, но сама не запускает и не останавливает игровое действие.

#### Жизненный цикл

`Initialize(CoreController core)` разрешает вызовы `BlockAction()` и `UnblockAction()`. `Deinitialize()` отключает эти операции. Набор блокировок при деинициализации не очищается.

**Важно:** методы Action Permission принадлежат `ActionsPermissionsController`, а не `CoreController`. Получайте компонент и вызывайте его API напрямую.

#### Public API

##### `CanPerform(ActionId action)`

**Сигнатура:**

```csharp
public bool CanPerform(ActionId action)
```

**Назначение:** проверить, зарегистрированы ли блокировки для действия.

**Параметры:** `action` — идентификатор действия.

**Возвращает:** `false`, если для действия есть хотя бы одна блокировка; иначе `true`.

**Пример 1 — проверка возможности бега:**

```csharp
if (permissions.CanPerform(MovementActions.Run))
{
    StartRunning();
}
```

**Пример 2 — совмещение проверки разрешения с состоянием земли:**

```csharp
bool canJump = checkGround.IsGrounded
               && permissions.CanPerform(MovementActions.Jump);
```

**Ограничения и примечания:**

- В текущей реализации метод не проверяет состояние инициализации модуля.
- Метод не проверяет `action.IsValid`. Для согласованной работы передавайте валидный `ActionId`.
- Проверка только читает состояние блокировок. Она не выполняет действие и не меняет блокировки.

##### `BlockAction(ActionId action, string blockerId)`

**Сигнатура:**

```csharp
public void BlockAction(ActionId action, string blockerId)
```

**Назначение:** добавить блокировку действия от имени указанного источника.

**Параметры:**

| Параметр | Тип | Описание и ограничения |
|---|---|---|
| `action` | `ActionId` | Валидный идентификатор действия. Невалидный идентификатор отклоняется с предупреждением. |
| `blockerId` | `string` | Непустой идентификатор источника блокировки. `null`, пустая строка и строка из пробелов отклоняются с предупреждением. |

**Возвращает:** ничего (`void`).

**Пример 1 — блокировка бега из-за выносливости:**

```csharp
permissions.BlockAction(MovementActions.Run, "Stamina");
```

**Пример 2 — независимые причины блокировки:**

```csharp
permissions.BlockAction(MovementActions.Run, "Stamina");
permissions.BlockAction(MovementActions.Run, "Injury");
```

**Ограничения и примечания:**

- Метод выполняет изменение только после `Initialize()`. До инициализации выводится предупреждение.
- Один и тот же `blockerId` не дублируется для одного действия.
- Добавление блокировки не останавливает уже выполняющееся действие. Код действия должен отдельно проверять `CanPerform()` в подходящий момент.

##### `UnblockAction(ActionId action, string blockerId)`

**Сигнатура:**

```csharp
public void UnblockAction(ActionId action, string blockerId)
```

**Назначение:** снять блокировку, принадлежащую указанному источнику.

**Параметры:**

| Параметр | Тип | Описание и ограничения |
|---|---|---|
| `action` | `ActionId` | Валидный идентификатор действия. |
| `blockerId` | `string` | Непустой идентификатор источника, ранее заблокировавшего действие. |

**Возвращает:** ничего (`void`).

**Пример 1 — снять только одну из нескольких блокировок:**

```csharp
permissions.BlockAction(MovementActions.Run, "Stamina");
permissions.BlockAction(MovementActions.Run, "Injury");
permissions.UnblockAction(MovementActions.Run, "Stamina");
// Действие всё ещё заблокировано источником Injury.
```

**Пример 2 — снять последнюю блокировку:**

```csharp
permissions.UnblockAction(MovementActions.Run, "Injury");
// Run снова разрешён, если других блокировок нет.
```

**Ограничения и примечания:**

- Метод выполняет изменение только после `Initialize()`.
- Если действие или блокировка не найдены, выводится предупреждение.
- `blockerId`, равный `null`, пустой строке или состоящий из пробелов, игнорируется без изменения состояния.
- После снятия последней блокировки ключ действия удаляется из внутренней коллекции блокировок.

##### `Initialize(CoreController core)` и `Deinitialize()`

Публичные методы контракта `ICoreModule`. `Initialize()` разрешает изменение блокировок, а `Deinitialize()` запрещает. Параметр `core` самим модулем не используется. Существующие блокировки сохраняются при деинициализации.

**Примеры:**

```csharp
core.AddCoreModule(permissions);
// CoreController вызывает Initialize(this).
```

```csharp
core.RemoveCoreModule(permissions);
// CoreController вызывает Deinitialize().
```

**Tags:** `#core-modules #actions #permissions`

---

## GameModules

В проверенной версии ветки `main` на момент обновления публичные компоненты `GameModules` и их подмодули в исходниках не обнаружены. Каталог `Assets/Scripts/Modules` не содержит исходных `.cs`-файлов, поэтому раздел пока не включает карточки игровых модулей.

При появлении игровых систем добавляйте их сюда, сохраняя фактическую иерархию родительских модулей и подмодулей. Не переносите сюда общие механизмы, которые относятся к `Core` или `CoreModules`.

**Tags:** `#game-modules`

## Общие замечания

- `CoreController` управляет подключением и отключением `ICoreModule`, но не содержит прямых методов Action Registry или Action Permission.
- `ActionsController` и `ActionsPermissionsController` — независимые модули. Первый отвечает за регистрацию действий и авторов, второй — за блокировки выполнения.
- `ActionId` можно создать через `default`, несмотря на то что конструктор не принимает пустое значение. Всегда проверяйте `IsValid` перед регистрацией или блокировкой.
- `CheckGround.IsGrounded` обновляется в `FixedUpdate()`, поэтому в других методах Unity читается результат последней выполненной физической проверки.
- Все сигнатуры и сведения в этом документе проверены по исходникам `main`; документация не гарантирует стабильность экспериментального API.

---

*Документ описывает публичные типы и члены, обнаруженные в текущем состоянии репозитория. После каждого изменения публичного контракта необходимо сверять и обновлять соответствующие разделы.*
