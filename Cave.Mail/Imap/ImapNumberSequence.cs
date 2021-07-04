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
        static readonly int[] Empty =
#if NET20 || NET35 || NET45
            new int[0];
#else
            Array.Empty<int>();
#endif

        /// <summary>
        /// Creates a <see cref="ImapNumberSequence"/> from the given string. The string is created by <see cref="ToString()"/> or received by <see cref="ImapClient.Search(ImapSearch)"/>.
        /// </summary>
        /// <param name="numbers"></param>
        /// <returns></returns>
        public static ImapNumberSequence FromString(string numbers)
        {
            numbers = numbers.UnboxBrackets(false);
            var result = new ImapNumberSequence();
            var list = new List<int>();
            foreach (var str in numbers.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (str.IndexOf(':') > -1)
                {
                    var parts = str.Split(':');
                    result += new ImapNumberSequence(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]));
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
        public static ImapNumberSequence CreateRange(int firstNumber, int count) => new(firstNumber, count + firstNumber - 1);

        /// <summary>
        /// Creates a <see cref="ImapNumberSequence"/> from the given message number list.
        /// </summary>
        /// <param name="numbers"></param>
        /// <returns></returns>
        public static ImapNumberSequence CreateList(params int[] numbers) => new(numbers);

        /// <summary>
        /// Adds to <see cref="ImapNumberSequence"/>s.
        /// </summary>
        /// <param name="seq1"></param>
        /// <param name="seq2"></param>
        /// <returns></returns>
        public static ImapNumberSequence operator +(ImapNumberSequence seq1, ImapNumberSequence seq2)
        {
            var resultList = new List<int>(seq1.Count + seq2.Count);

            if (seq1.IsRange)
            {
                for (var i = seq1.FirstNumber; i <= seq1.LastNumber; i++)
                {
                    resultList.Add(i);
                }
            }
            else
            {
                resultList.AddRange(seq1.Numbers);
            }
            if (seq2.IsRange)
            {
                for (var i = seq2.FirstNumber; i <= seq2.LastNumber; i++)
                {
                    if (resultList.Contains(i))
                    {
                        continue;
                    }

                    resultList.Add(i);
                }
            }
            else
            {
                foreach (var number in seq2.Numbers)
                {
                    if (resultList.Contains(number))
                    {
                        continue;
                    }

                    resultList.Add(number);
                }
            }
            return new ImapNumberSequence(resultList.ToArray());
        }

        readonly int[] Numbers;

        /// <summary>
        /// Creates a new empty <see cref="ImapNumberSequence"/>.
        /// </summary>
        public ImapNumberSequence()
        {
            Numbers = Empty;
            IsRange = false;
        }

        /// <summary>
        /// Creates a new <see cref="ImapNumberSequence"/> with the given message numbers.
        /// </summary>
        /// <param name="numbers"></param>
        public ImapNumberSequence(int[] numbers)
        {
            IsRange = false;
            Numbers = numbers;
        }

        /// <summary>
        /// Creates a new <see cref="ImapNumberSequence"/> with the given message number range.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="last"></param>
        public ImapNumberSequence(int first, int last)
        {
            IsRange = true;
            Numbers = new int[] { first, last };
        }

        /// <summary>
        /// Sorts the sequence numbers.
        /// </summary>
        public void Sort() => Array.Sort(Numbers);

        /// <summary>
        /// Provides the first number of the message list.
        /// </summary>
        public int FirstNumber { get { if (IsEmpty) { return -1; } return Numbers[0]; } }

        /// <summary>
        /// Provides the last number of the message list.
        /// </summary>
        public int LastNumber { get { if (IsEmpty) { return -1; } return Numbers[Numbers.Length - 1]; } }

        /// <summary>
        /// Returns true if the list does not contain single message numbers but a whole range of message numbers.
        /// </summary>
        public bool IsRange { get; private set; }

        /// <summary>
        /// Returns true if the list is empty.
        /// </summary>
        public bool IsEmpty => Numbers.Length == 0;

        /// <summary>
        /// Obtains the number of items in the list.
        /// </summary>
        public int Count { get { if (IsRange) { return LastNumber - FirstNumber + 1; } return Numbers.Length; } }

        /// <summary>
        /// Obtains the string representing the <see cref="ImapNumberSequence"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (IsRange)
            {
                return FirstNumber + ":" + LastNumber;
            }
            else
            {
                var strBuilder = new StringBuilder();
                strBuilder.Append(Numbers[0]);
                for (var i = 1; i < Numbers.Length; i++)
                {
                    strBuilder.Append(',');
                    strBuilder.Append(Numbers[i]);
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
                return Empty.GetEnumerator();
            }

            if (IsRange)
            {
                return new Counter(FirstNumber, LastNumber - FirstNumber + 1).GetEnumerator();
            }
            return Numbers.GetEnumerator();
        }
    }
}
