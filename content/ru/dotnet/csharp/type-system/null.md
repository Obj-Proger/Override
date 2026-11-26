---
id: csharp.types.null
title: null и Nullable
level: junior
tracks: [backend, fullstack]
minutes: 25
order: 4
requires: [csharp.types.boxing]
quickCheck: [null-length, nullable-sum, nullable-compare, nrt-runtime]
testMinutes: 6
---

::::warmup

```quiz
id: null-length
type: choice
prompt: Что произойдёт при выполнении кода?
code: |
  string? name = null;
  Console.WriteLine(name.Length);
options:
  - "Будет брошено NullReferenceException"
  - "Выведет 0"
  - "Выведет пустую строку"
  - "Ошибка компиляции"
answer: 0
explain: |
  В `name` нет объекта, а обратиться к члену несуществующего объекта нельзя. Компилятор в лучшем случае предупредит, но код соберётся, а при выполнении будет `NullReferenceException`.
variants:
  - code: |
      string? name = null;
      Console.WriteLine(name?.Length ?? 0);
    options:
      - "Будет брошено NullReferenceException"
      - "Выведет 0"
      - "Выведет пустую строку"
      - "Ошибка компиляции"
    answer: 1
    explain: |
      `name?.Length` при `null` даёт `null` вместо исключения, а `?? 0` заменяет его на 0.
```

::::

::::part null и NullReferenceException

**`null`** — значение ссылочной переменной, которая никуда не указывает: объекта нет. Это значение по умолчанию у всех ссылочных типов: у полей, элементов массивов, переменных `string`, `int[]`, `object`.

Обратиться к члену через `null` нельзя. Программа бросает **`NullReferenceException`** — самое частое исключение в .NET:

```csharp
string? text = null;
int length = text.Length;  // NullReferenceException
```

Компилятор не может поймать все такие ошибки: значение может прийти из базы данных, из сети или от пользователя. Поэтому `null` проверяют:

```csharp
if (text is null)  // самый надёжный способ
    Console.WriteLine("текста нет");

if (text is not null)
    Console.WriteLine(text.Length);

if (text != null) { }  // тоже работает
```

Почему `is null` лучше `== null`: оператор `==` можно переопределить в своём типе, и он начнёт делать что-то своё. `is null` всегда проверяет именно ссылку.

```quiz
id: is-null
type: output
prompt: Что выведет код?
code: |
  string?[] names = new string?[2];
  Console.WriteLine(names[0] is null);
answer: "True"
explain: |
  Новый массив заполнен значениями по умолчанию. Для ссылочного типа `string` это `null`.
variants:
  - code: |
      string?[] names = { "Анна", null };
      Console.WriteLine(names[1] is not null);
    answer: "False"
    explain: |
      Второй элемент — `null`, поэтому `is not null` даёт `False`.
```

::::

::::part Nullable<T>: null у значимых типов

У значимого типа всегда есть значение: `int` не может быть `null`. Но иногда «значения нет» — нормальная ситуация: возраст не указан, оценка ещё не выставлена, поле в базе пустое. Для этого есть **`Nullable<T>`**, коротко `T?`:

```csharp
int? age = null;  // то же, что Nullable<int> age = null;
age = 30;

if (age.HasValue)
    Console.WriteLine(age.Value);  // 30

int years = age ?? 0;                 // значение или 0
int other = age.GetValueOrDefault();  // значение или default(int)
```

`int?` — это структура с двумя полями: само значение и флаг `HasValue`. Обращение к `.Value`, когда значения нет, бросает `InvalidOperationException`.

**Арифметика с `null`.** Если в выражении есть `null`, результат тоже `null`:

```csharp
int? a = null;
int? b = a + 1;  // null
```

**Сравнения с `null`** дают `false` — все, включая противоположные:

```csharp
int? x = null;
Console.WriteLine(x > 0);      // False
Console.WriteLine(x <= 0);     // False — тоже False!
Console.WriteLine(x == null);  // True
```

Отсюда ловушка: `if (x > 0) ... else ...` отправит `null` в ветку `else`, как будто это «меньше или равно нулю».

