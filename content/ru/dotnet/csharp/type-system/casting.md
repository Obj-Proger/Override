---
id: csharp.types.casting
title: Приведение типов
level: junior
tracks: [backend, fullstack]
minutes: 25
order: 2
requires: [csharp.types.value-reference]
quickCheck: [truncate, byte-wrap, round-banker, as-null]
testMinutes: 6
---

::::warmup

```quiz
id: truncate
type: output
prompt: Что выведет код?
code: |
  double price = 9.99;
  int whole = (int)price;
  Console.WriteLine(whole);
answer: "9"
explain: |
  Приведение дробного числа к целому отбрасывает дробную часть, а не округляет. 9.99 становится 9.
variants:
  - code: |
      double value = -2.7;
      Console.WriteLine((int)value);
    answer: "-2"
    explain: |
      Дробная часть отбрасывается в сторону нуля: -2.7 становится -2, а не -3.
  - code: |
      double value = 0.999;
      Console.WriteLine((int)(value * 10));
    answer: "9"
    explain: |
      `0.999 * 10` = 9.99, а приведение к `int` отбрасывает дробную часть.
```

::::

::::part Неявные преобразования

**Неявное преобразование** компилятор делает сам, если оно безопасно: любое значение исходного типа помещается в целевой.

```csharp
int count = 100;
long big = count;      // int → long
double ratio = count;  // int → double
float f = 1.5f;
double d = f;          // float → double
char c = 'A';
int code = c;          // char → int: 65
```

Неявные числовые преобразования идут «от меньшего к большему»:

| Из | Неявно в |
|---|---|
| `byte` | `short`, `int`, `long`, `float`, `double`, `decimal` |
| `int` | `long`, `float`, `double`, `decimal` |
| `long` | `float`, `double`, `decimal` |
| `char` | `int`, `long`, `float`, `double`, `decimal` |
| `float` | `double` |

Обратите внимание: в `decimal` целые числа превращаются неявно, а `float` и `double` — **нет**. У `decimal` меньше диапазон, и двоичная дробь не всегда точно ложится в десятичную.

**Ловушка с точностью.** `long` → `double` и `int` → `float` разрешены неявно, хотя могут потерять точность: `double` хранит около 15–16 значащих цифр, а `long` — до 19. Значение не обрежется, но младшие цифры могут исказиться:

```csharp
long id = 9_007_199_254_740_993;
double d = id;
Console.WriteLine((long)d);  // 9007199254740992 — последняя цифра потерялась
```

```quiz
id: implicit-ok
type: multi
prompt: Какие строки скомпилируются без явного приведения?
options:
  - "`long a = 5;`"
  - "`int b = 5L;`"
  - "`double c = 5;`"
  - "`decimal d = 2.5;`"
  - "`float e = 'A';`"
answer: [0, 2, 4]
explain: |
  `int` → `long`, `int` → `double` и `char` → `float` безопасны и неявны. `5L` — `long`, в `int` он может не поместиться. `2.5` — `double`, а `double` в `decimal` неявно не превращается.
```

::::

::::part Явное приведение

Если преобразование может потерять данные, его нужно написать явно — **приведение** `(тип)значение`. Этим вы говорите компилятору: «знаю, что могу потерять данные».

```csharp
double price = 9.99;
int whole = (int)price;  // 9: дробная часть отброшена
long big = 3_000_000_000;
int small = (int)big;    // -1294967296: лишние старшие биты отброшены
byte b = (byte)300;      // ошибка компиляции: 300 — константа, компилятор видит переполнение
```

Что происходит при явном приведении:

- **дробное → целое:** дробная часть отбрасывается к нулю: `(int)9.99` = 9, `(int)-9.99` = -9;
- **целое → меньшее целое:** остаются только младшие биты. `(byte)300` из переменной даёт 44: 300 − 256. Это то же «заворачивание», что и при переполнении;
- **в `checked`-контексте** выход за границы бросает `OverflowException` вместо тихого искажения.

