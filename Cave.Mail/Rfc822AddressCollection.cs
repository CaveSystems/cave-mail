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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

namespace Cave.Mail
{
    /// <summary>
    /// Provides an <see cref="ICollection{Rfc822Message}"/> for <see cref="Rfc822Message"/>s
    /// This object directly works on the rfc822 header data of a message.
    /// </summary>
    public class Rfc822AddressCollection : ICollection<MailAddress>
    {
        #region private functionality
        readonly NameValueCollection m_Header;
        readonly string m_Key;
        readonly Encoding m_Encoding;

        internal Rfc822AddressCollection(string key, NameValueCollection header, Encoding encoding)
        {
            m_Encoding = encoding;
            m_Header = header;
            m_Key = key;
            if (m_Header[m_Key] == null)
            {
                m_Header[m_Key] = "";
            }
        }

        string m_Data
        {
            get { return (m_Header[m_Key] == null) ? "" : m_Header[m_Key].Trim(); }
            set { m_Header[m_Key] = value; }
        }

        List<MailAddress> m_Parse()
        {
            List<MailAddress> result = new List<MailAddress>();
            foreach (string address in m_Data.Split(','))
            {
                if (address.Contains("@"))
                {
                    result.Add(Rfc2047.DecodeMailAddress(address));
                }
            }
            return result;
        }

        void m_Write(List<MailAddress> addresses)
        {
            StringBuilder data = new StringBuilder();
            bool l_First = true;
            foreach (MailAddress address in addresses)
            {
                if (l_First) { l_First = false; } else { data.Append(", "); }
                data.Append(Rfc2047.EncodeMailAddress(TransferEncoding.QuotedPrintable, m_Encoding, address));
            }
            m_Data = data.ToString();
        }
        #endregion

        #region ICollection<MailAddress> Member

        /// <summary>
        /// Adds a <see cref="MailAddress"/>.
        /// </summary>
        /// <param name="item"></param>
        public void Add(MailAddress item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            string data = m_Data;
            if (!data.EndsWith(","))
            {
                data += ',';
            }
            data += ' ';
            data += item.ToString();
            m_Data = data;
        }

        /// <summary>
        /// Clears all addresses.
        /// </summary>
        public void Clear()
        {
            m_Data = "";
        }

        /// <summary>
        /// Checks whether a specified <see cref="MailAddress"/> is part of the list.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(MailAddress item)
        {
            return m_Parse().Contains(item);
        }

        /// <summary>
        /// Copies all <see cref="MailAddress"/>es to a specified array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(MailAddress[] array, int arrayIndex)
        {
            m_Parse().CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Obtains the number of <see cref="MailAddress"/> present.
        /// </summary>
        public int Count => m_Parse().Count;

        /// <summary>
        /// returns always false.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Removes a <see cref="MailAddress"/> from the list.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(MailAddress item)
        {
            List<MailAddress> result = m_Parse();
            if (result.Remove(item))
            {
                return false;
            }
            else
            {
                m_Write(result);
                return true;
            }
        }

        #endregion

        #region IEnumerable<MailAddress> Member

        /// <summary>
        /// Obtains a typed enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<MailAddress> GetEnumerator()
        {
            return m_Parse().GetEnumerator();
        }

        #endregion

        #region IEnumerable Member

        /// <summary>
        /// Obtains an untyped enumerator.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)m_Parse()).GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Provides the header data.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return m_Key + ": " + m_Data;
        }
    }
}
