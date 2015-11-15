// *************************************************************************************
// Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ulcsoftware.co.uk
// Support: info@ulcsoftware.co.uk
// 
// SymmetricEncryption.cs is part of Ecng.Xaml.Licensing.Core, a client-side licensing implementation 
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
using System;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System.Reflection;
using System.Linq;
namespace Ecng.Xaml.Licensing.Core
{
    [Obfuscation(Feature = "encryptmethod;encryptstrings;encryptconstants", Exclude = true, ApplyToMembers = true, StripAfterObfuscation = true)]
    internal class SymmetricEncryption
    {
        public static string Encrypt(string plainText)
        {
            return "";
        }
        public static string Decrypt(string plainText)
        {
            return "";
        }
        public static string Encrypt(string plainText, string password)
        {
            return "";
        }
        public static string Decrypt(string plainText, string password)
        {
            return "";
        }
    }
    public class EncryptionUtil
    {
        public static string Encrypt(string plainText, string password)
        {
            return SymmetricEncryption.Encrypt(plainText, password);
        }
        public static string Decrypt(string plainText, string password)
        {
            return SymmetricEncryption.Decrypt(plainText, password);
        }
        public static string Hash(string plainText)
        {
            SHA256Managed crypt = new SHA256Managed();
            string hash = String.Empty;
            byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(plainText), 0, Encoding.UTF8.GetByteCount(plainText));
            foreach (byte bit in crypto)
            {
                hash += bit.ToString("x2");
            }
            return hash;
        }
    }
}
