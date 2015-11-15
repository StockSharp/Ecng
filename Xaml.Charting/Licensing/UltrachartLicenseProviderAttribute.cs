// *************************************************************************************
// Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ulcsoftware.co.uk
// Support: info@ulcsoftware.co.uk
// 
// UltrachartLicenseProviderAttribute.cs is part of Ecng.Xaml.Licensing.Core, a client-side licensing implementation 
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
using System.Reflection;

namespace Ecng.Xaml.Licensing.Core
{
    [Obfuscation(Feature = "encryptmethod;encryptstrings;encryptconstants", Exclude = false, ApplyToMembers = true, StripAfterObfuscation = true)]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class UltrachartLicenseProviderAttribute : Attribute
    {
        public UltrachartLicenseProviderAttribute(Type providerType)
        {
            ProviderType = providerType;
        }

        internal Type ProviderType { get; private set; }
    }
}