using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class PluginServiceAttribute : Attribute { }

public interface IPlugin : IDisposable
{
    string Name { get; }
    string Version { get; }
    void Initialize();
}

public interface IPluginLogger
{
    void Info(string message);
    void Warn(string message);
    void Error(string message);
}

public class PluginManager
{
    private readonly object locker = new();
    private readonly Dictionary<string, (IPlugin Plugin, AssemblyLoadContext Context, string Path)> plugins = new();
    private readonly Dictionary<Type, object> services = new();
    private readonly FileSystemWatcher? watcher;
    private readonly string? pluginsDirectory;

    public PluginManager(string? directory = null)
    {
        pluginsDirectory = directory;
        if (!string.IsNullOrEmpty(pluginsDirectory) && Directory.Exists(pluginsDirectory))
        {
            watcher = new FileSystemWatcher(pluginsDirectory, "*.dll");
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime;
            watcher.Changed += OnPluginChanged;
            watcher.Created += OnPluginChanged;
            watcher.Renamed += OnPluginRenamed;
            watcher.EnableRaisingEvents = true;
        }
        else
            watcher = null;
    }

    public void AddService<T>(T service) where T : class
    {
        lock (locker)
            services[typeof(T)] = service!;
    }

    public void RemoveService<T>() where T : class
    {
        lock (locker)
            if (services.ContainsKey(typeof(T)))
                services.Remove(typeof(T));
    }

    public T? GetService<T>() where T : class
    {
        lock (locker)
            return services.ContainsKey(typeof(T)) ? (T)services[typeof(T)] : null;
    }

    private void InjectServices(IPlugin plugin)
    {
        Type type = plugin.GetType();
        foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            if (field.GetCustomAttribute<PluginServiceAttribute>() != null && services.ContainsKey(field.FieldType))
                field.SetValue(field.IsStatic ? null : plugin, services[field.FieldType]);
        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            if (prop.GetCustomAttribute<PluginServiceAttribute>() != null && prop.CanWrite && services.ContainsKey(prop.PropertyType))
                prop.SetValue(prop.GetGetMethod(true)?.IsStatic == true ? null : plugin, services[prop.PropertyType]);
    }

    private void OnPluginChanged(object sender, FileSystemEventArgs e) => Task.Run(() =>
        {
            string? pluginName = null;
            lock (locker)
                foreach (KeyValuePair<string, (IPlugin Plugin, AssemblyLoadContext Context, string Path)> pair in plugins)
                    if (pair.Value.Path == e.FullPath)
                    {
                        pluginName = pair.Key;
                        break;
                    }

            if (pluginName != null)
                ReloadPlugin(pluginName, e.FullPath);
        });

    private void OnPluginRenamed(object sender, RenamedEventArgs e) => Task.Run(() =>
        {
            string? pluginName = null;
            lock (locker)
                foreach (KeyValuePair<string, (IPlugin Plugin, AssemblyLoadContext Context, string Path)> pair in plugins)
                    if (pair.Value.Path == e.OldFullPath)
                    {
                        pluginName = pair.Key;
                        break;
                    }

            if (pluginName != null)
            {
                UnloadPlugin(pluginName);
                LoadPlugin(e.FullPath);
            }
        });

    public List<string> LoadAllPlugins()
    {
        List<string> loadedPlugins = new List<string>();
        if (string.IsNullOrEmpty(pluginsDirectory) || !Directory.Exists(pluginsDirectory))
            return loadedPlugins;

        string[] files = Directory.GetFiles(pluginsDirectory, "*.dll");
        foreach (string file in files)
            if (LoadPlugin(file))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                loadedPlugins.Add(name);
            }

        return loadedPlugins;
    }

    public void UnloadAllPlugins()
    {
        List<string> pluginNames = new List<string>();
        lock (locker)
            foreach (KeyValuePair<string, (IPlugin Plugin, AssemblyLoadContext Context, string Path)> pair in plugins)
                pluginNames.Add(pair.Key);
        foreach (string name in pluginNames)
            UnloadPlugin(name);
    }

    public bool LoadPlugin(string path)
    {
        AssemblyLoadContext? context = null;
        IPlugin? pluginInstance = null;
        string? pluginName = null;
        lock (locker)
        {
            try
            {
                context = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(path) + "_ALC", true);
                Assembly assembly = context.LoadFromAssemblyPath(Path.GetFullPath(path));
                foreach (Type type in assembly.GetTypes())
                {
                    if (!typeof(IPlugin).IsAssignableFrom(type))
                        continue;
                    pluginInstance = (IPlugin)Activator.CreateInstance(type)!;
                    InjectServices(pluginInstance);
                    pluginInstance.Initialize();
                    pluginName = pluginInstance.Name;
                    plugins[pluginName] = (pluginInstance, context, path);
                    GetService<IPluginLogger>()?.Info("Loaded plugin: " + pluginName + " v" + pluginInstance.Version);
                    return true;
                }
            }
            catch
            {
                GetService<IPluginLogger>()?.Error("Failed to load plugin: " + path);
                context?.Unload();
                return false;
            }
        }
        return false;
    }

    public bool UnloadPlugin(string name)
    {
        lock (locker)
        {
            if (!plugins.ContainsKey(name))
                return false;
            IPlugin plugin = plugins[name].Plugin;
            AssemblyLoadContext context = plugins[name].Context;
            try { plugin.Dispose(); } catch { }
            plugins.Remove(name);
            try { context.Unload(); } catch { }
            GetService<IPluginLogger>()?.Info("Unloaded plugin: " + name);
            return true;
        }
    }

    public bool ReloadPlugin(string name, string path)
    {
        bool unloaded = UnloadPlugin(name);
        bool loaded = LoadPlugin(path);
        return unloaded && loaded;
    }

    public List<string> GetLoadedPlugins()
    {
        lock (locker)
            return new List<string>(plugins.Keys);
    }

    public IPlugin? GetPlugin(string name)
    {
        lock (locker)
            return plugins.ContainsKey(name) ? plugins[name].Plugin : null;
    }
}