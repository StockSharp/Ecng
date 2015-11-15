// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AnnotationCollectionUltrachartLicenseProvider.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
// For full terms and conditions of the license, see http://www.ultrachart.com/ultrachart-eula/
// 
// This source code is protected by international copyright law. Unauthorized
// reproduction, reverse-engineering, or distribution of all or any portion of
// this source code is strictly prohibited.
// 
// This source code contains confidential and proprietary trade secrets of
// ulc software Services Ltd., and should at no time be copied, transferred, sold,
// distributed or made available without express written permission.
// *************************************************************************************
using System.ComponentModel;
using System.Reflection;
using Ecng.Xaml.Charting.Visuals.Annotations;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Licensing
{
    [Obfuscation(Feature = "encryptmethod;encryptstrings;encryptconstants", Exclude = false, ApplyToMembers = true, StripAfterObfuscation = true)]
    internal class AnnotationCollectionLicenseProvider : Credentials, IUltrachartLicenseProvider
    {
        public void Validate(object parameter)
        {
            var collection = parameter as AnnotationCollection;
            if (collection == null)
            {
                return;
            }

            bool isLicenseValid = LicenseType == Decoder.LicenseType.Full ||
                                  LicenseType == Decoder.LicenseType.Trial;            

            if (isLicenseValid)
            {
                collection.CollectionChanged += collection.AnnotationCollectionChanged;
            }
        }
    }
}