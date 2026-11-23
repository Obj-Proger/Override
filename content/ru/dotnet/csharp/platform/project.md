---
id: csharp.platform.project
title: Программа и проект
level: junior
tracks: [backend, fullstack]
minutes: 15
order: 2
requires: [csharp.platform.dotnet]
quickCheck: [args-count, target-framework, one-file, using-meaning]
testMinutes: 6
---

::::warmup

```quiz
id: args-count
type: output
prompt: |
  В проекте один файл `Program.cs` с единственной строкой ниже. Программу запустили командой `dotnet run -- one two`: всё, что после `--`, передаётся программе как аргументы. Что она выведет?
code: |
  Console.WriteLine(args.Length);
answer: "2"
explain: |
  Ни класса, ни метода `Main` здесь нет, но программа работает, и переменная `args` откуда-то взялась. Её создал компилятор вместе с методом `Main`. Аргументов два: `one` и `two`.
variants:
  - prompt: |
      В проекте один файл `Program.cs` с единственной строкой ниже. Программу запустили командой `dotnet run -- a b c`: всё, что после `--`, передаётся программе как аргументы. Что она выведет?
    answer: "3"
    explain: |
      Переменная `args` — параметр метода `Main`, который компилятор создал из операторов файла. В ней три аргумента: `a`, `b` и `c`.
  - prompt: |
      В проекте один файл `Program.cs` с единственной строкой ниже. Программу запустили командой `dotnet run` без аргументов. Что она выведет?
    answer: "0"
    explain: |
      Аргументов нет, поэтому массив `args` пустой. Это обычный параметр метода `Main`, который компилятор создал из операторов файла.
```

::::

::::part Проект

Программа на C# живёт в **проекте**. Создадим его:

```shell
dotnet new console -n Hello
```

Команда создаёт папку `Hello` с двумя файлами:

```text
Hello/
  Hello.csproj  // описание проекта
  Program.cs    // код
```

`Program.cs` содержит одну строку: `Console.WriteLine("Hello, World!");`. Главное — в `Hello.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

| Строка | Что значит |
|---|---|
| `Sdk="Microsoft.NET.Sdk"` | тип проекта: обычный. У веб-проекта здесь `Microsoft.NET.Sdk.Web` |
| `OutputType` | `Exe` — программа, которую запускают. Без этой строки получится библиотека, только `.dll` |
| `TargetFramework` | под какую версию .NET собирать: `net10.0`, `net11.0` |
| `ImplicitUsings` | подключить самые нужные пространства имён во всех файлах, о них — ниже |
| `Nullable` | предупреждать о возможном `null`, об этом — в теме про null |

Все `.cs`-файлы в папке проекта компилируются автоматически, перечислять их не нужно.

**Зависимости** тоже описываются в `.csproj`. NuGet-пакет и другой проект подключаются так:

```xml
<ItemGroup>
  <PackageReference Include="Serilog" Version="4.2.0" />
  <ProjectReference Include="..\Shop.Core\Shop.Core.csproj" />
</ItemGroup>
```

**Решение** (solution) объединяет несколько проектов: например, веб-приложение, библиотеку с логикой и тесты. Это файл `.slnx` (до .NET 10 по умолчанию был `.sln`), его открывают в Visual Studio или Rider.

**Команды `dotnet`**, которые нужны каждый день:

| Команда | Что делает |
|---|---|
| `dotnet new console -n Hello` | создаёт проект по шаблону |
| `dotnet build` | восстанавливает пакеты и собирает проект |
| `dotnet run` | собирает и сразу запускает |
| `dotnet test` | собирает и запускает тесты |
| `dotnet add package Serilog` | добавляет NuGet-пакет в `.csproj` |
| `dotnet publish -c Release` | готовит программу к выкладке на сервер |

Результат сборки — в `bin/Debug/net10.0/`: там `Hello.dll` и всё, что нужно для запуска. В папке `obj/` лежат промежуточные файлы. Обе папки создаются заново при каждой сборке и в Git не добавляются.

**Debug и Release.** По умолчанию проект собирается в конфигурации **Debug**: без оптимизаций, с подробной информацией для отладчика. **Release** (`-c Release`) включает оптимизации компилятора — так собирают для продакшена.

```quiz
id: target-framework
type: choice
prompt: Где указано, под какую версию .NET собирается проект?
options:
  - "В первой строке Program.cs"
  - "В файле .csproj, в свойстве TargetFramework"
  - "В файле решения .slnx"
  - "В папке bin"
