using System;
using System.Net.Mail;

namespace Al
{
    public static class MailManager
    {
        public static void SendVerificationCode(string targetEmail, string code)
        {
            try
            {
                string smtpHost = "127.0.0.1";
                int smtpPort = 25;

                var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = false,
                    UseDefaultCredentials = true 
                };

                var fromAddress = new MailAddress("Code@steel-team.ru", "Steel Connect");
                var toAddress = new MailAddress(targetEmail);

                var mailMessage = new MailMessage(fromAddress, toAddress)
                {
                    Subject = "Вход в аккаунт Steel Brawl",
                    Body = $"Привет, Игрок!\n\nТвой код для входа: {code}\n\nНикому не сообщай этот код. Если это был не ты, просто проигнорируй это письмо.",
                    IsBodyHtml = false, 
                };

                smtpClient.Send(mailMessage);
                Console.WriteLine($"[MAIL] Код {code} отправлен на {targetEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MAIL ERROR] Ошибка отправки: {ex.Message}");
            }
        }

        public static string GenerateCode()
        {
            Random rnd = new Random();
            return rnd.Next(100000, 999999).ToString();
        }
    }
}
