using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class Program
{
    public static async Task Main(string[] args)
    {
        PluginLoader pluginLoader = new PluginLoader();
        Task sp = pluginLoader.SearchPluginAsync();
        Task ip = pluginLoader.InvokeMethodLoadAsync();
        await Task.WhenAll(sp, ip);
        Console.WriteLine("Загрузка завершена");

    }
}

public interface IPlugin
{
    void Load();
}

public class PluginLoader
{
    private readonly string pluginFolderPath;
    private ConcurrentQueue<IPlugin> pluginQueue;
    private readonly ConcurrentQueue<IPlugin> pluginQueueRepeat;
    private volatile bool  isSearchCompleted;
    const string PATH_PLUGINS = "C:\\Users\\user\\source\\repos\\otusHW4\\HW4";
    const string MASK_PLUGINS = "Plugin*.dll";

    public PluginLoader()
    {
        this.pluginQueue = new ConcurrentQueue<IPlugin>();
        this.pluginQueueRepeat = new ConcurrentQueue<IPlugin>();
    }

    public async Task InvokeMethodLoadAsync()
    {
        await Task.Run(() =>
        {
            while (!isSearchCompleted)
            {
                Thread.Sleep(500); // Ждем окончания работы потока
                // Второй поток - вызов метода Load для каждого плагина в очереди
                while (pluginQueue.TryDequeue(out IPlugin plugin))
                {
                    try
                    {
                        plugin.Load();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Плагин"+ plugin.ToString() + "будет загружен повторно "+ex.Message) ;
                        pluginQueueRepeat.Enqueue(plugin);
                    }
                }
            }

            if (!pluginQueueRepeat.IsEmpty)
            {
                pluginQueue = pluginQueueRepeat;
                while (pluginQueue.TryDequeue(out IPlugin plugin))
                {
                    try
                    {
                        plugin.Load();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ошибка загрузки плагина "+ex.Message);
                    }
                }
            }

        });
        
    }

    public async Task SearchPluginAsync()
    {
        await Task.Run(() =>
        {
            
            string[] pluginFiles = Directory.GetFiles(PATH_PLUGINS, MASK_PLUGINS, SearchOption.AllDirectories);

            foreach (string pluginFile in pluginFiles)
            {
                try
                {
                    Assembly pluginAssembly = Assembly.LoadFrom(pluginFile);

                    Type[] pluginTypes = pluginAssembly.GetTypes()
                        .Where(type => typeof(IPlugin).IsAssignableFrom(type) && !type.IsAbstract)
                        .ToArray();

                    foreach (Type pluginType in pluginTypes)
                    {
                        IPlugin pluginInstance = (IPlugin)Activator.CreateInstance(pluginType);
                        pluginQueue.Enqueue(pluginInstance);
                        Console.WriteLine("Плагин реализующий IPlugin обнаружен : " + pluginFile);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        });
        isSearchCompleted = true;
    }
}