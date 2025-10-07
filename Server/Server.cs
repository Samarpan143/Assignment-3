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
                HandleClient(client);
            }
        }

       private void HandleClient(TcpClient client)
{
    var stream = client.GetStream();
    stream.ReadTimeout = 5000;

    try
    {
        while (client.Connected)
        {
            using var memStream = new MemoryStream();
            var buffer = new byte[4096];
            int bytesRead;

            try
            {
                // Read data
                do
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break; // Client closed connection
                    memStream.Write(buffer, 0, bytesRead);
                } while (stream.DataAvailable);

                if (memStream.Length == 0)
                    continue; // No data, keep waiting

                var requestJson = Encoding.UTF8.GetString(memStream.ToArray());
                var response = ProcessCJTPRequest(requestJson);
                var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                var responseBytes = Encoding.UTF8.GetBytes(responseJson);

                stream.Write(responseBytes, 0, responseBytes.Length);
            }
            catch (IOException)
            {
                // Read timeout or client disconnected
                break;
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unexpected client error: {ex.Message}");
    }
    finally
    {
        client.Close();
        Console.WriteLine("Client disconnected safely");
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

                // Invalid path
                if (urlParser.Path != "/api/categories")
                    return new Response { Status = "5 Not found" };

                // ID validation for Read, Update, Delete
                if ((request.Method.ToLower() == "read" ||
                     request.Method.ToLower() == "update" ||
                     request.Method.ToLower() == "delete") &&
                    urlParser.HasId && !int.TryParse(urlParser.Id, out _))
                {
                    return new Response { Status = "4 Bad Request" };
                }

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
            catch
            {
                return new Response { Status = "6 Error" };
            }
        }



        private Response HandleRead(UrlParser urlParser)
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

        private Response HandleCreate(Request request, UrlParser urlParser)
        {
            if (urlParser.HasId)
                return new Response { Status = "4 Bad Request" };

            try
            {
                var categoryData = JsonSerializer.Deserialize<Category>(request.Body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
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
            catch
            {
                return new Response { Status = "4 Bad Request" };
            }
        }

        private Response HandleUpdate(Request request, UrlParser urlParser)
        {
            if (!urlParser.HasId || !int.TryParse(urlParser.Id, out int id))
                return new Response { Status = "4 Bad Request" };

            try
            {
                var categoryData = JsonSerializer.Deserialize<Category>(request.Body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var success = _categoryService.UpdateCategory(id, categoryData.Name);

                return success ? new Response { Status = "3 Updated" } : new Response { Status = "5 Not found" };
            }
            catch
            {
                return new Response { Status = "4 Bad Request" };
            }
        }

        private Response HandleDelete(UrlParser urlParser)
        {
            if (!urlParser.HasId || !int.TryParse(urlParser.Id, out int id))
                return new Response { Status = "4 Bad Request" };

            var success = _categoryService.DeleteCategory(id);
            return success ? new Response { Status = "1 Ok" } : new Response { Status = "5 Not found" };
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace Server
{
	public class EchoServer
	{
		private TcpListener _server;
		private readonly CategoryService _categoryService = new CategoryService();
		private readonly RequestValidator _requestValidator = new RequestValidator();
		private readonly UrlParser _urlParser = new UrlParser();

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
				Task.Run(() => HandleClient(client));
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
				// Deserialize request
				var request = JsonSerializer.Deserialize<Request>(requestJson,
					new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

				if (request == null)
				{
					return new Response { Status = "4 Bad Request" };
				}

				// Validate request using Part I validator
				var validationResponse = _requestValidator.ValidateRequest(request);
				if (validationResponse.Status != "1 Ok")
				{
					return validationResponse;
				}

				// Parse URL using Part I parser
				if (!_urlParser.ParseUrl(request.Path))
				{
					return new Response { Status = "4 Bad Request" };
				}

				// Route request based on method and path
				return RouteRequest(request, _urlParser);
			}
			catch (JsonException)
			{
				return new Response { Status = "4 Bad Request" };
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error processing request: {ex.Message}");
				return new Response { Status = "4 Bad Request" };
			}
		}

		private Response RouteRequest(Request request, UrlParser urlParser)
		{
			string method = request.Method ?? "";
			return method.ToLower() switch
			{
				"echo" => HandleEcho(request),
				"read" => HandleRead(request, urlParser),
				"create" => HandleCreate(request, urlParser),
				"update" => HandleUpdate(request, urlParser),
				"delete" => HandleDelete(request, urlParser),
				_ => new Response { Status = "4 Bad Request" }
			};
		}

		private Response HandleEcho(Request request)
		{
			return new Response
			{
				Status = "1 Ok",
				Body = request.Body
			};
		}

		private Response HandleRead(Request request, UrlParser urlParser)
		{
			if (urlParser.Path != "/api/categories")
			{
				return new Response { Status = "5 Not Found" };
			}

			if (urlParser.HasId)
			{
				// Validate ID
				if (!int.TryParse(urlParser.Id, out int categoryId))
				{
					return new Response { Status = "4 Bad Request" };
				}

				// Read single category
				var category = _categoryService.GetCategory(categoryId);
				if (category == null)
				{
					return new Response { Status = "5 Not Found" };
				}

				var categoryJson = JsonSerializer.Serialize(new { cid = category.Id, name = category.Name },
					new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
				return new Response { Status = "1 Ok", Body = categoryJson };
			}
			else
			{
				// Read all categories
				var categories = _categoryService.GetCategories();
				var categoryList = categories.Select(c => new { cid = c.Id, name = c.Name }).ToList();
				var categoriesJson = JsonSerializer.Serialize(categoryList,
					new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
				return new Response { Status = "1 Ok", Body = categoriesJson };
			}
		}

		private Response HandleCreate(Request request, UrlParser urlParser)
		{
			if (urlParser.Path != "/api/categories")
			{
				return new Response { Status = "5 Not Found" };
			}

			if (urlParser.HasId)
			{
				return new Response { Status = "4 Bad Request" };
			}

			try
			{
				// Deserialize the create request (only needs name)
				using JsonDocument doc = JsonDocument.Parse(request.Body);
				if (!doc.RootElement.TryGetProperty("name", out JsonElement nameElement) ||
					nameElement.ValueKind != JsonValueKind.String)
				{
					return new Response { Status = "4 Bad Request" };
				}

				string categoryName = nameElement.GetString();
				if (string.IsNullOrEmpty(categoryName))
				{
					return new Response { Status = "4 Bad Request" };
				}

				// Find next available ID
				var allCategories = _categoryService.GetCategories();
				var nextId = allCategories.Count > 0 ? allCategories.Max(c => c.Id) + 1 : 1;

				var success = _categoryService.CreateCategory(nextId, categoryName);
				if (!success)
				{
					return new Response { Status = "4 Bad Request" };
				}

				var newCategory = _categoryService.GetCategory(nextId);
				var newCategoryJson = JsonSerializer.Serialize(new { cid = newCategory.Id, name = newCategory.Name },
					new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

				return new Response { Status = "2 Created", Body = newCategoryJson };
			}
			catch (JsonException)
			{
				return new Response { Status = "4 Bad Request" };
			}
		}

		private Response HandleUpdate(Request request, UrlParser urlParser)
		{
			if (urlParser.Path != "/api/categories")
			{
				return new Response { Status = "5 Not Found" };
			}

			if (!urlParser.HasId)
			{
				return new Response { Status = "4 Bad Request" };
			}

			try
			{
				if (!int.TryParse(urlParser.Id, out int categoryId))
				{
					return new Response { Status = "4 Bad Request" };
				}

				// Deserialize the update request
				using JsonDocument doc = JsonDocument.Parse(request.Body);
				if (!doc.RootElement.TryGetProperty("name", out JsonElement nameElement) ||
					nameElement.ValueKind != JsonValueKind.String)
				{
					return new Response { Status = "4 Bad Request" };
				}

				string categoryName = nameElement.GetString();
				if (string.IsNullOrEmpty(categoryName))
				{
					return new Response { Status = "4 Bad Request" };
				}

				var existingCategory = _categoryService.GetCategory(categoryId);
				if (existingCategory == null)
				{
					return new Response { Status = "5 Not Found" };
				}

				var success = _categoryService.UpdateCategory(categoryId, categoryName);
				if (!success)
				{
					return new Response { Status = "4 Bad Request" };
				}

				return new Response { Status = "3 Updated" };
			}
			catch (JsonException)
			{
				return new Response { Status = "4 Bad Request" };
			}
		}

		private Response HandleDelete(Request request, UrlParser urlParser)
		{
			if (urlParser.Path != "/api/categories")
			{
				return new Response { Status = "5 Not Found" };
			}

			if (!urlParser.HasId)
			{
				return new Response { Status = "4 Bad Request" };
			}

			try
			{
				if (!int.TryParse(urlParser.Id, out int categoryId))
				{
					return new Response { Status = "4 Bad Request" };
				}

				var existingCategory = _categoryService.GetCategory(categoryId);
				if (existingCategory == null)
				{
					return new Response { Status = "5 Not Found" };
				}

				var success = _categoryService.DeleteCategory(categoryId);
				if (!success)
				{
					return new Response { Status = "4 Bad Request" };
				}

				return new Response { Status = "1 Ok" };
			}
			catch (FormatException)
			{
				return new Response { Status = "4 Bad Request" };
			}
		}
	}
}
