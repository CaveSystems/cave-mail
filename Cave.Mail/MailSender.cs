#region CopyRight 2018
/*
    Copyright (c) 2010-2018 Andreas Rohleder (andreas@rohleder.cc)
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

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using Cave.IO;

namespace Cave.Mail
{
    /// <summary>
    /// Provides mail sending.
    /// </summary>
    public class MailSender
    {
        SmtpClient smtpClient;

        /// <summary>Initializes a new instance of the <see cref="MailSender" /> class.</summary>
        /// <param name="settings">The settings.</param>
        /// <exception cref="System.Exception"></exception>
        public MailSender(ISettings settings)
        {
            try
            {
                string user = settings.ReadSetting("Mail", "Username");
                string pass = settings.ReadSetting("Mail", "Password");
                string server = settings.ReadString("Mail", "Server", "localhost");
                string sender = settings.ReadString("Mail", "Sender", "postmaster@" + server);
                Sender = new MailAddress(sender);
                BCC = new List<MailAddress>();
                string bccString = settings.ReadSetting("Mail", "BCC");
                if (bccString != null)
                {
                    foreach (string addr in bccString.Split(';'))
                    {
                        if (addr.Trim() == "")
                        {
                            continue;
                        }

                        BCC.Add(new MailAddress(addr));
                    }
                }
                smtpClient = new SmtpClient();
                if (user != null && pass != null)
                {
                    smtpClient.Credentials = new NetworkCredential(user, pass);
                }

                smtpClient.Host = server;
                smtpClient.EnableSsl = settings.ReadBool("Mail", "DisableSSL", false) != true;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error while loading configuration from {0}", settings.Name), ex);
            }
        }

        /// <summary>
        /// The sender address.
        /// </summary>
        public MailAddress Sender { get; set; }

        /// <summary>Gets the BCC added to all sent messages.</summary>
        /// <value>The BCC added to all sent messages.</value>
        public List<MailAddress> BCC { get; }

        /// <summary>Gets the name of the log source.</summary>
        /// <value>The name of the log source.</value>
        public string LogSourceName => "MailSender";

        /// <summary>
        /// Enable or disable ssl.
        /// </summary>
        public bool EnableSsl => smtpClient.EnableSsl;

        /// <summary>
        /// The mail server to use.
        /// </summary>
        public string Server => smtpClient.Host;

        /// <summary>
        /// Sets the credentials.
        /// </summary>
        /// <param name="credentials"></param>
        public void SetCredentials(NetworkCredential credentials)
        {
            smtpClient.Credentials = credentials;
        }

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
            for (int i = 0; ; i++)
            {
                try
                {
                    smtpClient.Timeout = 5000;
                    Trace.TraceInformation("Sending mail message <cyan>{0}<default> to <cyan>{1}", message.Subject, message.To);
                    smtpClient.Send(message);
                    return true;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error while sending mail to <red>{0}", message.To);
                    if (i >= retries)
                    {
                        if (throwException)
                        {
                            throw;
                        }

                        return false;
                    }
                }
            }
        }
    }
}