answer: 1
explain: |
  `<TargetFramework>net10.0</TargetFramework>` в `.csproj` задаёт версию. Решение только перечисляет проекты, а `bin` — результат сборки.
variants:
  - prompt: Где перечислены NuGet-пакеты, от которых зависит проект?
    options:
      - "В Program.cs, директивами using"
      - "В файле .csproj, элементами PackageReference"
      - "В папке obj"
      - "В файле решения .slnx"
    answer: 1
    explain: |
      Зависимости проекта описаны в `.csproj`: `PackageReference` для пакетов, `ProjectReference` для других проектов. `using` только сокращает имена.
```

```quiz
id: run-command
type: choice
prompt: Какая команда собирает проект и сразу запускает программу?
options:
  - "dotnet build"
  - "dotnet run"
  - "dotnet new"
  - "dotnet publish"
answer: 1
explain: |
  `dotnet run` сначала собирает проект, если код изменился, потом запускает. `dotnet build` только собирает.
```

::::

::::part Точка входа: Main и top-level statements

Выполнение программы начинается с метода **`Main`** — точки входа. Классическая программа выглядит так:

```csharp
namespace Hello;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}
```

Правила для `Main`:

- метод статический (`static`) — его вызывают без создания объекта;
- возвращает `void` или `int`. Число — код завершения: 0 значит успех, другое — ошибка. Его видят скрипты и CI;
- параметр `string[] args` с аргументами командной строки необязателен;
- если внутри нужен `await`, метод возвращает `Task` или `Task<int>`: `static async Task Main()`.

**Top-level statements** (операторы верхнего уровня) убирают обёртку: код пишется прямо в файле, без класса и метода.

```csharp
Console.WriteLine("Hello, World!");
```

Это не другой вид программы. Компилятор сам создаёт класс `Program` и метод `Main` и переносит код внутрь. Поэтому:

- в файле доступна переменная **`args`** — это параметр сгенерированного `Main`;
- `return 1;` делает `Main` возвращающим `int`, а `await` — асинхронным;
- такой файл может быть **только один** на проект: точка входа одна;
- классы и другие типы можно объявить в том же файле, но **после** операторов.

```csharp
Console.WriteLine(Greeter.Hello());

class Greeter
{
    public static string Hello() => "Привет";
}
```

Новый проект по шаблону создаётся с top-level statements. В примерах на этом сайте код тоже пишется так, если не сказано другое.

```quiz
id: one-file
type: choice
prompt: Сколько файлов в одном проекте могут содержать top-level statements?
options:
  - "Ни одного: нужен метод Main"
  - "Только один"
  - "Сколько угодно"
  - "По одному в каждой папке"
answer: 1
explain: |
  Top-level statements становятся телом `Main`, а точка входа у программы одна. Если операторы верхнего уровня есть в двух файлах, проект не соберётся.
variants:
  - prompt: Где в файле с top-level statements можно объявить класс?
    options:
      - "Только до операторов верхнего уровня"
      - "Только после операторов верхнего уровня"
      - "В любом месте файла"
      - "Нигде: классы должны быть в других файлах"
    answer: 1
    explain: |
      Операторы верхнего уровня идут первыми, типы — после них. Класс перед операторами — ошибка компиляции.
```

```quiz
id: order-bug
type: find-bug
prompt: Файл с top-level statements не компилируется. В какой строке ошибка?
code: |
  class Greeter
  {
      public static string Hello() => "Привет";
  }

  Console.WriteLine(Greeter.Hello());
answer: 6
explain: |
  Операторы верхнего уровня должны идти до объявлений типов. Компилятор отмечает именно строку с оператором после класса. Достаточно перенести `Console.WriteLine` в начало файла.
