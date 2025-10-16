using System.Runtime.Loader;
using Devices.Abstractions;

namespace Devices.Core;

/// <summary>
/// Каталог плагинов, загружающий их из папки с .dll файлами.
/// </summary>
public sealed class DirectoryPluginCatalog : IDevicePluginCatalog
{
	private readonly string _directory;
	private readonly Dictionary<string, IDevicePlugin> _plugins = new();

	public DirectoryPluginCatalog(string directory)
	{
		_directory = directory;
		LoadPlugins();
	}

	/// <inheritdoc />
	public IDevicePlugin Resolve(string pluginId)
	{
		if (_plugins.TryGetValue(pluginId, out var plugin)) return plugin;
		throw new KeyNotFoundException($"Plugin '{pluginId}' not found");
	}

	/// <summary>
	/// Загружает плагины из файлов .dll, регистрируя классы, реализующие <see cref="IDevicePlugin"/>.
	/// </summary>
	private void LoadPlugins()
	{
		if (!Directory.Exists(_directory)) return;
		foreach (var file in Directory.EnumerateFiles(_directory, "*.dll", SearchOption.AllDirectories))
		{
			try
			{
				var alc = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(file), isCollectible: true);
				using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
				var asm = alc.LoadFromStream(fs);
				foreach (var type in asm.GetTypes().Where(t => typeof(IDevicePlugin).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass))
				{
					var instance = (IDevicePlugin)Activator.CreateInstance(type)!;
					_plugins[instance.PluginId] = instance;
				}
			}
			catch
			{
				// intentionally skip broken assemblies
			}
		}
	}
}
