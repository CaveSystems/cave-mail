#region CopyRight 2018
/*
    Copyright (c) 2007-2018 Andreas Rohleder (andreas@rohleder.cc)
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
#endregion
#region Authors & Contributors
/*
   Author:
     Andreas Rohleder <andreas@rohleder.cc>

   Contributors:
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using Cave.IO;
using Cave.Net;

namespace Cave.Mail.Imap
{
    /// <summary>
    /// Provides a simple imap client.
    /// </summary>
    public sealed class ImapClient : IDisposable
    {
        /// <summary>The imap new line characters.</summary>
        public const string ImapNewLine = "\r\n";

        int counter = 1;
        Stream stream;
        TcpClient client;

        #region properties        
        /// <summary>Gets the selected mailbox.</summary>
        /// <value>The selected mailbox.</value>
        public ImapMailboxInfo SelectedMailbox { get; private set; }

        /// <summary>Gets the name of the log source.</summary>
        /// <value>The name of the log source.</value>
        public string LogSourceName
        {
            get
            {
                if (client != null)
                {
                    return "ImapClient <" + client.Client.RemoteEndPoint + ">";
                }

                return "ImapClient <not connected>";
            }
        }
        #endregion

        #region private implementation
        ImapAnswer ReadAnswer(string id, bool throwEx)
        {
            var answer = ImapParser.Parse(id, stream);
            if (throwEx && !answer.Success)
            {
                answer.Throw();
            }

            return answer;
        }

        ImapAnswer SendCommand(string cmd, params object[] parameters) => SendCommand(string.Format(cmd, parameters));

        ImapAnswer SendCommand(string cmd)
        {
            var id = counter++.ToString("X2");
            var command = id + " " + cmd;
            var writer = new DataWriter(stream);
            writer.Write(ASCII.GetBytes(command + ImapNewLine));
            writer.Flush();
            var answer = ReadAnswer(id, true);
            return answer;
        }

        string PrepareLiteralDataCommand(string cmd)
        {
            var id = counter++.ToString("X2");
            var command = id + " " + cmd;
            var writer = new DataWriter(stream);
            writer.Write(ASCII.GetBytes(command + ImapNewLine));
            writer.Flush();
            var answer = ReadAnswer("+", false);
            if (!answer.Result.ToUpperInvariant().StartsWith("+ READY"))
            {
                answer.Throw();
            }

            return id;
        }

        void Login(string user, string password)
        {
            if (user.HasInvalidChars(ASCII.Strings.SafeUrlOptions))
            {
                throw new Exception("User has invalid characters!");
            }

            if (password.HasInvalidChars(ASCII.Strings.SafeUrlOptions))
            {
                throw new Exception("Password has invalid characters!");
            }

            ReadAnswer("*", true);
            SendCommand("LOGIN " + user + " " + password);
        }
        #endregion

        #region public implementation
        /// <summary>Does a Logon with SSL.</summary>
        /// <param name="con">The connection string.</param>
        public void LoginSSL(ConnectionString con) => LoginSSL(con.UserName, con.Password, con.Server, con.GetPort(993));

        /// <summary>Does a Logon with SSL.</summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <param name="server">The server.</param>
        /// <param name="port">The port.</param>
        public void LoginSSL(string user, string password, string server, int port = 993)
        {
            var sslClient = new SslClient();
            sslClient.Connect(server, port);
            sslClient.DoClientTLS(server);
            stream = sslClient.Stream;
            Login(user, password);
        }

        /// <summary>Obtains a list of all present mailboxes.</summary>
        /// <returns></returns>
        public string[] ListMailboxes()
        {
            var list = new List<string>();
            var answer = SendCommand("LIST \"\" *");
            foreach (var line in answer.GetDataLines())
            {
                if (line.StartsWith("* LIST "))
                {
                    var parts = ImapParser.SplitAnswer(line);
                    var mailbox = parts[4].UnboxText(false);
                    mailbox = UTF7.DecodeExtendedUTF7(mailbox);
                    list.Add(mailbox);
                }
            }
            return list.ToArray();
        }

        /// <summary>Creates a new mailbox.</summary>
        /// <param name="mailbox">The mailbox.</param>
        public void CreateMailbox(string mailbox) => SendCommand("CREATE \"{0}\"", mailbox);

        /// <summary>Selects the mailbox with the specified name.</summary>
        /// <param name="mailboxName">Name of the mailbox.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public ImapMailboxInfo Select(string mailboxName)
        {
            var mailbox = UTF7.EncodeExtendedUTF7(mailboxName);
            var answer = SendCommand("SELECT \"{0}\"", mailbox);
            if (!answer.Success)
            {
                throw new Exception(string.Format("Error at select mailbox {0}: {1}", mailboxName, answer.Result));
            }

            SelectedMailbox = ImapMailboxInfo.FromAnswer(mailboxName, answer);
            return SelectedMailbox;
        }

        /// <summary>examines a mailbox.</summary>
        /// <param name="mailboxName">Name of the mailbox.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public ImapMailboxInfo Examine(string mailboxName)
        {
            var mailbox = UTF7.EncodeExtendedUTF7(mailboxName);
            var answer = SendCommand("EXAMINE \"" + mailbox + "\"");
            if (!answer.Success)
            {
                throw new Exception(string.Format("Error at examine mailbox {0}: {1}", mailboxName, answer.Result));
            }

            return ImapMailboxInfo.FromAnswer(mailboxName, answer);
        }

        /// <summary>Retrieves a message by its internal number (1..<see cref="ImapMailboxInfo.Exist" />).</summary>
        /// <param name="number">The internal message number (1..<see cref="ImapMailboxInfo.Exist" />).</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public byte[] GetMessageData(int number)
        {
            if (number < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(number));
            }

            var answer = SendCommand("FETCH " + number + " BODY[]");
            var start = Array.IndexOf(answer.Data, (byte)'\n') + 1;
            var end = Array.LastIndexOf(answer.Data, (byte)')');
            var message = new byte[end - start];
            Array.Copy(answer.Data, start, message, 0, end - start);
            return message;
        }

        /// <summary>Retrieves a message by its internal number (1..<see cref="ImapMailboxInfo.Exist" />).</summary>
        /// <param name="number">The internal message number (1..<see cref="ImapMailboxInfo.Exist" />).</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public Rfc822Message GetMessage(int number)
        {
            if (number < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(number));
            }

            var answer = SendCommand("FETCH " + number + " BODY[]");
            var start = Array.IndexOf(answer.Data, (byte)'\n') + 1;
            var end = Array.LastIndexOf(answer.Data, (byte)')') - 2;
            var message = new byte[end - start];
            Array.Copy(answer.Data, start, message, 0, end - start);
            return Rfc822Message.FromBinary(message);
        }

        /// <summary>Retrieves a message header by its internal number (1..<see cref="ImapMailboxInfo.Exist" />).</summary>
        /// <param name="number">The internal message number (1..<see cref="ImapMailboxInfo.Exist" />).</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.FormatException"></exception>
        public Rfc822Message GetMessageHeader(int number)
        {
            if (number < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(number));
            }

            while (true)
            {
                var answer = SendCommand("FETCH " + number + " BODY[HEADER]");
                if (answer.Data.Length == 0)
                {
                    continue;
                }

                var streamReader = answer.GetStreamReader(0);
                var header = streamReader.ReadLine();
                var size = int.Parse(header.Substring(header.LastIndexOf('{')).Unbox("{", "}", true));
                var dataReader = answer.GetDataReader(header.Length + 2);
                var messageData = dataReader.ReadBytes(size);
                if (messageData.Length != size)
                {
                    throw new FormatException();
                }

                var message = Rfc822Message.FromBinary(messageData);
                return message;
            }
        }

        /// <summary>Uploads a message to the specified mailbox.</summary>
        /// <param name="mailboxName">Name of the mailbox.</param>
        /// <param name="messageData">The message data.</param>
        /// <exception cref="System.ArgumentNullException">messageData.</exception>
        public void UploadMessageData(string mailboxName, byte[] messageData)
        {
            var mailbox = UTF7.EncodeExtendedUTF7(mailboxName);
            if (messageData == null)
            {
                throw new ArgumentNullException(nameof(messageData));
            }

            var id = PrepareLiteralDataCommand("APPEND \"" + mailbox + "\" (\\Seen) {" + messageData.Length + "}");
            var writer = new DataWriter(stream);
            writer.Write(messageData);
            writer.Write((byte)13);
            writer.Write((byte)10);
            writer.Flush();
            ReadAnswer(id, true);
        }

        /// <summary>Searches the specified search.</summary>
        /// <param name="search">The search.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public ImapNumberSequence Search(ImapSearch search)
        {
            var answer = SendCommand("SEARCH {0}", search);
            if (!answer.Success)
            {
                throw new Exception(string.Format("Error at search {0}", search));
            }

            var sequence = new ImapNumberSequence();
            foreach (var line in answer.GetDataLines())
            {
                if (line.StartsWith("* SEARCH "))
                {
                    var s = line.Substring(9);
                    if (int.TryParse(s, out var value))
                    {
                        sequence += ImapNumberSequence.CreateList(value);
                    }
                    else
                    {
                        sequence += ImapNumberSequence.FromString(s);
                    }
                }
            }
            return sequence;
        }

        /// <summary>Sets the specified flags at the message with the given number.</summary>
        /// <param name="number">The number.</param>
        /// <param name="flags">The flags.</param>
        /// <exception cref="Exception">Error at store flags.</exception>
        public void SetFlags(int number, params string[] flags)
        {
            var answer = SendCommand("STORE {0} +FLAGS ({1})", number, string.Join(" ", flags));
            if (!answer.Success)
            {
                throw new Exception("Error at store flags");
            }
        }

        /// <summary>Expunges this instance.</summary>
        /// <exception cref="Exception">Error at store flags.</exception>
        public void Expunge()
        {
            var answer = SendCommand("EXPUNGE");
            if (!answer.Success)
            {
                throw new Exception("Error at store flags");
            }
        }

        /// <summary>Closes this instance.</summary>
        public void Close()
        {
            stream?.Close();
            client?.Close();
            Dispose();
        }
        #endregion

        #region IDisposable implementation

        /// <summary>Releases the unmanaged resources used by this instance and optionally releases the managed resources.</summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (client is IDisposable dispo)
                {
                    dispo.Dispose();
                }

                client = null;
                stream?.Dispose();
                stream = null; 
            }
        }

        /// <summary>
        /// Releases all resources used by the this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
