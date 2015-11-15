// *************************************************************************************
// Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ulcsoftware.co.uk
// Support: info@ulcsoftware.co.uk
// 
// Credentials.cs is part of Ecng.Xaml.Licensing.Core, a client-side licensing implementation 
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
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using Ecng.Xaml.Charting.Visuals;
namespace Ecng.Xaml.Licensing.Core
{
    [Obfuscation(Feature = "encryptmethod;encryptstrings;encryptconstants", Exclude = false, ApplyToMembers = true, StripAfterObfuscation = true)]
    public abstract class Credentials
    {
        bool? _valid;
	    public bool IsFeatureEnabled {get; private set;}

        public void SetTamper()
        {
        }
        protected bool IsTrial { get { return false; } }
        public bool IsLicenseValid { get { return _valid != null && _valid.Value; } }
        protected int LicenseDaysRemaining { get { return int.MaxValue; } }
        //public bool AllowDebugging { get { return true; } }
        
        public string ProductCode { get { return "SC-SRC"; } }
        protected Decoder.LicenseType LicenseType
        {
            get { EnsureFlag(); return _valid.Value ? Decoder.LicenseType.Full : Decoder.LicenseType.TrialExpired; }
        }

        static ulong CalculateHash(string read) {
            var hashedValue = 3074457345618258791ul;

            foreach(var t in read) {
                hashedValue += t;
                hashedValue *= 3074457345618258799ul;
            }

            return hashedValue;
        }

        void EnsureFlag()
        {
            if(_valid != null)
                return;

            _valid = false;

            try
            {
                var prop = typeof(UltrachartSurface).GetProperty(Encoding.UTF8.GetString(Convert.FromBase64String("TGljZW5zZUtleQ==")), BindingFlags.NonPublic | BindingFlags.Static);

                var str = prop.GetValue(null, null) as string;

                if (str == null) return;

                var part1 = str.Substring(0, 16);
                var part2 = str.Substring(16, 2);
                var part3 = str.Substring(18);

                var part1Num = ulong.Parse(part1, NumberStyles.HexNumber);

                _valid = CalculateHash(part2 + part3) == part1Num;

                var features = int.Parse(part2, NumberStyles.HexNumber);
	            IsFeatureEnabled = (features & 0x01) != 0;
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch { }
        }
    }
}
