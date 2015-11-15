// *************************************************************************************
// Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ulcsoftware.co.uk
// Support: info@ulcsoftware.co.uk
// 
// LookupUserRequest.cs is part of Ecng.Xaml.Licensing.Core, a client-side licensing implementation 
// used by ulc software controls such as Ultrachart
// 
// This source code is protected by international copyright law. Unauthorized
// reproduction, reverse-engineering, or distribution of all or any portion of
// this source code is strictly prohibited.
// 
// This source code contains confidential and proprietary trade secrets of
// ulc software Services Ltd., and should at no time be copied, transferred, sold,
// distributed or made available without express written permission.
// *************************************************************************************

using System.Reflection;

namespace Ecng.Xaml.Licensing.Core
{
    [Obfuscation(Feature = "encryptmethod;encryptstrings;encryptconstants", Exclude = false, ApplyToMembers = true, StripAfterObfuscation = true)]
    public class LookupUserRequest
    {
        public LookupUserRequest()
        {            
        }

        public LookupUserRequest(string username, string password)
        {
            UserName = username;
            Password = SymmetricEncryption.Encrypt(password);
        }

        public string UserName { get; set; }
        public string Password { get; set; }

        public string GetPassword() { return SymmetricEncryption.Decrypt(Password); }
    }

    public class LookupUserResponse
    {
        public LookupUserResponse()
        {
        }

        public bool HasUserAccount { get; set; }
    }
}