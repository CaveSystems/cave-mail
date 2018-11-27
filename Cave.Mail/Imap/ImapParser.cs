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

 using Cave.IO;
using Cave.Text;
using System;
using System.Collections.Generic;
using System.IO;

namespace Cave.Mail.Imap
{
    static class ImapParser
    {
        public static string[] SplitAnswer(string answer)
        {
            List<string> parts = new List<string>();
            Stack<char> l_Stack = new Stack<char>();
            int start = 0;
            for (int i = 0; i < answer.Length; i++)
            {
                char c = answer[i];
                switch (c)
                {
                    case '\'':
                    case '"':
                        if ((l_Stack.Count > 0) && (l_Stack.Peek() == c))
                        {
                            l_Stack.Pop();
                        }
                        else
                        {
                            l_Stack.Push(c);
                        }
                        break;

                    case ')': if (l_Stack.Pop() != '(') throw new FormatException(); break;
                    case '}': if (l_Stack.Pop() != '{') throw new FormatException(); break;
                    case ']': if (l_Stack.Pop() != '[') throw new FormatException(); break;
                    case '>': if (l_Stack.Pop() != '<') throw new FormatException(); break;

                    case '(':
                    case '{':
                    case '[':
                    case '<':
                        l_Stack.Push(c);
                        break;
                    case ' ':
                        if (l_Stack.Count == 0)
                        {
                            parts.Add(answer.Substring(start, i - start));
                            start = i + 1;
                            continue;
                        }
                        break;
                }
            }
            if (start < answer.Length)
            {
                parts.Add(answer.Substring(start));
            }
            return parts.ToArray();
        }

        public static ImapAnswer Parse(string id, Stream stream)
        {
            ImapAnswer answer = new ImapAnswer();
            answer.ID = id;

            FifoStream m_Buffer = new FifoStream();
            List<byte> current = new List<byte>(80);
            while (true)
            {
                int b = stream.ReadByte();
                if (b < 0) throw new EndOfStreamException();
                current.Add((byte)b);
                if (b == '\n')
                {
                    byte[] line = current.ToArray();
                    string str = ASCII.GetString(line);
                    if (str.StartsWith(answer.ID + " "))
                    {
                        answer.Result = str;
                        break;
                    }
                    m_Buffer.AppendBuffer(line, 0, line.Length);
                    current.Clear();
                }
            }

            answer.Data = m_Buffer.ToArray();
            return answer;
        }
    }
}
