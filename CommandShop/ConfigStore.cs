using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConfigStore
{
  /// <summary>
  ///   Allows resolving of abstract class members.
  /// </summary>
  /// <typeparam name="T">Abstract class where child classes are based of.</typeparam>
  public abstract class AbstractJsonConverter<T> : JsonConverter
  {
    protected abstract T Create(Type objectType, JObject jObject);

    public override bool CanConvert(Type objectType)
    {
      return typeof(T).IsAssignableFrom(objectType);
    }

    public override object ReadJson(
      JsonReader reader,
      Type objectType,
      object existingValue,
      JsonSerializer serializer)
    {
      var jObject = JObject.Load(reader);

      var target = Create(objectType, jObject);
      serializer.Populate(jObject.CreateReader(), target);

      return target;
    }

    public override void WriteJson(
      JsonWriter writer,
      object value,
      JsonSerializer serializer)
    {
      serializer.Serialize(writer, value);
    }

    protected static bool FieldExists(
      JObject jObject,
      string name,
      JTokenType type)
    {
      return jObject.TryGetValue(name, out JToken token) && token.Type == type;
    }
  }

  public interface IConfig
  {
    void Write();
  }

  /// <summary>
  ///   Class for simple de/serialization of a class to a json file.
  /// </summary>
  public abstract class JsonConfig : IConfig
  {
    private static readonly object _syncRoot = new object();

    [JsonConstructor]
    protected JsonConfig()
    {
    }

    protected JsonConfig(string savePath) : this()
    {
      SavePath = savePath;
    }

    public static JsonSerializerSettings SerializerSettings { get; }
      = new JsonSerializerSettings
      {
        NullValueHandling = NullValueHandling.Include,
        DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
        ObjectCreationHandling = ObjectCreationHandling.Replace
      };

    [JsonIgnore]
    public string SavePath { get; protected set; }

    public void Write()
    {
      Write(SerializerSettings);
    }

    public static T Read<T>(string path) where T : IConfig, new()
    {
      return Read<T>(path, SerializerSettings);
    }

    public static T Read<T>(string path, JsonSerializerSettings settings) where T : IConfig, new()
    {
      lock (_syncRoot)
      {
        T ret;
        try
        {
          var data = File.ReadAllText(path);
          if (string.IsNullOrWhiteSpace(data))
          {
            ret = new T();
            (ret as JsonConfig).SavePath = path;
            ret.Write();
            return ret;
          }
          ret = JsonConvert.DeserializeObject<T>(data, settings);
        }
        catch (FileNotFoundException)
        {
          ret = new T();
          (ret as JsonConfig).SavePath = path;
          ret.Write();
        }

        (ret as JsonConfig).SavePath = path;
        return ret;
      }
    }

    public void Write(JsonSerializerSettings settings)
    {
      lock (_syncRoot)
      {
        File.WriteAllText(SavePath, Serialize(settings));
      }
    }

    public string Serialize(JsonSerializerSettings settings)
    {
      return JsonConvert.SerializeObject(this, Formatting.Indented, settings);
    }

    public string Serialize()
    {
      return Serialize(SerializerSettings);
    }
  }

  public class ExampleConfig : JsonConfig
  {
    public string AString { get; set; } = "default";
    public double ADouble { get; set; } = 3;
    public int AnInt { get; set; } = 99;
    public DateTime ADateTime { get; set; }

    public List<string> Stuff { get; set; } = new List<string>
    {
      "this",
      "is",
      "a",
      "list"
    };
  }
}