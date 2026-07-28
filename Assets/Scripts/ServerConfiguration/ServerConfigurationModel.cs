using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public static class ServerConfigurationModel
{
    private const string serverConfigurationsKey = "server_configurations";
    private const string defaultConfigurationNameKey = "default_server_configuration";

    public static List<ServerConfiguration> ServerConfigurations;
    public static List<ServerConfiguration> SupportedServerConfigurations;
    
    private static ServerConfiguration activeConfiguration;
    public static ServerConfiguration ActiveConfiguration
    {
        get => activeConfiguration;
        set
        {
            activeConfiguration = value;
            ActiveConfigurationChanged?.Invoke();
        }
    }
    public static ServerConfiguration DefaultConfiguration { get; set; }

    public static Action ActiveConfigurationChanged;

    public static void Initialize(List<ServerConfiguration> serverConfigurations)
    {
        var json = PlayerPrefs.GetString(serverConfigurationsKey, string.Empty);
        ServerConfigurations = string.IsNullOrEmpty(json) == false ? JsonConvert.DeserializeObject<List<ServerConfiguration>>(json) : new List<ServerConfiguration>();
        DefaultConfiguration = GetDefaultConfiguration();
        SupportedServerConfigurations = new List<ServerConfiguration>(serverConfigurations);
    }

    private static ServerConfiguration GetDefaultConfiguration()
    {
        var defaultConfigurationName = PlayerPrefs.GetString(defaultConfigurationNameKey, string.Empty);
        return ServerConfigurations.FirstOrDefault(x => x.Name == defaultConfigurationName);
    }

    public static void SetAsDefault(ServerConfiguration config)
    {
        PlayerPrefs.SetString(defaultConfigurationNameKey, config.Name);
        PlayerPrefs.Save();
        DefaultConfiguration = config;
    }

    public static bool IsDefault(ServerConfiguration config)
    {
        return DefaultConfiguration != null && DefaultConfiguration.Name == config.Name;
    }

    public static void SetFavorite(ServerConfiguration config, bool favorite)
    {
        config.Favorite = favorite;
        SaveServerConfigurations();
    }

    public static void MarkConnected(ServerConfiguration config)
    {
        config.LastConnected = DateTime.UtcNow.ToString("o");
        SaveServerConfigurations();
    }

    public static void AddServerConfiguration(ServerConfiguration newConfiguration)
    {
        if (IsServerConfigurationNameValid(newConfiguration.Name) == false)
        {
            Debug.LogError($"Server configuration name {newConfiguration.Name} is not valid.");
            return;
        }
        ServerConfigurations.Add(newConfiguration);
        newConfiguration.CreateDirectoryToSaveFiles();
        SaveServerConfigurations();
    }

    public static bool IsServerConfigurationNameValid(string name)
    {
        return ServerConfigurations.All(x => x.Name != name);
    }

    public static void SaveServerConfigurations()
    {
        var json = JsonConvert.SerializeObject(ServerConfigurations);
        PlayerPrefs.SetString(serverConfigurationsKey, json);
        PlayerPrefs.Save();
    }

    private static string GetValidNewServerConfigurationName()
    {
        var name = "New Server Configuration";
        var validName = name;
        var index = 1;
        while (IsServerConfigurationNameValid(validName) == false && index < 1000)
        {
            validName = name + " " + index++;
        }

        return validName;
    }

    public static ServerConfiguration CreateNewServerConfiguration()
    {
        var newServerConfiguration = new ServerConfiguration
            {Name = GetValidNewServerConfigurationName()};
        return newServerConfiguration;
    }

    public static bool Contains(ServerConfiguration config)
    {
        return ServerConfigurations.Contains(config);
    }

    public static void DeleteConfiguration(ServerConfiguration config)
    {
        ServerConfigurations.Remove(config);
        var directoryInfo = new DirectoryInfo(config.GetPathToSaveFiles());
        if (directoryInfo.Exists)
        {
            directoryInfo.Delete(true);
        }
        SaveServerConfigurations();
    }

    public static void DeleteConfigurationFiles(ServerConfiguration config)
    {
        var directoryInfo = new DirectoryInfo(config.GetPathToSaveFiles());
        if (directoryInfo.Exists)
        {
            directoryInfo.Delete(true);
        }
        config.AllFilesDownloaded = false;
        SaveServerConfigurations();
    }
}