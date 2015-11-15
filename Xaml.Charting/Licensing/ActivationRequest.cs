// *************************************************************************************
// Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ulcsoftware.co.uk
// Support: info@ulcsoftware.co.uk
// 
// ActivationRequest.cs is part of Ecng.Xaml.Licensing.Core, a client-side licensing implementation 
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
    public class ActivationRequest
    {
        public Guid SerialKey { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string MachineId { get; set; }
        public bool Activate { get; set; }

        public ActivationRequest() { }

        public ActivationRequest(string serialKey, string username, string password, string machineId)
            : this(username, password, machineId)
        {
            NotNullOrEmpty(serialKey, "serialKey");
            Guid serial;
            if (Guid.TryParse(serialKey, out serial))
            {
                this.SerialKey = serial;
            }
            else
                throw new ArgumentException("SerialKey is invalid");
        }

        public ActivationRequest(Guid serialKey, string username, string password, string machineId)
            : this(username, password, machineId)
        {
            this.SerialKey = serialKey;
        }

        private ActivationRequest(string username, string password, string machineId)
        {
            NotNullOrEmpty(username, "username");
            this.UserName = username;
            this.Password = SymmetricEncryption.Encrypt(password);
            NotNullOrEmpty(machineId, "machineId");
            this.MachineId = machineId;
            this.Activate = true;
        }

        public string GetPassword()
        {
            try
            {
                return SymmetricEncryption.Decrypt(this.Password);
            }
            catch
            { return this.Password; }
        }

        private void NotNullOrEmpty(string argument, string argName)
        {
            if (string.IsNullOrEmpty(argument))
            {
                throw new ArgumentNullException(argName);
            }
        }
    }
}
