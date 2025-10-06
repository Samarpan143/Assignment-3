using System.Text.Json.Serialization;


    // Request class - represents what clients send to the server
    public class Request
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public string Date { get; set; }
        public string Body { get; set; }
    }

    // Response class - represents what server sends back to clients
    public class Response
    {
        public string Status { get; set; }
        public string Body { get; set; }
    }

    // Category class - represents a category in our database
    public class Category
    {
        [JsonPropertyName("cid")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
