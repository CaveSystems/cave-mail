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

        int m_Counter = 1;
        Stream m_Stream;
        TcpClient m_Client;

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
                if (m_Client != null)
                {
                    return "ImapClient <" + m_Client.Client.RemoteEndPoint + ">";
                }

                return "ImapClient <not connected>";
            }
        }
        #endregion

        #region private implementation
        ImapAnswer ReadAnswer(string id, bool throwEx)
        {
            ImapAnswer answer = ImapParser.Parse(id, m_Stream);
            if (throwEx && !answer.Success)
            {
                answer.Throw();
            }

            return answer;
        }

        ImapAnswer SendCommand(string cmd, params object[] parameters)
        {
            return SendCommand(string.Format(cmd, parameters));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "Cave.Text.ASCII.GetBytes(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "Cave.Text.XT.op_Implicit(System.String)")]
        ImapAnswer SendCommand(string cmd)
        {
            string id = m_Counter++.ToString("X2");
            string command = id + " " + cmd;
            DataWriter writer = new DataWriter(m_Stream);
            writer.Write(ASCII.GetBytes(command + ImapNewLine));
            writer.Flush();
            ImapAnswer answer = ReadAnswer(id, true);
            return answer;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "Cave.Text.ASCII.GetBytes(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "Cave.Text.XT.op_Implicit(System.String)")]
        string PrepareLiteralDataCommand(string cmd)
        {
            string id = m_Counter++.ToString("X2");
            string command = id + " " + cmd;
            DataWriter writer = new DataWriter(m_Stream);
            writer.Write(ASCII.GetBytes(command + ImapNewLine));
            writer.Flush();
            ImapAnswer answer = ReadAnswer("+", false);
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
        public void LoginSSL(ConnectionString con)
        {
            LoginSSL(con.UserName, con.Password, con.Server, con.GetPort(993));
        }

        /// <summary>Does a Logon with SSL.</summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <param name="server">The server.</param>
        /// <param name="port">The port.</param>
        public void LoginSSL(string user, string password, string server, int port = 993)
        {
            SslClient sslClient = new SslClient();
            sslClient.Connect(server, port);
            sslClient.DoClientTLS(server);
            m_Stream = sslClient.Stream;
            Login(user, password);
        }

        /// <summary>Obtains a list of all present mailboxes.</summary>
        /// <returns></returns>
        public string[] ListMailboxes()
        {
            List<string> list = new List<string>();
            ImapAnswer answer = SendCommand("LIST \"\" *");
            foreach (string line in answer.GetDataLines())
            {
                if (line.StartsWith("* LIST "))
                {
                    string[] parts = ImapParser.SplitAnswer(line);
                    string mailbox = parts[4].UnboxText(false);
                    mailbox = UTF7.DecodeExtendedUTF7(mailbox);
                    list.Add(mailbox);
                }
            }
            return list.ToArray();
        }

        /// <summary>Creates a new mailbox.</summary>
        /// <param name="mailbox">The mailbox.</param>
        public void CreateMailbox(string mailbox)
        {
            SendCommand("CREATE \"{0}\"", mailbox);
        }

        /// <summary>Selects the mailbox with the specified name.</summary>
        /// <param name="mailboxName">Name of the mailbox.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public ImapMailboxInfo Select(string mailboxName)
        {
            string mailbox = UTF7.EncodeExtendedUTF7(mailboxName);
            ImapAnswer answer = SendCommand("SELECT \"{0}\"", mailbox);
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
            string mailbox = UTF7.EncodeExtendedUTF7(mailboxName);
            ImapAnswer answer = SendCommand("EXAMINE \"" + mailbox + "\"");
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

            ImapAnswer answer = SendCommand("FETCH " + number + " BODY[]");
            int start = Array.IndexOf(answer.Data, (byte)'\n') + 1;
            int end = Array.LastIndexOf(answer.Data, (byte)')');
            byte[] l_Message = new byte[end - start];
            Array.Copy(answer.Data, start, l_Message, 0, end - start);
            return l_Message;
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

            ImapAnswer answer = SendCommand("FETCH " + number + " BODY[]");
            int start = Array.IndexOf(answer.Data, (byte)'\n') + 1;
            int end = Array.LastIndexOf(answer.Data, (byte)')') - 2;
            byte[] l_Message = new byte[end - start];
            Array.Copy(answer.Data, start, l_Message, 0, end - start);
            return Rfc822Message.FromBinary(l_Message);
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
                ImapAnswer answer = SendCommand("FETCH " + number + " BODY[HEADER]");
                if (answer.Data.Length == 0)
                {
                    continue;
                }

                StreamReader streamReader = answer.GetStreamReader(0);
                string header = streamReader.ReadLine();
                int size = int.Parse(header.Substring(header.LastIndexOf('{')).Unbox("{", "}", true));
                DataReader dataReader = answer.GetDataReader(header.Length + 2);
                byte[] l_MessageData = dataReader.ReadBytes(size);
                if (l_MessageData.Length != size)
                {
                    throw new FormatException();
                }

                Rfc822Message l_Message = Rfc822Message.FromBinary(l_MessageData);
                return l_Message;
            }
        }

        /// <summary>Uploads a message to the specified mailbox.</summary>
        /// <param name="mailboxName">Name of the mailbox.</param>
        /// <param name="messageData">The message data.</param>
        /// <exception cref="System.ArgumentNullException">messageData.</exception>
        public void UploadMessageData(string mailboxName, byte[] messageData)
        {
            string mailbox = UTF7.EncodeExtendedUTF7(mailboxName);
            if (messageData == null)
            {
                throw new ArgumentNullException("messageData");
            }

            string id = PrepareLiteralDataCommand("APPEND \"" + mailbox + "\" (\\Seen) {" + messageData.Length + "}");
            DataWriter writer = new DataWriter(m_Stream);
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
            ImapAnswer answer = SendCommand("SEARCH {0}", search);
            if (!answer.Success)
            {
                throw new Exception(string.Format("Error at search {0}", search));
            }

            ImapNumberSequence sequence = new ImapNumberSequence();
            foreach (string line in answer.GetDataLines())
            {
                if (line.StartsWith("* SEARCH "))
                {
                    string s = line.Substring(9);
                    int value;
                    if (int.TryParse(s, out value))
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
            ImapAnswer answer = SendCommand("STORE {0} +FLAGS ({1})", number, string.Join(" ", flags));
            if (!answer.Success)
            {
                throw new Exception("Error at store flags");
            }
        }

        /// <summary>Expunges this instance.</summary>
        /// <exception cref="Exception">Error at store flags.</exception>
        public void Expunge()
        {
            ImapAnswer answer = SendCommand("EXPUNGE");
            if (!answer.Success)
            {
                throw new Exception("Error at store flags");
            }
        }

        /// <summary>Closes this instance.</summary>
        public void Close()
        {
            m_Stream?.Close();
            m_Client?.Close();
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
                if (m_Client is IDisposable dispo)
                {
                    dispo.Dispose();
                }

                m_Client = null;
                m_Stream?.Dispose();
                m_Stream = null; 
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
