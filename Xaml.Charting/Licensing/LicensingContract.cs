// *************************************************************************************
// Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ulcsoftware.co.uk
// Support: info@ulcsoftware.co.uk
// 
// LicenseContract.cs is part of Ecng.Xaml.Licensing.Core, a client-side licensing implementation 
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
using System.Globalization;
using System.Reflection;
using System.Xml.Linq;

namespace Ecng.Xaml.Licensing.Core
{
    [Obfuscation(Feature = "encryptmethod;encryptstrings;encryptconstants", Exclude = false, ApplyToMembers = true, StripAfterObfuscation = true)]
    public sealed class LicenseContract
    {
        public static LicenseContract InvalidLicence = new LicenseContract()
        {
            LicenseType = Decoder.LicenseType.InvalidLicense,
            ExpiresOn = DateTime.MinValue
        };

        public LicenseContract()
        {
            this.MachineSpecific = false;
        }

        public Decoder.LicenseType LicenseType { get; set; }
        public string Customer { get; set; }
        public string OrderId { get; set; }
        public string LicensedTo { get; set; }
        public string ProductCode { get; set; }
        public string SerialKey { get; set; }
        public int LicensedDevelopers { get; set; }
        public DateTime ExpiresOn { get; set; }
        public bool IsTrialLicense { get; set; }
        public bool MachineSpecific { get; set; }
        public string KeyCode { get; set; }

        public bool AllowDebugging
        {
            get { return this.IsTrialLicense || !string.IsNullOrEmpty(this.SerialKey); }
        }                        

        public string Serialize()
        {
            var root = new XElement("LicenseContract",
                    new XElement("Customer", this.Customer),
                    new XElement("OrderId", this.OrderId),
                    new XElement("LicenseCount", this.LicensedDevelopers),
                    new XElement("IsTrialLicense", this.IsTrialLicense),
                    new XElement("SupportExpires", this.ExpiresOn.ToString(CultureInfo.InvariantCulture))
            );

            if (!string.IsNullOrEmpty(ProductCode))
                root.Add(new XElement("ProductCode", this.ProductCode));

            if (this.MachineSpecific)
                root.Add(new XElement("MachineSpecific", this.MachineSpecific));

            if (!string.IsNullOrEmpty(SerialKey))
                root.Add(new XElement("SerialKey", this.SerialKey));            

            if (!string.IsNullOrEmpty(LicensedTo))
                root.Add(new XElement("LicensedTo", this.LicensedTo));            

            if (!string.IsNullOrEmpty(KeyCode))
                root.Add(new XElement("KeyCode", this.KeyCode));

            return new XDocument(root).ToString(SaveOptions.None);
        }

        public static LicenseContract Deserialize(string rawXml)
        {
            var doc = XDocument.Parse(rawXml);
            var contract = new LicenseContract();
            contract.Customer = doc.Root.GetRequiredElementValue("Customer");
            contract.OrderId = doc.Root.GetRequiredElementValue("OrderId");
            contract.ExpiresOn = DateTime.Parse(doc.Root.GetRequiredElementValue("SupportExpires"), CultureInfo.InvariantCulture);
            contract.LicensedDevelopers = int.Parse(doc.Root.GetRequiredElementValue("LicenseCount"));
            contract.IsTrialLicense = bool.Parse(doc.Root.GetRequiredElementValue("IsTrialLicense"));
            string machineSpecificStr = doc.Root.GetOptionalElementValue("MachineSpecific");
            contract.MachineSpecific = String.IsNullOrEmpty(machineSpecificStr) ? false : bool.Parse(machineSpecificStr);
            contract.SerialKey = doc.Root.GetOptionalElementValue("SerialKey");
            contract.KeyCode = doc.Root.GetOptionalElementValue("KeyCode");
            contract.LicensedTo = doc.Root.GetOptionalElementValue("LicensedTo");
            contract.ProductCode = doc.Root.GetOptionalElementValue("ProductCode");

            return contract;
        }

    }
}
