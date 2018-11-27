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
using System.Text;

namespace Cave.Mail
{
    /// <summary>
    /// Provides UTF7 text en-/decoding
    /// </summary>
    public static class UTF7
    {
        static string EncodeUTF7Chunk(string text)
        {
            byte[] data = Encoding.BigEndianUnicode.GetBytes(text);
            return Base64.NoPadding.Encode(data);
        }

        static string DecodeUTF7Chunk(string code)
        {
            byte[] data = Base64.NoPadding.Decode(code);
            return Encoding.BigEndianUnicode.GetString(data);
        }

        /// <summary>
        /// Provides extended UTF-7 decoding (rfc 3501)
        /// </summary>
        public static string DecodeExtendedUTF7(string text)
        {
            if (text == null) throw new ArgumentNullException("text");
            if (!text.Contains("&")) return text;
            StringBuilder result = new StringBuilder();
            StringBuilder code = null;
            for (int i = 0; i < text.Length; i++)
            {
                if (code != null)
                {
                    if (text[i] == '-')
                    {
                        string decoded = DecodeUTF7Chunk(code.ToString());
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
        /// Provides extended UTF-7 encoding (rfc 3501)
        /// </summary>
        public static string EncodeExtendedUTF7(string text)
        {
            if (text == null) throw new ArgumentNullException("text");
            StringBuilder result = new StringBuilder();
            StringBuilder code = null;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                bool codeChar = ((c < 0x20) || (c > 0x25)) && ((c < 0x27) || (c > 0x7e));
                if (codeChar)
                {
                    if (c == '&')
                    {
                        if (code != null)
                        {
                            string l_Text = EncodeUTF7Chunk(code.ToString());
                            result.Append("&" + l_Text + "-&-");
                            code = null;
                        }
                        else
                        {
                            result.Append("&-");
                        }
                    }
                    else
                    {
                        if (code == null) code = new StringBuilder();
                        code.Append(c);
                    }
                }
                else
                {
                    if (code != null)
                    {
                        string encoded = EncodeUTF7Chunk(code.ToString());
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
                string encoded = EncodeUTF7Chunk(code.ToString());
                result.Append("&" + encoded + "-");
                code = null;
            }
            return result.ToString();
        }
    }
}
