﻿/*
 * Copyright 2015-2016 Mohawk College of Applied Arts and Technology
 *
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: khannan
 * Date: 2016-10-1
 */
using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHapi.Model.V25.Datatype;
using NHapi.Model.V25.Message;
using NHapi.Base.Model;
using OpenIZ.Core.Model.Entities;
using OpenIZ.Core.Model.Constants;
using System.Collections.Generic;
using MARC.HI.EHRS.SVC.Core;

namespace OpenIZ.Messaging.HL7.Test
{
	/// <summary>
	/// Contains tests for the <see cref="MessageUtil"/> class.
	/// </summary>
	[TestClass]
	public class MessageUtilTest
	{
		/// <summary>
		/// The internal reference to the <see cref="EntityTelecomAddress"/> instance.
		/// </summary>
		private EntityTelecomAddress entityTelecomAddress;

		/// <summary>
		/// The internal reference to the <see cref="XAD"/> instance.
		/// </summary>
		private XAD xad;

		/// <summary>
		/// The internal reference to the <see cref="XTN"/> instance.
		/// </summary>
		private XTN xtn;

		/// <summary>
		/// Runs cleanup after all tests have been completed.
		/// </summary>
		[ClassCleanup]
		public static void ClassCleanup()
		{
			ApplicationContext.Current.Dispose();
		}

		/// <summary>
		/// Runs initialization before any tests have started.
		/// </summary>
		/// <param name="context"></param>
		[ClassInitialize]
		public static void ClassStartup(TestContext context)
		{
			ApplicationContext.Current.Start();
		}

		/// <summary>
		/// Runs cleanup after each test execution.
		/// </summary>
		[TestCleanup]
		public void Cleanup()
		{
			this.entityTelecomAddress = null;
			this.xad = null;
			this.xtn = null;
		}

		/// <summary>
		/// Runs initialization before each test execution.
		/// </summary>
		[TestInitialize]
		public void Initialize()
		{
			this.entityTelecomAddress = new EntityTelecomAddress
			{
				AddressUse = new Core.Model.DataTypes.Concept
				{
					Key = TelecomAddressUseKeys.Public
				},
				Value = "9055751212"
			};

			this.xad = new XAD(Activator.CreateInstance(typeof(ADT_A01)) as IMessage);

			this.xad.AddressType.Value = "L";
			this.xad.City.Value = "Hamilton";
			this.xad.Country.Value = "Canada";
			this.xad.StateOrProvince.Value = "Ontario";
			this.xad.StreetAddress.StreetOrMailingAddress.Value = "123 Main street west";
			this.xad.StreetAddress.StreetName.Value = "Main St";
			this.xad.ZipOrPostalCode.Value = "L8N3T2";

			this.xtn = new XTN(Activator.CreateInstance(typeof(ADT_A01)) as IMessage);
		}

		[TestMethod]
		public void TestConvertAddress()
		{
			var actual = MessageUtil.ConvertAddress(xad);

			Assert.AreEqual(1, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.City));
			Assert.AreEqual("Hamilton", actual.Component.First(c => c.ComponentTypeKey == AddressComponentKeys.City).Value);

			Assert.AreEqual(1, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.Country));
			Assert.AreEqual("Canada", actual.Component.First(c => c.ComponentTypeKey == AddressComponentKeys.Country).Value);

