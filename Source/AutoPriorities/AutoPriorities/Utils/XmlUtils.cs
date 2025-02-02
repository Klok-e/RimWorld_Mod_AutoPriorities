using System;
using System.IO;
using System.Xml.Serialization;

namespace AutoPriorities.Utils
{
    public static class XmlUtils
    {
        public static byte[] GetBytesXml<T>(this T data)
        {
            using var stream = new MemoryStream();

            new XmlSerializer(typeof(T)).Serialize(stream, data ?? throw new ArgumentNullException(nameof(data)));

            stream.Position = 0;
            return stream.ToArray();
        }

        public static T DeserializeXml<T>(this Stream data)
        {
            return (T)new XmlSerializer(typeof(T)).Deserialize(data);
        }
    }
}