```

:::level middle

**Что генерирует компилятор.** Для top-level statements создаётся класс `Program` с методом, у которого служебное имя `<Main>$`: вызвать его из своего кода нельзя. Класс `Program` при этом обычный, и его можно дополнить через `partial`:

```csharp
public partial class Program { }
```

Так делают в веб-проектах на ASP.NET Core: интеграционным тестам нужно сослаться на класс `Program` приложения, а сгенерированный по умолчанию не публичный.

**Программа из одного файла.** С .NET 10 можно запустить файл без проекта: `dotnet run app.cs`. Пакеты подключаются директивой прямо в файле: `#:package Serilog@4.2.0`. Удобно для скриптов и экспериментов. Когда программа вырастает, `dotnet project convert app.cs` превращает её в обычный проект.

:::

::::

::::part Пространства имён и using

**Пространство имён** (namespace) группирует типы и не даёт одинаковым именам конфликтовать. Полное имя типа включает пространство: `Console` — это `System.Console`, а `List<T>` — `System.Collections.Generic.List<T>`.

Свои типы объявляют в пространстве. Обычно его имя повторяет путь к папке: файл `Orders/Order.cs` в проекте `Shop` — пространство `Shop.Orders`.

```csharp
namespace Shop.Orders;  // для всего файла

class Order { }
```

Старая форма с фигурными скобками `namespace Shop.Orders { ... }` работает так же, но добавляет лишний уровень отступа. В новом коде пишут вариант с точкой с запятой.

**Директива `using`** позволяет писать короткое имя вместо полного:

```csharp
using System.Text;

var builder = new StringBuilder();  // вместо System.Text.StringBuilder
```

`using` ничего не загружает и не подключает: он только сокращает имена. Какие сборки и пакеты доступны, решает `.csproj`. Тип из неподключённого пакета не появится от того, что написали `using`.

Другие формы:

```csharp
using static System.Math;       // члены класса без имени класса: Sqrt(16) вместо Math.Sqrt(16)
using Json = System.Text.Json;  // псевдоним: помогает, когда два пространства содержат одинаковые имена
global using System.Text.Json;  // действует во всех файлах проекта
```

**ImplicitUsings.** Настройка `<ImplicitUsings>enable</ImplicitUsings>` добавляет `global using` для самых частых пространств. Для консольного проекта это `System`, `System.Collections.Generic`, `System.IO`, `System.Linq`, `System.Net.Http`, `System.Threading` и `System.Threading.Tasks`. Поэтому `Console.WriteLine` работает без `using System;`.

```quiz
id: using-meaning
type: choice
prompt: Что делает строка `using System.Text;` в начале файла?
options:
  - "Подключает к проекту сборку System.Text"
  - "Позволяет писать имена типов из System.Text без префикса"
  - "Загружает пространство имён в память при запуске"
  - "Копирует код классов в текущий файл"
answer: 1
explain: |
  `using` — подсказка компилятору, где искать короткие имена. На то, какие сборки доступны, он не влияет: это задаёт `.csproj`.
variants:
  - prompt: Пакет Serilog в проект не добавлен, но в начале файла написано `using Serilog;`. Что произойдёт?
    options:
      - "Пакет скачается автоматически при сборке"
      - "Ошибка компиляции: пространство имён не найдено"
      - "Код соберётся, а using просто ничего не сделает"
      - "Программа соберётся, но упадёт при запуске"
    answer: 1
    explain: |
      `using` только сокращает имена и ничего не подключает. Пакета нет в `.csproj`, значит нет и пространства имён `Serilog`, и компилятор сообщит об ошибке.
```

```quiz
id: using-static
type: fill
prompt: Вставьте директиву, чтобы код компилировался.
code: |
  ___ System.Math;

  Console.WriteLine(Sqrt(16));
answer: using static
explain: |
  `using static` открывает статические члены класса: `Sqrt` вместо `Math.Sqrt`.
```

::::

::::part Ошибки компиляции и ошибки выполнения

Ошибки бывают двух видов, и на собеседованиях часто спрашивают, какая где:

- **Ошибка компиляции** (compile time) — компилятор нашёл проблему при сборке: не тот тип, неизвестное имя, пропущенная скобка. Программа не собирается и не запускается вовсе.
- **Ошибка выполнения** (runtime) — код собрался, но во время работы что-то пошло не так. Программа бросает **исключение** и, если его никто не обработал, завершается.