С .NET 9 приведение слишком большого дробного числа к целому **насыщается**: `(int)1e20` даёт `int.MaxValue`, а `(int)double.NaN` — 0. Раньше результат зависел от процессора.

**Округление.** Приведение не округляет. Для округления есть `Math.Round`, и у него неожиданное поведение по умолчанию — **банковское округление**: число ровно посередине округляется к ближайшему **чётному**.

```csharp
Console.WriteLine(Math.Round(2.5));                                 // 2
Console.WriteLine(Math.Round(3.5));                                 // 4
Console.WriteLine(Math.Round(2.5, MidpointRounding.AwayFromZero));  // 3 — «школьное» округление
```

Банковское округление выбрано потому, что при сложении многих округлённых сумм оно не накапливает ошибку в одну сторону. Для «школьного» нужно явно указать `MidpointRounding.AwayFromZero`. Кроме того, есть `Math.Floor` (вниз), `Math.Ceiling` (вверх) и `Math.Truncate` (к нулю).

```quiz
id: byte-wrap
type: output
prompt: Что выведет код?
code: |
  int value = 300;
  byte b = (byte)value;
  Console.WriteLine(b);
answer: "44"
explain: |
  В `byte` помещаются числа от 0 до 255. Приведение оставляет только младшие 8 бит: 300 − 256 = 44. Ошибки нет: по умолчанию переполнение не проверяется.
variants:
  - code: |
      int value = 256;
      byte b = (byte)value;
      Console.WriteLine(b);
    answer: "0"
    explain: |
      256 — это ровно 2⁸: младшие 8 бит у него нулевые. Приведение к `byte` даёт 0.
  - code: |
      int value = -1;
      byte b = (byte)value;
      Console.WriteLine(b);
    answer: "255"
    explain: |
      У -1 все биты единичные. Младшие 8 единичных бит в `byte` — это 255.
```

```quiz
id: round-banker
type: output
prompt: Что выведет код?
code: |
  Console.WriteLine(Math.Round(2.5));
answer: "2"
explain: |
  По умолчанию `Math.Round` округляет середину к ближайшему чётному: 2.5 → 2. Для «школьного» округления нужно `MidpointRounding.AwayFromZero`.
variants:
  - code: |
      Console.WriteLine(Math.Round(3.5));
    answer: "4"
    explain: |
      Середина округляется к ближайшему чётному: из 3 и 4 чётное — 4.
  - code: |
      Console.WriteLine(Math.Round(2.5, MidpointRounding.AwayFromZero));
    answer: "3"
    explain: |
      `AwayFromZero` округляет середину от нуля — привычное «школьное» правило.
```

::::

::::part Convert, Parse и TryParse

Строку в число приводом не превратить: `(int)"42"` не компилируется, это разные по природе типы. Для разбора строк есть методы.

```csharp
int a = int.Parse("42");           // 42
int b = int.Parse("4x");           // FormatException: не число
int c = int.Parse("99999999999");  // OverflowException: не помещается в int

bool ok = int.TryParse("4x", out int d);  // ok = false, d = 0, исключения нет
```

- **`Parse`** бросает исключение, если строка не подходит. Его берут, когда неверный формат — действительно ошибка.
- **`TryParse`** возвращает `bool` и кладёт результат в `out`-параметр. Его берут для пользовательского ввода и любых данных, которые могут оказаться неверными: исключения дорогие и для обычной ситуации не нужны.

**`Convert`** — класс с методами `Convert.ToInt32`, `Convert.ToDouble` и другими. Отличия от `Parse`:

- `Convert.ToInt32(null)` возвращает 0, а `int.Parse(null)` бросает `ArgumentNullException`;
- `Convert` принимает не только строки: `Convert.ToInt32(3.7)` — это **округление** по банковскому правилу (4), а не отбрасывание, как у `(int)3.7` (3).

**Культура.** Разбор дробных чисел зависит от языка системы: на русской `double.Parse("3.14")` бросит исключение — там разделитель запятая. Данные, которые пишет программа (JSON, CSV, конфиги), разбирают с `CultureInfo.InvariantCulture`:

