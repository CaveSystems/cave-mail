using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using Cave.Logging;

namespace Cave.Mail
{
    /// <summary>
    /// Provides mail sending.
    /// </summary>
    public class MailSender
    {
        static readonly Logger Log = new(nameof(MailSender));
        readonly SmtpClient Client;

        /// <summary>Initializes a new instance of the <see cref="MailSender" /> class.</summary>
        /// <param name="settings">The settings.</param>
        /// <exception cref="System.Exception"></exception>
        public MailSender(IniReader settings)
        {
            try
            {
                var user = settings.ReadSetting("Mail", "Username");
                var pass = settings.ReadSetting("Mail", "Password");
                var server = settings.ReadString("Mail", "Server", "localhost");
                var sender = settings.ReadString("Mail", "Sender", "postmaster@" + server);
                Sender = new MailAddress(sender);
                BCC = new List<MailAddress>();
                var bccString = settings.ReadSetting("Mail", "BCC");
                if (bccString != null)
                {
                    foreach (var addr in bccString.Split(';'))
                    {
                        if (addr.Trim() == "")
                        {
                            continue;
                        }

                        BCC.Add(new MailAddress(addr));
                    }
                }
                Client = new SmtpClient();
                if (user != null && pass != null)
                {
                    Client.Credentials = new NetworkCredential(user, pass);
                }

                Client.Host = server;
                Client.EnableSsl = settings.ReadBool("Mail", "DisableSSL", false) != true;
            }
            catch (Exception ex)
            {
                var msg = string.Format("Error while loading configuration from {0}", settings);
                Log.LogWarning(ex, msg);
                throw new Exception(msg, ex);
            }
        }

        /// <summary>
        /// The sender address.
        /// </summary>
        public MailAddress Sender { get; set; }

        /// <summary>Gets the BCC added to all sent messages.</summary>
        /// <value>The BCC added to all sent messages.</value>
        public List<MailAddress> BCC { get; }

        /// <summary>
        /// Enable or disable ssl.
        /// </summary>
        public bool EnableSsl => Client.EnableSsl;

        /// <summary>
        /// The mail server to use.
        /// </summary>
        public string Server => Client.Host;

        /// <summary>
        /// Sets the credentials.
        /// </summary>
        /// <param name="credentials"></param>
        public void SetCredentials(NetworkCredential credentials) => Client.Credentials = credentials;

        /// <summary>Sends the specified message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="retries">The retries.</param>
        /// <param name="throwException">if set to <c>true</c> [throw exception].</param>
        /// <returns></returns>
        public bool Send(MailMessage message, int retries = 3, bool throwException = true)
        {
            foreach (var bcc in BCC)
            {
                message.Bcc.Add(bcc);
            }

            message.From = Sender;
            for (var i = 1; ; i++)
            {
                try
                {
                    Client.Timeout = 5000;
                    Log.LogVerbose("Sending mail message <cyan>{0}<default> to <cyan>{1}", message.Subject, message.To);
                    Client.Send(message);
                    Log.LogInfo("Sent mail message <cyan>{0}<default> to <cyan>{1}", message.Subject, message.To);
                    return true;
                }
                catch (Exception ex)
                {
                    if (i > retries)
                    {
                        if (throwException)
                        {
                            Log.LogError(ex, "Error while sending mail to <red>{0}", message.To);
                            throw;
                        }

                        return false;
                    }
                    Log.LogWarning(ex, "Error while sending mail to <red>{0}<default>. Try <red>{1}<default>..", message.To, i);
                }
            }
        }
    }
}
