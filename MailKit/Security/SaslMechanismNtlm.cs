﻿//
// SaslMechanismNtlm.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Net;

using MailKit.Security.Ntlm;

namespace MailKit.Security {
	/// <summary>
	/// The NTLM SASL mechanism.
	/// </summary>
	/// <remarks>
	/// A SASL mechanism based on NTLM.
	/// </remarks>
	public class SaslMechanismNtlm : SaslMechanism
	{
		enum LoginState {
			Initial,
			Challenge
		}

		LoginState state;

		/// <summary>
		/// Initializes a new instance of the <see cref="MailKit.Security.SaslMechanismNtlm"/> class.
		/// </summary>
		/// <param name="uri">The URI of the service.</param>
		/// <param name="credentials">The user's credentials.</param>
		public SaslMechanismNtlm (Uri uri, ICredentials credentials) : base (uri, credentials)
		{
		}

		/// <summary>
		/// Gets the name of the mechanism.
		/// </summary>
		/// <value>The name of the mechanism.</value>
		public override string MechanismName {
			get { return "NTLM"; }
		}

		/// <summary>
		/// Parses the server's challenge token and returns the next challenge response.
		/// </summary>
		/// <returns>The next challenge response.</returns>
		/// <param name="token">The server's challenge token.</param>
		/// <param name="startIndex">The index into the token specifying where the server's challenge begins.</param>
		/// <param name="length">The length of the server's challenge.</param>
		/// <exception cref="SaslException">
		/// An error has occurred while parsing the server's challenge token.
		/// </exception>
		protected override byte[] Challenge (byte[] token, int startIndex, int length)
		{
			if (IsAuthenticated)
				throw new InvalidOperationException ();

			var cred = Credentials.GetCredential (Uri, MechanismName);
			string password = cred.Password ?? string.Empty;
			string userName = cred.UserName;
			string domain = cred.Domain;
			MessageBase message;

			if (string.IsNullOrEmpty (domain)) {
				int index = userName.IndexOf ('\\');
				if (index == -1)
					index = userName.IndexOf ('/');

				if (index >= 0) {
					domain = userName.Substring (0, index);
					userName = userName.Substring (index + 1);
				}
			}

			switch (state) {
			case LoginState.Initial:
				message = GetInitialResponse (domain);
				state = LoginState.Challenge;
				break;
			case LoginState.Challenge:
				message = GetChallengeResponse (userName, password, domain, token, startIndex, length);
				IsAuthenticated = true;
				break;
			default:
				throw new IndexOutOfRangeException ("state");
			}

			return message.Encode ();
		}

		static MessageBase GetInitialResponse (string domain)
		{
			var type1 = new Type1Message (string.Empty, domain);
			type1.Flags |= NtlmFlags.NegotiateNtlm2Key;

			return type1;
		}

		static MessageBase GetChallengeResponse (string userName, string password, string domain, byte[] token, int startIndex, int length)
		{
			var type2 = new Type2Message (token, startIndex, length);
			var type3 = new Type3Message (type2, userName, string.Empty);
			type3.Password = password;
			type3.Domain = domain;

			return type3;
		}

		/// <summary>
		/// Resets the state of the SASL mechanism.
		/// </summary>
		public override void Reset ()
		{
			state = LoginState.Initial;
			base.Reset ();
		}
	}
}
