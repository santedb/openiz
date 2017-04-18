﻿using GIIS.DataLayer;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using MohawkCollege.Util.Console.Parameters;
using OpenIZ.Core;
using OpenIZ.Core.Extensions;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Acts;
using OpenIZ.Core.Model.Collection;
using OpenIZ.Core.Model.Constants;
using OpenIZ.Core.Model.DataTypes;
using OpenIZ.Core.Model.Entities;
using OpenIZ.Core.Model.EntityLoader;
using OpenIZ.Core.Model.Extensions;
using OpenIZ.Core.Model.Interfaces;
using OpenIZ.Core.Model.Roles;
using OpenIZ.Core.Model.Security;
using OpenIZ.Core.Persistence;
using OpenIZ.Core.Security;
using OpenIZ.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OizDevTool
{
    /// <summary>
    /// Import routines for GIIS
    /// </summary>
    [Description("Tooling for importing data from Generic Immunization Information System (GIIS)")]
    public static class GiisImport
    {

        /// <summary>
        /// Patient import parameters
        /// </summary>
        public class FacilityImportParameters : ConsoleParameters
        {

            /// <summary>
            /// Gets or sets the facility identiifer
            /// </summary>
            [Parameter("facility")]
            [Description("The facility from which data should be extracted")]
            public StringCollection FacilityId { get; set; }

            /// <summary>
            /// Gets or sets the authority
            /// </summary>
            [Parameter("aut")]
            [Description("The assigning authority of barcode IDs")]
            public String BarcodeAuthority { get; set; }
        }

        /// <summary>
        /// Console parameters
        /// </summary>
        public class ConsoleParameters
        {
            /// <summary>
            /// Single transaction
            /// </summary>
            [Description("Instructs the process to run the entire batch as a single execution")]
            [Parameter("tx1")]
            public bool SingleTransaction { get; set; }

            /// <summary>
            /// Live migration
            /// </summary>
            [Parameter("live")]
            [Description("Indicates the migration should be done in place (opposed to generating files)")]
            public bool LiveMigration { get; set; }

            /// <summary>
            /// Output directory
            /// </summary>
            [Parameter("output")]
            [Parameter("o")]
            [Description("The directory in which to output generate files")]
            public String OutputDirectory { get; set; }

        }

        private static Guid industryManufacturer = Guid.NewGuid();
        private static Guid industryHealthDelivery = Guid.NewGuid();
        private static Guid industryOther = Guid.NewGuid();
        private static Dictionary<Int32, Guid> facilityMap = new Dictionary<int, Guid>();
        private static Dictionary<Int32, Guid> facilityTypeId = new Dictionary<int, Guid>();
        private static Dictionary<Int32, Guid> roleMap = new Dictionary<int, Guid>();
        private static Dictionary<Int32, Guid> userMap = new Dictionary<int, Guid>();
        private static Dictionary<Int32, Guid> userEntityMap = new Dictionary<int, Guid>();
        private static Dictionary<Int32, Guid> placeEntityMap = new Dictionary<int, Guid>();
        private static Dictionary<Int32, Guid> manufacturerMap = new Dictionary<int, Guid>();
        private static Dictionary<Int32, Guid> manufacturedMaterialMap = new Dictionary<Int32, Guid>();
        private static Dictionary<Int32, Guid> materialMap = new Dictionary<int, Guid>()
        {
            { 42, Guid.Parse("ED144BD2-A334-40A2-9A8F-B767A1397D07") },
            { 45, Guid.Parse("41B008A6-FCF8-40BC-AB96-7567E94BCF8F") },
            { 48, Guid.Parse("DD53B8FF-2F4D-4359-A031-9405AD384893") },
            { 61, Guid.Parse("6506FA75-9CD9-47DC-9B94-CBD55B6B6C8B") },
            { 46, Guid.Parse("7C5A4FF6-4E81-4C6C-88E9-FC75CE61A4FB") },
            { 60, Guid.Parse("E829C3D1-5243-474E-A2D6-BA35D99610C4") },
            { 59, Guid.Parse("D8049BE9-19D7-4DD8-9DC1-7D8F3886FF97") },
            { 44, Guid.Parse("790BE5CA-D07D-46C6-8FA0-9D4F5ADF388C") }
        };
        private static Dictionary<String, Guid> nonVaccinationReasonMap = new Dictionary<String, Guid>()
        {
            { "0", Guid.Parse("b75bf533-9804-4450-83c7-23f0332f87b8") },
            { "1", Guid.Parse("42351a36-f60f-4687-b334-7a41b091bae1") },
            { "2", Guid.Parse("4ff3617b-bb91-4f3f-b4d2-2425f477037f") },
            { "4", Guid.Parse("c7469fad-f190-40a2-a28d-f97d1863e8cf") },
            { "5", Guid.Parse("9d947e6d-8406-42f3-bb8a-634fb3c81a08") },
        };
        private static Dictionary<Int32, Guid> materialTypeMap = new Dictionary<int, Guid>()
        {
            { 42, Guid.Parse("19AFE679-EF94-48B4-9D6A-3C9827C4C8E2") },
            { 45, Guid.Parse("C20CE9A2-57FD-4FFB-9C12-04A3957D732A") },
            { 48, Guid.Parse("9606ACF1-8A70-4664-944B-87B3750CA7CC") },
            { 61, Guid.Parse("6549730D-EBBE-4700-8052-3DD3F390213F") },
            { 46, Guid.Parse("519AD6CD-2E07-4734-91C0-175242B387B8") },
            { 60, Guid.Parse("1185DEF1-6AD4-4995-B67A-406DB08945B5") },
            { 59, Guid.Parse("7B73529C-4C3D-4720-BB14-FDF8688F7D3B") },
            { 44, Guid.Parse("C7F4980B-E338-4363-83F7-2B3D38933E7E") }
        };

        static Guid GT_MALE = Guid.Empty;
        static Guid GT_FEMALE = Guid.Empty;

        public static readonly Guid PT_TERRITORY = Guid.NewGuid();
        public static readonly Guid PT_REGION = Guid.NewGuid();
        public static readonly Guid PT_DISTRICT = Guid.NewGuid();
        public static readonly Guid PT_DISTRICT_COUNCIL = Guid.NewGuid();
        public static readonly Guid PT_VILLAGE = Guid.NewGuid();

        private static EntityAddress MapAddress(GIIS.DataLayer.Place place)
        {
            var retVal = new EntityAddress();
            retVal.AddressUseKey = AddressUseKeys.Direct;
            if (!String.IsNullOrEmpty(place.Code))
                retVal.Component.Add(new EntityAddressComponent(AddressComponentKeys.CensusTract, place.Code));

            Queue<Guid> addressParts = new Queue<Guid>(new Guid[] {
                AddressComponentKeys.AdditionalLocator,
                AddressComponentKeys.StreetAddressLine,
                AddressComponentKeys.City,
                AddressComponentKeys.County,
                AddressComponentKeys.State,
                AddressComponentKeys.Country
            });

            // Queue places 
            Queue<GIIS.DataLayer.Place> domicileParts = new Queue<GIIS.DataLayer.Place>();
            GIIS.DataLayer.Place current = place;
            while (current != null)
            {
                domicileParts.Enqueue(current);
                current = current.Parent;
            }

            // Now trim
            while (addressParts.Count > domicileParts.Count)
                addressParts.Dequeue();

            // Now map
            while (domicileParts.Count > 0)
                retVal.Component.Add(new EntityAddressComponent(addressParts.Dequeue(), domicileParts.Dequeue().Name));
            return retVal;
        }

        /// <summary>
        /// Map a facility
        /// </summary>
        private static OpenIZ.Core.Model.Entities.ManufacturedMaterial MapMaterial(ItemLot item, DatasetInstall context)
        {
            Guid id = Guid.NewGuid();
            manufacturedMaterialMap.Add(item.Id, id);

            Guid materialId = Guid.Empty;
            if (!materialMap.TryGetValue(item.ItemId, out materialId))
            {
                materialId = Guid.NewGuid();
                Material material = new Material()
                {
                    Key = materialId,
                    ExpiryDate = item.ItemObject.ExitDate,
                    FormConceptKey = Guid.Parse(item.ItemObject.Name == "OPV" || item.ItemObject.Name.ToLower().Contains("rota") ? "66CBCE3A-2E77-401D-95D8-EE0361F4F076" : "9902267C-8F77-4233-BFD3-E6B068AB326A"),
                    DeterminerConceptKey = DeterminerKeys.Described,
                    Identifiers = new List<EntityIdentifier>()
                    {
                        new EntityIdentifier(new AssigningAuthority("GIIS_ITEM", "GIIS Item Identifiers", "1.3.6.1.4.1.<<YOUR.PEN>>.1"), item.ItemId.ToString())
                    },
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.OfficialRecord, item.ItemObject.Name)
                    },
                    StatusConceptKey = item.ItemObject.IsActive ? StatusKeys.Active : StatusKeys.Obsolete
                };
                context.Action.Add(new DataUpdate() { InsertIfNotExists = true, Element = material });
                materialMap.Add(item.ItemId, materialId);
            }

            // Organization map?
            Guid organizationId = Guid.Empty;
            var gtinObject = ItemManufacturer.GetItemManufacturerByGtin(item.Gtin);
            if (gtinObject != null && !manufacturerMap.TryGetValue(gtinObject.ManufacturerId, out organizationId))
            {
                organizationId = Guid.NewGuid();
                Organization organization = new Organization()
                {
                    Key = organizationId,
                    Identifiers = new List<EntityIdentifier>()
                    {
                        new EntityIdentifier(new AssigningAuthority("MANUFACTURER_CODE", "Manufacturer Codes", "1.3.6.1.4.1.<<YOUR.PEN>>.2"), gtinObject .ManufacturerObject.Code),
                        new EntityIdentifier(new AssigningAuthority("GIIS_MANUFACTURER", "GIIS Manufacturer Identifiers", "1.3.6.1.4.1.<<YOUR.PEN>>.3"), gtinObject .ManufacturerId.ToString())
                    },
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.OfficialRecord, gtinObject .ManufacturerObject.Name)
                    },
                    StatusConceptKey = gtinObject.ManufacturerObject.IsActive ? StatusKeys.Active : StatusKeys.Obsolete,
                    IndustryConceptKey = industryManufacturer
                };
                context.Action.Add(new DataUpdate() { InsertIfNotExists = true, Element = organization });
                manufacturerMap.Add(gtinObject.ManufacturerId, organizationId);
            }

            Guid typeConceptKey = Guid.Empty;
            materialTypeMap.TryGetValue(item.ItemId, out typeConceptKey);

            // TODO: Migrate over kit items
            // TODO: Link boxes/vials/doses
            // Core construction of place
            ManufacturedMaterial retVal = new ManufacturedMaterial()
            {
                Key = id,
                TypeConceptKey = typeConceptKey == Guid.Empty ? (Guid?)null : typeConceptKey,
                Relationships = new List<EntityRelationship>()
                {
                    new EntityRelationship(EntityRelationshipTypeKeys.ManufacturedProduct, id) { SourceEntityKey = materialId },
                },
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Assigned, String.Format("{0} ({1})", item.ItemObject.Name, gtinObject?.ManufacturerObject.Name))
                },
                ExpiryDate = item.ExpireDate,
                LotNumber = item.LotNumber,
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("GTIN", "GS1 Global Trade Identification Number (GTIN)", "1.3.160"), item.Gtin),
                    new EntityIdentifier(new AssigningAuthority("GIIS_ITEM_LOT", "GIIS ItemLot Identifiers", "1.3.6.1.4.1.<<YOUR.PEN>>.4"), item.Id.ToString())
                },
                IsAdministrative = false,
                StatusConceptKey = StatusKeys.Active
            };
            if (organizationId != Guid.Empty)
                retVal.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.WarrantedProduct, id) { SourceEntityKey = organizationId });

            return retVal;
        }

        private static Dictionary<Int32, Int32> m_cachedDose = new Dictionary<int, Int32>();

        /// <summary>
        /// Map substance administration
        /// </summary>
        private static List<SubstanceAdministration> MapSubstanceAdministrations(Patient patient, Child child)
        {

            return VaccinationEvent.GetChildVaccinationEvent(child.Id).Where(o => o.NonvaccinationReasonId != 0 || o.VaccinationStatus).AsParallel().Select(o =>
            {

                Int32 matId = 0;
                if (!m_cachedDose.TryGetValue(o.VaccineLotId, out matId))
                {
                    matId = o.Dose.ScheduledVaccination.ItemId;
                    lock(m_cachedDose)
                        if(!m_cachedDose.ContainsKey(o.VaccineLotId))
                            m_cachedDose.Add(o.VaccineLotId, matId);
                }

                try
                {
                    return new SubstanceAdministration()
                    {
                        Key = Guid.NewGuid(),
                        ActTime = o.VaccinationDate,
                        DoseQuantity = 1,
                        Identifiers = new OpenIZ.Core.Model.Collection.VersionedAssociationCollection<ActIdentifier>()
                {
                    new ActIdentifier(new AssigningAuthority("GIIS_VACC", "GIIS Vaccination Events", "1.3.6.1.4.1.<<YOUR.PEN>>.5"), o.Id.ToString())
                },
                        IsNegated = o.NonvaccinationReasonId != 0,
                        ReasonConceptKey = o.NonvaccinationReasonId > 0 ? (Guid?)nonVaccinationReasonMap[o.NonVaccinationReason.Code] : null,
                        MoodConceptKey = ActMoodKeys.Eventoccurrence,
                        SequenceId = o.Dose?.DoseNumber ?? 0,
                        TypeConceptKey = Guid.Parse("6e7a3521-2967-4c0a-80ec-6c5c197b2178"),
                        RouteKey = NullReasonKeys.NoInformation,
                        DoseUnitKey = Guid.Parse("A77B8D83-1CC9-4806-A268-5D1738154AFA"),
                        Participations = new OpenIZ.Core.Model.Collection.VersionedAssociationCollection<ActParticipation>()
                {
                    new ActParticipation(ActParticipationKey.Location, facilityMap[o.HealthFacilityId]),
                    new ActParticipation(ActParticipationKey.RecordTarget, patient.Key.Value),
                    o.VaccineLotId <= 0 ? null : new ActParticipation(ActParticipationKey.Consumable, manufacturedMaterialMap[o.VaccineLotId]),
                    o.Dose?.ScheduledVaccination?.ItemId == null ? null : new ActParticipation(ActParticipationKey.Product, materialMap[matId]),
                    userEntityMap[o.ModifiedBy] != Guid.Empty ? new ActParticipation(ActParticipationKey.Authororiginator, userEntityMap[o.ModifiedBy]) : null
                }
                    };
                }
                catch(Exception e)
                {
                    Console.Error.WriteLine("Error: Data consistency issue on vaccination record {0}, skipping", o.Id);
                    return null;
                }
            }).ToList();

        }

        /// <summary>
        /// Map weights
        /// </summary>
        private static List<QuantityObservation> MapWeights(Patient patient, Child child)
        {

            return ChildWeight.GetChildWeightByChildId(child.Id).Select(o => new QuantityObservation()
            {
                Key = Guid.NewGuid(),
                ActTime = o.Date,
                MoodConceptKey = ActMoodKeys.Eventoccurrence,
                TypeConceptKey = Guid.Parse("a261f8cd-69b0-49aa-91f4-e6d3e5c612ed"),
                Value = (decimal)o.Weight,
                UnitOfMeasureKey = Guid.Parse("a0a8d4db-db72-4bc7-9b8c-c07cef7bc796"),
                Participations = new OpenIZ.Core.Model.Collection.VersionedAssociationCollection<ActParticipation>()
                {
                    new ActParticipation(ActParticipationKey.RecordTarget, patient.Key.Value),
                    userEntityMap[o.ModifiedBy] != Guid.Empty ? new ActParticipation(ActParticipationKey.Authororiginator, userEntityMap[o.ModifiedBy]) : null
                }
            }).ToList();

        }
        /// <summary>
        /// Map child
        /// </summary>
        private static Patient MapChild(Child child, AssigningAuthority barcodeAut)
        {
            Guid id = Guid.NewGuid();

            // Create core attributes
            var retVal = new Patient()
            {
                Key = id,
                Addresses = child.Domicile == null ? null : new List<EntityAddress>() { MapAddress(child.Domicile) },
                DateOfBirth = child.Birthdate,
                DateOfBirthPrecision = DatePrecision.Day,
                DeceasedDate = child.Status.Name == "Dead" ? (DateTime?)DateTime.MinValue : null,
                GenderConceptKey = child.Gender ? GT_MALE : GT_FEMALE,
                Identifiers = new List<EntityIdentifier>()
                {
                    new OpenIZ.Core.Model.DataTypes.EntityIdentifier(new AssigningAuthority("GIIS_CHILD", "GIIS Child Identifiers", "1.3.6.1.4.1.<<YOUR.PEN>>.6"), child.Id.ToString()),
                    new OpenIZ.Core.Model.DataTypes.EntityIdentifier(barcodeAut, child.BarcodeId)
                },
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, child.Lastname1, child.Firstname1, child.Firstname2)
                },
                StatusConceptKey = child.IsActive ? StatusKeys.Active : StatusKeys.Obsolete,
                Telecoms = new List<EntityTelecomAddress>()
                {
                    new EntityTelecomAddress(TelecomAddressUseKeys.MobileContact, child.Mobile)
                },
                Tags = new List<EntityTag>()
                {
                    new EntityTag("http://openiz.org/tags/contrib/importedData", "true")
                }
            };

            // Child health centre
            var hfAssigned = facilityMap[child.HealthcenterId];
            retVal.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.DedicatedServiceDeliveryLocation, hfAssigned));

            // Mother? 
            if (!String.IsNullOrEmpty(child.MotherFirstname))
            {
                retVal.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.Mother, new Person()
                {
                    Key = Guid.NewGuid(),
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.Legal, child.MotherLastname, child.MotherFirstname)
                    },
                    Telecoms = new List<EntityTelecomAddress>()
                    {
                    new EntityTelecomAddress(TelecomAddressUseKeys.MobileContact, child.Mobile)
                    }
                }));
                if (!String.IsNullOrEmpty(child.MotherId))
                    retVal.Relationships.Last().TargetEntity.Identifiers = new List<EntityIdentifier>()
                    {
                        new OpenIZ.Core.Model.DataTypes.EntityIdentifier(new AssigningAuthority("GIIS_MOTHER", "GIIS Mother Identifiers", "1.3.6.1.4.1.<<YOUR.PEN>>.7"), child.MotherId),
                    };
            }
            if (!String.IsNullOrEmpty(child.FatherFirstname))
            {
                retVal.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.Father, new Person()
                {
                    Key = Guid.NewGuid(),
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.Legal, child.FatherLastname, child.FatherFirstname)
                    },
                    Telecoms = new List<EntityTelecomAddress>()
                    {
                    new EntityTelecomAddress(TelecomAddressUseKeys.MobileContact, child.Mobile)
                    }
                }));
            }
            if (!String.IsNullOrEmpty(child.CaretakerFirstname))
            {
                retVal.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.NextOfKin, new Person()
                {
                    Key = Guid.NewGuid(),
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.Legal, child.CaretakerLastname, child.CaretakerFirstname)
                    },
                    Telecoms = new List<EntityTelecomAddress>()
                    {
                    new EntityTelecomAddress(TelecomAddressUseKeys.MobileContact, child.Mobile)
                    }
                }));
                if (!String.IsNullOrEmpty(child.CaretakerId))
                    retVal.Relationships.Last().TargetEntity.Identifiers = new List<EntityIdentifier>()
                    {
                        new OpenIZ.Core.Model.DataTypes.EntityIdentifier(new AssigningAuthority("GIIS_CARETAKER", "GIIS Caretaker Identifiers", "1.3.6.1.4.1.<<YOUR.PEN>>.8"), child.CaretakerId),
                    };
            }

            if (child.Birthplace != null)
                retVal.Extensions.Add(new EntityExtension()
                {
                    ExtensionType = new ExtensionType("http://openiz.org/extensions/patient/contrib/timr/birthPlaceType", typeof(DictionaryExtensionHandler))
                    {
                        Key = Guid.Parse("25dfb527-d3ab-4a97-8171-316086ad3f74")
                    },
                    ExtensionValue = child.Birthplace.Name
                });

            return retVal;
        }

        /// <summary>
        /// Map a facility
        /// </summary>
        private static OpenIZ.Core.Model.Entities.Place MapFacility(HealthFacility hf)
        {
            Guid id = Guid.NewGuid();
            if (facilityMap.ContainsKey(hf.Id))
                id = facilityMap[hf.Id];
            else
                facilityMap.Add(hf.Id, id);

            var hfcd = HealthFacilityCohortData.GetHealthFacilityCohortDataByHealthFacilityId(hf.Id);
            // Core construction of place
            var retVal = new OpenIZ.Core.Model.Entities.Place()
            {
                Key = id,
                ClassConceptKey = EntityClassKeys.ServiceDeliveryLocation,
                TypeConceptKey = facilityTypeId[hf.TypeId],
                Names = new List<EntityName>() { new EntityName(NameUseKeys.OfficialRecord, hf.Name) },
                Identifiers = new List<OpenIZ.Core.Model.DataTypes.EntityIdentifier>()
                {
                    new OpenIZ.Core.Model.DataTypes.EntityIdentifier(new AssigningAuthority("GIIS_FACID", "GIIS Facility Identifiers", "1.3.6.1.4.1.<<YOUR.PEN>>.9"), hf.Id.ToString()),
                    new OpenIZ.Core.Model.DataTypes.EntityIdentifier(new AssigningAuthority("HIE_FRID", "Facility Register Identifiers", "1.3.6.1.4.1.<<YOUR.PEN>>.10"), hf.Code)
                },
                StatusConceptKey = hf.IsActive ? StatusKeys.Active : StatusKeys.Nullfied,
                Extensions = new List<EntityExtension>()
                {
                    new EntityExtension() {
                        ExtensionType = new ExtensionType("http://openiz.org/extensions/contrib/GIIS/isleaf", typeof(BooleanExtensionHandler)) {
                            Key = Guid.Parse("19449384-ba34-4b31-abc2-65e83032b794"),
                        },
                        ExtensionValue = hf.Leaf
                    },
                    new EntityExtension() {
                        ExtensionType = new ExtensionType("http://openiz.org/extensions/contrib/GIIS/isVaccinationPoint", typeof(BooleanExtensionHandler)) {
                            Key = Guid.Parse("19449384-ba34-4b31-abc2-65e83032b743"),
                        },
                        ExtensionValue = hf.VaccinationPoint
                    },
                    new EntityExtension() {
                        ExtensionType = new ExtensionType("http://openiz.org/extensions/contrib/GIIS/vaccineStore", typeof(BooleanExtensionHandler)) {
                            Key = Guid.Parse("19449384-ba34-4b31-abc2-65e83032b768"),
                        },
                        ExtensionValue = hf.VaccineStore
                    },
                    new EntityExtension()
                    {
                        ExtensionType = new ExtensionType("http://openiz.org/extensions/contrib/GIIS/coldStorageCapacity", typeof(DecimalExtensionHandler))
                        {
                            Key = Guid.Parse("19449384-ba34-4b31-abc2-65e83032b79d"),
                        },
                        ExtensionValue = (Decimal)hf.ColdStorageCapacity
                    },
                    new EntityExtension()
                    {
                        ExtensionType = new ExtensionType("http://openiz.org/extensions/contrib/bid/targetPopulation", typeof(DecimalExtensionHandler))
                        {
                            Key = Guid.Parse("f9552ed8-66aa-4644-b6a8-108ad54f2476")
                        },
                        ExtensionValue = (Decimal)(hfcd?.Cohort ?? 0)
                    }
                },
                Tags = new List<EntityTag>()
                            {
                                new EntityTag("http://openiz.org/tags/contrib/importedData", "true")
                            }
            };

            if (hf.ParentId != 0)
                retVal.Relationships = new List<EntityRelationship>()
                {
                    new EntityRelationship(EntityRelationshipTypeKeys.Parent, new Entity() { Key = facilityMap[hf.ParentId] })
                };

            // Health facility stock policy
            // Stock balances
            List<dynamic> stockPolicyObject = new List<dynamic>();

            foreach (var itm in HealthFacilityBalance.GetHealthFacilityBalanceByHealthFacility(hf.Id))
            {
                var stockPolicy = GtinHfStockPolicy.GetGtinHfStockPolicyByHealthFacilityCodeAndGtin(hf.Code, itm.Gtin);

                // first add stock relationship
                var itml = ItemLot.GetItemLotByGtinAndLotNo(itm.Gtin, itm.LotNumber);
                var mmat = manufacturedMaterialMap[itml.Id];

                retVal.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.OwnedEntity, mmat) { Quantity = (int)0 });
                stockPolicyObject.Add(new
                {
                    ManufacturedMaterialEntityId = mmat,
                    ReorderQuantity = stockPolicy?.ReorderQty,
                    SafetyQuantity = stockPolicy?.SafetyStock
                });
            }

            // Entity extension
            retVal.Extensions.Add(new EntityExtension()
            {
                ExtensionType = new ExtensionType("http://openiz.org/extensions/contrib/bid/stockPolicy", typeof(DictionaryExtensionHandler))
                {
                    Key = Guid.Parse("DFCA3C81-A3C4-4C82-A901-8BC576DA307C")
                },
                ExtensionValue = stockPolicyObject
            });

            // TODO: Fix author key needing to be present in DB
            /*
            if (!String.IsNullOrEmpty(hf.Notes))
            {
                if(!userEntityMap.ContainsKey(hf.ModifiedBy))
                    userEntityMap.Add(hf.ModifiedBy, Guid.NewGuid());
                retVal.Notes.Add(new EntityNote()
                {
                    AuthorKey = userEntityMap[hf.ModifiedBy],
                    Text = hf.Notes
                });
            }*/
            return retVal;
        }

        /// <summary>
        /// Map a facility
        /// </summary>
        private static OpenIZ.Core.Model.Entities.Place MapPlace(GIIS.DataLayer.Place plc)
        {
            Guid id = Guid.NewGuid();
            placeEntityMap.Add(plc.Id, id);

            Guid classCode = EntityClassKeys.Place, typeCode = PT_VILLAGE;
            switch (plc.Code.Length)
            {
                case 2: // EX: "TZ"
                    classCode = EntityClassKeys.Country;
                    typeCode = EntityClassKeys.Country;
                    break;
                case 5: // EX: TZ.NT
                    classCode = EntityClassKeys.Place;
                    typeCode = PT_TERRITORY;
                    break;
                case 8: // EX: TZ.NT.AS
                    classCode = EntityClassKeys.State;
                    typeCode = PT_REGION;
                    break;
                case 11: // EX: TZ.NT.AS.AM
                    classCode = EntityClassKeys.CountyOrParish;
                    typeCode = PT_DISTRICT;
                    break;
                default:
                    if (plc.Code.Length <= 14)
                    {
                        classCode = EntityClassKeys.CityOrTown;
                        typeCode = PT_DISTRICT_COUNCIL;
                    }
                    else
                    {
                        classCode = EntityClassKeys.Place;
                        typeCode = PT_VILLAGE;
                    }
                    break;
            }


            // Core construction of place
            var retVal = new OpenIZ.Core.Model.Entities.Place()
            {
                Key = id,
                ClassConceptKey = classCode,
                TypeConceptKey = typeCode,
                Names = new List<EntityName>() { new EntityName(NameUseKeys.OfficialRecord, plc.Name) },
                Identifiers = new List<OpenIZ.Core.Model.DataTypes.EntityIdentifier>()
                {
                    new OpenIZ.Core.Model.DataTypes.EntityIdentifier(new AssigningAuthority("GIIS_PLCID", "GIIS Place Identifiers", "1.3.6.1.4.1.<<YOUR.PEN>>.11"), plc.Id.ToString()),
                    new OpenIZ.Core.Model.DataTypes.EntityIdentifier(new AssigningAuthority("HIE_VRID", "Village Register Codes", "1.3.6.1.4.1.<<YOUR.PEN>>.12"), plc.Code)
                },
                StatusConceptKey = plc.IsActive ? StatusKeys.Active : StatusKeys.Nullfied,
                Extensions = new List<EntityExtension>()
                {
                    new EntityExtension() {
                        ExtensionType = new ExtensionType("http://openiz.org/extensions/contrib/GIIS/isleaf", typeof(BooleanExtensionHandler)) {
                            Key = Guid.Parse("19449384-ba34-4b31-abc2-65e83032b794"),
                        },
                        ExtensionValue = plc.Leaf
                    }
                },
                Addresses = new List<EntityAddress>()
                {
                    MapAddress(plc)
                },
                Tags = new List<EntityTag>()
                            {
                                new EntityTag("http://openiz.org/tags/contrib/importedData", "true")
                            }
            };

            if (plc.ParentId != 0)
                retVal.Relationships = new List<EntityRelationship>()
                {
                    new EntityRelationship(EntityRelationshipTypeKeys.Parent, new Entity() { Key = placeEntityMap[plc.ParentId] })
                };
            if (plc.HealthFacilityId != null && plc.HealthFacilityId != 0)
                retVal.Relationships = new List<EntityRelationship>()
                {
                    new EntityRelationship(EntityRelationshipTypeKeys.DedicatedServiceDeliveryLocation, new Entity() { Key = facilityMap[plc.HealthFacilityId.Value] })
                };
            // TODO: Fix author key needing to be present in DB
            /*
            if (!String.IsNullOrEmpty(hf.Notes))
            {
                if(!userEntityMap.ContainsKey(hf.ModifiedBy))
                    userEntityMap.Add(hf.ModifiedBy, Guid.NewGuid());
                retVal.Notes.Add(new EntityNote()
                {
                    AuthorKey = userEntityMap[hf.ModifiedBy],
                    Text = hf.Notes
                });
            }*/
            return retVal;
        }

        [Description("Extracts patients from a particular facility and maps them to OpenIZ IMS format")]
        [ParameterClass(typeof(FacilityImportParameters))]
        [Example("Extract patients from facility 12943 and 12944", @"--facility=12943 --facility=12944 --output=C:\data\giis")]
        public static void ImportFacilityData(String[] args)
        {

            var parms = new ParameterParser<FacilityImportParameters>().Parse(args);

            Console.WriteLine("Cleaning GIIS Data");
            DBManager.ExecuteNonQueryCommand("UPDATE \"CHILD\" SET \"MODIFIED_ON\" = CURRENT_TIMESTAMP, \"MODIFIED_BY\" = 1 WHERE \"MODIFIED_ON\" = '-infinity'", System.Data.CommandType.Text, new List<Npgsql.NpgsqlParameter>());
            DBManager.ExecuteNonQueryCommand("UPDATE \"VACCINATION_EVENT\" SET \"MODIFIED_ON\" = CURRENT_TIMESTAMP, \"MODIFIEDON\" = CURRENT_TIMESTAMP, \"MODIFIED_BY\" = 1 WHERE \"MODIFIED_ON\" = '-infinity' OR \"MODIFIEDON\" = '-infinity'", System.Data.CommandType.Text, new List<Npgsql.NpgsqlParameter>());
            DBManager.ExecuteNonQueryCommand("UPDATE \"CHILD_WEIGHT\" SET \"MODIFIED_ON\" = CURRENT_TIMESTAMP, \"MODIFIED_BY\" = 1 WHERE \"MODIFIED_ON\" = '-infinity'", System.Data.CommandType.Text, new List<Npgsql.NpgsqlParameter>());
            //DBManager.ExecuteNonQueryCommand("UPDATE \"CHILD\" SET \"PREVLOGIN\" = NULL WHERE \"PREVLOGIN\" = '-infinity'", System.Data.CommandType.Text, new List<Npgsql.NpgsqlParameter>());

            // Step 1: Load mapping arrays
            Console.WriteLine("Loading GIIS <> IMS Maps");
            ApplicationServiceContext.Current = ApplicationContext.Current;
            //cp.Repository = new SeederProtocolRepositoryService();
            ApplicationContext.Current.Start();
            var placePersister = ApplicationContext.Current.GetService<IDataPersistenceService<OpenIZ.Core.Model.Entities.Place>>();
            var bundlePersister = ApplicationContext.Current.GetService<IDataPersistenceService<Bundle>>();
            var materialPersister = ApplicationContext.Current.GetService<IDataPersistenceService<ManufacturedMaterial>>();
            var conceptPersister = ApplicationContext.Current.GetService<IDataPersistenceService<Concept>>();
            var uePersister = ApplicationContext.Current.GetService<IDataPersistenceService<UserEntity>>();
            var barcodeAut = ApplicationContext.Current.GetService<IDataPersistenceService<AssigningAuthority>>().Query(o => o.DomainName == parms.BarcodeAuthority, AuthenticationContext.SystemPrincipal).FirstOrDefault();

            if (barcodeAut == null)
            {
                Console.Error.WriteLine("Authority {0} not found", parms.BarcodeAuthority);
                return;
            }

            // Now load the places from the database and load materials
            int tr = 0;
            Console.WriteLine("Load OpenIZ Villages...");
            var places = (placePersister as IFastQueryDataPersistenceService<OpenIZ.Core.Model.Entities.Place>).QueryFast(o => o.ClassConceptKey != EntityClassKeys.ServiceDeliveryLocation && o.Identifiers.Any(i => i.Authority.DomainName == "GIIS_PLCID"), Guid.Empty, 0, null, AuthenticationContext.AnonymousPrincipal, out tr);
            Console.WriteLine("Load OpenIZ Facilities...");
            var facilities = (placePersister as IFastQueryDataPersistenceService<OpenIZ.Core.Model.Entities.Place>).QueryFast(o => o.Identifiers.Any(i => i.Authority.DomainName == "GIIS_FACID"), Guid.Empty, 0, null, AuthenticationContext.SystemPrincipal, out tr);
            Console.WriteLine("Load OpenIZ Materials...");
            var materials = (materialPersister as IFastQueryDataPersistenceService<OpenIZ.Core.Model.Entities.ManufacturedMaterial>).QueryFast(o => o.Identifiers.Any(i => i.Authority.DomainName == "GIIS_ITEM_LOT"), Guid.Empty, 0, null, AuthenticationContext.AnonymousPrincipal, out tr);

            Console.WriteLine("Map OpenIZ Users...");
            userEntityMap = User.GetUserList().ToDictionary(o => o.Id, o => uePersister.Query(u => u.SecurityUser.UserName == o.Username, AuthenticationContext.AnonymousPrincipal).FirstOrDefault()?.Key.Value ?? Guid.Empty);

            GT_MALE = conceptPersister.Query(o => o.Mnemonic.Contains("male"), AuthenticationContext.SystemPrincipal).FirstOrDefault().Key.Value;
            GT_FEMALE = conceptPersister.Query(o => o.Mnemonic.Contains("male"), AuthenticationContext.SystemPrincipal).FirstOrDefault().Key.Value;

            // Load materials and places
            //var pfacilities = facilities.ToDictionary(o => Int32.Parse(o.Identifiers.First(i => i.Authority.DomainName == "GIIS_FACID").Value), o => o.Key.Value);

            Console.WriteLine("Map OpenIZ Places...");
            placeEntityMap = places.ToDictionary(o => Int32.Parse(o.Identifiers.First(i => i.Authority.DomainName == "GIIS_PLCID").Value), o => o.Key.Value);
            facilityMap = facilities.ToDictionary(o => Int32.Parse(o.Identifiers.First(i => i.Authority.DomainName == "GIIS_FACID").Value), o => o.Key.Value);
            facilityTypeId = conceptPersister.Query(o => o.ConceptSets.Any(c => c.Mnemonic == "HealthFacilityTypes"), AuthenticationContext.SystemPrincipal).ToDictionary(o => HealthFacilityType.GetHealthFacilityTypeList().First(t => o.Mnemonic.EndsWith(t.Name.Replace(" ", ""))).Id, o => o.Key.Value);

            Console.WriteLine("Map OpenIZ Materials...");
            manufacturedMaterialMap = materials.ToDictionary(o => Int32.Parse(o.Identifiers.First(i => i.Authority.DomainName == "GIIS_ITEM_LOT").Value), o => o.Key.Value);

            DatasetInstall resultSet = new DatasetInstall() { Id = $"Ad-hoc GIIS import {String.Join(":", parms.FacilityId)}" };

            foreach (var facId in parms.FacilityId)
            {
                HealthFacility giisHf = HealthFacility.GetHealthFacilityById(Int32.Parse(facId));
                if (giisHf == null)
                {
                    Console.Error.WriteLine("Facility {0} not found!!!", facId);
                    continue;
                }
                var children = Child.GetChildByHealthFacilityId(giisHf.Id);

                var dbFacility = placePersister.Query(o => o.Identifiers.Any(i => i.Value == facId && i.Authority.DomainName == "GIIS_FACID"), AuthenticationContext.AnonymousPrincipal).FirstOrDefault();

                if (dbFacility == null)
                {
                    Console.WriteLine("Will create facility {0} ({1} patients)...", giisHf.Name, children.Count);
                    dbFacility = MapFacility(giisHf);
                    // Insert
                    if (parms.LiveMigration)
                        placePersister.Insert(dbFacility, AuthenticationContext.SystemPrincipal, TransactionMode.Commit);
                }
                else
                {
                    Console.WriteLine("Will update facility {0} ({1} patients)...", giisHf.Name, children.Count);

                    // Clear and update
                    dbFacility.Relationships.Clear();
                    var bkup = MapFacility(giisHf);
                    dbFacility.Relationships = bkup.Relationships;
                    dbFacility.Extensions = bkup.Extensions;

                    // Insert
                    if (parms.LiveMigration && !parms.SingleTransaction)
                        placePersister.Update(dbFacility, AuthenticationContext.SystemPrincipal, TransactionMode.Commit);

                }

                // Result set
                if (!parms.LiveMigration || parms.SingleTransaction)
                    resultSet.Action.Add(new DataUpdate()
                    {
                        InsertIfNotExists = true,
                        Element = dbFacility
                    });

                TimeSpan remaining = TimeSpan.MaxValue;

                // Now map the children... think of the children!!!!!
                var start = DateTime.Now;
                for (int i = 0; i < children.Count; i++)
                {
                    //remainSw.Stop();
                    var ips = (((double)(DateTime.Now - start).Ticks / i) * (children.Count - i));
                    remaining = new TimeSpan((long)ips);
                    //remainSw.Start();

                    var chld = children[i];
                    var dbChild = MapChild(chld, barcodeAut);
                    Console.CursorLeft = 0;
                    float pdone = i / (float)children.Count;

                    int bdone = (int)(pdone * (Console.WindowWidth - 55) / 2);
                    Console.Write("  Child: {0} > {1} [{2}{3}] [{4:#0}% - ETA:{5}] ", chld.Id, dbChild.Key,
                        Console.WindowWidth - 55 > 2 ? new String('=', bdone) : "",
                        Console.WindowWidth - 55 > 2 ? new String(' ', (int)(((Console.WindowWidth - 55) / 2) - bdone)) : "",
                        pdone * 100, remaining.ToString("mm'm 'ss's'"));


                    List<IdentifiedData> childData = new List<IdentifiedData>();
                    childData.AddRange(dbChild.Relationships.Where(o => o.TargetEntity != null).Select(o => o.TargetEntity));
                    childData.Add(dbChild);
                    childData.AddRange(MapSubstanceAdministrations(dbChild, chld));
                    childData.AddRange(MapWeights(dbChild, chld));

                    if (!parms.LiveMigration || parms.SingleTransaction)
                        resultSet.Action.AddRange(childData.Select(o => new DataInsert() { Element = o }));
                    else
                    {
                        bundlePersister.Insert(new Bundle() { Item = childData }, AuthenticationContext.SystemPrincipal, TransactionMode.Commit);
                    }

                }
                Console.WriteLine();
            }

            if (!parms.LiveMigration)
            {

                XmlSerializer xsz = new XmlSerializer(typeof(DatasetInstall));
                using (var fs = File.Create(Path.Combine(parms.OutputDirectory, $"999-Facility-{String.Join("-", parms.FacilityId.OfType<String>().ToArray())}.dataset")))
                    xsz.Serialize(fs, resultSet);
            }
            else
            {
                Bundle b = new Bundle() { Item = resultSet.Action.Select(o => o.Element).ToList() };
                Console.WriteLine("Applying database {0} changes in single transaction...", b.Item.Count);

                var start = DateTime.Now;
                TimeSpan remaining = TimeSpan.MaxValue;

                // Progress has changed
                if (bundlePersister is IReportProgressChanged)
                    (bundlePersister as IReportProgressChanged).ProgressChanged += (o, e) =>
                    {
                        if (e.ProgressPercentage % 2 == 0)
                        {
                            var ips = ((double)(DateTime.Now - start).Ticks / e.ProgressPercentage) * (100 - e.ProgressPercentage);
                            remaining = new TimeSpan((long)ips);
                        }

                        float pdone = (float)e.ProgressPercentage / 100;
                        Console.CursorLeft = 0;

                        int bdone = (int)(pdone * (Console.WindowWidth - 55) / 2);
                        Console.Write("  DB: {0} [{1}{2}] [{3:#0}% - ETA:{4}] ", (e.UserState as IIdentifiedEntity)?.Key.Value,
                               Console.WindowWidth - 55 > 2 ? new String('=', bdone) : "",
                               Console.WindowWidth - 55 > 2 ? new String(' ', (int)(((Console.WindowWidth - 55) / 2)-bdone)) : "",
                               pdone * 100, remaining.ToString("mm'm 'ss's'"));
                    };
                bundlePersister.Insert(b, AuthenticationContext.SystemPrincipal, TransactionMode.Commit);
            }

        }

        /// <summary>
        /// Imports the Core data
        /// </summary>
        [Description("Extracts the core data from GIIS (Places, Facilities, Materials, Lots, etc.). Note: Due to a limitation of the GIIS data layer API, there must be a configuration connection string called GIIS in the oizdt.exe.config file")]
        [ParameterClass(typeof(ConsoleParameters))]
        [Example("Extract all core data from connection to a directory", @"--output=C:\data\giis")]
        [Example("Extract all core data from connection and push into OpenIZ Database", "--live")]
        public static void ImportCoreData(string[] args)
        {

            ConsoleParameters parms = new ParameterParser<ConsoleParameters>().Parse(args);

            // Concepts
            Console.WriteLine("Generating OpenIZ Concepts to support GIIS data");
            DatasetInstall conceptDataset = new DatasetInstall() { Id = "Concepts to support GIIS data", Action = new List<DataInstallAction>() };
            DataInsert healthFacilityTypes = new DataInsert()
            {
                Element = new ConceptSet()
                {
                    Key = Guid.NewGuid(),
                    Mnemonic = "HealthFacilityTypes",
                    Oid = "1.3.6.1.4.1.<<YOUR.PEN>>.13",
                    Name = "Health Facility Types",
                    Url = "http://ivd.moh.go.tz/valueset/timr/HealthFacilityTypes"
                },
                Association = new List<DataAssociation>()
            },
            placeTypes = new DataInsert()
            {
                Element = new ConceptSet()
                {
                    Key = Guid.NewGuid(),
                    Mnemonic = "PlaceTypes",
                    Oid = "1.3.6.1.4.1.<<YOUR.PEN>>.14",
                    Name = "Place Sub-Classifications",
                    Url = "http://openiz.org/valueset/timr/PlaceTypes"
                },
                Association = new List<DataAssociation>()
                {
                    new DataAssociation()
                    {
                        PropertyName = "Concepts",
                        Element = new Concept()
                        {
                            Key = PT_DISTRICT,
                            StatusConceptKey = StatusKeys.Active,
                            IsSystemConcept = false,
                            ClassKey = ConceptClassKeys.Other,
                            Mnemonic = "PlaceType-District",
                            ConceptNames = new List<ConceptName>()
                            {
                                new ConceptName()
                                {
                                    Language = "en",
                                    Name = "District"
                                }
                            }

                        }
                    },
                    new DataAssociation()
                    {
                        PropertyName = "Concepts",
                        Element = new Concept()
                        {
                            Key = PT_DISTRICT_COUNCIL,
                            StatusConceptKey = StatusKeys.Active,
                            IsSystemConcept = false,
                            ClassKey = ConceptClassKeys.Other,
                            Mnemonic = "PlaceType-DistrictCouncil",
                            ConceptNames = new List<ConceptName>()
                            {
                                new ConceptName()
                                {
                                    Language = "en",
                                    Name = "District Council"
                                }
                            }

                        }
                    },
                    new DataAssociation()
                    {
                        PropertyName = "Concepts",
                        Element = new Concept()
                        {
                            Key = PT_REGION,
                            StatusConceptKey = StatusKeys.Active,
                            IsSystemConcept = false,
                            ClassKey = ConceptClassKeys.Other,
                            Mnemonic = "PlaceType-Region",
                            ConceptNames = new List<ConceptName>()
                            {
                                new ConceptName()
                                {
                                    Language = "en",
                                    Name = "Region"
                                }
                            }

                        }
                    },
                    new DataAssociation()
                    {
                        PropertyName = "Concepts",
                        Element = new Concept()
                        {
                            Key = PT_TERRITORY,
                            StatusConceptKey = StatusKeys.Active,
                            IsSystemConcept = false,
                            ClassKey = ConceptClassKeys.Other,
                            Mnemonic = "PlaceType-Territory",
                            ConceptNames = new List<ConceptName>()
                            {
                                new ConceptName()
                                {
                                    Language = "en",
                                    Name = "Territory"
                                }
                            }

                        }
                    },
                                        new DataAssociation()
                    {
                        PropertyName = "Concepts",
                        Element = new Concept()
                        {
                            Key = PT_VILLAGE,
                            StatusConceptKey = StatusKeys.Active,
                            IsSystemConcept = false,
                            ClassKey = ConceptClassKeys.Other,
                            Mnemonic = "PlaceType-Village",
                            ConceptNames = new List<ConceptName>()
                            {
                                new ConceptName()
                                {
                                    Language = "en",
                                    Name = "Village"
                                }
                            }

                        }
                    }
                }
            };



            var materialDataset = new DatasetInstall() { Id = "Manufactured Materials from GIIS" };
            foreach (var il in ItemLot.GetItemLotList())
            {
                var itm = MapMaterial(il, materialDataset);
                materialDataset.Action.Add(new DataUpdate() { InsertIfNotExists = true, Element = itm });
            }

            foreach (var itm in HealthFacilityType.GetHealthFacilityTypeList().OrderBy(o => o.Id))
            {
                facilityTypeId.Add(itm.Id, Guid.NewGuid());

                healthFacilityTypes.Association.Add(
                    new DataAssociation()
                    {
                        PropertyName = "Concepts",
                        Element = new Concept()
                        {
                            Key = facilityTypeId[itm.Id],
                            StatusConceptKey = StatusKeys.Active,
                            IsSystemConcept = false,
                            ClassKey = ConceptClassKeys.Other,
                            Mnemonic = "Facility-" + itm.Name.Replace(" ", ""),
                            ConceptNames = new List<ConceptName>()
                            {
                                new ConceptName()
                                {
                                    Language = "en",
                                    Name = itm.Name
                                }
                            }

                        }
                    }
                );
            }


            (healthFacilityTypes.Element as ConceptSet).ConceptsXml = healthFacilityTypes.Association.Select(o => o.Element.Key.Value).ToList();
            (placeTypes.Element as ConceptSet).ConceptsXml = placeTypes.Association.Select(o => o.Element.Key.Value).ToList();
            conceptDataset.Action.AddRange(healthFacilityTypes.Association.Select(o => new DataInsert() { Element = o.Element }));
            conceptDataset.Action.AddRange(placeTypes.Association.Select(o => new DataInsert() { Element = o.Element }));
            conceptDataset.Action.AddRange(new DataInstallAction[]
            {
                new DataInsert() { Element = new Concept() { Key = industryManufacturer, Mnemonic = "Industry-Manufacturing", ClassKey = ConceptClassKeys.Other, IsSystemConcept = false, StatusConceptKey = StatusKeys.Active, ConceptNames = new List<ConceptName>() { new ConceptName() { Language = "en", Name = "Manufacturing"  } }, ConceptSetsXml = new List<Guid>() { Guid.Parse("d1597e50-845a-46e1-b9ae-6f99ff93d9db") } } },
                new DataInsert() { Element = new Concept() { Key = industryOther, Mnemonic = "Industry-OtherUnknown", ClassKey = ConceptClassKeys.Other, IsSystemConcept = false, StatusConceptKey = StatusKeys.Active, ConceptNames = new List<ConceptName>() { new ConceptName() { Language = "en", Name = "Other/Unknown"  } } , ConceptSetsXml = new List<Guid>() { Guid.Parse("d1597e50-845a-46e1-b9ae-6f99ff93d9db") } } },
                new DataInsert() { Element = new Concept() { Key = industryHealthDelivery, Mnemonic = "Industry-HealthDelivery", ClassKey = ConceptClassKeys.Other, IsSystemConcept = false, StatusConceptKey = StatusKeys.Active , ConceptNames = new List<ConceptName>() { new ConceptName() { Language = "en", Name = "Healthcare"  } } , ConceptSetsXml = new List<Guid>() { Guid.Parse("d1597e50-845a-46e1-b9ae-6f99ff93d9db") } } }
            });
            healthFacilityTypes.Association.Clear();
            placeTypes.Association.Clear();
            conceptDataset.Action.Add(healthFacilityTypes);
            conceptDataset.Action.Add(placeTypes);

            // Facilities
            Console.WriteLine("Exporting GIIS Facilities to OpenIZ IMS Format");
            DatasetInstall facilityDataset = new DatasetInstall() { Action = new List<DataInstallAction>() };
            facilityDataset.Id = "Facilities from GIIS";
            foreach (var itm in HealthFacility.GetHealthFacilityList().OrderBy(o => o.Id))
                facilityDataset.Action.Add(new DataInsert()
                {
                    Element = MapFacility(itm)
                });

            // Places
            Console.WriteLine("Exporting GIIS Places to OpenIZ IMS Format");
            DatasetInstall placeDataset = new DatasetInstall() { Action = new List<DataInstallAction>() };
            placeDataset.Id = "Places from GIIS";
            foreach (var itm in GIIS.DataLayer.Place.GetPlaceList().OrderBy(o => o.ParentId))
                placeDataset.Action.Add(new DataInsert()
                {
                    Element = MapPlace(itm)
                });

            DBManager.ExecuteNonQueryCommand("UPDATE \"USER\" SET \"LASTLOGIN\" = NULL WHERE \"LASTLOGIN\" = '-infinity'", System.Data.CommandType.Text, new List<Npgsql.NpgsqlParameter>());
            DBManager.ExecuteNonQueryCommand("UPDATE \"USER\" SET \"PREVLOGIN\" = NULL WHERE \"PREVLOGIN\" = '-infinity'", System.Data.CommandType.Text, new List<Npgsql.NpgsqlParameter>());

            // Users
            Console.WriteLine("Exporting GIIS Users to OpenIZ IMS Format");
            DatasetInstall userDataset = new DatasetInstall() { Action = new List<DataInstallAction>() };
            userDataset.Id = "Users from GIIS";
            foreach (var itm in User.GetUserList())
            {
                if (userDataset.Action.Any(o => (o.Element as SecurityUser)?.UserName.Trim().ToLower() == itm.Username.Trim().ToLower()) ||
                    itm.Username.ToLower() == "administrator")
                    continue; /// Apparently user names are distinct based on case?
                Guid userId = Guid.NewGuid(), entityId = Guid.NewGuid();
                userMap.Add(itm.Id, userId);

                if (!userEntityMap.TryGetValue(itm.Id, out entityId))
                {
                    entityId = Guid.NewGuid();
                    userEntityMap.Add(itm.Id, entityId);
                }
                var securityUser = new SecurityUser()
                {
                    Key = userId,
                    UserName = itm.Username,
                    Email = itm.Email,
                    EmailConfirmed = !String.IsNullOrEmpty(itm.Email),
                    LastLoginTime = itm.Lastlogin,
                    SecurityHash = Guid.Empty.ToString(),
                    Lockout = itm.IsActive ? null : (DateTime?)DateTime.MaxValue,
                    PasswordHash = BitConverter.ToString(Convert.FromBase64String(itm.Password)).Replace("-", ""),
                    UserClass = UserClassKeys.HumanUser,
                    TwoFactorEnabled = false,
                    ObsoletionTime = itm.Deleted ? (DateTime?)DateTime.Now : null,
                    ObsoletedByKey = itm.Deleted ? (Guid?)Guid.Parse(AuthenticationContext.SystemUserSid) : null,
                };
                var userEntity = new UserEntity()
                {
                    Key = entityId,
                    Names = new List<EntityName>() { new EntityName(NameUseKeys.OfficialRecord, itm.Lastname, itm.Firstname) },

                    SecurityUserKey = userId,
                    Identifiers = new List<EntityIdentifier>()
                            {
                                new EntityIdentifier(new AssigningAuthority("GIIS_USER_ID", "GIIS User Identifiers", "1.3.6.1.4.1.<<YOUR.PEN>>.15"), itm.Id.ToString())
                            },
                    Tags = new List<EntityTag>()
                            {
                                new EntityTag("http://openiz.org/tags/contrib/importedData", "true")
                            },
                    StatusConceptKey = itm.IsActive ? StatusKeys.Active : StatusKeys.Obsolete

                };
                if (!String.IsNullOrEmpty(itm.Email))
                    userEntity.Telecoms = new List<EntityTelecomAddress>() { new EntityTelecomAddress(TelecomAddressUseKeys.WorkPlace, itm.Email) };

                Guid facilityId = Guid.Empty;
                if (facilityMap.TryGetValue(itm.HealthFacilityId, out facilityId))
                    userEntity.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.Employee, new Entity() { Key = facilityId }));

                // data element 
                var securityUserData = new DataInsert()
                {
                    Element = securityUser,
                    Association = new List<DataAssociation>()
                };

                // Role
                foreach (var r in Role.GetRolesOfUser(itm.Id))
                {
                    Guid roleId = Guid.Empty;
                    if (!roleMap.TryGetValue(r.Id, out roleId))
                    {
                        roleId = Guid.NewGuid();
                        roleMap.Add(r.Id, roleId);
                    }

                    var role = new SecurityRole()
                    {
                        Key = roleId,
                        Name = r.Name,
                        ObsoletionTime = r.IsActive ? null : (DateTime?)DateTime.Now,
                        ObsoletedByKey = r.IsActive ? null : (Guid?)Guid.Parse(AuthenticationContext.SystemUserSid)
                    };

                    // Add roles to the user
                    securityUserData.Association.Add(new DataAssociation()
                    {
                        PropertyName = "Roles",
                        Element = new SecurityRole() { Key = role.Key }
                    });

                    // Add role
                    userDataset.Action.Add(new DataInsert()
                    {
                        Element = role
                    });

                    // Vaccinator?
                    if (r.Name == "Vaccinator")
                    {

                        // Provider entity
                        var providerEntity = new Provider()
                        {
                            Key = Guid.NewGuid(),
                            Names = userEntity.Names,
                            Telecoms = userEntity.Telecoms,
                            Identifiers = userEntity.Identifiers.Select(o => new EntityIdentifier(new AssigningAuthority("PROVIDER_ID", "TImR Assigned Provider ID", "1.3.6.1.4.1.<<YOUR.PEN>>.16"), o.Value)).ToList(),
                            Tags = new List<EntityTag>()
                            {
                                new EntityTag("http://openiz.org/tags/contrib/importedData", "true")
                            },
                            StatusConceptKey = itm.IsActive ? StatusKeys.Active : StatusKeys.Obsolete
                        };
                        userDataset.Action.Add(new DataInsert() { Element = providerEntity });

                        // Create a heath care provider
                        userEntity.Relationships.Add(new EntityRelationship()
                        {
                            RelationshipTypeKey = EntityRelationshipTypeKeys.AssignedEntity,
                            TargetEntityKey = providerEntity.Key
                        });
                    }
                }


                userDataset.Action.Add(securityUserData);
                userDataset.Action.Add(new DataInsert()
                {
                    Element = userEntity
                });
            }



            // Write datasets
            if (!parms.LiveMigration)
            {
                XmlSerializer xsz = new XmlSerializer(typeof(DatasetInstall));
                using (var fs = File.Create(Path.Combine(parms.OutputDirectory, "990-GIIS.concepts.dataset")))
                    xsz.Serialize(fs, conceptDataset);
                using (var fs = File.Create(Path.Combine(parms.OutputDirectory, "991-GIIS.facilities.dataset")))
                    xsz.Serialize(fs, facilityDataset);
                using (var fs = File.Create(Path.Combine(parms.OutputDirectory, "992-GIIS.places.dataset")))
                    xsz.Serialize(fs, placeDataset);
                using (var fs = File.Create(Path.Combine(parms.OutputDirectory, "993-GIIS.users.dataset")))
                    xsz.Serialize(fs, userDataset);
                using (var fs = File.Create(Path.Combine(parms.OutputDirectory, "994-GIIS.materials.dataset")))
                    xsz.Serialize(fs, materialDataset);
            }
            else
            {
                Console.WriteLine("Installing concepts...");
                var installer = new OpenIZ.Core.Persistence.DataInitializationService();
                installer.InstallDataset(conceptDataset);
                installer.InstallDataset(facilityDataset);
                installer.InstallDataset(placeDataset);
                installer.InstallDataset(userDataset);
                installer.InstallDataset(materialDataset);
            }
        }

    }


}
