---
id: csharp.types.enum
title: Перечисления (enum)
level: junior
tracks: [backend, fullstack]
minutes: 20
order: 5
requires: [csharp.types.null]
quickCheck: [enum-value, enum-out-of-range, flags-combine, enum-default]
testMinutes: 6
---

::::warmup

```quiz
id: enum-value
type: output
prompt: Что выведет код?
code: |
  Console.WriteLine((int)Status.Shipped);

  enum Status { New, Paid, Shipped }
answer: "2"
explain: |
  Если значения не указаны, они идут с нуля по порядку: `New` — 0, `Paid` — 1, `Shipped` — 2. Приведение к `int` даёт число.
variants:
  - code: |
      Console.WriteLine((int)Status.Shipped);

      enum Status { New = 1, Paid, Shipped }
    answer: "3"
    explain: |
      Счёт начинается с явно заданного значения: `New` — 1, дальше каждое следующее на единицу больше.
  - code: |
      Console.WriteLine(Status.Paid);

      enum Status { New, Paid, Shipped }
    answer: Paid
    explain: |
      `WriteLine` вызывает `ToString()`, а у перечисления он возвращает имя значения, а не число.
```

::::

::::part Объявление и базовый тип

**Перечисление** (`enum`) — тип с набором именованных констант. Вместо «магических чисел» в коде появляются имена:

```csharp
enum OrderStatus
{
    New,        // 0
    Paid,       // 1
    Shipped,    // 2
    Cancelled,  // 3
}

OrderStatus status = OrderStatus.Paid;
if (status == OrderStatus.Paid)
    Console.WriteLine("можно отправлять");
```

Перечисление — **значимый тип**, внутри это обычное целое число. По умолчанию — `int`, но можно указать другой целый **базовый тип**:

```csharp
enum Priority : byte { Low = 1, Normal = 5, High = 10 }
```

Значения можно задать явно, в том числе с пропусками. Без значения элемент получает предыдущее плюс 1.

**Преобразования** между перечислением и числом — только явные:

```csharp
int code = (int)OrderStatus.Shipped;  // 2
OrderStatus s = (OrderStatus)1;       // Paid
```

**Строки.** `ToString()` возвращает имя: `OrderStatus.Paid.ToString()` — `"Paid"`. Обратно — `Enum.Parse` или безопасный `Enum.TryParse`:

```csharp
if (Enum.TryParse("Shipped", out OrderStatus parsed))
    Console.WriteLine((int)parsed);  // 2

OrderStatus[] all = Enum.GetValues<OrderStatus>();  // все значения
```

**Значение по умолчанию** у перечисления — 0, как у числа. Даже если среди элементов нет нуля, `default(Priority)` будет 0. Поэтому принято делать нулевой элемент осмысленным, например `None` или `Unknown`.

```quiz
id: enum-default
type: output
prompt: Что выведет код?
code: |
  Priority p = default;
  Console.WriteLine((int)p);

  enum Priority { Low = 1, Normal = 5, High = 10 }
answer: "0"
explain: |
  Значение по умолчанию у перечисления — 0, как у его базового типа, даже если элемента с нулём нет.
variants:
  - code: |
      Priority p = default;
      Console.WriteLine(p);

      enum Priority { None, Low, High }
    answer: None
    explain: |
      Значение по умолчанию — 0, а 0 здесь — элемент `None`. Поэтому нулевой элемент и делают осмысленным.
```

::::

::::part Значения вне перечисления

Перечисление **не ограничивает** значения своими элементами. Любое число базового типа можно привести к нему, и ошибки не будет:

```csharp
OrderStatus odd = (OrderStatus)42;
Console.WriteLine(odd);  // 42: имени нет, выводится число
```

Так бывает на практике: значение пришло из базы данных, где кто-то добавил новый статус, или из JSON от другой версии программы. Поэтому:

- `switch` по перечислению должен иметь ветку `default` или `_` для неизвестных значений;
- проверить, есть ли такой элемент, можно через `Enum.IsDefined(odd)`;
- `Enum.TryParse("42", out OrderStatus s)` тоже вернёт `true`: число в строке разбирается даже без такого элемента.

