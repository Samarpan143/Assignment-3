using System.Linq;

    // UrlParser class - parses URLs to extract path and ID
    public class UrlParser
    {
        public bool HasId { get; set; }
        public string Id { get; set; }
        public string Path { get; set; }
        
        public bool ParseUrl(string url)
        {
            // Reset properties
            HasId = false;
            Id = null;
            Path = null;
            
            if (string.IsNullOrEmpty(url))
                return false;
                
            // Remove leading slash if present
            if (url.StartsWith("/"))
                url = url.Substring(1);
                
            // Split the URL by '/'
            var parts = url.Split('/');
            
            // Check if we have at least 2 parts (api/categories)
            if (parts.Length < 2)
                return false;
                
            // Check if last part is a number (ID)
            if (parts.Length >= 3 && int.TryParse(parts[parts.Length - 1], out _))
            {
                HasId = true;
                Id = parts[parts.Length - 1];
                // Reconstruct path without the ID
                Path = "/" + string.Join("/", parts.Take(parts.Length - 1));
            }
            else
            {
                HasId = false;
                Path = "/" + string.Join("/", parts);
            }
            
            return true;
        }
    }
