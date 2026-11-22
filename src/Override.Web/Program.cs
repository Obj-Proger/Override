using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Override.Web;
using Override.Web.Content;
using Override.Web.Progress;
using Override.Web.Running;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<ContentClient>();
builder.Services.AddSingleton<ProfileStore>();
builder.Services.AddSingleton<ProgressStore>();
builder.Services.AddSingleton<Coach>();
builder.Services.AddSingleton<SkillCard>();
builder.Services.AddSingleton<CodeRunService>();

var host = builder.Build();
host.Services.GetRequiredService<ProfileStore>().Load();
host.Services.GetRequiredService<ProgressStore>().Load();
await host.RunAsync();
