using System.Text.Json.Serialization;

  public class Request
{
    [JsonPropertyName("method")]
    public string Method { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("date")]
    public string Date { get; set; }

    [JsonPropertyName("body")]
    public string Body { get; set; }
}
    public class Response
    {
        public string Status { get; set; }
        public string Body { get; set; }
    }
    public class Category
    {
        [JsonPropertyName("cid")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
