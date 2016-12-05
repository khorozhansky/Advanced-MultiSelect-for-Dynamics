namespace TuneMultiSelect.Utils
{
  using System.IO;
  using System.Text;
  using System.Runtime.Serialization.Json;

  public static class JsonHelper
  {
    public static string SerializeJson<T>(T t)
    {
      var stream = new MemoryStream();
      var serializer = new DataContractJsonSerializer(typeof(T));
      var settings = new DataContractJsonSerializerSettings();
      serializer.WriteObject(stream, t);
      var jsonString = Encoding.UTF8.GetString(stream.ToArray());
      stream.Close();
      return jsonString;
    }

    public static T DeserializeJson<T>(string jsonString)
    {
      var serializer = new DataContractJsonSerializer(typeof(T));
      var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
      T obj = (T)serializer.ReadObject(stream);
      return obj;
    }
  }
}
