// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// CoordinateCalculatorLicenseProvider.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Licensing.Core;
namespace Ecng.Xaml.Charting.Licensing
{
    [Obfuscation(Feature = "encryptmethod;encryptstrings;encryptconstants", Exclude = false, ApplyToMembers = true, StripAfterObfuscation = true)]
    internal class CoordinateCalculatorLicenseProvider : Credentials, IUltrachartLicenseProvider
    {
        private static readonly Random rand = new Random();
        private static bool? _flag = null;
        public void Validate(object parameter)
        {
            var coordinateCalculator = parameter as DoubleCoordinateCalculator;
            if (coordinateCalculator != null && rand.NextDouble() < 0.33 && GetTamper())
            {
                coordinateCalculator.CoordinateConstant *= rand.NextDouble();
            }
            var coordinateCalculatorex = parameter as FlippedDoubleCoordinateCalculator;
            if (coordinateCalculator != null && rand.NextDouble() < 0.33 && GetTamper())
            {
                coordinateCalculator.CoordinateConstant *= rand.NextDouble();
            }
        }
        [Obfuscation(Feature = "encryptmethod;encryptstrings;encryptconstants", Exclude = false, ApplyToMembers = true, StripAfterObfuscation = true)]
        private bool GetTamper()
        {
            if (!_flag.HasValue)
            {
                _flag = false;
            }
            return _flag.Value;
        }
    }
}
