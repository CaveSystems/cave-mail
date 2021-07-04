using System;
using System.Text;

namespace Cave.Mail
{
    /// <summary>
    /// Provides UTF7 text en-/decoding.
    /// </summary>
    public static class UTF7
    {
        static string EncodeUTF7Chunk(string text)
        {
            var data = Encoding.BigEndianUnicode.GetBytes(text);
            return Base64.NoPadding.Encode(data);
        }

        static string DecodeUTF7Chunk(string code)
        {
            var data = Base64.NoPadding.Decode(code);
            return Encoding.BigEndianUnicode.GetString(data);
        }

        /// <summary>
        /// Provides extended UTF-7 decoding (rfc 3501).
        /// </summary>
        public static string DecodeExtendedUTF7(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (!text.Contains("&"))
            {
                return text;
            }

            var result = new StringBuilder();
            StringBuilder code = null;
            for (var i = 0; i < text.Length; i++)
            {
                if (code != null)
                {
                    if (text[i] == '-')
                    {
                        var decoded = DecodeUTF7Chunk(code.ToString());
                        result.Append(decoded);
                        code = null;
                    }
                    else
                    {
                        code.Append(text[i]);
                    }
                }
                else
                {
                    if (text[i] == '&')
                    {
                        if (text[++i] == '-')
                        {
                            result.Append('&');
                        }
                        else
                        {
                            code = new StringBuilder();
                            code.Append(text[i]);
                        }
                    }
                    else
                    {
                        result.Append(text[i]);
                    }
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Provides extended UTF-7 encoding (rfc 3501).
        /// </summary>
        public static string EncodeExtendedUTF7(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var result = new StringBuilder();
            StringBuilder code = null;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                var codeChar = ((c < 0x20) || (c > 0x25)) && ((c < 0x27) || (c > 0x7e));
                if (codeChar)
                {
                    if (c == '&')
                    {
                        if (code != null)
                        {
                            var chunk = EncodeUTF7Chunk(code.ToString());
                            result.Append("&" + chunk + "-&-");
                            code = null;
                        }
                        else
                        {
                            result.Append("&-");
                        }
                    }
                    else
                    {
                        if (code == null)
                        {
                            code = new StringBuilder();
                        }

                        code.Append(c);
                    }
                }
                else
                {
                    if (code != null)
                    {
                        var encoded = EncodeUTF7Chunk(code.ToString());
                        result.Append("&" + encoded + "-" + c);
                        code = null;
                    }
                    else
                    {
                        result.Append(c);
                    }
                }
            }
            if (code != null)
            {
                var encoded = EncodeUTF7Chunk(code.ToString());
                result.Append("&" + encoded + "-");
            }
            return result.ToString();
        }
    }
}