```csharp
int a = "5";             // ошибка компиляции: строку нельзя положить в int
int b = int.Parse("x");  // собирается, но при выполнении — исключение: "x" не число
```

Компилятор проверяет то, что видно из кода: типы, имена, синтаксис. Значения, которые появятся только при запуске (ввод пользователя, содержимое файла), он проверить не может.

Кроме ошибок компилятор выдаёт **предупреждения** (warnings). Программа с ними собирается, но они часто указывают на настоящую проблему, например на возможный `null`. В рабочих проектах предупреждения стараются не копить, а иногда включают `TreatWarningsAsErrors`, чтобы они останавливали сборку.

```quiz
id: compile-or-runtime
type: choice
prompt: Что произойдёт с этим кодом?
code: |
  string text = "abc";
  int number = int.Parse(text);
  Console.WriteLine(number);
options:
  - "Ошибка компиляции во второй строке"
  - "Программа соберётся, но упадёт при выполнении второй строки"
  - "Выведет 0"
  - "Выведет abc"
answer: 1
explain: |
  Типы в порядке: `int.Parse` принимает строку и возвращает `int`. Что `"abc"` — не число, выяснится только при выполнении: метод бросит исключение.
```

::::

::::practice

```quiz
id: main-parsons
type: parsons
prompt: Соберите программу с классическим методом Main, которая выводит «Привет».
lines:
  - "namespace Hello;"
  - "class Program"
  - "{"
  - "    static void Main()"
  - "    {"
  - "        Console.WriteLine(\"Привет\");"
  - "    }"
  - "}"
distractors:
  - "    void Main()"
explain: |
  Точка входа — статический метод `Main`. Без `static` компилятор не найдёт точку входа: для обычного метода нужен объект, а создавать его некому.
```

```quiz
id: main-exit-code
type: choice
prompt: Чем закончится программа из одного файла с такими строками?
code: |
  Console.WriteLine("Готово");
  return 3;
options:
  - "Ошибка компиляции: return нельзя писать вне метода"
  - "Выведет «Готово» и завершится с кодом 3"
  - "Выведет «Готово» и 3"
  - "Выведет «Готово», а return проигнорирует"
answer: 1
explain: |
  Операторы верхнего уровня — тело метода `Main`. `return 3;` делает его возвращающим `int`, и 3 становится кодом завершения программы.
```

::::

::::traps

- **`using` не подключает библиотеку.** Он только сокращает имена. Пакет добавляют в `.csproj` через `dotnet add package`.
- **Top-level statements — только в одном файле** и только до объявлений типов.
- **В продакшен выкладывают Release**, а не Debug: в Debug нет оптимизаций.
- **`bin` и `obj` не хранят в Git.** Их пересоздаёт каждая сборка.
- **`args` в top-level коде не магия:** это параметр метода `Main`, который сгенерировал компилятор.

::::

::::summary

```quiz
id: project-recall
type: recall
prompt: Из чего состоит консольный проект на C# и с чего начинается выполнение программы?
answer: |
  Проект описывает файл **`.csproj`**: тип вывода (`Exe` или библиотека), версия .NET (`TargetFramework`), пакеты и ссылки на другие проекты. Все `.cs`-файлы папки компилируются автоматически. Несколько проектов объединяет решение (`.slnx`).
  Выполнение начинается со статического метода **`Main`**. С **top-level statements** его не пишут: компилятор сам создаёт класс `Program` и `Main` из операторов файла. Такой файл в проекте один, `args` доступен как переменная.
  **Пространства имён** группируют типы, а **`using`** позволяет писать короткие имена. `ImplicitUsings` добавляет частые `using` во все файлы.
  Ошибки компиляции не дают собрать программу, ошибки выполнения — исключения во время работы.
```

::::

::::test