```quiz
id: enum-out-of-range
type: output
prompt: Что выведет код?
code: |
  Color c = (Color)7;
  Console.WriteLine(c);

  enum Color { Red, Green, Blue }
answer: "7"
explain: |
  Приведение не проверяет, есть ли такой элемент. Значение 7 хранится как есть, а `ToString()` при отсутствии имени выводит число.
variants:
  - code: |
      Color c = (Color)7;
      Console.WriteLine(Enum.IsDefined(c));

      enum Color { Red, Green, Blue }
    answer: "False"
    explain: |
      Элемента со значением 7 нет. `Enum.IsDefined` — способ это проверить.
  - code: |
      Color c = (Color)1;
      Console.WriteLine(c);

      enum Color { Red, Green, Blue }
    answer: Green
    explain: |
      1 — значение элемента `Green`, поэтому выводится его имя.
```

::::

::::part Флаги

Иногда нужно хранить не одно значение, а набор: права доступа, дни недели, параметры. Для этого значения делают степенями двойки — каждое занимает свой бит — и помечают перечисление атрибутом **`[Flags]`**:

```csharp
[Flags]
enum Permissions
{
    None = 0,
    Read = 1,    // 0001
    Write = 2,   // 0010
    Delete = 4,  // 0100
    Admin = 8,   // 1000
}

Permissions p = Permissions.Read | Permissions.Write;  // объединить: 0011

bool canWrite = (p & Permissions.Write) != 0;  // проверить бит
bool canRead = p.HasFlag(Permissions.Read);    // то же короче

p &= ~Permissions.Write;  // снять бит
p |= Permissions.Delete;  // добавить бит

Console.WriteLine(p);  // Read, Delete
```

- `|` объединяет флаги, `&` проверяет, `& ~` снимает. Это побитовые операторы из темы про операторы.
- `[Flags]` влияет на `ToString()`: комбинация выводится как список имён через запятую. Без атрибута выводилось бы число.
- Значения должны быть степенями двойки, иначе флаги перекроются: `Write = 3` совпал бы с `Read | Write`.
- Удобно добавлять готовые комбинации: `ReadWrite = Read | Write`.

```quiz
id: flags-combine
type: output
prompt: Что выведет код?
code: |
  Access a = Access.Read | Access.Delete;
  Console.WriteLine((int)a);

  [Flags]
  enum Access { None = 0, Read = 1, Write = 2, Delete = 4 }
answer: "5"
explain: |
  `Read` — бит 1, `Delete` — бит 4. `|` объединяет биты: 1 + 4 = 5.
variants:
  - code: |
      Access a = Access.Read | Access.Write;
      Console.WriteLine(a.HasFlag(Access.Delete));

      [Flags]
      enum Access { None = 0, Read = 1, Write = 2, Delete = 4 }
    answer: "False"
    explain: |
      В комбинации есть биты `Read` и `Write`, бита `Delete` нет.
  - code: |
      Access a = Access.Read | Access.Write;
      a &= ~Access.Read;
      Console.WriteLine(a);

      [Flags]
      enum Access { None = 0, Read = 1, Write = 2, Delete = 4 }
    answer: Write
    explain: |
      `& ~Access.Read` снимает бит `Read`, остаётся только `Write`.
```

:::level middle

**Что внутри.** Каждое перечисление — структура, унаследованная от `System.Enum`. Поэтому оно значимое и хранится как своё базовое число, без накладных расходов.

**Цена удобных методов.** `Enum.ToString()`, `Enum.Parse`, `Enum.GetValues` используют рефлексию и выделяют память, в горячем коде их избегают. `HasFlag` раньше упаковывал аргумент, с .NET Core 2.1 JIT заменяет его на побитовую проверку.

**Полнота `switch`.** Для выражения `switch` по перечислению компилятор предупреждает, если перечислены не все элементы и нет `_`. Но даже со всеми элементами `_` нужен: значение может быть любым числом.

```quiz
id: enum-is-struct
type: choice
level: middle
prompt: К какому виду типов относится перечисление?
options:
  - "Значимый тип: структура, унаследованная от System.Enum"
  - "Ссылочный тип, как класс"
  - "Ссылочный тип, как string"
  - "Особый тип, не значимый и не ссылочный"
answer: 0
explain: |
  Перечисление хранится как своё базовое целое число и копируется по значению.
```

:::

::::

::::practice