```csharp
double pi = double.Parse("3.14", CultureInfo.InvariantCulture);
```

Обратно в строку число превращает `ToString()`: `42.ToString()` — `"42"`.

```quiz
id: tryparse-fail
type: output
prompt: Что выведет код? `$"{ok} {n}"` подставляет значения через пробел.
code: |
  bool ok = int.TryParse("12.5", out int n);
  Console.WriteLine($"{ok} {n}");
answer: False 0
explain: |
  `"12.5"` — не целое число, поэтому `int.TryParse` возвращает `false` и записывает в `n` 0. Исключения нет.
variants:
  - code: |
      bool ok = int.TryParse(" 42 ", out int n);
      Console.WriteLine($"{ok} {n}");
    answer: True 42
    explain: |
      Пробелы по краям `int.TryParse` допускает по умолчанию, и строка разбирается как 42.
```

```quiz
id: convert-vs-cast
type: output
prompt: Что выведет код? `$"{a} {b}"` подставляет значения через пробел.
code: |
  double value = 3.7;
  int a = (int)value;
  int b = Convert.ToInt32(value);
  Console.WriteLine($"{a} {b}");
answer: 3 4
explain: |
  Приведение `(int)` отбрасывает дробную часть, а `Convert.ToInt32` округляет. Результаты различаются.
variants:
  - code: |
      double value = 2.5;
      int a = (int)value;
      int b = Convert.ToInt32(value);
      Console.WriteLine($"{a} {b}");
    answer: 2 2
    explain: |
      `(int)2.5` отбрасывает дробную часть — 2. `Convert.ToInt32(2.5)` округляет по банковскому правилу к чётному — тоже 2.
```

::::

::::part Ссылочные приведения: is и as

У ссылочных типов приведение не меняет объект, а меняет только то, **как на него смотрят**.

**Восходящее приведение** (к более общему типу) всегда безопасно и неявно: любая строка — это `object`.

```csharp
string text = "кот";
object obj = text;  // неявно: string — частный случай object
```

**Нисходящее приведение** (обратно к конкретному типу) пишется явно и проверяется во время выполнения:

```csharp
object obj = "кот";
string s = (string)obj;  // ок: внутри действительно строка
int[] a = (int[])obj;    // InvalidCastException: внутри не массив
```

Компилятор такие ошибки не ловит: у `obj` тип `object`, и в нём может оказаться что угодно. Чтобы не получить исключение, тип проверяют заранее:

**`is`** проверяет, подходит ли объект под тип, и сразу даёт переменную нужного типа. Это основной способ в современном C#:

```csharp
if (obj is string s)
    Console.WriteLine(s.Length);  // s — уже string
```

**`as`** пробует привести и возвращает `null` вместо исключения:

```csharp
string? s = obj as string;  // null, если obj — не строка
if (s != null)
    Console.WriteLine(s.Length);
```

`as` работает только с типами, которые могут быть `null`: со ссылочными и с `int?`. Записать `obj as int` нельзя — у `int` нет `null`, чтобы сообщить о неудаче.

Разница при неудаче:

| Запись | Если тип не подошёл |
|---|---|
| `(string)obj` | `InvalidCastException` |
| `obj as string` | `null` |
| `obj is string s` | `false`, переменная `s` не используется |

Для `null`: `(string)null` — просто `null`, `null as string` — `null`, `null is string` — `false`.

Иерархии классов, где такие приведения встречаются чаще всего, — тема раздела про ООП.

```quiz
id: as-null
type: output
prompt: Что выведет код?
code: |
  object obj = 42;
  string? s = obj as string;
  Console.WriteLine(s == null);
answer: "True"
explain: |
  В `obj` лежит упакованный `int`, а не строка. `as` при неудаче возвращает `null`, исключения нет.
variants:
  - code: |
      object obj = "42";
      Console.WriteLine(obj is string);
    answer: "True"
    explain: |
      В `obj` лежит строка, проверка `is string` истинна.
  - code: |
      object obj = 42;
      Console.WriteLine(obj is string);
    answer: "False"
    explain: |
      В `obj` лежит `int`, а не строка. `is` просто возвращает `false`.
```

