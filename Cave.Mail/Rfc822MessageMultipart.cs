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
using System.Net.Mime;

namespace Cave.Mail
{
    /// <summary>
    /// Provides <see cref="Rfc822Message"/> accessor for multipart messages
    /// </summary>
    public class Rfc822MessageMultipart : IEnumerable<Rfc822Message>
    {
        #region private functionality
        readonly List<Rfc822Message> m_Parts;

        /// <summary>
        /// Gets the <see cref="ContentTypes"/> in this multipart message. This does not traverse into nested multiparts !
        /// </summary>
        /// <returns></returns>
        List<ContentType> m_GetContentTypes()
        {
            List<ContentType> result = new List<ContentType>();
            foreach (Rfc822Message part in m_Parts)
            {
                result.Add(part.ContentType);
            }
            return result;
        }
        #endregion

        internal Rfc822MessageMultipart(List<Rfc822Message> items)
        {
            m_Parts = items;
        }

        /// <summary>
        /// Obtains the number of parts
        /// </summary>
        public int Count { get { return m_Parts.Count; } }

        /// <summary>
        /// Obtains whether the message contains at least one plain text part
        /// </summary>
        public bool HasPart(string mediaType)
        {
            foreach (Rfc822Message part in m_Parts)
            {
                if (part.HasPart(mediaType)) return true;
            }
            return false;
        }

        /// <summary>
        /// Obtains the first plain text part found in the message. This can only be accessed after checking <see cref="HasPart"/>
        /// </summary>
        /// <returns></returns>
        public Rfc822Message GetPart(string mediaType)
        {
            foreach (Rfc822Message part in m_Parts)
            {
                if (part.HasPart(mediaType)) return part.GetPart(mediaType);
            }
            throw new ArgumentException(string.Format("Message part {0} cannot be found!", mediaType));
        }

        /// <summary>
        /// Obtains all <see cref="ContentType"/>s found. This does not traverse into nested multiparts !
        /// </summary>
        public ContentType[] ContentTypes
        {
            get
            {
                return m_GetContentTypes().ToArray();
            }
        }

        /// <summary>
        /// Obtains the part with the specified <see cref="ContentType"/>. This does not traverse into nested multiparts !
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public Rfc822Message this[ContentType contentType]
        {
            get
            {
                if (contentType == null) throw new ArgumentNullException("contentType");
                return this[contentType.MediaType];
            }
        }

        /// <summary>
        /// Obtains the first part found with the specified MediaType (e.g. text/plain). This does not traverse into nested multiparts !
        /// </summary>
        /// <param name="mediaType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Thrown if the media type cannot be found</exception>
        public Rfc822Message this[string mediaType]
        {
            get
            {
                mediaType = mediaType.GetValidChars("abcdefghijklmnopqrstuvwxyz/");
                ContentType[] contentTypes = ContentTypes;
                for (int i = 0; i < contentTypes.Length; i++)
                {
                    string l_MediaType = contentTypes[i].MediaType.ToUpperInvariant().GetValidChars("abcdefghijklmnopqrstuvwxyz/");
                    if (l_MediaType == mediaType) return this[i];
                }
                throw new ArgumentException(string.Format("ContentType {0} not found!", mediaType));
            }
        }

        /// <summary>
        /// Obtains the part with the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Rfc822Message this[int index]
        {
            get
            {
                return m_Parts[index];
            }
        }

        #region IEnumerable<Rfc822Message> Member

        /// <summary>
        /// Obtains a Rfc822Message enumerator for all parts
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Rfc822Message> GetEnumerator()
        {
            return m_Parts.GetEnumerator();
        }

        #endregion

        #region IEnumerable Member

        /// <summary>
        /// Obtains a Rfc822Message enumerator for all parts
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_Parts.GetEnumerator();
        }

        #endregion
    }
}
