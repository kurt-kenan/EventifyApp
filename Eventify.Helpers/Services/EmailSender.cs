using MailKit.Net.Smtp;
using MimeKit;
using System.Threading.Tasks;

public class EmailSender
{
    private readonly string _smtpServer = "smtp.gmail.com";
    private readonly int _smtpPort = 587;
    private readonly string _email;
    private readonly string _password;

    public EmailSender(string email, string password)
    {
        _email = email;
        _password = password;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Eventify", _email));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_email, _password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
