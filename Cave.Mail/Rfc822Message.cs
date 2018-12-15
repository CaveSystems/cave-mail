using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Cave.IO;

namespace Cave.Mail
{
    /// <summary>
    /// Provides functions for rfc822 email parsing and processing.
    /// Multiple additions are made to accept malformed mail messages from providers like gmail (encoding errors, empty multiparts), gmx (bad headers), ...
    /// </summary>
    public class Rfc822Message
    {
        /// <summary>
        /// Provides a new boundary string for multipart messages (the current boundary can be obtained via <see cref="ContentType"/>)
        /// </summary>
        /// <returns></returns>
        public static string CreateBoundary()
        {
            return "_" + Rfc2047.GetRandomPrintableString(38) + "_";
        }

        /// <summary>
        /// Reads a <see cref="Rfc822Message"/> from the specified binary data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Rfc822Message FromBinary(byte[] data)
        {
            return new Rfc822Message(data);
        }

        /// <summary>
        /// Reads a <see cref="Rfc822Message"/> from the specified file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static Rfc822Message FromFile(string fileName)
        {
            return new Rfc822Message(File.ReadAllBytes(fileName));
        }

        int m_StartOfBody = 0;
        byte[] m_Body = null;
        NameValueCollection m_Header = new NameValueCollection();
        List<Rfc822Message> m_Parts = new List<Rfc822Message>();
        bool ro;

