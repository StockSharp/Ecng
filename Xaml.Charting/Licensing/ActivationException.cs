// *************************************************************************************
// Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ulcsoftware.co.uk
// Support: info@ulcsoftware.co.uk
// 
// ActivationException.cs is part of Ecng.Xaml.Licensing.Core, a client-side licensing implementation 
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
using System.Runtime.Serialization;
using System.Text;

namespace Ecng.Xaml.Licensing.Core
{
    [Obfuscation(Feature = "encryptmethod;encryptstrings;encryptconstants", Exclude = false, ApplyToMembers = true, StripAfterObfuscation = true)]
    [Serializable]
    public class ActivationException : ApplicationException
    {
        public ActivationFailureReason FailureReason { get; private set; }

        public ActivationException(string message, Exception innerException, ActivationFailureReason failureReason) 
            : base(message, innerException) 
        {
            FailureReason = failureReason;
        }

        public static readonly Dictionary<ActivationFailureReason, string> ActivationFailureStrings = new Dictionary
            <ActivationFailureReason, string>()
        {
            { ActivationFailureReason.InvalidSerialKey, "Invalid Serial Key" },
            { ActivationFailureReason.InvalidUser, "Invalid User" },
            { ActivationFailureReason.MaxActivationsReached, "Max Activations Reached" },
            { ActivationFailureReason.SerialActivatedBySomeoneElse, "Serial Activated by Somebody Else" },
            { ActivationFailureReason.ServerError, "Server Error" },
            { ActivationFailureReason.NotActivated, "Not Activated" },
            { ActivationFailureReason.ServerUnreachable, "Server Unreacheable" },
            { ActivationFailureReason.SerializationError, "Serialization Error" },
        };
    }

    public enum ActivationFailureReason
    {
        InvalidSerialKey,
        InvalidUser,
        MaxActivationsReached,
        SerialActivatedBySomeoneElse,
        ServerError,
        NotActivated,
        ServerUnreachable, 
        SerializationError
    }
}
