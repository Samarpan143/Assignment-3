using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Util;
using Assignment3;

namespace Server
{
    public class EchoServer
    {
        private TcpListener _server;
        private readonly CategoryService _categoryService = new CategoryService();
        private readonly RequestValidator _requestValidator = new RequestValidator();

        public int Port { get; set; }

        public EchoServer(int port)
        {
            Port = port;
        }

        public void Run()
        {
            _server = new TcpListener(IPAddress.Loopback, Port);
            _server.Start();
            Console.WriteLine($"CJTP Server started on port {Port}");

            while (true)
            {
                TcpClient client = _server.AcceptTcpClient();
                Console.WriteLine("Client connected");
                System.Threading.Tasks.Task.Run(() => HandleClient(client));
            }
        }

        private void HandleClient(TcpClient client)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    var buffer = new byte[4096];
                    var bytesRead = stream.Read(buffer, 0, buffer.Length);
                    
                    if (bytesRead == 0)
                    {
                        return; // Empty connection
                    }
                    
                    var requestJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (string.IsNullOrWhiteSpace(requestJson))
                    {
                        return; // Ignore empty requests
                    }

                    // Process request and get response
                    var response = ProcessCJTPRequest(requestJson);

                    // Send response
                    var responseJson = JsonSerializer.Serialize(response, 
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    var responseBytes = Encoding.UTF8.GetBytes(responseJson);
                    stream.Write(responseBytes, 0, responseBytes.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
        }

        private Response ProcessCJTPRequest(string requestJson)
        {
            try
            {
                var request = JsonSerializer.Deserialize<Request>(requestJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (request == null)
                {
                    return new Response { Status = "4 Bad Request" };
                }

                // Validate request first
                var validationResponse = _requestValidator.ValidateRequest(request);
                if (validationResponse.Status != "1 Ok")
                {
                    return validationResponse;
                }

                // Handle Echo
                if (request.Method.ToLower() == "echo")
                {
                    return new Response
                    {
                        Status = "1 Ok",
                        Body = request.Body
                    };
                }

                // Parse URL
                var urlParser = new UrlParser();
                if (!urlParser.ParseUrl(request.Path))
                    return new Response { Status = "4 Bad Request" };

                // Invalid path - only /api/categories is supported
                if (urlParser.Path != "/api/categories")
                    return new Response { Status = "5 Not found" };

                // Route methods
                return request.Method.ToLower() switch
                {
                    "read" => HandleRead(urlParser),
                    "create" => HandleCreate(request, urlParser),
                    "update" => HandleUpdate(request, urlParser),
                    "delete" => HandleDelete(urlParser),
                    _ => new Response { Status = "4 Bad Request" }
                };
            }
            catch (JsonException)
            {
                return new Response { Status = "4 Bad Request" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Internal server error: {ex.Message}");
                return new Response { Status = "6 Error" };
            }
        }

        private Response HandleRead(UrlParser urlParser)
        {
            try
            {
                if (urlParser.HasId)
                {
                    if (!int.TryParse(urlParser.Id, out int id))
                        return new Response { Status = "4 Bad Request" };

                    var category = _categoryService.GetCategory(id);
                    if (category == null)
                        return new Response { Status = "5 Not found" };

                    return new Response
                    {
                        Status = "1 Ok",
                        Body = JsonSerializer.Serialize(category, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        })
                    };
                }
                else
                {
                    var categories = _categoryService.GetCategories();
                    return new Response
                    {
                        Status = "1 Ok",
                        Body = JsonSerializer.Serialize(categories, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        })
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleRead: {ex.Message}");
                return new Response { Status = "6 Error" };
            }
        }

        private Response HandleCreate(Request request, UrlParser urlParser)
        {
            try
            {
                if (urlParser.HasId)
                    return new Response { Status = "4 Bad Request" };

                var categoryData = JsonSerializer.Deserialize<Category>(request.Body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (categoryData == null || string.IsNullOrEmpty(categoryData.Name))
                {
                    return new Response { Status = "4 Bad Request" };
                }

                var newCategory = _categoryService.CreateCategory(categoryData.Name);

                return new Response
                {
                    Status = "2 Created",
                    Body = JsonSerializer.Serialize(newCategory, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    })
                };
            }
            catch (JsonException)
            {
                return new Response { Status = "4 Bad Request" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleCreate: {ex.Message}");
                return new Response { Status = "6 Error" };
            }
        }

        private Response HandleUpdate(Request request, UrlParser urlParser)
        {
            try
            {
                if (!urlParser.HasId || !int.TryParse(urlParser.Id, out int id))
                    return new Response { Status = "4 Bad Request" };

                var categoryData = JsonSerializer.Deserialize<Category>(request.Body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (categoryData == null || string.IsNullOrEmpty(categoryData.Name))
                {
                    return new Response { Status = "4 Bad Request" };
                }

                var success = _categoryService.UpdateCategory(id, categoryData.Name);

                return success ? new Response { Status = "3 Updated" } : new Response { Status = "5 Not found" };
            }
            catch (JsonException)
            {
                return new Response { Status = "4 Bad Request" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleUpdate: {ex.Message}");
                return new Response { Status = "6 Error" };
            }
        }

        private Response HandleDelete(UrlParser urlParser)
        {
            try
            {
                if (!urlParser.HasId || !int.TryParse(urlParser.Id, out int id))
                    return new Response { Status = "4 Bad Request" };

                var success = _categoryService.DeleteCategory(id);
                return success ? new Response { Status = "1 Ok" } : new Response { Status = "5 Not found" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleDelete: {ex.Message}");
                return new Response { Status = "6 Error" };
            }
        }
    }
}