        /// <summary>
        /// Parses rfc822 data and fills the internal structures
        /// </summary>
        /// <param name="data"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "Cave.Net.Mail.Rfc822Content.Encode(System.Net.Mime.TransferEncoding,System.Text.Encoding,System.String)")]
        protected void m_Parse(byte[] data)
        {
            //reset
            m_Parts.Clear();
            m_Header.Clear();
            //create reader
            Rfc822Reader reader = new Rfc822Reader(data);
            //read header
            string line = Rfc2047.Decode(reader.ReadLine());
            //find first line
            while (string.IsNullOrEmpty(line))
            {
                line = Rfc2047.Decode(reader.ReadLine());
            }
            //read header
            while (!string.IsNullOrEmpty(line))
            {
                string header = line;
                line = reader.ReadLine();
                //add folded content to current line
                while ((line != null) && (line.StartsWith(" ") || line.StartsWith("\t")))
                {
                    header += " " + line.TrimStart(' ', '\t');
                    line = reader.ReadLine();
                }
                //split "key: value" pair
                int splitPos = header.IndexOf(':');
                if (splitPos < 0)
                {
                    break;
                }

                string headerKey = header.Substring(0, splitPos);
                splitPos += 2;
                string headerVal = (splitPos >= header.Length) ? "" : header.Substring(splitPos);
                try
                {
                    Rfc2047.Decode(headerVal);
                }
                catch (Exception ex)
                {
                    headerVal = Rfc2047.Encode(TransferEncoding.Base64, Encoding, headerVal);
                    Trace.WriteLine($"Invalid header {headerKey}\n{ex}");
                }
                //add "key: value" pair to header
                m_Header.Add(headerKey, headerVal);
            }
            //get body start position
            m_StartOfBody = reader.Position;
            //multipart message ?
            if (!IsMultipart)
            {
                //no, single part
                m_Body = reader.ReadToEndData();
            }
            else
            {
                //yes, multipart, get boundary
                MultiPartBoundary = "--" + ContentType.Boundary;
                //load body content without multiparts
                int endOfPart = reader.Position;
                line = reader.ReadLine();
                while (line != null)
                {
                    if (line.StartsWith(MultiPartBoundary))
                    {
                        break;
                    }

                    endOfPart = reader.Position;
                    line = reader.ReadLine();
                }
                //decode part
                m_Body = reader.Extract(m_StartOfBody, endOfPart - m_StartOfBody);

                //index start of current read part
                int startOfPart = reader.Position;
                //current read position (needed because multipart boundary does not belong to part content)
                int currentPosition = startOfPart;
                //load parts
                line = reader.ReadLine();

                NameValueCollection contentHeader = new NameValueCollection(m_Header);
                bool inHeader = true;
                while (line != null)
                {
                    if (line == "." || line == "")
                    {
                        inHeader = false;
                    }
                    else if (inHeader)
                    {
                        string[] parts = line.Split(new char[] { ':' }, 2);
                        if (parts.Length != 2)
                        {
                            inHeader = false;
                        }
                        else
                        {
                            contentHeader[parts[0]] = parts[1].TrimStart();
                        }
                    }
                    //part boundary detected ?
                    if (line.StartsWith(MultiPartBoundary))
                    {
                        //yes, get data of part as new buffer
                        byte[] buffer = reader.Extract(startOfPart, currentPosition - startOfPart);
                        //decode part
                        m_Parts.Add(new Rfc822Message(contentHeader, buffer));
                        //set next start position
                        startOfPart = reader.Position;
                        contentHeader = new NameValueCollection(m_Header);
                        inHeader = true;
                    }
                    currentPosition = reader.Position;
                    line = reader.ReadLine();
                }
                //unclean message ending ? (missing "--\n" at end of file ?)
                if (currentPosition > startOfPart + 3)
                {
                    //yes, get data of part as new buffer
                    byte[] buffer = reader.Extract(startOfPart, currentPosition - startOfPart);
                    //decode part
                    m_Parts.Add(new Rfc822Message(contentHeader, buffer));
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "Cave.IO.DataWriter.WriteLine(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "Cave.IO.DataWriter.WriteString(System.String,System.Boolean)")]
        void m_Save(DataWriter writer)
        {
            //write header
            foreach (string key in m_Header.Keys)
            {
                string[] values = GetHeaders(key);
                foreach (string value in values)
                {
                    writer.Write(key);
                    writer.Write(": ");
                    if (ASCII.IsClean(value))
                    {
                        writer.WriteLine(value);
                    }
                    else
                    {
                        string str = Rfc2047.Encode(TransferEncoding.QuotedPrintable, Encoding.UTF8, value);
                        writer.WriteLine(str);
                    }
                }
            }
            if (!IsMultipart)
            {
                writer.WriteLine();
                writer.Write(m_Body);
            }
            else
            {
                //write main body
                writer.Write(m_Body);
                //write additional parts
                string boundary = "--" + ContentType.Boundary;
                foreach (Rfc822Message part in Multipart)
                {
                    writer.WriteLine();
                    writer.WriteLine(boundary);
                    part.m_Save(writer);
                }
                writer.WriteLine("--");
            }
        }

        /// <summary>
        /// Saves a Rfc822 message to a file
        /// </summary>
        /// <param name="fileName"></param>
        public void Save(string fileName)
        {
            using (Stream stream = File.Create(fileName))
            {
                DataWriter writer = new DataWriter(stream);
                m_Save(writer);
                writer.Close();
            }
        }

        private Rfc822Message(byte[] data)
        {
            m_Parse(data);
        }

        private Rfc822Message(NameValueCollection header, byte[] data)
        {
            m_Parse(data);
            m_Header = header;
            ro = true;
        }

        /// <summary>
        /// Obtains the <see cref="System.Text.Encoding"/> used
        /// </summary>
        public Encoding Encoding
        {
            get
            {
                try
                {
                    string charSet = ContentType.CharSet;
                    if (string.IsNullOrEmpty(charSet))
                    {
                        return Encoding.GetEncoding("iso-8859-1");
                    }

                    return Encoding.GetEncoding(charSet.UnboxText(false));
                }
                catch
                {
                    return Encoding.GetEncoding("iso-8859-1");
                }
            }
        }

        /// <summary>
        /// Retrieve the first header field with the specified name
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetFirstHeader(string key)
        {
            string result = m_Header[key];
            if (result == null)
            {
                return "";
            }

            return result;
        }

        /// <summary>
        /// Retrieve all header fields with the specified name
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string[] GetHeaders(string key)
        {
            return m_Header.GetValues(key);
        }

        /// <summary>
        /// Obtains a copy of all header lines
        /// </summary>
        public string[] Header
        {
            get
            {
                string[] result = new string[m_Header.Count];
                int i = 0;
                foreach (string key in m_Header.Keys)
                {
                    result[i++] = key + ": " + m_Header[key];
                }
                return result;
            }
        }

        /// <summary>
        /// Gets / sets the 'From:' Header field
        /// </summary>
        public MailAddress From
        {
            get => Rfc2047.DecodeMailAddress(GetFirstHeader("From"));
            set
            {
                if (ro)
                {
                    throw new ReadOnlyException();
                }

                m_Header["From"] = Rfc2047.EncodeMailAddress(TransferEncoding.QuotedPrintable, Encoding, value);
            }
        }

        /// <summary>
        /// Gets / sets the 'Delivered-To:' Header field
        /// </summary>
        public MailAddress DeliveredTo
        {
            get => Rfc2047.DecodeMailAddress(GetFirstHeader("Delivered-To"));
            set
            {
                if (ro)
                {
                    throw new ReadOnlyException();
                }

                m_Header["Delivered-To"] = Rfc2047.EncodeMailAddress(TransferEncoding.QuotedPrintable, Encoding, value);
            }
        }

        /// <summary>
        /// Gets / sets the 'Message-ID:' Header field
        /// </summary>
        public MailAddress MessageID
        {
            get => Rfc2047.DecodeMailAddress(GetFirstHeader("Message-ID"));
            set
            {
                if (ro)
                {
                    throw new ReadOnlyException();
                }

                m_Header["Message-ID"] = Rfc2047.EncodeMailAddress(TransferEncoding.QuotedPrintable, Encoding, value);
            }
        }

        /// <summary>
        /// Gets / sets the 'Return-Path:' Header field
        /// </summary>
        public MailAddress ReturnPath
        {
            get => Rfc2047.DecodeMailAddress(GetFirstHeader("Return-Path"));
            set
            {
                if (ro)
                {
                    throw new ReadOnlyException();
                }

                m_Header["Return-Path"] = Rfc2047.EncodeMailAddress(TransferEncoding.QuotedPrintable, Encoding, value);
            }
        }

        /// <summary>
        /// Gets / sets the 'MIME-Version:' Header field
        /// </summary>
        public Version MimeVersion
        {
            get { try { return new Version(GetFirstHeader("MIME-Version")); } catch { return new Version("1.0"); } }
            set
            {
                if (ro)
                {
                    throw new ReadOnlyException();
                }

                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                m_Header["MIME-Version"] = value.Major.ToString() + "." + value.Minor.ToString();
            }
        }

        /// <summary>
        /// Gets / sets the 'Subject:' Header field
        /// </summary>
        public string Subject
        {
            get => Rfc2047.Decode(GetFirstHeader("Subject"));
            set
            {
                if (ro)
                {
                    throw new ReadOnlyException();
                }

                m_Header["Subject"] = Rfc2047.Encode(TransferEncoding.QuotedPrintable, Encoding, value);
            }
        }

        /// <summary>
        /// Accesses the 'To:' Header field
        /// </summary>
        public Rfc822AddressCollection To { get { try { return new Rfc822AddressCollection("To", m_Header, Encoding); } catch { return null; } } }

        /// <summary>
        /// Accesses the 'Cc:' Header field (CarbonCopy)
        /// </summary>
        public Rfc822AddressCollection Cc => new Rfc822AddressCollection("Cc", m_Header, Encoding);

        /// <summary>
        /// Accesses the 'Bcc:' Header field (BlindCarbonCopy)
        /// </summary>
        public Rfc822AddressCollection Bcc => new Rfc822AddressCollection("Bcc", m_Header, Encoding);

        /// <summary>
        /// Accesses the 'Date:' Header field
        /// </summary>
        public DateTime Date
        {
            get => Rfc822DateTime.Decode(GetFirstHeader("Date"));
            set
            {
                if (ro)
                {
                    throw new ReadOnlyException();
                }

                m_Header["Date"] = Rfc822DateTime.Encode(value);
            }
        }

        /// <summary>
        /// Obtains the <see cref="ContentType"/>
        /// </summary>
        public ContentType ContentType
        {
            get
            {
                ContentType contentType = new ContentType
                {
                    MediaType = "text/plain",
                    CharSet = "iso-8859-1",
                    Name = ""
                };
                string contentTypeString = m_Header["Content-Type"];
                if (contentTypeString == null)
                {
                    return contentType;
                }

                try
                {
                    string[] parts = contentTypeString.Split(';');
                    try { contentType.MediaType = parts[0].Trim().UnboxText(false).ToLower().Replace(" ", ""); }
                    catch { }
                    foreach (string part in parts)
                    {
                        string name = part.Trim().ToLower();
                        string value = "";
                        int index = part.IndexOf('=');
                        if (index < 0)
                        {
                            continue;
                        }

                        value = part.Substring(index + 1).Trim().UnboxText(false);
                        name = part.Substring(0, index).Trim().ToLower();
                        switch (name)
                        {
                            case "charset": contentType.CharSet = value; break;
                            case "boundary": contentType.Boundary = value; break;
                            case "name": contentType.Name = value; break;
                        }
                    }
                }
                catch
                {
                    //malformed content type
                }
                return contentType;
            }
        }

        /// <summary>
        /// Obtains the <see cref="TransferEncoding"/>
        /// </summary>
        public TransferEncoding TransferEncoding
        {
            get
            {
                //load transfer encoding
                string l_TransferEncoding = m_Header["Content-Transfer-Encoding"];
                if (l_TransferEncoding == null)
                {
                    return TransferEncoding.Unknown;
                }

                switch (l_TransferEncoding.ToUpperInvariant())
                {
                    case "QUOTED-PRINTABLE": return TransferEncoding.QuotedPrintable;
                    case "BASE64": return TransferEncoding.Base64;
                    case "7BIT": return TransferEncoding.SevenBit;
                    default: return TransferEncoding.Unknown;
                }
            }
        }

        /// <summary>
        /// Obtains whether the message looks valid or not
        /// </summary>
        public bool IsValid => (m_Header["From"] != null) && (m_Header["To"] != null) && (m_Header["Subject"] != null) && HasPlainTextPart;

        /// <summary>
        /// Gets / sets the content of the message
        /// </summary>
        public string Content
        {
            get { try { return Rfc2047.DecodeText(TransferEncoding, Encoding, m_Body); } catch { return Encoding.GetString(m_Body); } }
            set
            {
                if (ro)
                {
                    throw new ReadOnlyException();
                }

                m_Body = Rfc2047.EncodeText(TransferEncoding, Encoding, value);
            }
        }

        /// <summary>
        /// Obtains whether the message is multipart or not
        /// </summary>
        public bool IsMultipart
        {
            get
            {
                try { return !string.IsNullOrEmpty(ContentType.Boundary); }
                catch { return false; }
            }
        }

        /// <summary>
        /// Obtains whether the message contains at least one plain text part
        /// </summary>
        public bool HasPlainTextPart => HasPart("text/plain");

        /// <summary>
        /// Obtains whether the message contains at least one part with the specified media type
        /// </summary>
        public bool HasPart(string mediaType)
        {
            if (IsMultipart)
            {
                return Multipart.HasPart(mediaType);
            }

            return string.Equals(ContentType.MediaType, mediaType);
        }

        /// <summary>
        /// Obtains the first part with the specified media type found in the message.
        /// This can only be accessed after checking <see cref="HasPart"/>
        /// </summary>
        /// <returns></returns>
        public Rfc822Message GetPart(string mediaType)
        {
            if (IsMultipart)
            {
                if (HasPart(mediaType))
                {
                    return Multipart.GetPart(mediaType);
                }
            }
            else if (string.Equals(ContentType.MediaType, mediaType))
            {
                return this;
            }

            throw new ArgumentException(string.Format("Message part {0} cannot be found!", mediaType));
        }

        /// <summary>
        /// Obtains the first plain text part found in the message. This can only be accessed after checking <see cref="HasPlainTextPart"/>
        /// </summary>
        /// <returns></returns>
        public Rfc822Message GetPlainTextPart()
        {
            return GetPart("text/plain");
        }

        /// <summary>
        /// Obtains all parts of the message. This can only be accessed after checking <see cref="IsMultipart"/>
        /// </summary>
        public Rfc822MessageMultipart Multipart => new Rfc822MessageMultipart(m_Parts);

        /// <summary>Gets the name of the log source.</summary>
        /// <value>The name of the log source.</value>
        public string LogSourceName => "Rfc822Message";

        /// <summary>Gets the multi part boundary.</summary>
        /// <value>The multi part boundary.</value>
        public string MultiPartBoundary { get; private set; }

        /// <summary>
        /// Obtains the ContentType of the Message as string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Subject.ToString();
        }

        /// <summary>
        /// Obtains the hash code for the body of the message
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return m_Body.GetHashCode();
        }
    }
}