```quiz
id: nullable-sum
type: output
prompt: Что выведет код?
code: |
  int? a = null;
  int? b = a + 5;
  Console.WriteLine(b.HasValue);
answer: "False"
explain: |
  Арифметика с `null` даёт `null`: неизвестное плюс 5 — всё ещё неизвестное. У `b` нет значения.
variants:
  - code: |
      int? a = 3;
      int? b = a + 5;
      Console.WriteLine(b);
    answer: "8"
    explain: |
      У `a` есть значение, поэтому сложение обычное: 3 + 5.
  - code: |
      int? a = null;
      int b = (a + 5) ?? -1;
      Console.WriteLine(b);
    answer: "-1"
    explain: |
      `a + 5` даёт `null`, и `??` заменяет его на -1.
```

```quiz
id: nullable-compare
type: output
prompt: Что выведет код? `$"{a} {b}"` подставляет значения через пробел.
code: |
  int? x = null;
  bool a = x > 0;
  bool b = x <= 0;
  Console.WriteLine($"{a} {b}");
answer: False False
explain: |
  Сравнение с `null` даёт `false` в любую сторону: неизвестно, больше ли значение нуля, и неизвестно, меньше ли. Поэтому обе проверки ложны.
variants:
  - code: |
      int? x = null;
      bool a = x == null;
      bool b = x != 5;
      Console.WriteLine($"{a} {b}");
    answer: True True
    explain: |
      Равенство с `null` работает как обычно: `x == null` истинно. `null != 5` тоже истинно — значения разные.
```

::::

::::part Операторы для null

Вы уже знаете три оператора, теперь их можно использовать полностью:

```csharp
string? name = GetName();

int length = name?.Length ?? 0;  // ?. — обратиться, только если не null
string shown = name ?? "гость";  // ?? — заменить null
name ??= "гость";                // ??= — присвоить, если null
char? first = name?[0];          // ?[] — индекс, только если не null
```

**Цепочки.** `?.` прерывает всю цепочку: если `order` равен `null`, `order?.Customer.Name` вернёт `null` и не обратится ни к `Customer`, ни к `Name`. Но если `null` может быть и `Customer`, нужен второй `?.`: `order?.Customer?.Name`.

**Результат `?.` всегда допускает `null`.** `name?.Length` — это `int?`, а не `int`, потому что при `null` результат — `null`. Поэтому после `?.` часто ставят `??`.

