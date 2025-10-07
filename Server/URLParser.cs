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
        var parts = url.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        // Check if we have at least 2 parts (api/categories)
        if (parts.Length < 2)
            return false;

        // If there are exactly 3 parts, the last one must be a valid integer ID
        if (parts.Length == 3)
        {
            if (int.TryParse(parts[2], out _))
            {
                HasId = true;
                Id = parts[2];
                Path = "/" + parts[0] + "/" + parts[1];
                return true;
            }
            else
            {
                // Invalid ID → fail parse → server returns 4 Bad Request
                return false;
            }
        }

        // If only 2 parts → just /api/categories
        if (parts.Length == 2)
        {
            Path = "/" + parts[0] + "/" + parts[1];
            return true;
        }

        // More than 3 parts not allowed
        return false;
    }
}