```quiz
id: describe-status
type: code
lang: csharp
prompt: |
  Допишите метод: он возвращает русское название статуса заказа. Для значения, которого нет в перечислении, — «неизвестно».
starter: |
  public enum OrderStatus { New, Paid, Shipped }

  public static class Orders
  {
      public static string Describe(OrderStatus status)
      {
      }
  }
tests: |
  Assert.Equal("новый", Orders.Describe(OrderStatus.New));
  Assert.Equal("оплачен", Orders.Describe(OrderStatus.Paid));
  Assert.Equal("отправлен", Orders.Describe(OrderStatus.Shipped));
  Assert.Equal("неизвестно", Orders.Describe((OrderStatus)9));
solution: |
  public enum OrderStatus { New, Paid, Shipped }

  public static class Orders
  {
      public static string Describe(OrderStatus status) => status switch
      {
          OrderStatus.New => "новый",
          OrderStatus.Paid => "оплачен",
          OrderStatus.Shipped => "отправлен",
          _ => "неизвестно",
      };
  }
explain: |
  Ветка `_` обязательна: в перечислении может оказаться любое число, и без неё выражение `switch` бросило бы исключение.
variants:
  - prompt: |
      Допишите метод: он проверяет, может ли пользователь с правами `permissions` редактировать документ. Для этого нужны оба флага: `Read` и `Write`.
    starter: |
      [System.Flags]
      public enum Permissions { None = 0, Read = 1, Write = 2, Delete = 4 }

      public static class Access
      {
          public static bool CanEdit(Permissions permissions)
          {
          }
      }
    tests: |
      Assert.True(Access.CanEdit(Permissions.Read | Permissions.Write));
      Assert.True(Access.CanEdit(Permissions.Read | Permissions.Write | Permissions.Delete));
      Assert.False(Access.CanEdit(Permissions.Read));
      Assert.False(Access.CanEdit(Permissions.None));
    solution: |
      [System.Flags]
      public enum Permissions { None = 0, Read = 1, Write = 2, Delete = 4 }

      public static class Access
      {
          public static bool CanEdit(Permissions permissions)
          {
              var needed = Permissions.Read | Permissions.Write;
              return (permissions & needed) == needed;
          }
      }
    explain: |
      `& needed` оставляет только нужные биты. Если результат равен всей маске, оба флага стоят. Проверка `!= 0` подошла бы для «хотя бы один».
```

```quiz
id: flags-remove
type: fill
prompt: Вставьте выражение, чтобы снять флаг `Write`.
code: |
  access &= ___Permissions.Write;
answer: "~"
explain: |
  `~` инвертирует биты: остаются все, кроме `Write`. `&` с этой маской обнуляет бит `Write`.
```

::::

::::traps

- **Значение по умолчанию — 0**, даже если элемента с нулём нет. Делайте нулевой элемент осмысленным.
- **Перечисление не ограничивает значения:** `(Color)42` допустим. В `switch` нужна ветка для неизвестных.
- **Число и перечисление преобразуются только явно.**
- **Флаги — степени двойки**, иначе биты перекроются.
- **Без `[Flags]` комбинация выводится числом**, с ним — списком имён.
- **`Enum.TryParse("42", ...)` успешен** и без элемента 42.

::::

::::summary

```quiz
id: enum-recall
type: recall
prompt: Расскажите, как устроено перечисление в C# и как работают флаги.
answer: |
  `enum` — значимый тип с именованными целыми константами; базовый тип по умолчанию `int`, можно указать другой целый. Без явных значений элементы нумеруются с 0. Число и перечисление преобразуются явным приведением, `ToString()` даёт имя, `Enum.TryParse` разбирает строку.
  Значение по умолчанию — 0, поэтому нулевой элемент делают осмысленным (`None`). Перечисление не ограничивает значения: `(T)42` допустим, имя тогда не выводится, а `switch` должен иметь ветку для неизвестного.
  **Флаги**: значения — степени двойки, атрибут `[Flags]`. `|` объединяет, `&` проверяет, `& ~` снимает, `HasFlag` проверяет короче. `[Flags]` выводит комбинацию списком имён.
```

::::

::::test

```quiz
id: test-enum-base
type: choice
part: "Объявление и базовый тип"
prompt: Какой базовый тип у перечисления, если он не указан?
options:
  - "int"
  - "byte"
  - "long"
  - "string"
answer: 0
explain: |
  По умолчанию — `int`. Другой целый тип задают через двоеточие: `enum Priority : byte`.
```

```quiz
id: test-enum-parse
type: output
part: "Объявление и базовый тип"
prompt: Что выведет код?
code: |
  bool ok = Enum.TryParse("Large", out Size size);
  Console.WriteLine((int)size);

  enum Size { Small, Medium, Large }
answer: "2"
explain: |
  `TryParse` нашёл элемент `Large`, его значение — 2.
```

```quiz
id: test-enum-implicit
type: find-bug
part: "Объявление и базовый тип"
prompt: Код не компилируется. В какой строке ошибка?
code: |
  int raw = 1;
  Level level = raw;
  Console.WriteLine(level);

  enum Level { Low, High }
answer: 2
explain: |
  Число в перечисление превращается только явно: `Level level = (Level)raw;`. Единственное исключение — литерал 0, его можно присвоить неявно.
```