**Присваивание через `?.`** (C# 14): `order?.Status = "оплачен";` — присвоит, только если `order` не `null`.

**Вызов события или делегата** пишут как `handler?.Invoke(args)` — об этом в теме про события.

```quiz
id: chain-null
type: output
prompt: Что выведет код? `text?.Trim().Length` — длина строки после удаления пробелов.
code: |
  string? text = null;
  int length = text?.Trim().Length ?? -1;
  Console.WriteLine(length);
answer: "-1"
explain: |
  `text` равен `null`, и `?.` обрывает всю цепочку: ни `Trim`, ни `Length` не вызываются, результат — `null`. `?? -1` превращает его в -1.
variants:
  - code: |
      string? text = "  кот  ";
      int length = text?.Trim().Length ?? -1;
      Console.WriteLine(length);
    answer: "3"
    explain: |
      `text` не `null`, поэтому цепочка выполняется целиком: `Trim()` убирает пробелы, длина — 3.
```

::::

::::part Nullable reference types

Долгое время любая переменная `string` могла оказаться `null`, и компилятор об этом молчал. С C# 8 есть **nullable reference types** (NRT): ссылочные типы делятся на допускающие `null` и не допускающие.

```csharp
string name = "Анна";     // не должна быть null
string? nickname = null;  // может быть null
```

Режим включается в `.csproj` настройкой `<Nullable>enable</Nullable>` — в новых проектах он включён по умолчанию. Компилятор следит за потоком значений и **предупреждает**:

```csharp
string? nickname = GetNickname();
Console.WriteLine(nickname.Length);  // предупреждение: возможно разыменование null

if (nickname is not null)
    Console.WriteLine(nickname.Length);  // предупреждения нет: компилятор видит проверку

string name = null;  // предупреждение: null в переменную, которая не должна его содержать
```

Главное, что спрашивают на собеседованиях: **NRT — это только проверка при компиляции.** Во время выполнения `string` и `string?` — один и тот же тип, никаких проверок не добавляется. Код с предупреждениями соберётся, и `null` в `string` всё равно может оказаться — например, из старой библиотеки или из JSON.

**Оператор `!`** (null-forgiving) говорит компилятору: «я знаю, что здесь не `null`». Предупреждение пропадает, но проверки нет:

```csharp
string name = FindName()!;  // если FindName вернул null, исключение будет позже
```

Используют `!` редко — там, где программист знает больше компилятора. Частое `!` по коду — признак того, что проверки `null` просто заглушены.

```quiz
id: nrt-runtime
type: choice
prompt: Что делает включённый режим nullable reference types?
options:
  - "Компилятор предупреждает о возможном null; во время выполнения ничего не проверяется"
  - "Переменная string больше никогда не может стать null"
  - "Во время выполнения при записи null в string бросается исключение"
  - "string? хранится в куче, а string — в стеке"
answer: 0
explain: |
  NRT — аннотации и анализ при компиляции. В скомпилированном коде `string` и `string?` — один тип, `null` по-прежнему возможен.
```

```quiz
id: null-forgiving
type: choice
prompt: Что делает `!` в строке `string name = FindName()!;`?
options:
  - "Убирает предупреждение компилятора о возможном null, не добавляя проверки"
  - "Бросает исключение, если FindName вернул null"
  - "Заменяет null на пустую строку"
  - "Отрицает результат FindName"
answer: 0
explain: |
  Null-forgiving оператор действует только на анализ компилятора. Если значение окажется `null`, ошибка проявится позже, при обращении к нему.
```

:::level middle

**Атрибуты для анализа.** Компилятор не видит, что делает метод внутри другой сборки. Подсказки дают атрибуты:

- `[NotNullWhen(true)]` у `out`-параметра: если метод вернул `true`, значение не `null` — так размечен `Dictionary.TryGetValue`;
- `[MaybeNull]`, `[NotNull]`, `[MemberNotNull]` — для полей и результатов;
- `string.IsNullOrEmpty(text)` размечен так, что после `if (!string.IsNullOrEmpty(text))` компилятор знает: `text` не `null`.

**Упаковка `Nullable<T>`.** Упакованный `int?` со значением становится обычным упакованным `int`, а без значения — просто `null`, без обёртки. Поэтому `object o = (int?)null;` даёт `o == null`.

:::

::::

::::practice

```quiz
id: length-or-zero
type: code
lang: csharp
prompt: |
  Допишите метод: он возвращает длину строки без пробелов по краям, а если строки нет (`null`) — 0. Исключений быть не должно.
starter: |
  public static class Text
  {
      public static int TrimmedLength(string? text)
      {
      }
  }
tests: |
  Assert.Equal(3, Text.TrimmedLength("  кот "));
  Assert.Equal(0, Text.TrimmedLength(null));
  Assert.Equal(0, Text.TrimmedLength("   "));
solution: |
  public static class Text
  {
      public static int TrimmedLength(string? text)
      {
          return text?.Trim().Length ?? 0;
      }
  }
explain: |
  `?.` обрывает цепочку на `null`, а `??` подставляет 0. Одна строка вместо `if`.
variants:
  - prompt: |
      Допишите метод: он превращает строку в число, а если строка пустая, `null` или не число — возвращает `null`.
    starter: |
      public static class Parsing
      {
          public static int? ToIntOrNull(string? text)
          {
          }
      }
    tests: |
      Assert.Equal((int?)42, Parsing.ToIntOrNull("42"));
      Assert.Null(Parsing.ToIntOrNull(null));
      Assert.Null(Parsing.ToIntOrNull("сорок"));
    solution: |
      public static class Parsing
      {
          public static int? ToIntOrNull(string? text)
          {
              return int.TryParse(text, out int value) ? value : null;
          }
      }
    explain: |
      `int?` позволяет вернуть «значения нет», не придумывая особое число вроде -1. `TryParse` принимает `null` и просто возвращает `false`.
```

```quiz
id: coalesce-assign
type: fill
prompt: Вставьте оператор, чтобы присвоить значение, только если в переменной сейчас `null`.
code: |
  string? title = null;
  title ___ "Без названия";
answer: "??="
explain: |
  `??=` присваивает правую часть, только если левая равна `null`.
```

::::

::::traps

- **Обращение к члену через `null` — `NullReferenceException`.** Компилятор ловит не всё.
- **`int?` сравнивается с `null` как «ложь»:** и `x > 0`, и `x <= 0` при `null` — `false`.
- **Арифметика с `null` даёт `null`.**
- **`.Value` у пустого `int?` бросает `InvalidOperationException`.**
- **`?.` возвращает тип, допускающий `null`:** `text?.Length` — `int?`.
- **NRT не проверяет ничего во время выполнения.** Это предупреждения компилятора.
- **`!` глушит предупреждение, а не проверяет значение.**
- **`is null` надёжнее `== null`:** оператор `==` можно переопределить.

::::

::::summary

```quiz
id: null-recall
type: recall
prompt: Расскажите, что такое null, Nullable<T> и nullable reference types и чем они отличаются.
answer: |
  **`null`** — ссылка, которая никуда не указывает; значение по умолчанию ссылочных типов. Обращение к члену через `null` бросает `NullReferenceException`. Проверяют через `is null` / `is not null`.
  **`Nullable<T>`** (`int?`) — структура, которая добавляет значимому типу состояние «нет значения»: `HasValue`, `Value` (исключение без значения), `GetValueOrDefault`. Арифметика с `null` даёт `null`, сравнения `<`, `>` с `null` — `false`.
  Операторы `?.`, `?[]`, `??`, `??=` работают с `null` без исключений; результат `?.` допускает `null`.
  **Nullable reference types** (C# 8, `<Nullable>enable</Nullable>`) делят ссылочные типы на `string` и `string?` и дают предупреждения при компиляции. Во время выполнения ничего не проверяется. `!` глушит предупреждение без проверки.
```

::::

::::test

```quiz
id: test-default-reference
type: choice
part: "null и NullReferenceException"
prompt: Чему равно поле типа `object`, если ему ничего не присвоили?
options:
  - "null"
  - "Пустому объекту"
  - "0"
  - "Такое поле нельзя оставить без значения"
answer: 0
explain: |
  Значение по умолчанию у любого ссылочного типа — `null`.
```

```quiz
id: test-nre-where
type: find-bug
part: "null и NullReferenceException"
prompt: В какой строке программа упадёт?
code: |
  string?[] words = new string?[3];
  words[0] = "кот";
  int total = 0;
  foreach (string? word in words)
      total += word.Length;
  Console.WriteLine(total);
answer: 5
explain: |
  Элементы 1 и 2 так и остались `null`. На втором проходе `word.Length` бросит `NullReferenceException`. Защита — `word?.Length ?? 0`.
```

```quiz
id: test-is-null-why
type: choice
part: "null и NullReferenceException"
prompt: Почему для проверки на `null` советуют `x is null`, а не `x == null`?
options:
  - "Оператор == можно переопределить в типе, а is null всегда сравнивает ссылку"
  - "x == null не компилируется"
  - "is null быстрее в десять раз"
  - "== null бросает исключение, если x равен null"
answer: 0
explain: |
  Обычно оба варианта работают одинаково. Но тип со своим `==` может сравнивать по-своему, а `is null` от этого не зависит.
```

```quiz
id: test-value-exception
type: choice
part: "Nullable<T>: null у значимых типов"
prompt: Что произойдёт при `int? x = null; int y = x.Value;`?
options:
  - "Будет брошено InvalidOperationException"
  - "y станет 0"
  - "Ошибка компиляции"
  - "y станет null"
answer: 0
explain: |
  `.Value` можно читать, только когда `HasValue` равно `true`. Безопасные варианты — `x ?? 0` или `x.GetValueOrDefault()`.
```

```quiz
id: test-nullable-type
type: choice
part: "Nullable<T>: null у значимых типов"
prompt: Что такое `int?`?
options:
  - "Структура Nullable<int>: значение и флаг HasValue"
  - "Ссылка на int в куче"
  - "Тип int, в который компилятор разрешает писать null"
  - "Псевдоним для object"
answer: 0
explain: |
  `int?` — значимый тип `Nullable<int>`. Он хранит число и признак, есть ли значение.
```

```quiz
id: test-nullable-if
type: output
part: "Nullable<T>: null у значимых типов"
prompt: Что выведет код?
code: |
  int? score = null;
  if (score >= 50)
      Console.WriteLine("сдал");
  else
      Console.WriteLine("не сдал");
answer: не сдал
explain: |
  Сравнение с `null` даёт `false`, поэтому выполняется `else`, хотя оценки просто нет. Такой случай лучше проверять отдельно: `if (score is null)`.
```

```quiz
id: test-get-value-or-default
type: output
part: "Nullable<T>: null у значимых типов"
prompt: Что выведет код?
code: |
  double? price = null;
  Console.WriteLine(price.GetValueOrDefault());
answer: "0"
explain: |
  Без значения `GetValueOrDefault` возвращает значение по умолчанию типа — 0 для `double`.
```

```quiz
id: test-index-null
type: output
part: "Операторы для null"
prompt: Что выведет код?
code: |
  int[]? numbers = null;
  int first = numbers?[0] ?? -1;
  Console.WriteLine(first);
answer: "-1"
explain: |
  `?[]` не обращается к индексу, если массива нет, и даёт `null`. `?? -1` подставляет -1.
variants:
  - code: |
      int[]? numbers = { 7, 8 };
      int first = numbers?[0] ?? -1;
      Console.WriteLine(first);
    answer: "7"
    explain: |
      Массив есть, поэтому `?[0]` возвращает первый элемент.
```

```quiz
id: test-conditional-type
type: choice
part: "Операторы для null"
prompt: Какой тип у выражения `text?.Length`, если `text` имеет тип `string?`?
options:
  - "int?"
  - "int"
  - "string"
  - "object"
answer: 0
explain: |
  При `text == null` выражение даёт `null`, а `int` его не вмещает. Поэтому результат — `int?`.
```

```quiz
id: test-nrt-enable
type: choice
part: "Nullable reference types"
prompt: Где включается режим nullable reference types для проекта?
options:
  - "В .csproj: <Nullable>enable</Nullable>"
  - "В Program.cs: using Nullable;"
  - "Он включён всегда, выключить нельзя"
  - "В настройках операционной системы"
answer: 0
explain: |
  Настройка проекта `Nullable`. В новых проектах по шаблону она уже включена. Для отдельного файла есть директива `#nullable enable`.
```

```quiz
id: test-nrt-warning
type: choice
part: "Nullable reference types"
prompt: Что сделает компилятор со строкой `string name = null;` при включённых NRT?
options:
  - "Выдаст предупреждение, но код соберётся"
  - "Выдаст ошибку компиляции"
  - "Заменит null на пустую строку"
  - "Ничего"
answer: 0
explain: |
  Нарушения правил NRT — предупреждения. Превратить их в ошибки можно настройкой `TreatWarningsAsErrors` или `WarningsAsErrors`.
```

```quiz
id: test-nrt-same-type
type: choice
part: "Nullable reference types"
prompt: Чем `string` и `string?` различаются во время выполнения программы?
options:
  - "Ничем: это один тип, разница только для компилятора"
  - "string? занимает больше памяти"
  - "string? — это Nullable<string>"
  - "В string нельзя записать null: CLR бросит исключение"
answer: 0
explain: |
  Для ссылочных типов `?` — аннотация. `Nullable<T>` существует только для значимых типов.
```

```quiz
id: test-not-null-when
type: choice
part: "Nullable reference types"
level: middle
prompt: Зачем у `out`-параметра метода `TryGetValue` стоит атрибут `[NotNullWhen(true)]`?
options:
  - "Чтобы компилятор знал: если метод вернул true, значение не null"
  - "Чтобы во время выполнения проверить значение на null"
  - "Чтобы метод не мог вернуть false"
  - "Чтобы значение было неизменяемым"
answer: 0
explain: |
  Атрибуты nullable-анализа описывают поведение метода для компилятора. После `if (dict.TryGetValue(key, out var value))` предупреждений о `value` не будет.
```

::::