```quiz
id: test-implicit-usings
type: choice
part: "Пространства имён и using"
prompt: Почему в новом консольном проекте `Console.WriteLine` работает без строки `using System;`?
options:
  - "Пространство System подключено всегда, во всех версиях C#"
  - "Настройка ImplicitUsings добавляет global using для частых пространств имён"
  - "Console не входит ни в одно пространство имён"
  - "Компилятор сам угадывает недостающие using"
answer: 1
explain: |
  `<ImplicitUsings>enable</ImplicitUsings>` в `.csproj` добавляет `global using System;` и ещё несколько частых пространств. Если выключить настройку, `using System;` понадобится.
variants:
  - prompt: В проекте выключили ImplicitUsings. Что нужно, чтобы снова работал вызов `Console.WriteLine("Привет")`?
    options:
      - "Добавить в файл using System; или писать System.Console"
      - "Установить NuGet-пакет System"
      - "Ничего: Console доступен всегда"
      - "Перейти на .NET Framework"
    answer: 0
    explain: |
      `Console` лежит в пространстве `System`. Без ImplicitUsings директиву `using System;` нужно написать самому или указывать полное имя `System.Console`.
```

```quiz
id: test-exit-code
type: choice
part: "Точка входа: Main и top-level statements"
prompt: Что означает число, которое возвращает метод `Main`?
options:
  - "Количество выведенных строк"
  - "Код завершения программы: 0 — успех"
  - "Время работы в секундах"
  - "Номер строки, где произошла ошибка"
answer: 1
explain: |
  Возвращаемое значение `Main` получает тот, кто запустил программу: скрипт, CI или другая программа. По соглашению 0 означает успех.
variants:
  - prompt: Метод объявлен как `static void Main()`. С каким кодом завершится программа, если дошла до конца без ошибок?
    options:
      - "0"
      - "1"
      - "-1"
      - "Код не возвращается вовсе"
    answer: 0
    explain: |
      Если `Main` ничего не возвращает и программа закончилась нормально, код завершения — 0, то есть успех.
```

```quiz
id: test-file-namespace
type: fill
part: "Пространства имён и using"
prompt: Объявите пространство имён для всего файла.
code: |
  namespace Shop.Orders___

  class Order { }
answer: ";"
explain: |
  `namespace Shop.Orders;` действует до конца файла, без фигурных скобок и лишнего отступа.
variants:
  - prompt: Вставьте ключевое слово, чтобы объявить пространство имён для всего файла.
    code: |
      ___ Shop.Orders;

      class Order { }
    answer: namespace
    explain: |
      `namespace Shop.Orders;` с точкой с запятой действует до конца файла.
```

