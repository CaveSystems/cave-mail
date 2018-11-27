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
#endregion License
#region Authors & Contributors
/*
   Author:
     Andreas Rohleder <andreas@rohleder.cc>

   Contributors:
 */
#endregion Authors & Contributors

using Cave.Collections.Generic;
using Cave.Net;
using System;
using System.Net.Mail;

namespace Cave.Mail
{
    /// <summary>
    /// Provides an <see cref="MailAddress"/> Extension for verifying an email address at the mail server responsible for the address.
    /// </summary>
    public static class MailAddressExtension
    {
        /// <summary>Checks the specified email address for validity with the mail server responsible for the address.</summary>
        /// <param name="address">The email address to verify.</param>
        public static void Verify(this MailAddress address)
        {
            SmtpValidator validator = new SmtpValidator(NetTools.HostName, new MailAddress(Environment.UserName + '@' + NetTools.HostName));
            validator.Validate(address, true);
        }

        /// <summary>Checks the specified email address for validity with the mail server responsible for the address.</summary>
        /// <param name="address">The email address to verify.</param>
        /// <param name="serverName">Name of the server.</param>
        /// <exception cref="ArgumentOutOfRangeException">serverName;ServerName needs to be a full qualified domain name!</exception>
        public static void Verify(this MailAddress address, string serverName)
        {
            int i = serverName.IndexOf('.');
            int n = (i == -1) ? -1 : serverName.IndexOf('.', i + 1);
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(serverName), "ServerName needs to be a full qualified domain name!");
            string email = serverName.Substring(0, i) + '@' + serverName.Substring(i + 1);
            SmtpValidator validator = new SmtpValidator(serverName, new MailAddress(email));
            validator.Validate(address, true);
        }

        /// <summary>Loads the addresses from a address array. Each address is checked for validity and uniqueness.</summary>
        /// <param name="receipients">The receipients.</param>
        /// <param name="addresses">The addresses.</param>
        /// <param name="throwErrors">if set to <c>true</c> [throw errors].</param>
        public static void LoadAddresses(this Set<MailAddress> receipients, string[] addresses, bool throwErrors = false)
        {
            foreach (string address in addresses)
            {
                try
                {
                    receipients.Include(new MailAddress(address));
                }
                catch
                {
                    if (throwErrors) throw;
                }
            }
        }
    }
}
