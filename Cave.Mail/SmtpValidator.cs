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

using Cave.Net;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;

namespace Cave.Mail
{
    /// <summary>
    /// Provides email validation by asking the reciepients smtp server.
    /// </summary>
    public class SmtpValidator
    {
        /// <summary>
        /// Provides available <see cref="SmtpValidator"/> results.
        /// </summary>
        public enum SmtpValidatorResult
        {
            /// <summary>success</summary>
            Success = 0,

            /// <summary>error: network not available</summary>
            ErrorMyNetwork = 1,

            /// <summary>error: my settings are invalid (target does not accept <see cref="SmtpValidator"/> as sender)</summary>
            ErrorMySettings,

            /// <summary>error: connection to the server cannot be established</summary>
            ErrorServer = 0x101,

            /// <summary>error address is invalid</summary>
            ErrorAddress,
        }

        bool IsOk(int v)
        {
            return (v >= 250) && (v < 260);
        }

        int ParseAnswer(string answer)
        {
            string[] parts = answer.Split(new char[] { ' ' }, 2);
            int code;
            if (!int.TryParse(parts[0], out code))
            {
                throw new InvalidDataException("SmtpValidator_ProtocolError");
            }
            if (parts.Length > 1)
            {
                Trace.TraceInformation(string.Format("Server_AnswerWithResult {0} {1}", code, parts[1]));
            }
            else
            {
                Trace.TraceInformation(string.Format("Server_Answer {0}", code));
            }
            return code;
        }

        /// <summary>Gets the name of the log source.</summary>
        /// <value>The name of the log source.</value>
        public string LogSourceName => "SmtpValidator";

        /// <summary>Gets our full qualified server address (has to match rdns).</summary>
        /// <value>Our full qualified server address.</value>
        public string Server { get; private set; }

        /// <summary>Gets our email address (has to exist).</summary>
        /// <value>Our email address.</value>
        public MailAddress Sender { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="SmtpValidator"/> class.</summary>
        /// <param name="server">Our full qualified server address.</param>
        /// <param name="sender">Our email address.</param>
        public SmtpValidator(string server, MailAddress sender)
        {
            if (Dns.GetHostEntry(server) == null)
            {
                throw new NetworkException("Cannot find my own server name in dns!");
            }

            Server = server;
            Sender = sender;
        }

        /// <summary>Validates the specified target email address.</summary>
        /// <param name="target">The target email address.</param>
        /// <param name="throwException">if set to <c>true</c> [throw exception].</param>
        /// <returns>bool on success, false otherwise.</returns>
        /// <exception cref="ArgumentException">
        /// Server not available!;target.Host
        /// or
        /// Server does not accept me as sender!;Server
        /// or
        /// Target address does not exist!;target.Address
        /// or
        /// Server does not accept me as sender!;Sender.Address
        /// or
        /// Target address does not exist!;target.Address.
        /// </exception>
        /// <exception cref="InvalidDataException">Smtp protocol error!.</exception>
        public SmtpValidatorResult Validate(MailAddress target, bool throwException)
        {
            foreach (int port in new int[] { 25, 587 })
            {
                try
                {
                    using (TcpClient client = new TcpClient(target.Host, port))
                    {
                        using (Stream stream = client.GetStream())
                        {
                            StreamWriter writer = new StreamWriter(stream);
                            StreamReader reader = new StreamReader(stream);
                            if (ParseAnswer(reader.ReadLine()) != 220)
                            {
                                if (throwException)
                                {
                                    throw new ArgumentException($"SmtpValidator_ServerNotAvailable {target}");
                                }

                                return SmtpValidatorResult.ErrorServer;
                            }

                            writer.WriteLine("HELO " + Server);
                            if (!IsOk(ParseAnswer(reader.ReadLine())))
                            {
                                if (throwException)
                                {
                                    throw new ArgumentException($"SmtpValidator_ServerDoesNotAcceptMe {target}");
                                }

                                return SmtpValidatorResult.ErrorMySettings;
                            }

                            writer.WriteLine("VRFY " + target.Address);
                            if (!IsOk(ParseAnswer(reader.ReadLine())))
                            {
                                if (throwException)
                                {
                                    throw new ArgumentException($"Error_TargetAddressInvalid {target}");
                                }

                                return SmtpValidatorResult.ErrorAddress;
                            }

                            writer.WriteLine("MAIL " + Sender.Address);
                            if (!IsOk(ParseAnswer(reader.ReadLine())))
                            {
                                if (throwException)
                                {
                                    throw new ArgumentException($"SmtpValidator_ServerDoesNotAcceptMe {target}");
                                }

                                return SmtpValidatorResult.ErrorMySettings;
                            }

                            writer.WriteLine("RCPT " + target.Address);
                            if (!IsOk(ParseAnswer(reader.ReadLine())))
                            {
                                if (throwException)
                                {
                                    throw new ArgumentException($"Error_TargetAddressInvalid {target}");
                                }

                                return SmtpValidatorResult.ErrorAddress;
                            }

                            writer.WriteLine("RSET");
                            string s = reader.ReadLine();

                            writer.WriteLine("QUIT");
                            s = reader.ReadLine();
                        }
                    }
                    return SmtpValidatorResult.Success;
                }
                catch (ArgumentException ex)
                {
                    if (port == 587)
                    {
                        throw;
                    }
                }
                catch (SocketException ex)
                {
                    if (port == 587)
                    {
                        if (throwException)
                        {
                            throw;
                        }

                        switch (ex.SocketErrorCode)
                        {
                            case SocketError.ConnectionRefused:
                            case SocketError.ConnectionReset:
                            case SocketError.HostDown:
                            case SocketError.HostNotFound:
                            case SocketError.HostUnreachable:
                                return SmtpValidatorResult.ErrorServer;

                            default:
                                return SmtpValidatorResult.ErrorMyNetwork;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (port == 587)
                    {
                        if (throwException)
                        {
                            throw;
                        }
                    }
                }
            }
            return SmtpValidatorResult.ErrorServer;
        }
    }
}
