using System.Linq;

public class UrlParser
{
    public bool HasId { get; set; }
    public string Id { get; set; }
    public string Path { get; set; }
    
    public bool ParseUrl(string url)
    {
        HasId = false;
        Id = null;
        Path = null;
        
        if (string.IsNullOrEmpty(url))
            return false;
            
        if (url.StartsWith("/"))
            url = url.Substring(1);
            
        var parts = url.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length < 2)
            return false;

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
                return false;
            }
        }

        if (parts.Length == 2)
        {
            Path = "/" + parts[0] + "/" + parts[1];
            return true;
        }

        return false;
    }
}
