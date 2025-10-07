using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

    // RequestValidator class - validates incoming CJTP requests
    public class RequestValidator
    {
        public Response ValidateRequest(Request request)
        {
            var errors = new List<string>();
            
            // Check method
            if (string.IsNullOrEmpty(request.Method))
            {
                errors.Add("missing method");
            }
            else if (!IsValidMethod(request.Method))
            {
                errors.Add("illegal method");
            }
            
            // Check path
            if (string.IsNullOrEmpty(request.Path))
            {
                errors.Add("missing path");
            }
            
            // Check date
            if (string.IsNullOrEmpty(request.Date))
            {
                errors.Add("missing date");
            }
            else if (!IsValidUnixTimestamp(request.Date))
            {
                errors.Add("illegal date");
            }
            
            // Check body for methods that require it
            if (RequiresBody(request.Method))
            {
                if (string.IsNullOrEmpty(request.Body))
                {
                    errors.Add("missing body");
                }
                else if (RequiresJsonBody(request.Method) && !IsValidJson(request.Body))
                {
                    errors.Add("illegal body");
                }
            }
            
            // Return response
            if (errors.Count > 0)
            {
                return new Response
                {
                    Status = "4 " + string.Join(", ", errors)
                };
            }
            
            return new Response { Status = "1 Ok" };
        }
        
        private bool IsValidMethod(string method)
        {
            var validMethods = new[] { "create", "read", "update", "delete", "echo" };
            return validMethods.Contains(method.ToLower());
        }
        
        private bool IsValidUnixTimestamp(string date)
        {
            return long.TryParse(date, out _);
        }
        
        private bool RequiresBody(string method)
        {
            if (string.IsNullOrEmpty(method)) return false;
            var methodsRequiringBody = new[] { "create", "update", "echo" };
            return methodsRequiringBody.Contains(method.ToLower());
        }
        
        private bool RequiresJsonBody(string method)
        {
            if (string.IsNullOrEmpty(method)) return false;
            var methodsRequiringJsonBody = new[] { "create", "update" };
            return methodsRequiringJsonBody.Contains(method.ToLower());
        }
        
        private bool IsValidJson(string json)
        {
            try
            {
                JsonDocument.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
