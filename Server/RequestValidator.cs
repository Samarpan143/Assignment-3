using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Assignment1;

public class Request
{
    public string Method { get; set; }
    public string Path { get; set; }
    public string Date { get; set; }
    public string Body { get; set; }
}

public class Response
{
    public string Status { get; set; }
}

public class RequestValidator
{
    public Response ValidateRequest(Request request)
    {
        if (request == null)
            return new Response { Status = "4 missing request" };
        
        var facts = new
        {
            MethodLower = request.Method?.Trim().ToLower(),
            HasPath = !string.IsNullOrWhiteSpace(request.Path),
            HasDate = !string.IsNullOrWhiteSpace(request.Date),
            HasBody = !string.IsNullOrWhiteSpace(request.Body)
        };

        bool bodyRequired = facts.MethodLower == "create" || facts.MethodLower == "update" ||
                            facts.MethodLower == "echo";

        var reason = new List<string>();

        if (string.IsNullOrWhiteSpace(facts.MethodLower))
        {
            reason.Add("missing method");
        }
        else
        {
            bool allowed = facts.MethodLower is "read" or "create" or "update" or "delete" or "echo";
            if (!allowed) reason.Add("illegal method");
        }
        
        // path: missing
        if (!facts.HasPath) reason.Add("missing path");
        
        // date: missing / illegal number
        if (!facts.HasDate)
        {
            reason.Add("missing date");
        }
        else
        {
            long _;
            if (!long.TryParse(request.Date, out _))
                reason.Add("illegal date");
        }

        if (bodyRequired)
        {
            if (!facts.HasBody)
            {
                reason.Add("missing body");
            }
            else
            {
                try
                {
                    JsonDocument.Parse(request.Body);
                }
                catch
                {
                    reason.Add("illegal body");
                }
            }
        }
        
        if (reason.Count > 0)
        {
            string msg = reason[0];
            for (int i = 1; i < reason.Count; i++)
                msg += ", " + reason[i];

            return new Response { Status = "4 " + msg };
        }

        // Success: tests expect "1 Ok" for all valid cases
        return new Response { Status = "1 Ok" };
    }
}
