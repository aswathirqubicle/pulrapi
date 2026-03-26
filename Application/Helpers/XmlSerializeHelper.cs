
using System.IO;
using System.Xml.Serialization;

namespace Core.Application.Helpers
{
    public static class XmlSerializeHelper
    {
        public static string SerializeObject<T>(this T toSerialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(toSerialize.GetType());

            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);
                return textWriter.ToString();
            }
        }

        public static T DesirializeObject<T>(string toDesirialize)
        {
            var serializer = new XmlSerializer(typeof(T));
            T result;

            using (TextReader reader = new StringReader(toDesirialize))
            {
                result = (T)serializer.Deserialize(reader);
            }

            return result;
        }
    }
}
