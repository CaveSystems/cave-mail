using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Cave.Net;

namespace Cave.Mail
{
    /// <summary>
    /// Provides en-/decoding routines for rfc2047 encoded strings
    /// </summary>
    public static class Rfc2047
    {
        static Rfc2047() { DefaultEncoding = Encoding.GetEncoding(1252); }

        /// <summary>
        /// Provides the default encoding of email content / header lines in quoted printable format
        /// </summary>
        public static Encoding DefaultEncoding { get; private set; }

        /// <summary>
        /// Obtains a random printable string of the specified length
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public static string GetRandomPrintableString(int count)
        {
            Random l_Random = new Random();
            char[] l_Buffer = new char[count];
            for (int i = 0; i < count; i++)
            {
                l_Buffer[i] = (char)l_Random.Next(33, 127);
            }

            return new string(l_Buffer);
        }

        static int m_IndexOfEndMark(string text, int start)
        {
            if (start < 0)
            {
                return -1;
            }

            int l_Markers = 4;
            for (int i = start; i < text.Length; i++)
            {
                if (text[i] == '?')
                {
                    if (--l_Markers == 0)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Decodes quoted printable 7bit ascii data to the specified <see cref="Encoding"/>
        /// </summary>
        /// <param name="encoding">The binary encoding to use</param>
        /// <param name="data">TransferEncoded ascii data</param>
        /// <returns></returns>
        public static string DecodeQuotedPrintable(Encoding encoding, byte[] data)
        {
            return DecodeQuotedPrintable(encoding, ASCII.GetString(data));
        }

        /// <summary>
        /// Decodes quoted printable 7bit ascii data to the specified <see cref="Encoding"/>
        /// </summary>
        /// <param name="encoding">The binary encoding to use</param>
        /// <param name="data">TransferEncoded ascii data</param>
        /// <returns></returns>
        public static string DecodeQuotedPrintable(Encoding encoding, string data)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            List<byte> l_Data = new List<byte>(data.Length);
            for (int i = 0; i < data.Length; i++)
            {
                int l_Byte = data[i];

                if (l_Byte > 127)
                {
                    throw new FormatException(string.Format("Invalid input character at position '{0}' encountered!", i));
                }
                //got '_' == encoded space ?
                if (l_Byte == '_')
                {
                    l_Data.AddRange(encoding.GetBytes(" "));
                    continue;
                }
                //got '=' == start of 2 digit hex value
                if (l_Byte == '=')
                {
                    i += 1;
                    //got line extension ?
                    if (data[i] == '\r')
                    {
                        if (data[i + 1] == '\n')
                        {
                            i++;
                        }

                        continue;
                    }
                    if (data[i] == '\n')
                    {
                        continue;
                    }
                    //no line extension, decode hex value
                    string l_HexValue = data.Substring(i, 2);
                    int l_Value = Convert.ToInt32(l_HexValue, 16);
                    l_Data.Add((byte)l_Value);
                    i += 1;
                    continue;
                }
                l_Data.AddRange(encoding.GetBytes(((char)l_Byte).ToString()));
            }
            return encoding.GetString(l_Data.ToArray());
        }

        /// <summary>
        /// Encodes a text to quoted printable 7bit ascii data. The data is not split into parts of the correct length. The caller has to do this manually by inserting '=' + LF at the approprioate positions.
        /// </summary>
        /// <param name="encoding">The binary encoding to use</param>
        /// <param name="text">The text to encode</param>
        /// <returns></returns>
        public static byte[] EncodeQuotedPrintable(Encoding encoding, string text)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            List<byte> result = new List<byte>();
            foreach (byte l_Byte in encoding.GetBytes(text))
            {
                if ((l_Byte > 32) && (l_Byte < 128))
                {
                    result.Add(l_Byte);
                    continue;
                }
                switch (l_Byte)
                {
                    case (byte)' ':
                        result.Add((byte)'_');
                        continue;
                    case (byte)'_':
                    case (byte)'=':
                    case (byte)'?':
                    default:
                        result.Add((byte)'=');
                        string l_Hex = l_Byte.ToString("X2");
                        result.Add((byte)l_Hex[0]);
                        result.Add((byte)l_Hex[1]);
                        continue;
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Obtains whether the specified string is rfc2047 encoded
        /// </summary>
        /// <param name="data">TransferEncoded ascii data</param>
        /// <returns></returns>
        public static bool IsEncodedString(string data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            return (!data.Contains(" ") && data.StartsWith("=?") && data.EndsWith("?="));
        }

        /// <summary>
        /// Decodes ascii 7bit data using the specified <see cref="TransferEncoding"/>
        /// </summary>
        /// <param name="transferEncoding">The transfer encoding to use</param>
        /// <param name="encoding">The binary encoding to use</param>
        /// <param name="data">TransferEncoded ascii data</param>
        /// <returns></returns>
        public static string DecodeText(TransferEncoding transferEncoding, Encoding encoding, string data)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            switch (transferEncoding)
            {
                case TransferEncoding.Base64:
                    //convert: byte array -> base64 ascii string -> raw data -> string
                    return encoding.GetString(Convert.FromBase64String(data.RemoveNewLine()));
                case TransferEncoding.QuotedPrintable:
                    return DecodeQuotedPrintable(encoding, data);
                case TransferEncoding.SevenBit:
                    return encoding.GetString(ASCII.GetBytes(data));
                default:
                    throw new InvalidDataException(string.Format("The specified encoding '{0}' is not valid for text parts!", transferEncoding.ToString()));
            }
        }

        /// <summary>
        /// Decodes ascii 7bit data using the specified <see cref="TransferEncoding"/>
        /// </summary>
        /// <param name="transferEncoding">The transfer encoding to use</param>
        /// <param name="encoding">The binary encoding to use</param>
        /// <param name="data">TransferEncoded ascii data</param>
        /// <returns></returns>
        public static string DecodeText(TransferEncoding transferEncoding, Encoding encoding, byte[] data)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            if (transferEncoding == TransferEncoding.Unknown)
            {
                return encoding.GetString(data);
            }

            return DecodeText(transferEncoding, encoding, ASCII.GetString(data));
        }

        /// <summary>
        /// Encodes text without start and end marks
        /// </summary>
        /// <param name="transferEncoding">The transfer encoding to use</param>
        /// <param name="encoding">The binary encoding to use</param>
        /// <param name="text">The text to encode</param>
        /// <returns></returns>
        public static byte[] EncodeText(TransferEncoding transferEncoding, Encoding encoding, string text)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            switch (transferEncoding)
            {
                case TransferEncoding.SevenBit:
                    return ASCII.GetBytes(text);
                case TransferEncoding.Base64:
                    byte[] l_Bytes = encoding.GetBytes(text);
                    return ASCII.GetBytes(Base64.NoPadding.Encode(l_Bytes));
                case TransferEncoding.QuotedPrintable:
                    return EncodeQuotedPrintable(encoding, text);
                default:
                    throw new InvalidDataException(string.Format("The specified encoding '{0}' is not valid for text!", transferEncoding.ToString()));
            }
        }

        /// <summary>
        /// Decodes a rfc2047 string with correct start and end marks. If a header line needs to be decoded can be tested with <see cref="IsEncodedString"/>.
        /// </summary>
        /// <param name="data">TransferEncoded ascii data</param>
        /// <returns></returns>
        static string m_Decode(string data)
        {
            if (IsEncodedString(data))
            {
                string l_Data = data.Substring(2, data.Length - 4);
                string[] l_Parts = new string[3];
                int l_Part = 0;
                int l_Start = 0;
                for (int i = 0; i < l_Data.Length; i++)
                {
                    if (l_Data[i] == '?')
                    {
                        l_Parts[l_Part++] = l_Data.Substring(l_Start, i - l_Start);
                        l_Start = ++i;
                        if (l_Part == 2)
                        {
                            break;
                        }
                    }
                }
                l_Parts[2] = l_Data.Substring(l_Start, l_Data.Length - l_Start);
                //load default encoding used as fallback
                Encoding l_Encoding = Encoding.GetEncoding(1252);
                //try to get encoding by webname, many non standard email services use whatever they want as encoding string
                try { l_Encoding = Encoding.GetEncoding(l_Parts[0].UnboxText(false)); }
                catch
                { //.. so we get in one of 10 emails no valid encoding here. maybe they used the codepage prefixed or suffixed with a string:
                    try { l_Encoding = Encoding.GetEncoding(int.Parse(l_Parts[0].GetValidChars(ASCII.Strings.Digits))); }
                    catch { /* no they didn't, so we use the default and hope the best... */ }
                }
                try
                {
                    switch (l_Parts[1].ToUpperInvariant())
                    {
                        case "B":
                            return l_Encoding.GetString(Convert.FromBase64String(l_Parts[2]));
                        case "Q":
                            return DecodeQuotedPrintable(l_Encoding, l_Parts[2]);
                        default:
                            throw new InvalidDataException(string.Format("Unknown encoding '{0}'!", l_Parts[1]));
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException(string.Format("Invalid data encountered!"), ex);
                }
            }
            else
            {
                return data;
            }
        }

        /// <summary>
        /// Decodes a rfc2047 string with correct start and end marks. If a header line needs to be decoded can be tested with <see cref="IsEncodedString"/>.
        /// </summary>
        /// <param name="data">TransferEncoded ascii data</param>
        /// <returns></returns>
        public static string Decode(byte[] data)
        {
            return Decode(ASCII.GetString(data));
        }

        /// <summary>
        /// Decodes multiple <see cref="MailAddress"/>es
        /// </summary>
        /// <param name="data">TransferEncoded ascii data</param>
        /// <returns></returns>
        public static string Decode(string data)
        {
            if (data == null)
            {
                return null;
            }

            int l_Start = data.IndexOf("=?");
            int l_End = m_IndexOfEndMark(data, l_Start);
            if (l_Start >= l_End)
            {
                return data;
            }

            StringBuilder result = new StringBuilder(data.Length);
            int pos = 0;
            int size;
            while ((l_Start > -1) && (l_Start < l_End))
            {
                //copy text without decoding ?
                size = l_Start - pos;
                if (size > 0)
                {
                    result.Append(data.Substring(pos, size));
                }
                //decode
                size = l_End - l_Start + 2;
                result.Append(m_Decode(data.Substring(l_Start, size)));
                pos = l_End + 2;
                l_Start = data.IndexOf("=?", pos);
                l_End = m_IndexOfEndMark(data, l_Start);
            }
            size = data.Length - pos;
            if (size > 0)
            {
                result.Append(data.Substring(pos, size));
            }

            return result.ToString();
        }

        /// <summary>
        /// Encodes a string to a valid rfc2047 string with the specified <see cref="TransferEncoding"/>
        /// </summary>
        /// <param name="transferEncoding">The transfer encoding to use</param>
        /// <param name="encoding">The binary encoding to use</param>
        /// <param name="text">The string to encode</param>
        /// <returns></returns>
        public static string Encode(TransferEncoding transferEncoding, Encoding encoding, string text)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }
            //TODO BREAK LINE AFTER 76 CHARS, NEXT LINE STARTS WITH \t
            switch (transferEncoding)
            {
                case TransferEncoding.QuotedPrintable:
                    return "=?" + encoding.WebName + "?Q?" + ASCII.GetString(EncodeQuotedPrintable(encoding, text)) + "?=";
                case TransferEncoding.Base64:
                    return "=?" + encoding.WebName + "?B?" + Base64.NoPadding.Encode(encoding.GetBytes(text)) + "?=";
                default:
                    throw new InvalidDataException(string.Format("The specified encoding '{0}' is not valid for text!", transferEncoding.ToString()));
            }
        }

        /// <summary>
        /// Decodes a <see cref="MailAddress"/>
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static MailAddress DecodeMailAddress(string data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            int l_Index = data.LastIndexOf('@');
            bool l_Recreate = (l_Index == -1) || (l_Index != data.IndexOf('@'));
            if (!l_Recreate)
            {
                try { return new MailAddress(Decode(data)); }
                catch { }
            }
            string l_CleanString = data.ReplaceInvalidChars(ASCII.Strings.SafeName, " ");
            return new MailAddress(GetRandomPrintableString(20) + "@" + NetTools.HostName, l_CleanString);
        }

        /// <summary>
        /// Encodes a <see cref="MailAddress"/>
        /// </summary>
        /// <param name="transferEncoding">The transfer encoding to use</param>
        /// <param name="encoding">The binary encoding to use</param>
        /// <param name="address">The address to encode</param>
        /// <returns></returns>
        public static string EncodeMailAddress(TransferEncoding transferEncoding, Encoding encoding, MailAddress address)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (string.IsNullOrEmpty(address.DisplayName))
            {
                return address.Address;
            }
            return "\"" + Encode(transferEncoding, encoding, address.DisplayName) + "\" <" + address.Address + ">";
        }
    }
}
