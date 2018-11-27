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

using System;
using System.Text;

namespace Cave.Mail.Imap
{
    /// <summary>
    /// Provides a search class for imap searches
    /// </summary>
    public class ImapSearch
    {
        static string m_CheckString(string p_String)
        {
            if (Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(p_String)) != p_String) throw new Exception("ImapSearch does not allow string searches with characters not part of US-ASCII !");
            return p_String;
        }

        /// <summary>
        /// Searches all messages in the mailbox
        /// </summary>
        public static ImapSearch ALL { get { return new ImapSearch(ImapSearchType.ALL); } }

        /// <summary>
        /// Searches for messages that match all search keys.
        /// </summary>
        public static ImapSearch AND(params ImapSearch[] p_Searches)
        {
            if (p_Searches.Length < 1) throw new Exception("ImapSearch.AND needs at least 1 search!");
            StringBuilder l_Result = new StringBuilder();
            l_Result.Append(p_Searches[0]);
            for (int i = 1; i < p_Searches.Length; i++)
            {
                l_Result.Append(' ');
                l_Result.Append(p_Searches[i].ToString());
            }
            return new ImapSearch(ImapSearchType._MULTIPLE, l_Result.ToString());
        }

        /// <summary>
        /// Searches messages with the \Answered flag set.
        /// </summary>
        public static ImapSearch ANSWERED { get { return new ImapSearch(ImapSearchType.ANSWERED); } }

        /// <summary>
        /// Searches messages that contain the specified string in the envelope structure's BCC field.
        /// </summary>
        public static ImapSearch BCC { get { return new ImapSearch(ImapSearchType.BCC); } }

        /// <summary>
        /// Searches messages whose internal date (disregarding time and timezone) is earlier than the specified date.
        /// </summary>
        public static ImapSearch BEFORE(DateTime p_DateTime) { return new ImapSearch(ImapSearchType.BEFORE, p_DateTime.ToString("d-mmm-yyyy")); }

        /// <summary>
        /// Searches messages that contain the specified string in the body of the message.
        /// </summary>
        public static ImapSearch BODY(string p_String) { return new ImapSearch(ImapSearchType.BODY, m_CheckString(p_String)); }

        /// <summary>
        /// Searches messages that contain the specified string in the envelope structure's CC field.
        /// </summary>
        public static ImapSearch CC(string p_String) { return new ImapSearch(ImapSearchType.CC, m_CheckString(p_String)); }

        /// <summary>
        /// Searches for messages with the \Deleted flag set.
        /// </summary>
        public static ImapSearch DELETED { get { return new ImapSearch(ImapSearchType.DELETED); } }

        /// <summary>
        /// Searches for messages with the \Draft flag set.
        /// </summary>
        public static ImapSearch DRAFT { get { return new ImapSearch(ImapSearchType.DRAFT); } }

        /// <summary>
        /// Searches for messages with the \Flagged flag set.
        /// </summary>
        public static ImapSearch FLAGGED { get { return new ImapSearch(ImapSearchType.FLAGGED); } }

        /// <summary>
        /// Searches for messages that contain the specified string in the envelope structure's FROM field.
        /// </summary>
        public static ImapSearch FROM(string p_String) { return new ImapSearch(ImapSearchType.FROM, m_CheckString(p_String)); }

        /// <summary>
        /// Searches for messages that have a header with the specified field-name (as defined in [RFC-2822]) and that contains the specified string in the text of the header (what comes after the colon). If the string to search is zero-length, this matches all messages that have a header line with the specified field-name regardless of the contents.
        /// </summary>
        public static ImapSearch HEADER(string p_FieldName, string p_String) { return new ImapSearch(ImapSearchType.HEADER, m_CheckString(p_FieldName + " " + p_String)); }

        /// <summary>
        /// Searches for messages with the specified keyword flag set.
        /// </summary>
        public static ImapSearch KEYWORD(string p_Flag) { return new ImapSearch(ImapSearchType.KEYWORD, m_CheckString(p_Flag)); }

