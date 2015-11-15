// *************************************************************************************
// Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ulcsoftware.co.uk
// Support: info@ulcsoftware.co.uk
// 
// UltrachartLicensingException.cs is part of Ecng.Xaml.Licensing.Core, a client-side licensing implementation 
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ecng.Xaml.Licensing.Core
{
    [Obfuscation(Feature = "encryptmethod;encryptstrings;encryptconstants", Exclude = false, ApplyToMembers = true, StripAfterObfuscation = true)]
    public class UltrachartLicensingException: Exception
    {
        public UltrachartLicensingException(string message) : base(message) { }

        public static UltrachartLicensingException Create(bool isTrialExpired, bool isRuntimeKeyPresent)
        {
            string message;
            if (isRuntimeKeyPresent)
                message = "Ultrachart must be activated on this machine using a purchased serial key to allow development.";
            else if (isTrialExpired)
                message = "Your trial of Ultrachart has expired.";
            else
                message = "Your Ultrachart licence is invalid";

            message += Environment.NewLine + "Please contact support@ultrachart.com";

            return new UltrachartLicensingException(message);

        }
    }
}
