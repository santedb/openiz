﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Core.Model.Constants
{
    /// <summary>
    /// Telecommunications address use keys
    /// </summary>
    public static class TelecomAddressUseKeys
    {
        /// <summary>
        /// For use in the workplace
        /// </summary>
        public static readonly Guid WorkPlace = Guid.Parse("EAA6F08E-BB8E-4457-9DC0-3A1555FADF5C");
        /// <summary>
        /// Emergency contact
        /// </summary>
        public static readonly Guid EmergencyContact = Guid.Parse("25985F42-476A-4455-A977-4E97A554D710");
        /// <summary>
        /// temporary contact
        /// </summary>
        public static readonly Guid TemporaryAddress = Guid.Parse("CEF6EA31-A097-4F59-8723-A38C727C6597");
        /// <summary>
        /// answering service
        /// </summary>
        public static readonly Guid AnsweringService = Guid.Parse("1ECD7B17-B5FF-4CAE-9C3B-C1258132D137");
        /// <summary>
        /// pager
        /// </summary>
        public static readonly Guid Pager = Guid.Parse("788000B4-E37A-4055-A2AA-C650089CE3B1");
        /// <summary>
        /// public (800 number example) contact
        /// </summary>
        public static readonly Guid Public = Guid.Parse("EC35EA7C-55D2-4619-A56B-F7A986412F7F");
        /// <summary>
        /// Mobile phone contact
        /// </summary>
        public static readonly Guid MobileContact = Guid.Parse("E161F90E-5939-430E-861A-F8E885CC353D");
    }
}