        /// <summary>
        /// Searches for messages with an [RFC-2822] size larger than the specified number of octets.
        /// </summary>
        public static ImapSearch LARGER(int p_Size) { return new ImapSearch(ImapSearchType.LARGER, p_Size.ToString()); }

        /// <summary>
        /// Searches for messages that have the \Recent flag set but not the \Seen flag. This is functionally equivalent to "(RECENT UNSEEN)".
        /// </summary>
        public static ImapSearch NEW { get { return new ImapSearch(ImapSearchType.NEW); } }

        /// <summary>
        /// Searches messages that do not match the specified search key.
        /// </summary>
        public static ImapSearch NOT(ImapSearch p_Search) { return new ImapSearch(ImapSearchType.NOT, p_Search.ToString()); }

        /// <summary>
        /// Searches for messages that do not have the \Recent flag set. This is functionally equivalent to "NOT RECENT" (as opposed to "NOT NEW").
        /// </summary>
        public static ImapSearch OLD { get { return new ImapSearch(ImapSearchType.OLD); } }

        /// <summary>
        /// Searches for messages whose internal date (disregarding time and timezone) is within the specified date.
        /// </summary>
        public static ImapSearch ON(DateTime p_Date) { return new ImapSearch(ImapSearchType.ON, p_Date.ToString("d-mmm-yyyy")); }

        /// <summary>
        /// Searches for messages that match either search key.
        /// </summary>
        public static ImapSearch OR(params ImapSearch[] p_Searches)
        {
            if (p_Searches.Length < 2) throw new Exception("ImapSearch.OR needs at least 2 searches!");
            StringBuilder l_Result = new StringBuilder();
            l_Result.Append(p_Searches[0]);
            for (int i = 1; i < p_Searches.Length; i++)
            {
                l_Result.Append(" OR ");
                l_Result.Append(p_Searches[i].ToString());
            }
            return new ImapSearch(ImapSearchType._MULTIPLE, l_Result.ToString());
        }

        /// <summary>
        /// Messages that have the \Recent flag set.
        /// </summary>
        public static ImapSearch RECENT { get { return new ImapSearch(ImapSearchType.RECENT); } }

        /// <summary>
        /// Messages that have the \Seen flag set.
        /// </summary>
        public static ImapSearch SEEN { get { return new ImapSearch(ImapSearchType.SEEN); } }

        /// <summary>
        /// Searches for messages whose [RFC-2822] Date: header (disregarding time and timezone) is earlier than the specified date.
        /// </summary>
        public static ImapSearch SENTBEFORE(DateTime p_Date) { return new ImapSearch(ImapSearchType.SENTBEFORE, p_Date.ToString("d-mmm-yyyy")); }

        /// <summary>
        /// Searches for messages whose [RFC-2822] Date: header (disregarding time and timezone) is within the specified date.
        /// </summary>
        public static ImapSearch SENTON(DateTime p_Date) { return new ImapSearch(ImapSearchType.SENTON, p_Date.ToString("d-mmm-yyyy")); }

        /// <summary>
        /// Searches for messages whose [RFC-2822] Date: header (disregarding time and timezone) is within or later than the specified date.
        /// </summary>
        public static ImapSearch SENTSINCE(DateTime p_Date) { return new ImapSearch(ImapSearchType.SENTSINCE, p_Date.ToString("d-mmm-yyyy")); }

        /// <summary>
        /// Searches for messages whose internal date (disregarding time and timezone) is within or later than the specified date.
        /// </summary>
        public static ImapSearch SINCE(DateTime p_Date) { return new ImapSearch(ImapSearchType.SINCE, p_Date.ToString("d-mmm-yyyy")); }

