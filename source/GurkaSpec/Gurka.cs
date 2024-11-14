using System.Xml.Serialization;
using System.Xml;

namespace SpecGurka.GurkaSpec
{
    public class Gurka
    {
        public static Testrun ReadGurkaFile(string path)
        {
            var xml = System.IO.File.ReadAllText(path);
            XmlSerializer serializer = new XmlSerializer(typeof(Testrun));
            Testrun result;
            using (TextReader reader = new StringReader(xml))
            {
                result = (Testrun)serializer.Deserialize(reader);
            }
            return result;
        }

        public static void WriteGurkaFile(string path, Testrun testrun)
        {
            XmlSerializer xsSubmit = new XmlSerializer(typeof(Testrun));
            var xml = "";

            using (var sww = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sww))
                {
                    xsSubmit.Serialize(writer, testrun);
                    xml = sww.ToString(); // Your XML
                }
            }

            System.IO.File.WriteAllText($"{path}testrun_{DateTime.UtcNow.ToString("s").Replace(':', '_')}.gurka", xml);
        }
    }
}
