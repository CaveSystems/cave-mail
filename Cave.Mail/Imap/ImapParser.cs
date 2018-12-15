using System;
using System.Collections.Generic;
using System.IO;
using Cave.IO;

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

                    case ')': if (l_Stack.Pop() != '(') { throw new FormatException(); } break;
                    case '}': if (l_Stack.Pop() != '{') { throw new FormatException(); } break;
                    case ']': if (l_Stack.Pop() != '[') { throw new FormatException(); } break;
                    case '>': if (l_Stack.Pop() != '<') { throw new FormatException(); } break;

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
            ImapAnswer answer = new ImapAnswer
            {
                ID = id
            };

            FifoStream m_Buffer = new FifoStream();
            List<byte> current = new List<byte>(80);
            while (true)
            {
                int b = stream.ReadByte();
                if (b < 0)
                {
                    throw new EndOfStreamException();
                }

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