```quiz
id: test-enum-explicit-values
type: output
part: "Объявление и базовый тип"
prompt: Что выведет код?
code: |
  Console.WriteLine((int)Code.D);

  enum Code { A = 10, B, C = 20, D }
answer: "21"
explain: |
  Элемент без значения получает предыдущее плюс 1: `B` — 11, `D` — 21.
variants:
  - code: |
      Console.WriteLine((int)Code.B);

      enum Code { A = 10, B, C = 20, D }
    answer: "11"
    explain: |
      `B` идёт после `A = 10` и получает 11.
```

```quiz
id: test-enum-unknown
type: choice
part: "Значения вне перечисления"
prompt: Почему в `switch` по перечислению нужна ветка `default` или `_`, даже если перечислены все элементы?
options:
  - "В переменной может оказаться число, для которого нет элемента"
  - "Иначе switch не компилируется"
  - "default ускоряет switch"
  - "Не нужна, если перечислены все элементы"
answer: 0
explain: |
  Перечисление не ограничивает значения: из базы или JSON может прийти число без имени. Выражение `switch` без подходящей ветки бросит исключение.
```

```quiz
id: test-enum-isdefined
type: output
part: "Значения вне перечисления"
prompt: Что выведет код?
code: |
  Console.WriteLine(Enum.IsDefined((Day)0));

  enum Day { Mon = 1, Tue, Wed }
answer: "False"
explain: |
  Элементы — 1, 2, 3. Нуля среди них нет, хотя `(Day)0` — допустимое значение типа.
```

```quiz
id: test-enum-tostring-number
type: output
part: "Значения вне перечисления"
prompt: Что выведет код?
code: |
  Day d = (Day)5;
  Console.WriteLine(d);

  enum Day { Mon = 1, Tue, Wed }
answer: "5"
explain: |
  Для значения без имени `ToString()` возвращает число.
```

```quiz
id: test-flags-powers
type: choice
part: "Флаги"
prompt: Почему значения флагов делают степенями двойки?
options:
  - "Каждое значение занимает свой бит, и комбинации не перекрываются"
  - "Так быстрее сравнивать"
  - "Этого требует атрибут [Flags], иначе ошибка компиляции"
  - "Степени двойки меньше занимают памяти"
answer: 0
explain: |
  Если значение не степень двойки, оно совпадёт с комбинацией других флагов, и проверки станут неверными. Компилятор этого не проверяет.
```

```quiz
id: test-flags-tostring
type: output
part: "Флаги"
prompt: Что выведет код?
code: |
  Console.WriteLine(Days.Mon | Days.Wed);

  [Flags]
  enum Days { None = 0, Mon = 1, Tue = 2, Wed = 4 }
answer: Mon, Wed
explain: |
  С атрибутом `[Flags]` комбинация выводится списком имён через запятую.
```

```quiz
id: test-flags-check
type: output
part: "Флаги"
prompt: Что выведет код?
code: |
  Days d = Days.Mon | Days.Tue;
  Console.WriteLine((d & Days.Tue) != 0);

  [Flags]
  enum Days { None = 0, Mon = 1, Tue = 2, Wed = 4 }
answer: "True"
explain: |
  `&` оставляет только бит `Tue`, он установлен — результат не ноль.
variants:
  - code: |
      Days d = Days.Mon | Days.Tue;
      Console.WriteLine((d & Days.Wed) != 0);

      [Flags]
      enum Days { None = 0, Mon = 1, Tue = 2, Wed = 4 }
    answer: "False"
    explain: |
      Бита `Wed` в комбинации нет, `&` даёт ноль.
```

```quiz
id: test-flags-add
type: fill
part: "Флаги"
prompt: Вставьте оператор, чтобы добавить флаг `Delete` к имеющимся.
code: |
  access ___ Permissions.Delete;
answer: "|="
explain: |
  `|=` устанавливает бит `Delete` и не трогает остальные.
```

```quiz
id: test-enum-reflection
type: choice
part: "Флаги"
level: middle
prompt: Почему `Enum.ToString()` и `Enum.Parse` избегают в часто выполняемом коде?
options:
  - "Они используют рефлексию и выделяют память"
  - "Они бросают исключения при каждом вызове"
  - "Они не работают с флагами"
  - "Они меняют значение перечисления"
answer: 0
explain: |
  Поиск имени по значению идёт через метаданные типа. Для горячего кода используют собственный `switch` или генераторы кода.
```

::::
