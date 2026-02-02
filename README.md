# PluginManager DLL

PluginManager is a C# DLL for dynamically loading, unloading, and managing plugins at runtime. It supports service injection, automatic reload on file changes, optional logging via `IPluginLogger`, and works in both Debug and Release modes. It has no dependencies except .NET 10+.

Features:
- Load a single plugin with `LoadPlugin("path/to/plugin.dll")`
- Load all plugins in a directory with `LoadAllPlugins()`
- Unload a plugin with `UnloadPlugin("PluginName")`
- Unload all plugins with `UnloadAllPlugins()`
- Reload a plugin with `ReloadPlugin("PluginName", "path/to/plugin.dll")`
- Get a loaded plugin instance with `GetPlugin("PluginName")`
- List all loaded plugins with `GetLoadedPlugins()`
- Add a service for injection into plugins with `AddService<IMyService>(myServiceInstance)`
- Remove a service with `RemoveService<IMyService>()`
- Retrieve a service from the manager with `GetService<IMyService>()`

License / Credit: Free to use for personal or commercial projects. Please credit **William Ward** if used. No warranty is provided.
