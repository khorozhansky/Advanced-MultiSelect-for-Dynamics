namespace TuneMultiSelect.Utils
{
  using System;
  using System.IO;
  using System.Text;
  using System.Xml.Serialization;

  public static class XmlStringSerializeHelper
  {
    private static readonly XmlSerializerNamespaces RemovedNamespaces;

    static XmlStringSerializeHelper()
    {
      RemovedNamespaces = new XmlSerializerNamespaces();
      RemovedNamespaces.Add(string.Empty, string.Empty);
    }

    public static string SerializeToString(this object xmlObj)
    {
      var serializer = new XmlSerializer(xmlObj.GetType());
      var stringBuilder = new StringBuilder();

      using (TextWriter writer = new StringWriter(stringBuilder))
      {
        serializer.Serialize(writer, xmlObj, RemovedNamespaces);
      }

      return stringBuilder.ToString();
    }

    public static T Deserialize<T>(this string textData)
    {
      return (T)Deserialize(textData, typeof(T));
    }

    public static object Deserialize(this string textData, Type type, string ns = "")
    {
      var serializer = new XmlSerializer(type, ns);
      object result;

      using (TextReader reader = new StringReader(textData))
      {
        result = serializer.Deserialize(reader);
      }

      return result;
    }
  }
}