```quiz
id: cast-throws
type: choice
prompt: Какая строка бросит исключение, если `object obj = "текст";`?
options:
  - "int[] a = (int[])obj;"
  - "string s = (string)obj;"
  - "int[]? b = obj as int[];"
  - "bool c = obj is int[];"
answer: 0
explain: |
  Явное приведение к неподходящему типу бросает `InvalidCastException`. `as` вернёт `null`, а `is` — `false`.
```

:::level middle

**Пользовательские преобразования.** Свой тип может объявить, как превращаться в другой: `implicit operator` — неявно, `explicit operator` — только с приведением. Например, тип «метры» можно неявно превращать в `double` и явно — обратно. Неявными делают только преобразования без потерь и исключений.

```csharp
public readonly record struct Meters(double Value)
{
    public static implicit operator double(Meters m) => m.Value;
    public static explicit operator Meters(double value) => new(value);
}

Meters height = (Meters)1.8;
double raw = height;  // неявно
```

**Приведение и упаковка.** Приведение значимого типа к `object` — это упаковка, а обратно — распаковка. У распаковки строгое правило: тип должен совпадать точно. Это следующая тема.

:::

::::

::::practice

```quiz
id: parse-or-default
type: code
lang: csharp
prompt: |
  Допишите метод: он превращает строку в число, а если это не удаётся — возвращает `fallback`. Исключений быть не должно.
starter: |
  public static class Parsing
  {
      public static int ToIntOr(string text, int fallback)
      {
      }
  }
tests: |
  Assert.Equal(42, Parsing.ToIntOr("42", -1));
  Assert.Equal(-1, Parsing.ToIntOr("сорок два", -1));
  Assert.Equal(0, Parsing.ToIntOr("99999999999", 0));
  Assert.Equal(-7, Parsing.ToIntOr("-7", 100));
solution: |
  public static class Parsing
  {
      public static int ToIntOr(string text, int fallback)
      {
          return int.TryParse(text, out int value) ? value : fallback;
      }
  }
explain: |
  `TryParse` не бросает исключений ни для неверного формата, ни для слишком большого числа. Результат — в `out`-переменной, а признак успеха решает, что вернуть.
variants:
  - prompt: |
      Допишите метод: он возвращает сумму тех элементов массива строк, которые являются целыми числами. Остальные пропускаются.
    starter: |
      public static class Parsing
      {
          public static int SumNumbers(string[] parts)
          {
          }
      }
    tests: |
      Assert.Equal(6, Parsing.SumNumbers(new[] { "1", "два", "5" }));
      Assert.Equal(0, Parsing.SumNumbers(new[] { "a", "" }));
      Assert.Equal(-3, Parsing.SumNumbers(new[] { "-4", "1", "1.5" }));
    solution: |
      public static class Parsing
      {
          public static int SumNumbers(string[] parts)
          {
              int sum = 0;
              foreach (string part in parts)
              {
                  if (int.TryParse(part, out int value))
                      sum += value;
              }
              return sum;
          }
      }
    explain: |
      `TryParse` для каждого элемента: если разобралось — прибавляем, если нет — пропускаем без исключений. `"1.5"` для `int` не подходит.
```

```quiz
id: is-pattern
type: fill
prompt: Вставьте проверку, чтобы внутри `if` можно было обращаться к строке через `s`.
code: |
  object obj = "привет";
  if (obj ___)
      Console.WriteLine(s.Length);
answer: is string s
explain: |
  `is string s` проверяет тип и сразу объявляет переменную `s` типа `string`. Она доступна там, где проверка точно прошла.
```

::::

::::traps

- **`(int)9.99` — это 9, а не 10.** Приведение отбрасывает дробную часть.
- **`Math.Round(2.5)` — это 2.** По умолчанию банковское округление к чётному.
- **`(int)` и `Convert.ToInt32` для дробных различаются:** первое отбрасывает, второе округляет.
- **`(byte)300` из переменной — это 44.** Явное приведение целых по умолчанию не проверяет переполнение.
- **`long` → `double` неявно, но с потерей точности** для больших значений.
- **`double.Parse` зависит от культуры.** Для машинных данных — `CultureInfo.InvariantCulture`.
- **Нисходящее приведение `(T)obj` может упасть.** Проверяют через `is T x`, `as` даёт `null`.