			Assert.AreEqual(1, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.State));
			Assert.AreEqual("Ontario", actual.Component.First(c => c.ComponentTypeKey == AddressComponentKeys.State).Value);

			Assert.AreEqual(1, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.StreetName));
			Assert.AreEqual("Main St", actual.Component.First(c => c.ComponentTypeKey == AddressComponentKeys.StreetName).Value);

			Assert.AreEqual(1, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.PostalCode));
			Assert.AreEqual("L8N3T2", actual.Component.First(c => c.ComponentTypeKey == AddressComponentKeys.PostalCode).Value);
		}

		[TestMethod]
		public void TestConvertAddressEmptyCity()
		{
			this.xad.City.Value = string.Empty;

			var actual = MessageUtil.ConvertAddress(xad);

			Assert.AreEqual(0, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.City));
			Assert.IsNull(actual.Component.FirstOrDefault(c => c.ComponentTypeKey == AddressComponentKeys.City)?.Value);

			Assert.AreEqual(1, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.Country));
			Assert.AreEqual("Canada", actual.Component.First(c => c.ComponentTypeKey == AddressComponentKeys.Country).Value);

			Assert.AreEqual(1, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.State));
			Assert.AreEqual("Ontario", actual.Component.First(c => c.ComponentTypeKey == AddressComponentKeys.State).Value);

			Assert.AreEqual(1, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.StreetName));
			Assert.AreEqual("Main St", actual.Component.First(c => c.ComponentTypeKey == AddressComponentKeys.StreetName).Value);

			Assert.AreEqual(1, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.PostalCode));
			Assert.AreEqual("L8N3T2", actual.Component.First(c => c.ComponentTypeKey == AddressComponentKeys.PostalCode).Value);
		}

		[TestMethod]
		public void TestConvertAddressNullCity()
		{
			this.xad.City.Value = null;

			var actual = MessageUtil.ConvertAddress(xad);

			Assert.AreEqual(0, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.City));
			Assert.IsNull(actual.Component.FirstOrDefault(c => c.ComponentTypeKey == AddressComponentKeys.City)?.Value);

			Assert.AreEqual(1, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.Country));
			Assert.AreEqual("Canada", actual.Component.First(c => c.ComponentTypeKey == AddressComponentKeys.Country).Value);

			Assert.AreEqual(1, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.State));
			Assert.AreEqual("Ontario", actual.Component.First(c => c.ComponentTypeKey == AddressComponentKeys.State).Value);

			Assert.AreEqual(1, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.StreetName));
			Assert.AreEqual("Main St", actual.Component.First(c => c.ComponentTypeKey == AddressComponentKeys.StreetName).Value);

			Assert.AreEqual(1, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.PostalCode));
			Assert.AreEqual("L8N3T2", actual.Component.First(c => c.ComponentTypeKey == AddressComponentKeys.PostalCode).Value);
		}

		[TestMethod]
		public void TestConvertAddressEmptyCountry()
		{
			this.xad.Country.Value = string.Empty;

			var actual = MessageUtil.ConvertAddress(xad);

			Assert.AreEqual(1, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.City));
			Assert.AreEqual("Hamilton", actual.Component.First(c => c.ComponentTypeKey == AddressComponentKeys.City).Value);

			Assert.AreEqual(0, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.Country));
			Assert.IsNull(actual.Component.FirstOrDefault(c => c.ComponentTypeKey == AddressComponentKeys.Country)?.Value);

			Assert.AreEqual(1, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.State));
			Assert.AreEqual("Ontario", actual.Component.First(c => c.ComponentTypeKey == AddressComponentKeys.State).Value);

			Assert.AreEqual(1, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.StreetName));
			Assert.AreEqual("Main St", actual.Component.First(c => c.ComponentTypeKey == AddressComponentKeys.StreetName).Value);

			Assert.AreEqual(1, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.PostalCode));
			Assert.AreEqual("L8N3T2", actual.Component.First(c => c.ComponentTypeKey == AddressComponentKeys.PostalCode).Value);
		}

		[TestMethod]
		public void TestConvertAddressNullCountry()
		{
			this.xad.Country.Value = null;

			var actual = MessageUtil.ConvertAddress(xad);

			Assert.AreEqual(1, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.City));
			Assert.AreEqual("Hamilton", actual.Component.First(c => c.ComponentTypeKey == AddressComponentKeys.City).Value);

			Assert.AreEqual(0, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.Country));
			Assert.IsNull(actual.Component.FirstOrDefault(c => c.ComponentTypeKey == AddressComponentKeys.Country)?.Value);

			Assert.AreEqual(1, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.State));
			Assert.AreEqual("Ontario", actual.Component.First(c => c.ComponentTypeKey == AddressComponentKeys.State).Value);

			Assert.AreEqual(1, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.StreetName));
			Assert.AreEqual("Main St", actual.Component.First(c => c.ComponentTypeKey == AddressComponentKeys.StreetName).Value);

			Assert.AreEqual(1, actual.Component.Count(c => c.ComponentTypeKey == AddressComponentKeys.PostalCode));
			Assert.AreEqual("L8N3T2", actual.Component.First(c => c.ComponentTypeKey == AddressComponentKeys.PostalCode).Value);
		}

		[TestMethod]
		public void TestConvertTSDateTime()
		{
			var ts = new TS(Activator.CreateInstance(typeof(ADT_A01)) as IMessage);

			Assert.IsNotNull(ts);

			ts.Time.Value = new DateTime(1970, 01, 01).ToString("yyyyMMddHHmmss");

			var actual = MessageUtil.ConvertTS(ts);

			Assert.IsTrue(actual.HasValue);
			Assert.AreEqual(new DateTime(1970, 01, 01), actual.Value);
		}

		[TestMethod]
		public void TestConvertTSDateTimeOffset()
		{
			var ts = new TS(Activator.CreateInstance(typeof(ADT_A01)) as IMessage);

			Assert.IsNotNull(ts);

			ts.Time.Value = new DateTimeOffset(1970, 01, 01, 0, 0, 0, 0, TimeSpan.FromHours(12)).ToString("yyyyMMddHHmmss");

			var actual = MessageUtil.ConvertTS(ts);

			Assert.IsTrue(actual.HasValue);
			Assert.AreEqual(new DateTime(1970, 01, 01), actual.Value);
		}

		/// <summary>
		/// Tests that the AnyText value property is set, when "tel:" is not provided.
		/// </summary>
		[TestMethod]
		public void TestEntityTelecomToXTN()
		{
			MessageUtil.XTNFromTel(this.entityTelecomAddress, this.xtn);

			Assert.AreEqual("9055751212", this.xtn.AnyText.Value);
		}
	}
}