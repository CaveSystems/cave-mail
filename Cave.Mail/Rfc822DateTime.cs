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
using System.IO;

namespace Cave.Mail
{
    /// <summary>
    /// Provides conversion routines for rfc822 datetime fields
    /// </summary>
    public static class Rfc822DateTime
    {
        static bool m_CheckString(ref string date, string pattern)
        {
            int index = date.IndexOf(pattern);
            if (index < 0) return false;
            date=date.Remove(index, pattern.Length);
            return true;
        }

        static List<int> m_ValueExtractor(string date)
        {
            List<int> result = new List<int>();
            bool l_GotOne = false;
            int current = 0;
            foreach (char c in date)
            {
                switch (c)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        l_GotOne = true;
                        current = current * 10 + (c - '0');
                        break;
                    default:
                        if (l_GotOne)
                        {
                            result.Add(current);
                            current = 0;
                            l_GotOne = false;
                        }
                        break;
                }
            }
            if (l_GotOne) result.Add(current);
            return result;
        }

        struct SimpleDate
        {
            public int Day;
            public int Month;
            public int Year;
            public int Hour;
            public int Min;
            public int Sec;
        }

        /// <summary>
        /// Decodes a rfc822 datetime field
        /// </summary>
        /// <param name="rfc822DateTime"></param>
        /// <returns></returns>
        public static DateTime Decode(string rfc822DateTime)
        {
            if (rfc822DateTime == null) throw new ArgumentNullException("rfc822DateTime");
            //try default parser first
            {
                DateTime result;
                if (DateTime.TryParse(rfc822DateTime, out result)) return result;
            }
            //try to manually parse
            try
            {
                SimpleDate result = new SimpleDate();
                double l_LocalDifference = 0;

                string l_Date = rfc822DateTime.ToUpperInvariant();
                if (m_CheckString(ref l_Date, "JAN")) result.Month = 1;
                else if (m_CheckString(ref l_Date, "FEB")) result.Month = 2;
                else if (m_CheckString(ref l_Date, "MAR")) result.Month = 3;
                else if (m_CheckString(ref l_Date, "APR")) result.Month = 4;
                else if (m_CheckString(ref l_Date, "MAY")) result.Month = 5;
                else if (m_CheckString(ref l_Date, "JUN")) result.Month = 6;
                else if (m_CheckString(ref l_Date, "JUL")) result.Month = 7;
                else if (m_CheckString(ref l_Date, "AUG")) result.Month = 8;
                else if (m_CheckString(ref l_Date, "SEP")) result.Month = 9;
                else if (m_CheckString(ref l_Date, "OCT")) result.Month = 10;
                else if (m_CheckString(ref l_Date, "NOV")) result.Month = 11;
                else if (m_CheckString(ref l_Date, "DEC")) result.Month = 12;

                int timeZoneIndex = l_Date.IndexOfAny(new char[] { '+', '-' });
                if (timeZoneIndex > -1)
                {
                    string timeZone = l_Date.Substring(timeZoneIndex).Trim();
                    l_Date = l_Date.Substring(0, timeZoneIndex);
                    try
                    {
                        if (timeZone.Length > 5) timeZone = timeZone.Substring(0, 5);
                        l_LocalDifference = int.Parse(timeZone) / 100.0;
                    }
                    catch
                    {
                        l_LocalDifference = 0;
                    }
                }
                if (l_LocalDifference == 0)
                {
                    if (m_CheckString(ref l_Date, " mst")) l_LocalDifference = -7;
                    else if (m_CheckString(ref l_Date, " mdt")) l_LocalDifference = -6;
                    else if (m_CheckString(ref l_Date, " cst")) l_LocalDifference = -6;
                    else if (m_CheckString(ref l_Date, " pst")) l_LocalDifference = -5;
                    else if (m_CheckString(ref l_Date, " cdt")) l_LocalDifference = -5;
                    else if (m_CheckString(ref l_Date, " est")) l_LocalDifference = -5;
                    else if (m_CheckString(ref l_Date, " pdt")) l_LocalDifference = -4;
                    else if (m_CheckString(ref l_Date, " edt")) l_LocalDifference = -4;
                    else if (m_CheckString(ref l_Date, " a")) l_LocalDifference = +1;
                    else if (m_CheckString(ref l_Date, " b")) l_LocalDifference = +2;
                    else if (m_CheckString(ref l_Date, " c")) l_LocalDifference = +3;
                    else if (m_CheckString(ref l_Date, " d")) l_LocalDifference = +4;
                    else if (m_CheckString(ref l_Date, " e")) l_LocalDifference = +5;
                    else if (m_CheckString(ref l_Date, " f")) l_LocalDifference = +6;
                    else if (m_CheckString(ref l_Date, " g")) l_LocalDifference = +7;
                    else if (m_CheckString(ref l_Date, " h")) l_LocalDifference = +8;
                    else if (m_CheckString(ref l_Date, " i")) l_LocalDifference = +9;
                    else if (m_CheckString(ref l_Date, " k")) l_LocalDifference = +10;
                    else if (m_CheckString(ref l_Date, " l")) l_LocalDifference = +12;
                    else if (m_CheckString(ref l_Date, " m")) l_LocalDifference = +12;
                    else if (m_CheckString(ref l_Date, " n")) l_LocalDifference = -1;
                    else if (m_CheckString(ref l_Date, " o")) l_LocalDifference = -2;
                    else if (m_CheckString(ref l_Date, " p")) l_LocalDifference = -3;
                    else if (m_CheckString(ref l_Date, " q")) l_LocalDifference = -4;
                    else if (m_CheckString(ref l_Date, " r")) l_LocalDifference = -5;
                    else if (m_CheckString(ref l_Date, " s")) l_LocalDifference = -6;
                    else if (m_CheckString(ref l_Date, " t")) l_LocalDifference = -7;
                    else if (m_CheckString(ref l_Date, " u")) l_LocalDifference = -8;
                    else if (m_CheckString(ref l_Date, " v")) l_LocalDifference = -9;
                    else if (m_CheckString(ref l_Date, " w")) l_LocalDifference = -10;
                    else if (m_CheckString(ref l_Date, " x")) l_LocalDifference = -11;
                    else if (m_CheckString(ref l_Date, " y")) l_LocalDifference = -12;
                }

                List<int> values = m_ValueExtractor(l_Date);
                for (int i = 0; i < values.Count; i++)
                {
                    if (values[i] > 1900)
                    {
                        result.Year = values[i];
                        values.RemoveAt(i);
                        break;
                    }
                }
                if (result.Year == 0)
                {
                    if (values.Count >= 2)
                    {
                        result.Year = values[1];
                        values.RemoveAt(1);
                    }
                }

                if (values.Count >= 1) result.Day = values[0];
                if (values.Count >= 2) result.Hour = values[1];
                if (values.Count >= 3) result.Min = values[2];
                if (values.Count >= 4) result.Sec = values[3];

                return new DateTime(result.Year, result.Month, result.Day, result.Hour, result.Min, result.Sec, DateTimeKind.Utc).AddHours(-l_LocalDifference).ToLocalTime();
            }
            catch (Exception ex)
            {
                throw new InvalidDataException(string.Format("Invalid date format '{0}'!", rfc822DateTime), ex);
            }
        }

        /// <summary>
        /// Encodes a rfc822 datetime field
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string Encode(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return dateTime.ToString("ddd, dd MMM yyyy HH':'mm':'ss GMT");
            }
            int localDifference = (int)(100.0 * DateTimeOffset.Now.Offset.TotalHours);
            if (localDifference > 0)
            {
                return dateTime.ToString("ddd, dd MMM yyyy HH':'mm':'ss +" + localDifference);
            }
            else
            {
                return dateTime.ToString("ddd, dd MMM yyyy HH':'mm':'ss " + localDifference);
            }
        }
    }
}