::::

::::summary

```quiz
id: casting-recall
type: recall
prompt: Расскажите, какие бывают преобразования типов в C# и чем отличаются (int), Convert, Parse, TryParse, is и as.
answer: |
  **Неявные** преобразования компилятор делает сам, когда они безопасны по диапазону: `int` → `long`, `int` → `double`, `char` → `int`. `long` → `double` неявно, но может потерять точность. `double` → `decimal` — только явно.
  **Явное приведение** `(T)x` разрешает потерю данных: дробное → целое отбрасывает дробную часть к нулю, целое → меньшее оставляет младшие биты. В `checked` выход за границы бросает `OverflowException`. `Math.Round` по умолчанию округляет середину к чётному.
  Строки разбирают **`Parse`** (исключение при ошибке) и **`TryParse`** (`bool` и `out`, без исключений — для ввода). **`Convert`** возвращает 0 для `null` и округляет дробные. Разбор дробных зависит от культуры.
  У ссылочных типов восходящее приведение неявно, нисходящее `(T)obj` проверяется при выполнении и бросает `InvalidCastException`. **`is T x`** проверяет и объявляет переменную, **`as`** возвращает `null` при неудаче и работает только с типами, допускающими `null`.
```

::::

::::test

```quiz
id: test-decimal-from-double
type: find-bug
part: "Неявные преобразования"
prompt: Код не компилируется. В какой строке ошибка?
code: |
  int count = 3;
  double price = 19.99;
  decimal total = price * count;
answer: 3
explain: |
  `price * count` — `double`, а `double` в `decimal` неявно не превращается. Нужно `(decimal)price * count` или хранить цену сразу в `decimal`.
```

```quiz
id: test-char-int
type: output
part: "Неявные преобразования"
prompt: Что выведет код? Код символа `'a'` — 97.
code: |
  int code = 'a';
  Console.WriteLine(code);
answer: "97"
explain: |
  `char` неявно превращается в `int` — свой код в Юникоде.
```

```quiz
id: test-long-to-int
type: choice
part: "Неявные преобразования"
prompt: Почему `int x = someLong;` не компилируется без приведения?
options:
  - "Значение long может не поместиться в int"
  - "long и int — несовместимые типы, приведение невозможно"
  - "int — значимый тип, а long — ссылочный"
  - "Неявные преобразования между числами запрещены"
answer: 0
explain: |
  Неявно разрешены только безопасные по диапазону преобразования. Из `long` в `int` — с потерей данных, поэтому только явно: `(int)someLong`.
```

```quiz
id: test-negative-cast
type: output
part: "Явное приведение"
prompt: Что выведет код?
code: |
  double value = -5.9;
  Console.WriteLine((int)value);
answer: "-5"
explain: |
  Приведение отбрасывает дробную часть к нулю: -5.9 → -5. Вниз округляет `Math.Floor`, он дал бы -6.
```

```quiz
id: test-checked-cast
type: choice
part: "Явное приведение"
prompt: Что произойдёт при выполнении кода?
code: |
  int value = 1000;
  byte b = checked((byte)value);
options:
  - "Будет брошено OverflowException"
  - "b станет 232"
  - "b станет 255"
  - "Ошибка компиляции"
answer: 0
explain: |
  В `checked` выход за границы `byte` (0–255) бросает исключение вместо тихого отбрасывания старших битов.
variants:
  - code: |
      int value = 1000;
      byte b = (byte)value;
    options:
      - "Будет брошено OverflowException"
      - "b станет 232"
      - "b станет 255"
      - "Ошибка компиляции"
    answer: 1
    explain: |
      Без `checked` остаются младшие 8 бит: 1000 − 3 × 256 = 232.
```

