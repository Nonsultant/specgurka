using Newtonsoft.Json;

namespace SpecGurka.Specflow;

public class SpecflowFileReader
{
    public dynamic ReadSpecflowFile(string path)
    {
        dynamic specflowExecutionResults;

        using (StreamReader reader = new StreamReader(path))
        {
            string json = reader.ReadToEnd();
            specflowExecutionResults = JsonConvert.DeserializeObject<dynamic>(json).ExecutionResults;
        }
        return specflowExecutionResults;
    }
}
