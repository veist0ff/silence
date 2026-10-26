using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Al
{
    public static class AuthServer
    {
        private static HttpListener _listener;
        private static string _connectionString;

        public static void Init(string dbUser, string dbPassword, string dbName)
        {
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder
            {
                Server = "127.0.0.1",
                UserID = dbUser,
                Password = dbPassword,
                Database = dbName,
                CharacterSet = "utf8mb4",
                SslMode = MySqlSslMode.Disabled,
                AllowPublicKeyRetrieval = true
            };
            _connectionString = builder.ToString();

            _listener = new HttpListener();
            _listener.Prefixes.Add("http://127.0.0.1:9283/");
            _listener.Start();

            Console.WriteLine("[AUTH] Conncect Auth Started");

            Task.Run(() => ListenAsync());
        }

        private static async Task ListenAsync()
        {
            while (true)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequestAsync(context));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AUTH ERROR] Listen: {ex.Message}");
                }
            }
        }

        private static async Task HandleRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (request.HttpMethod == "OPTIONS")
            {
                SendResponse(response, 200, "");
                return;
            }

            try
            {
                string path = request.Url.AbsolutePath;

                if (request.HttpMethod == "POST" && (path == "/send_code" || path == "/api/send_code"))
                {
                    Console.WriteLine($"[AUTH] Получен запрос на отправку кода"); 
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        string body = await reader.ReadToEndAsync();
                        JObject json = JObject.Parse(body);
                        string email = json["email"]?.ToString();

                        if (string.IsNullOrEmpty(email))
                        {
                            SendResponse(response, 400, "{\"status\": \"error\", \"message\": \"Email is required\"}");
                            return;
                        }

                        string code = MailManager.GenerateCode();
                        DateTime expiresAt = DateTime.Now.AddMinutes(10);

                        using (var conn = new MySqlConnection(_connectionString))
                        {
                            await conn.OpenAsync();
                            string query = "INSERT INTO email_verification (Email, Code, ExpiresAt) VALUES (@email, @code, @expires) " +
                                           "ON DUPLICATE KEY UPDATE Code=@code, ExpiresAt=@expires";
                            using (var cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@email", email);
                                cmd.Parameters.AddWithValue("@code", code);
                                cmd.Parameters.AddWithValue("@expires", expiresAt);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        MailManager.SendVerificationCode(email, code);
                        SendResponse(response, 200, "{\"status\": \"ok\"}");
                        return;
                    }
                }

                if (request.HttpMethod == "POST" && (path == "/verify_code" || path == "/api/verify_code"))
                {
                    Console.WriteLine($"[AUTH] Получен запрос на проверку кода"); 
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        string body = await reader.ReadToEndAsync();
                        JObject json = JObject.Parse(body);
                        string email = json["email"]?.ToString();
                        string code = json["code"]?.ToString();

                        using (var conn = new MySqlConnection(_connectionString))
                        {
                            await conn.OpenAsync();
                            string query = "SELECT Code, ExpiresAt FROM email_verification WHERE Email = @email";
                            using (var cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@email", email);
                                using (var dbReader = await cmd.ExecuteReaderAsync())
                                {
                                    if (await dbReader.ReadAsync())
                                    {
                                        string dbCode = dbReader.GetString(0);
                                        DateTime expiresAt = dbReader.GetDateTime(1);

                                        if (dbCode == code && expiresAt > DateTime.Now)
                                        {
                                            dbReader.Close();
                                            
                                            string delQuery = "DELETE FROM email_verification WHERE Email = @email";
                                            using (var delCmd = new MySqlCommand(delQuery, conn))
                                            {
                                                delCmd.Parameters.AddWithValue("@email", email);
                                                await delCmd.ExecuteNonQueryAsync();
                                            }

                                            long accountId = 0;
                                            string passToken = "";

                                            // Ищем аккаунт
                                            string checkAcc = "SELECT Id, Data FROM accounts WHERE Email = @email LIMIT 1";
                                            using (var cmd2 = new MySqlCommand(checkAcc, conn))
                                            {
                                                cmd2.Parameters.AddWithValue("@email", email);
                                                using (var accReader = await cmd2.ExecuteReaderAsync())
                                                {
                                                    if (await accReader.ReadAsync())
                                                    {
                                                        accountId = accReader.GetInt64(0);
                                                        Account account = JsonConvert.DeserializeObject<Account>(accReader.GetString(1));
                                                        passToken = account.PassToken;
                                                    }
                                                }
                                            }

                                            if (accountId == 0)
                                            {
                                                Account newAcc = Accounts.Create();
                                                accountId = newAcc.AccountId;
                                                passToken = newAcc.PassToken;

                                                string updateAcc = "UPDATE accounts SET Email = @email WHERE Id = @id";
                                                using (var cmd3 = new MySqlCommand(updateAcc, conn))
                                                {
                                                    cmd3.Parameters.AddWithValue("@email", email);
                                                    cmd3.Parameters.AddWithValue("@id", accountId);
                                                    await cmd3.ExecuteNonQueryAsync();
                                                }
                                            }

                                            string jsonResponse = JsonConvert.SerializeObject(new
                                            {
                                                status = "ok",
                                                accountId = accountId,
                                                passToken = passToken
                                            });
                                            SendResponse(response, 200, jsonResponse);
                                            return;
                                        }
                                    }
                                }
                            }
                            SendResponse(response, 403, "{\"status\": \"error\", \"message\": \"Invalid or expired code\"}");
                            return;
                        }
                    }

                    SendResponse(response, 404, "{\"status\": \"error\", \"message\": \"Not found\"}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUTH ERROR] {ex.Message}");
                SendResponse(response, 500, "{\"status\": \"error\", \"message\": \"Internal server error\"}");
            }
        }

        private static void SendResponse(HttpListenerResponse response, int statusCode, string responseString)
        {
            response.StatusCode = statusCode;
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            using (var output = response.OutputStream)
            {
                output.Write(buffer, 0, buffer.Length);
            }
            response.Close();
        }
    }
}
