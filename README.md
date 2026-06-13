# PluginManager

A lightweight, production-ready C# library for dynamically loading, unloading, and managing plugins at runtime in .NET 10+ applications. Supports service injection, hot-reload on file changes, optional logging, and clean lifecycle management — with zero external dependencies.

---

## Features

- **Dynamic load/unload** — load and unload plugin DLLs at runtime without restarting the host application
- **Directory scanning** — load all plugins from a folder in a single call
- **Hot-reload** — reload a plugin from an updated DLL file on demand
- **Service injection** — register shared services that are automatically injected into plugins at load time
- **Plugin enumeration** — list all currently loaded plugins
- **Optional logging** via a pluggable `IPluginLogger` interface
- **No external dependencies** — pure .NET 10, no NuGet packages required

---

## Requirements

- .NET 10 or later

---

## Installation

Clone or download the repository and add the `PluginManager` project as a reference, or build it as a DLL.

```bash
git clone https://github.com/WilliamW1979/PluginManager.git
```

---

## Quick Start

```csharp
var manager = new PluginManager();

// Load a single plugin
manager.LoadPlugin("plugins/MyPlugin.dll");

// Load all plugins in a directory
manager.LoadAllPlugins("plugins/");

// Get a loaded plugin instance
var plugin = manager.GetPlugin("MyPlugin");

// List all loaded plugins
foreach (var name in manager.GetLoadedPlugins())
{
    Console.WriteLine($"Loaded: {name}");
}
```

---

## Unloading and Reloading

```csharp
// Unload a single plugin by name
manager.UnloadPlugin("MyPlugin");

// Unload all plugins at once
manager.UnloadAllPlugins();

// Reload a plugin from an updated DLL (hot-reload)
manager.ReloadPlugin("MyPlugin", "plugins/MyPlugin.dll");
```

---

## Service Injection

Register shared application services before loading plugins. PluginManager injects them automatically at load time:

```csharp
// Register services
manager.AddService<IDatabase>(new SqlDatabase(connectionString));
manager.AddService<IEventBus>(new EventBus());

// Now load plugins — services are injected automatically
manager.LoadAllPlugins("plugins/");

// Retrieve a service from the manager directly
var db = manager.GetService<IDatabase>();

// Remove a service
manager.RemoveService<IDatabase>();
```

---

## Optional Logging

Pass any `IPluginLogger` implementation to capture load/unload events and errors:

```csharp
IPluginLogger logger = new MyConsoleLogger();
var manager = new PluginManager(logger);

manager.LoadPlugin("plugins/MyPlugin.dll");
// Logger receives: "Loaded plugin: MyPlugin"
```

---

## Design Notes

- Plugins are loaded into isolated contexts where supported, enabling true runtime unload without leaking memory.
- The service injection model decouples plugins from the host application — plugins receive interfaces, not concrete types.
- `IPluginLogger` keeps the library unopinionated about your logging infrastructure; pass any adapter.
- `ReloadPlugin` is designed for development hot-swap workflows and live-update scenarios in long-running services.

---

## License

Free to use for personal or commercial projects. Please credit **William Ward** if used. No warranty is provided. See [LICENSE.txt](LICENSE.txt) for full terms.

---

## Author

**William Ward** — [github.com/WilliamW1979](https://github.com/WilliamW1979)
