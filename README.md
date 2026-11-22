# Override

A self-study site about C# and .NET, PostgreSQL and related tools.

Each topic is a short, concrete explanation with diagrams, followed by
questions right in the text and exercises. Code and SQL exercises run directly
in the browser: C# is compiled with Roslyn and SQL runs on PostgreSQL compiled
to WebAssembly.

The site works entirely on your device. Progress is stored in the browser and
is never sent anywhere.

## Development

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```shell
dotnet run --project src/Override.Web
```

The code editor is a CodeMirror 6 bundle kept in
`src/Override.Web/wwwroot/lib/editor`. To rebuild it after changing
[`tools/editor`](tools/editor), use [Node.js](https://nodejs.org/) 20 or later:

```shell
cd tools/editor
npm ci
npm run build
```

SQL tasks run on [PGlite](https://pglite.dev/), PostgreSQL compiled to WebAssembly, kept in
`src/Override.Web/wwwroot/lib/pglite` and rebuilt the same way from [`tools/pglite`](tools/pglite).

Every push to `main` is built and deployed to GitHub Pages by
[the workflow](.github/workflows/pages.yml).

## License

- Source code: [MIT](LICENSE).
- Learning materials in [`content/`](content/): [CC BY-NC 4.0](content/LICENSE.md).
