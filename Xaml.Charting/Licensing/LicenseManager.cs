// *************************************************************************************
// Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ulcsoftware.co.uk
// Support: info@ulcsoftware.co.uk
// 
// LicenseManager.cs is part of Ecng.Xaml.Licensing.Core, a client-side licensing implementation 
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
using System.Linq;
using System.Reflection;

namespace Ecng.Xaml.Licensing.Core
{
    [Obfuscation(Feature = "encryptmethod;encryptstrings;encryptconstants", Exclude = false, ApplyToMembers = true, StripAfterObfuscation = true)]
    public sealed class LicenseManager : ILicenseManager
    {
        [Obfuscation(Feature = "encryptmethod;encryptstrings;encryptconstants", Exclude = false, StripAfterObfuscation = true)]
        public void Validate<T>(T instance, IProviderFactory factory)
        {
            if (ReferenceEquals(instance, default(T)))
                return;

            var attr = typeof(T).GetCustomAttributes(typeof (UltrachartLicenseProviderAttribute), true)
                .FirstOrDefault() as UltrachartLicenseProviderAttribute;

            if (attr == null)
            {
                attr = instance.GetType().GetCustomAttributes(typeof(UltrachartLicenseProviderAttribute), true)
                    .FirstOrDefault() as UltrachartLicenseProviderAttribute;
            }

            if (attr == null)
            {
                return;
            }

            var provider = factory.CreateInstance(attr.ProviderType);
            provider.Validate(instance);
        }
    }
}
