// *************************************************************************************
// Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ulcsoftware.co.uk
// Support: info@ulcsoftware.co.uk
// 
// ActivationResponse.cs is part of Ecng.Xaml.Licensing.Core, a client-side licensing implementation 
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
using System.Threading.Tasks;

namespace Ecng.Xaml.Licensing.Core
{
    [Obfuscation(Feature = "encryptmethod;encryptstrings;encryptconstants", Exclude = false, ApplyToMembers = true, StripAfterObfuscation = true)]
    public class ActivationResponse
    {
        public bool Success { get; set; }
        public string DesignTimeLicense { get; set; }
        public string OrderID { get; set; }
        public string DeveloperName { get; set; }
        public string DeveloperEmail { get; set; }
        public int Quantity { get; set; }
        public string ProductCode { get; set; }
        public DateTime? SupportExpiryDate { get; set; }
        public Guid SerialKey { get; set; }
        public int ActivationID { get; set; }
        public DateTime? ActivationDate { get; set; }
        public string ActivationErrorMessage { get; set; }
        public ActivationFailureReason ActivationFailureReason { get; set; }

        public ActivationResponse() { }

        public ActivationResponse(Guid serialKey, int activationID, DateTime? activationDate, string orderId, string developerName, string productCode, DateTime? supportExpiry, string developerEmail, int quantity)
        {
            this.Success = true;
            this.OrderID = orderId;
            this.DeveloperName = developerName;
            this.ProductCode = productCode;
            this.SupportExpiryDate = supportExpiry;
            this.SerialKey = serialKey;
            this.ActivationID = activationID;
            this.ActivationDate = activationDate;
            this.DeveloperEmail = developerEmail;
            this.Quantity = quantity;
        }

        public ActivationResponse(ActivationException error)
        {
            this.Success = false;
            this.ActivationErrorMessage = error.Message;
            this.ActivationFailureReason = error.FailureReason;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (ReferenceEquals(this, obj)) return true;

            if (obj.GetType() != GetType()) return false;

            return Equals((ActivationResponse)obj);
        }

        protected bool Equals(ActivationResponse other)
        {
            return Success == other.Success &&
                   string.Equals(DesignTimeLicense, other.DesignTimeLicense) &&
                   string.Equals(OrderID, other.OrderID) &&
                   string.Equals(DeveloperName, other.DeveloperName) &&
                   string.Equals(ProductCode, other.ProductCode) &&
                   SupportExpiryDate == other.SupportExpiryDate &&
                   Guid.Equals(SerialKey, other.SerialKey) &&
                   ActivationID == other.ActivationID &&
                   ActivationDate == other.ActivationDate &&
                   Quantity == other.Quantity &&
                   string.Equals(DeveloperEmail, other.DeveloperEmail) &&
                   (ActivationErrorMessage == other.ActivationErrorMessage || (ActivationErrorMessage != null && ActivationErrorMessage.Equals(other.ActivationErrorMessage)));
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Success.GetHashCode();
                hashCode = (hashCode * 397) ^ (DesignTimeLicense != null ? DesignTimeLicense.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (OrderID != null ? OrderID.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DeveloperName != null ? DeveloperName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ProductCode != null ? ProductCode.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ SupportExpiryDate.GetHashCode();
                hashCode = (hashCode * 397) ^ (SerialKey != null ? SerialKey.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ActivationID.GetHashCode();
                hashCode = (hashCode * 397) ^ ActivationDate.GetHashCode();
                return hashCode;
            }
        }
    }
}
