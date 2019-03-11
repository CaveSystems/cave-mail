#region CopyRight 2018
/*
    Copyright (c) 2006-2018 Andreas Rohleder (andreas@rohleder.cc)
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

using System.Text;
using System.Collections.Generic;
using System.Collections;
using System;
using Cave.Collections;

namespace Cave.Mail.Imap
{
    /// <summary>
    /// Provides a class for imap message sequence numbers.
    /// </summary>
    public sealed class ImapNumberSequence : IEnumerable
    {
        /// <summary>
        /// Creates a <see cref="ImapNumberSequence"/> from the given string. The string is created by <see cref="ToString()"/> or received by <see cref="ImapClient.Search(ImapSearch)"/>.
        /// </summary>
        /// <param name="numbers"></param>
        /// <returns></returns>
        public static ImapNumberSequence FromString(string numbers)
        {
            numbers = numbers.UnboxBrackets(false);
            ImapNumberSequence result = new ImapNumberSequence();
            List<int> list = new List<int>();
            foreach (string str in numbers.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (str.IndexOf(':') > -1)
                {
                    string[] l_Parts = str.Split(':');
                    result += new ImapNumberSequence(Convert.ToInt32(l_Parts[0]), Convert.ToInt32(l_Parts[1]));
                    continue;
                }
                list.Add(Convert.ToInt32(str));
            }
            return result + new ImapNumberSequence(list.ToArray());
        }

        /// <summary>
        /// Creates a <see cref="ImapNumberSequence"/> from the given message number range.
        /// </summary>
        /// <param name="firstNumber"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static ImapNumberSequence CreateRange(int firstNumber, int count)
        {
            return new ImapNumberSequence(firstNumber, count + firstNumber - 1);
        }

        /// <summary>
        /// Creates a <see cref="ImapNumberSequence"/> from the given message number list.
        /// </summary>
        /// <param name="numbers"></param>
        /// <returns></returns>
        public static ImapNumberSequence CreateList(params int[] numbers)
        {
            return new ImapNumberSequence(numbers);
        }

        /// <summary>
        /// Adds to <see cref="ImapNumberSequence"/>s.
        /// </summary>
        /// <param name="seq1"></param>
        /// <param name="seq2"></param>
        /// <returns></returns>
        public static ImapNumberSequence operator +(ImapNumberSequence seq1, ImapNumberSequence seq2)
        {
            List<int> l_ResultList = new List<int>(seq1.Count + seq2.Count);

            if (seq1.IsRange)
            {
                for (int i = seq1.FirstNumber; i <= seq1.LastNumber; i++)
                {
                    l_ResultList.Add(i);
                }
            }
            else
            {
                l_ResultList.AddRange(seq1.m_Numbers);
            }
            if (seq2.IsRange)
            {
                for (int i = seq2.FirstNumber; i <= seq2.LastNumber; i++)
                {
                    if (l_ResultList.Contains(i))
                    {
                        continue;
                    }

                    l_ResultList.Add(i);
                }
            }
            else
            {
                foreach (int l_Number in seq2.m_Numbers)
                {
                    if (l_ResultList.Contains(l_Number))
                    {
                        continue;
                    }

                    l_ResultList.Add(l_Number);
                }
            }
            return new ImapNumberSequence(l_ResultList.ToArray());
        }

        bool m_IsRange;
        int[] m_Numbers;

        /// <summary>
        /// Creates a new empty <see cref="ImapNumberSequence"/>.
        /// </summary>
        public ImapNumberSequence()
        {
            m_Numbers = new int[0];
            m_IsRange = false;
        }

        /// <summary>
        /// Creates a new <see cref="ImapNumberSequence"/> with the given message numbers.
        /// </summary>
        /// <param name="p_Numbers"></param>
        public ImapNumberSequence(int[] p_Numbers)
        {
            m_IsRange = false;
            m_Numbers = p_Numbers;
        }

        /// <summary>
        /// Creates a new <see cref="ImapNumberSequence"/> with the given message number range.
        /// </summary>
        /// <param name="p_FirstNumber"></param>
        /// <param name="p_LastNumber"></param>
        public ImapNumberSequence(int p_FirstNumber, int p_LastNumber)
        {
            m_IsRange = true;
            m_Numbers = new int[] { p_FirstNumber, p_LastNumber };
        }

        /// <summary>
        /// Sorts the sequence numbers.
        /// </summary>
        public void Sort()
        {
            Array.Sort<int>(m_Numbers);
        }

        /// <summary>
        /// Provides the first number of the message list.
        /// </summary>
        public int FirstNumber { get { if (IsEmpty) { return -1; } return m_Numbers[0]; } }

        /// <summary>
        /// Provides the last number of the message list.
        /// </summary>
        public int LastNumber { get { if (IsEmpty) { return -1; } return m_Numbers[m_Numbers.Length - 1]; } }

        /// <summary>
        /// Returns true if the list does not contain single message numbers but a whole range of message numbers.
        /// </summary>
        public bool IsRange => m_IsRange;

        /// <summary>
        /// Returns true if the list is empty.
        /// </summary>
        public bool IsEmpty => m_Numbers.Length == 0;

        /// <summary>
        /// Obtains the number of items in the list.
        /// </summary>
        public int Count { get { if (IsRange) { return LastNumber - FirstNumber + 1; } return m_Numbers.Length; } }

        /// <summary>
        /// Obtains the string representing the <see cref="ImapNumberSequence"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (m_IsRange)
            {
                return FirstNumber + ":" + LastNumber;
            }
            else
            {
                StringBuilder strBuilder = new StringBuilder();
                strBuilder.Append(m_Numbers[0]);
                for (int i = 1; i < m_Numbers.Length; i++)
                {
                    strBuilder.Append(',');
                    strBuilder.Append(m_Numbers[i].ToString());
                }
                return strBuilder.ToString();
            }
        }

        /// <summary>Gibt einen Enumerator zurück, der eine Auflistung durchläuft.</summary>
        /// <returns>Ein <see cref="T:System.Collections.IEnumerator" />-Objekt, das zum Durchlaufen der Auflistung verwendet werden kann.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            if (IsEmpty)
            {
                return new int[0].GetEnumerator();
            }

            if (IsRange)
            {
                return new Counter(FirstNumber, LastNumber - FirstNumber + 1).GetEnumerator();
            }
            return m_Numbers.GetEnumerator();
        }
    }
}