```quiz
id: test-floor
type: output
part: "Явное приведение"
prompt: Что выведет код?
code: |
  Console.WriteLine(Math.Floor(-2.3));
answer: "-3"
explain: |
  `Math.Floor` округляет вниз, к меньшему числу: для отрицательных это от нуля. Приведение `(int)-2.3` дало бы -2.
variants:
  - code: |
      Console.WriteLine(Math.Ceiling(2.1));
    answer: "3"
    explain: |
      `Math.Ceiling` округляет вверх, к большему числу.
```

```quiz
id: test-parse-format
type: choice
part: "Convert, Parse и TryParse"
prompt: Что сделает `int.Parse("12a")`?
options:
  - "Бросит FormatException"
  - "Вернёт 12"
  - "Вернёт 0"
  - "Вернёт false"
answer: 0
explain: |
  `Parse` не умеет частично разбирать строку и бросает исключение при неверном формате. Без исключения — `TryParse`.
```

```quiz
id: test-convert-null
type: output
part: "Convert, Parse и TryParse"
prompt: Что выведет код?
code: |
  string? text = null;
  Console.WriteLine(Convert.ToInt32(text));
answer: "0"
explain: |
  `Convert.ToInt32(null)` возвращает 0. `int.Parse(null)` бросил бы `ArgumentNullException`.
```

```quiz
id: test-culture
type: choice
part: "Convert, Parse и TryParse"
prompt: Почему `double.Parse("3.14")` может бросить исключение на компьютере с русской системой?
options:
  - "В русской культуре разделитель дробной части — запятая"
  - "double не умеет хранить 3.14"
  - "Parse работает только с целыми"
  - "Строка слишком длинная"
answer: 0
explain: |
  Разбор по умолчанию использует культуру системы. Для данных, записанных программой, передают `CultureInfo.InvariantCulture`.
```

```quiz
id: test-as-value
type: find-bug
part: "Ссылочные приведения: is и as"
prompt: Код не компилируется. В какой строке ошибка?
code: |
  object obj = 5;
  int number = obj as int;
  Console.WriteLine(number);
answer: 2
explain: |
  `as` при неудаче возвращает `null`, а у `int` нет `null`. Поэтому `as int` запрещён. Подходят `(int)obj`, `obj is int n` или `obj as int?`.
```

```quiz
id: test-upcast
type: choice
part: "Ссылочные приведения: is и as"
prompt: Какое приведение всегда безопасно и пишется неявно?
options:
  - "string → object"
  - "object → string"
  - "object → int[]"
  - "double → int"
answer: 0
explain: |
  Восходящее приведение к более общему типу: любая строка — это `object`. Обратное направление проверяется при выполнении.
```

```quiz
id: test-is-pattern-output
type: output
part: "Ссылочные приведения: is и as"
prompt: Что выведет код?
code: |
  object obj = "кошка";
  if (obj is string s && s.Length > 3)
      Console.WriteLine(s.ToUpper());
  else
      Console.WriteLine("нет");
answer: КОШКА
explain: |
  Проверка `is string s` прошла и объявила `s`. Благодаря `&&` вторая часть выполняется только после неё, и `s` уже можно использовать.
variants:
  - code: |
      object obj = 12345;
      if (obj is string s && s.Length > 3)
          Console.WriteLine(s.ToUpper());
      else
          Console.WriteLine("нет");
    answer: нет
    explain: |
      В `obj` лежит `int`, проверка `is string s` ложна, и до `s.Length` дело не доходит.
```

```quiz
id: test-implicit-operator
type: choice
part: "Ссылочные приведения: is и as"
level: middle
prompt: Когда пользовательское преобразование стоит делать неявным (implicit)?
options:
  - "Когда оно не теряет данных и не бросает исключений"
  - "Всегда: так удобнее"
  - "Когда оно может бросить исключение"
  - "Только для ссылочных типов"
answer: 0
explain: |
  Неявное преобразование срабатывает незаметно для читающего код. Если в нём возможна потеря данных или исключение, его делают явным, чтобы приведение было видно.
```

::::
