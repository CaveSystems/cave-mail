#region CopyRight 2018
/*
    Copyright (c) 2003-2018 Andreas Rohleder (andreas@rohleder.cc)
    All rights reserved
*/
#endregion
#region License LGPL-3
/*
    This program/library/sourcecode is free software; you can redistribute it
    and/or modify it under the terms of the GNU Lesser General Public License
    version 3 as published by the Free Software Foundation subsequent called
    the License.

    You may not use this program/library/sourcecode except in compliance
    with the License. The License is included in the LICENSE file
    found at the installation directory or the distribution package.

    The above copyright notice and this permission notice shall be included
    in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
    NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
    LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
    OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
    WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion License
#region Authors & Contributors
/*
   Author:
     Andreas Rohleder <andreas@rohleder.cc>

   Contributors:
 */
#endregion Authors & Contributors

using Cave.Collections.Generic;
using Cave.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading;

namespace Cave.Mail
{
    /// <summary>
    /// Provides (html) email sending.
    /// </summary>
    public class SimpleMailer 
    {
        #region private implementation
        string server;
        string username;
        string password;
        int port;

        /// <summary>Gets or sets from address.</summary>
        /// <value>From address.</value>
        public MailAddress From { get; set; }

        /// <summary>Gets to addresses.</summary>
        /// <value>Toaddresses.</value>
        public Set<MailAddress> To { get; } = new Set<MailAddress>();

        /// <summary>Gets the BCC addresses.</summary>
        /// <value>The BCC addresses.</value>
        public Set<MailAddress> Bcc { get; } = new Set<MailAddress>();

        /// <summary>Gets or sets the subject.</summary>
        /// <value>The subject.</value>
        public string Subject { get; set; }

        /// <summary>Gets or sets the content HTML.</summary>
        /// <value>The content HTML.</value>
        public string ContentHtml { get; set; }

        /// <summary>Gets or sets the content text.</summary>
        /// <value>The content text.</value>
        public string ContentText { get; set; }

        /// <summary>Gets the name of the log source.</summary>
        /// <value>The name of the log source.</value>
        public string LogSourceName
        {
            get
            {
                if (Username.Contains("@"))
                {
                    return $"SimpleMailer {Username}";
                }

                return $"SimpleMailer {Username}@{Server}";
            }
        }

        /// <summary>Gets or sets the server.</summary>
        /// <value>The server.</value>
        public string Server { get { return server; } set { server = value; } }

        /// <summary>Gets or sets the port.</summary>
        /// <value>The port.</value>
        public int Port { get { return port; } set { port = value; } }

        /// <summary>Gets or sets the password.</summary>
        /// <value>The password.</value>
        public string Password { get { return password; } set { password = value; } }

        /// <summary>Gets or sets the username.</summary>
        /// <value>The username.</value>
        public string Username { get { return username; } set { username = value; } }

        #endregion

        #region constructor        
        /// <summary>Initializes a new instance of the <see cref="SimpleMailer"/> class using the given configuration.</summary>
        /// <param name="config">The configuration.</param>
        public SimpleMailer(ISettings config)
        {
            if (!config.GetValue("Mail", "Server", ref server) ||
                !config.GetValue("Mail", "Port", ref port) ||
                !config.GetValue("Mail", "Password", ref password) ||
                !config.GetValue("Mail", "Username", ref username))
            {
                throw new Exception("[Mail] configuration is invalid!");
            }
            //TODO: Optional Display Name for Sender
            string from = config.ReadSetting("Mail", "From");
            From = new MailAddress(from);
            To.LoadAddresses(config.ReadSection("SendTo", true));
            Bcc.LoadAddresses(config.ReadSection("BlindCarbonCopy", true));
        }

        #endregion

        #region public functionality

        /// <summary>Sends an email.</summary>
        public void Send(Dictionary<string, string> headers = null)
        {
            if (To.Count == 0)
            {
                throw new Exception("No recepient (SendTo) address.");
            }

            using (MailMessage message = new MailMessage())
            {
                if (headers != null) { foreach (var i in headers)
                    {
                        message.Headers[i.Key] = i.Value;
                    }
                }
                foreach (MailAddress a in To)
                {
                    message.To.Add(a);
                }

                foreach (MailAddress a in Bcc)
                {
                    message.Bcc.Add(a);
                }

                message.Subject = Subject;
                message.From = From;
                AlternateView plainText = AlternateView.CreateAlternateViewFromString(ContentText, null, MediaTypeNames.Text.Plain);
                AlternateView htmlText = AlternateView.CreateAlternateViewFromString(ContentHtml, Encoding.UTF8, MediaTypeNames.Text.Html);
                message.AlternateViews.Add(plainText);
                message.AlternateViews.Add(htmlText);
                for (int i = 0; ; i++)
                {
					try
					{
						SmtpClient client = new SmtpClient(Server, Port);
						client.Timeout = 50000;
						client.EnableSsl = true;
						client.Credentials = new NetworkCredential(Username, Password);
						client.Send(message);
						(client as IDisposable)?.Dispose();
						Trace.TraceInformation("Sent email '<green>{0}<default>' to <cyan>{1}", message.Subject, message.To);
						break;
					}
					catch
					{
						if (i > 3)
                        {
                            throw;
                        }

                        Thread.Sleep(1000);
					}
                }
            }
        }

        /// <summary>Loads a content from html and txt file for the specified culture.</summary>
        /// <param name="folder">The folder.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="culture">The culture.</param>
        public void LoadContent(string folder, string fileName, CultureInfo culture)
        {
            var path = Path.Combine(folder, fileName + "." + culture.TwoLetterISOLanguageName + ".html");
            if (File.Exists(path))
            {
                ContentHtml = File.ReadAllText(path);
            }
            else
            {
                ContentHtml = File.ReadAllText(Path.Combine(folder, fileName + ".html"));
            }
            path = Path.Combine(folder, fileName + "." + culture.TwoLetterISOLanguageName + ".txt");
            if (File.Exists(path))
            {
                ContentHtml = File.ReadAllText(path);
            }
            else
            {
                ContentHtml = File.ReadAllText(Path.Combine(folder, fileName + ".txt"));
            }
        }
#endregion
    }
}