        /// <summary>
        /// Searches for messages with an [RFC-2822] size smaller than the specified number of octets.
        /// </summary>
        public static ImapSearch SMALLER(int p_Size) { return new ImapSearch(ImapSearchType.SMALLER, p_Size.ToString()); }

        /// <summary>
        /// Searches for messages that contain the specified string in the envelope structure's SUBJECT field.
        /// </summary>
        public static ImapSearch SUBJECT(string p_String) { return new ImapSearch(ImapSearchType.SUBJECT, m_CheckString(p_String)); }

        /// <summary>
        /// Searches for messages that contain the specified string in the header or body of the message.
        /// </summary>
        public static ImapSearch TEXT(string p_String) { return new ImapSearch(ImapSearchType.TEXT, m_CheckString(p_String)); }

        /// <summary>
        /// Searches for messages that contain the specified string in the envelope structure's TO field.
        /// </summary>
        public static ImapSearch TO(string p_String) { return new ImapSearch(ImapSearchType.TO, m_CheckString(p_String)); }

        /// <summary>
        /// Searches for messages with unique identifiers corresponding to the specified unique identifier set. Sequence set ranges are permitted.
        /// </summary>
        public static ImapSearch UID(uint p_UID) { return new ImapSearch(ImapSearchType.UID, p_UID.ToString()); }

        /// <summary>
        /// Searches for messages with unique identifiers corresponding to the specified unique identifier set. Sequence set ranges are permitted.
        /// </summary>
        /// <param name="p_UIDStart"></param>
        /// <param name="p_UIDEnd"></param>
        /// <returns></returns>
        public static ImapSearch UID(uint p_UIDStart, int p_UIDEnd) { return new ImapSearch(ImapSearchType.UID, p_UIDStart.ToString() + " " + p_UIDEnd.ToString()); }

        /// <summary>
        /// Searches messages that do not have the \Deleted flag set.
        /// </summary>
        public static ImapSearch UNANSWERED { get { return new ImapSearch(ImapSearchType.UNANSWERED); } }

        /// <summary>
        /// Searches messages that do not have the \Deleted flag set.
        /// </summary>
        public static ImapSearch UNDELETED { get { return new ImapSearch(ImapSearchType.UNDELETED); } }

        /// <summary>
        /// Searches messages that do not have the \Draft flag set.
        /// </summary>
        public static ImapSearch UNDRAFT { get { return new ImapSearch(ImapSearchType.UNDRAFT); } }

        /// <summary>
        /// Searches messages that do not have the \Flagged flag set.
        /// </summary>
        public static ImapSearch UNFLAGGED { get { return new ImapSearch(ImapSearchType.UNFLAGGED); } }

        /// <summary>
        /// Searches messages that do not have the specified keyword flag set.
        /// </summary>
        public static ImapSearch UNKEYWORD(string p_Keyword)
        {
            if (String.IsNullOrEmpty(p_Keyword)) throw new ArgumentNullException("Keyword");
            return new ImapSearch(ImapSearchType.UNKEYWORD, p_Keyword);
        }

        /// <summary>
        /// Searches messages that do not have the \Seen flag set.
        /// </summary>
        public static ImapSearch UNSEEN { get { return new ImapSearch(ImapSearchType.UNSEEN); } }

        string m_Text;

        private ImapSearch(ImapSearchType p_Type)
            : this(p_Type, null)
        { }

        private ImapSearch(ImapSearchType p_Type, string p_Parameters)
        {
            m_Text = "";
            if (String.IsNullOrEmpty(p_Parameters))
            {
                if (p_Type == ImapSearchType._MULTIPLE) throw new InvalidOperationException();
                m_Text = p_Type.ToString();
                return;
            }
            if (p_Type == ImapSearchType._MULTIPLE)
            {
                m_Text = p_Parameters;
                return;
            }
            m_Text = p_Type.ToString() + " " + p_Parameters;
        }

        /// <summary>
        /// Provides the searchtext
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return m_Text;
        }
    }
}
