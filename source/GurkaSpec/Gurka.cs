using System.Xml.Serialization;
using System.Xml;

namespace SpecGurka.GurkaSpec;

public class Gurka
{
    public static Testrun? ReadGurkaFile(string path)
    {
        var xml = File.ReadAllText(path);
        XmlSerializer serializer = new XmlSerializer(typeof(Testrun));
        Testrun? result;
        using (TextReader reader = new StringReader(xml))
        {
            result = (Testrun)serializer.Deserialize(reader)!;
        }
        return result;
    }

    public static void WriteGurkaFile(string path, Testrun testRun)
    {
        XmlSerializer xsSubmit = new XmlSerializer(typeof(Testrun));
        var xml = "";

        using (var sww = new StringWriter())
        {
            using (XmlWriter writer = XmlWriter.Create(sww))
            {
                xsSubmit.Serialize(writer, testRun);
                xml = sww.ToString(); // Your XML
            }
        }

        File.WriteAllText($"{path}{testRun.Name}_{DateTime.UtcNow.ToString("s").Replace(':', '_')}.gurka", xml);
    }
}