```quiz
id: test-compile-error
type: choice
part: "Ошибки компиляции и ошибки выполнения"
prompt: Какая строка не даст программе собраться?
options:
  - "`int count = int.Parse(\"ten\");`"
  - "`int count = \"10\";`"
  - "`int count = 10 / 1;`"
  - "`int count = int.Parse(\"10\");`"
answer: 1
explain: |
  Строку нельзя присвоить переменной типа `int`, и компилятор это видит. `int.Parse("ten")` соберётся, а исключение бросит уже при выполнении.
variants:
  - options:
      - "`Console.WritLine(\"Привет\");`"
      - "`int count = int.Parse(\"abc\");`"
      - "`Console.WriteLine(\"Привет\");`"
      - "`int count = int.Parse(\"7\");`"
    answer: 0
    explain: |
      У `Console` нет метода `WritLine`, и компилятор видит это сразу. `int.Parse("abc")` соберётся, а исключение бросит уже при выполнении.
```


```quiz
id: test-output-type
type: choice
part: "Проект"
prompt: Что изменится, если удалить из `.csproj` строку `<OutputType>Exe</OutputType>`?
options:
  - "Проект соберётся как библиотека: получится .dll, которую нельзя запустить"
  - "Ничего не изменится"
  - "Проект перестанет собираться"
  - "Программа будет собираться в Release"
answer: 0
explain: |
  `OutputType` задаёт, что получится при сборке. Без `Exe` проект — библиотека: её подключают к другим проектам, но не запускают.
```

```quiz
id: test-cs-files
type: choice
part: "Проект"
prompt: Как добавить новый файл `Order.cs` в компиляцию проекта?
options:
  - "Положить его в папку проекта: все .cs-файлы компилируются автоматически"
  - "Перечислить его в .csproj"
  - "Написать using Order; в Program.cs"
  - "Выполнить dotnet add file Order.cs"
answer: 0
explain: |
  SDK-проект сам включает все `.cs`-файлы своей папки и подпапок. Перечислять их в `.csproj` не нужно.
```

```quiz
id: test-dotnet-test
type: choice
part: "Проект"
prompt: Какая команда собирает проект и запускает его тесты?
options:
  - "dotnet test"
  - "dotnet run"
  - "dotnet build"
  - "dotnet check"
answer: 0
explain: |
  `dotnet test` собирает проекты и запускает тесты. `dotnet run` запускает программу, `dotnet build` только собирает.
```

```quiz
id: test-release
type: choice
part: "Проект"
prompt: Чем конфигурация Release отличается от Debug?
options:
  - "Включены оптимизации компилятора"
  - "Release работает только на Linux"
  - "Debug-сборку нельзя запустить без отладчика"
  - "В Release не бывает исключений"
answer: 0
explain: |
  Debug собирается без оптимизаций и с подробной информацией для отладчика. Release включает оптимизации, так собирают для продакшена.
```

```quiz
id: test-args-source
type: choice
part: "Точка входа: Main и top-level statements"
prompt: Откуда в файле с top-level statements берётся переменная `args`?
options:
  - "Это параметр метода Main, который сгенерировал компилятор"
  - "Это глобальная переменная .NET"
  - "Она описана в .csproj"
  - "Её нужно объявить самому"
answer: 0
explain: |
  Компилятор переносит операторы файла в метод `Main(string[] args)`. Поэтому `args` доступна, хотя её никто не объявлял.
```

```quiz
id: test-main-static
type: choice
part: "Точка входа: Main и top-level statements"
prompt: Почему метод `Main` объявлен как `static`?
options:
  - "Его вызывают без создания объекта: создавать объект до старта программы некому"
  - "Статические методы быстрее"
  - "Чтобы Main нельзя было вызвать дважды"
  - "Этого требует Windows"
answer: 0
explain: |
  Для обычного метода нужен объект класса. Точка входа вызывается, когда объектов ещё нет, поэтому она статическая.
```

```quiz
id: test-using-alias
type: fill
part: "Пространства имён и using"
prompt: Вставьте ключевое слово, чтобы объявить псевдоним пространства имён.
code: |
  ___ Json = System.Text.Json;
answer: using
explain: |
  `using Json = System.Text.Json;` вводит короткое имя `Json`. Помогает, когда в двух пространствах есть типы с одинаковыми именами.
```

```quiz
id: test-build-guarantee
type: choice
part: "Ошибки компиляции и ошибки выполнения"
prompt: Программа собралась без ошибок. Что это гарантирует?
options:
  - "Типы, имена и синтаксис в порядке, но при выполнении исключения всё ещё возможны"
  - "Программа никогда не упадёт"
  - "В программе нет логических ошибок"
  - "Все тесты пройдут"
answer: 0
explain: |
  Компилятор проверяет только то, что видно из кода. Неверный ввод, отсутствующий файл или деление на ноль в переменной обнаружатся только при выполнении.
```

```quiz
id: test-partial-program
type: choice
part: "Точка входа: Main и top-level statements"
level: middle
prompt: Зачем в веб-проекте с top-level statements пишут `public partial class Program { }`?
options:
  - "Чтобы интеграционные тесты могли сослаться на класс Program приложения"
  - "Чтобы приложение стартовало быстрее"
  - "Чтобы в проекте было два метода Main"
  - "Без этой строки проект не соберётся"
answer: 0
explain: |
  Класс `Program`, который создаёт компилятор, по умолчанию не публичный. `partial` дополняет его и делает публичным, и тесты могут передать его, например, в `WebApplicationFactory<Program>`.
```

```quiz
id: test-file-based
type: choice
part: "Точка входа: Main и top-level statements"
level: middle
prompt: Что делает команда `dotnet run app.cs` в .NET 10?
options:
  - "Запускает программу из одного файла без проекта"
  - "Создаёт проект с именем app"
  - "Компилирует app.cs в библиотеку"
  - "Проверяет файл на ошибки, не запуская"
answer: 0
explain: |
  С .NET 10 программу из одного файла можно запустить без `.csproj`. Пакеты подключаются директивами `#:package` в самом файле.
```

::::
