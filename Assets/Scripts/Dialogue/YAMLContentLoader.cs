using UnityEngine;
using YamlDotNet.Serialization;
using System;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class RouteContent
{
    public string place;
    public string description;
    public List<string> values;
}

public class YAMLContentLoader : MonoBehaviour
{
    [Header("Route Files")]
    [Tooltip("List of YAML route files to load from Assets folder")]
    public List<string> routePaths = new List<string>();
    
    private Dictionary<string, RouteContent> routeContents = new Dictionary<string, RouteContent>();

    void Start()
    {
        LoadAllRoutes();
    }

    void LoadAllRoutes()
    {
        foreach (var path in routePaths)
        {
            if (!string.IsNullOrEmpty(path))
            {
                LoadRoute(path);
            }
        }
    }

    void LoadRoute(string path)
    {
        #if UNITY_EDITOR
        string fullPath = Application.dataPath + "/" + path;
        
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"YAMLContentLoader: Route file not found: {fullPath}");
            return;
        }
        
        try
        {
            var yaml = File.ReadAllText(fullPath);
            var deserializer = new DeserializerBuilder()
                .Build();

            RouteContent loadedContent = deserializer.Deserialize<RouteContent>(yaml);
            if (loadedContent != null && !string.IsNullOrEmpty(loadedContent.place))
            {
                routeContents[loadedContent.place] = loadedContent;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"YAMLContentLoader: Failed to load route '{path}': {e.Message}");
        }
        #else
        Debug.LogWarning($"YAMLContentLoader: File loading not supported in builds. Use embedded data instead.");
        #endif
    }

    public RouteContent GetContent(string place)
    {
        if (routeContents.TryGetValue(place, out RouteContent content))
        {
            return content;
        }
        else
        {
            throw new Exception("Content not found for place: " + place);
        }
    }
}
