﻿/*
 * Copyright 2015-2017 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 2017-3-24
 */
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using OpenIZ.Core.Model.Constants;
using OpenIZ.Core.Model.Entities;
using OpenIZ.Core.Services;
using OpenIZ.Messaging.GS1.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.DataTypes;
using System.Diagnostics;
using OpenIZ.Core.Diagnostics;
using OpenIZ.Core.Model.Acts;
using OpenIZ.Core.Model.Collection;
using OpenIZ.Messaging.GS1.Configuration;
using OpenIZ.Core.Model.Extensions;

namespace OpenIZ.Messaging.GS1.Wcf
{
    /// <summary>
    /// Stock service behavior
    /// </summary>
    [ServiceBehavior(Name = "GS1BMS_Behavior", ConfigurationName = "GS1BMS")]
    public class StockServiceBehavior : IStockService
    {

        // Configuration
        private Gs1Configuration m_configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection("openiz.messaging.gs1") as Gs1Configuration;

        // Act repository
        private IActRepositoryService m_actRepository;
        // Material repository
        private IMaterialRepositoryService m_materialRepository;
        // Place repository
        private IPlaceRepositoryService m_placeRepository;
        // Stock service
        private IStockManagementRepositoryService m_stockService;
        // GS1 Utility
        private Gs1Util m_gs1Util;

        // Tracer
        private TraceSource m_tracer = new TraceSource("OpenIZ.Messaging.GS1");

        /// <summary>
        /// Default ctor setting services
        /// </summary>
        public StockServiceBehavior()
        {
            ApplicationContext.Current.Started += (o, e) =>
            {
                this.m_actRepository = ApplicationContext.Current.GetService<IActRepositoryService>();
                this.m_materialRepository = ApplicationContext.Current.GetService<IMaterialRepositoryService>();
                this.m_placeRepository = ApplicationContext.Current.GetService<IPlaceRepositoryService>();
                this.m_stockService = ApplicationContext.Current.GetService<IStockManagementRepositoryService>();
                this.m_gs1Util = new Gs1Util();
            };
        }

        // IMSI Trace host
        private readonly TraceSource traceSource = new TraceSource("OpenIZ.Messaging.GS1");
        
        /// <summary>
        /// The issue despactch advice message will insert a new shipped order into the TImR system.
        /// </summary>
        public void IssueDespatchAdvice(DespatchAdviceMessageType advice)
        {
            // TODO: Validate the standard header
            // Loop 
            Bundle orderTransaction = new Bundle();

            foreach (var adv in advice.despatchAdvice)
            {

                Place sourceLocation = this.m_gs1Util.GetLocation(adv.shipper),
                    destinationLocation = this.m_gs1Util.GetLocation(adv.receiver);
                if (sourceLocation == null)
                    throw new KeyNotFoundException("Shipper location not found");
                else if (destinationLocation == null)
                    throw new KeyNotFoundException("Receiver location not found");

                // Find the original order which this despatch advice is fulfilling
                Act orderRequestAct = this.m_gs1Util.GetOrder(adv.orderResponse ?? adv.purchaseOrder, ActMoodKeys.Request);
                if (orderRequestAct != null) // Orderless despatch!
                {
                    // If the original order request is not comlete, then complete it
                    orderRequestAct.StatusConceptKey = StatusKeys.Completed;
                    orderTransaction.Add(orderRequestAct);
                }

                // Find the author of the shipment

                var oidService = ApplicationContext.Current.GetService<IOidRegistrarService>();
                var gln = oidService.GetOid("GLN");
                var issuingAuthority = oidService.FindData($"{gln.Oid}.{adv.despatchAdviceIdentification.contentOwner.gln}");
                if (issuingAuthority == null)
                    issuingAuthority = oidService.GetOid(this.m_configuration.DefaultContentOwnerAssigningAuthority);

                if (issuingAuthority == null)
                    throw new KeyNotFoundException("Cannot find default issuing authority for advice identification. Please configure a valid OID");


                // Now we want to create a new Supply act which that fulfills the old act
                Act fulfillAct = new Act()
                {
                    MoodConceptKey = ActMoodKeys.Eventoccurrence,
                    ClassConceptKey = ActClassKeys.Supply,
                    StatusConceptKey = StatusKeys.Active,
                    TypeConceptKey = Guid.Parse("14d69b32-f6c4-4a49-a527-a74893dbcf4a"), // Order
                    ActTime = adv.despatchInformation.despatchDateTimeSpecified ? adv.despatchInformation.despatchDateTime : DateTime.Now,
                    Extensions = new List<ActExtension>()
                    {
                        new ActExtension(Gs1ModelExtensions.ActualShipmentDate, typeof(DateExtensionHandler), adv.despatchInformation.actualShipDateTime),
                        new ActExtension(Gs1ModelExtensions.ExpectedDeliveryDate, typeof(DateExtensionHandler), adv.despatchInformation.estimatedDeliveryDateTime)
                    },
                    Tags = new List<ActTag>()
                    {
                        new ActTag("orderNumber", adv.despatchAdviceIdentification.entityIdentification),
                        new ActTag("orderStatus", "shipped"),
                        new ActTag("http://openiz.org/tags/contrib/importedData", "true")
                    },
                    Identifiers = new List<ActIdentifier>()
                    {
                        new ActIdentifier(new AssigningAuthority(issuingAuthority.Mnemonic, issuingAuthority.Name, issuingAuthority.Oid), adv.despatchAdviceIdentification.entityIdentification)
                    },
                    Participations = new List<ActParticipation>()
                    {
                        // TODO: Author
                        // TODO: Performer
                        new ActParticipation(ActParticipationKey.Location, sourceLocation.Key),
                        new ActParticipation(ActParticipationKey.Destination, destinationLocation.Key)
                    }
                };
                orderTransaction.Add(fulfillAct);

                // Fullfillment
                if (orderRequestAct != null)
                    fulfillAct.Relationships = new List<ActRelationship>()
                    {
                        new ActRelationship(ActRelationshipTypeKeys.Fulfills, orderRequestAct.Key)
                    };


                // Now add participations for each material in the despatch
                foreach (var dal in adv.despatchAdviceLogisticUnit)
                {
                    foreach (var line in dal.despatchAdviceLineItem)
                    {
                        if (line.despatchedQuantity.measurementUnitCode != "dose")
                            throw new InvalidOperationException("Despatched quantity must be reported in doses");

                        var material = this.m_gs1Util.GetManufacturedMaterial(line.transactionalTradeItem, this.m_configuration.AutoCreateMaterials);

                        // Add a participation
                        fulfillAct.Participations.Add(new ActParticipation(ActParticipationKey.Consumable, material.Key)
                        {
                            Quantity = (int)line.despatchedQuantity.Value
                        });
                    }
                }

            }

            // insert transaction
            try
            {
                ApplicationContext.Current.GetService<IBatchRepositoryService>().Insert(orderTransaction);
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Error issuing despatch advice: {0}", e);
                throw new Exception($"Error issuing despatch advice: {e.Message}", e);
            }
        }

        /// <summary>
        /// Requests the issuance of a BMS1 inventory report request
        /// </summary>
        public LogisticsInventoryReportMessageType IssueInventoryReportRequest(LogisticsInventoryReportRequestMessageType parameters)
        {
            // Status
            LogisticsInventoryReportMessageType retVal = new LogisticsInventoryReportMessageType()
            {
                StandardBusinessDocumentHeader = this.m_gs1Util.CreateDocumentHeader("logisticsInventoryReport", null)
            };


            // Date / time of report

            DateTime? reportFrom = parameters.logisticsInventoryReportRequest.First().reportingPeriod?.beginDate ?? DateTime.MinValue,
                reportTo = parameters.logisticsInventoryReportRequest.First().reportingPeriod?.endDate ?? DateTime.Now;

            // return value
            LogisticsInventoryReportType report = new LogisticsInventoryReportType()
            {
                creationDateTime = DateTime.Now,
                documentStatusCode = DocumentStatusEnumerationType.ORIGINAL,
                documentActionCode = DocumentActionEnumerationType.CHANGE_BY_REFRESH,
                logisticsInventoryReportIdentification = new Ecom_EntityIdentificationType() { entityIdentification = BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0).ToString("X") },
                structureTypeCode = new StructureTypeCodeType() { Value = "LOCATION_BY_ITEM" },
                documentActionCodeSpecified = true
            };

            var locationStockStatuses = new List<LogisticsInventoryReportInventoryLocationType>();

            // Next, we want to know which facilities for which we're getting the inventory report
            List<Place> filterPlaces = null;
            if (parameters.logisticsInventoryReportRequest.First().logisticsInventoryRequestLocation != null &&
                parameters.logisticsInventoryReportRequest.First().logisticsInventoryRequestLocation.Length > 0)
            {
                foreach (var filter in parameters.logisticsInventoryReportRequest.First().logisticsInventoryRequestLocation)
                {
                    int tc = 0;
                    var id = filter.inventoryLocation.gln ?? filter.inventoryLocation.additionalPartyIdentification?.FirstOrDefault()?.Value;
                    var place = this.m_placeRepository.Find(o => o.Identifiers.Any(i => i.Value == id), 0, 1, out tc).FirstOrDefault();
                    if (place == null)
                        throw new FileNotFoundException($"Place {filter.inventoryLocation.gln} not found");
                    if (filterPlaces == null)
                        filterPlaces = new List<Place>() { place };
                    else
                        filterPlaces.Add(place);
                }
            }
            else
                filterPlaces = this.m_placeRepository.Find(o => o.ClassConceptKey == EntityClassKeys.ServiceDeliveryLocation).ToList();

            // Get the GLN AA data
            var oidService = ApplicationContext.Current.GetService<IOidRegistrarService>();
            var gln = oidService.GetOid("GLN");
            var gtin = oidService.GetOid("GTIN");

            if (gln == null || gln.Oid == null)
                throw new InvalidOperationException("GLN configuration must carry OID and be named GLN in repository");
            if (gtin == null || gtin.Oid == null)
                throw new InvalidOperationException("GTIN configuration must carry OID and be named GTIN in repository");

            // Create the inventory report
            filterPlaces.AsParallel().ForAll(place =>
            {

                try
                {
                    var locationStockStatus = new LogisticsInventoryReportInventoryLocationType();
                    lock (locationStockStatuses)
                        locationStockStatuses.Add(locationStockStatus);

                    // TODO: Store the GLN configuration domain name
                    locationStockStatus.inventoryLocation = this.m_gs1Util.CreateLocation(place);

                    var tradeItemStatuses = new List<TradeItemInventoryStatusType>();

                    // What are the relationships of held entities
                    foreach (var rel in place.Relationships.Where(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.OwnedEntity))
                    {
                        if (rel.TargetEntity == null)
                            rel.TargetEntity = this.m_materialRepository.GetManufacturedMaterial(rel.TargetEntityKey.Value, Guid.Empty);

                        var mmat = rel.TargetEntity as ManufacturedMaterial;
                        if (!(mmat is ManufacturedMaterial))
                            continue;

                        var mat = this.m_materialRepository.FindMaterial(o => o.Relationships.Where(r => r.RelationshipType.Mnemonic == "ManufacturedProduct").Any(r => r.TargetEntity.Key == mmat.Key)).FirstOrDefault();

                        decimal balanceOH = rel.Quantity ?? 0;

                        // get the adjustments the adjustment acts are allocations and transfers
                        var adjustments = this.m_stockService.FindAdjustments(mmat.Key.Value, place.Key.Value, reportFrom, reportTo);

                        // We want to roll back to the start time and re-calc balance oh at time?
                        if (reportTo.Value.Date < DateTime.Now.Date)
                        {
                            var prevAdjustments = this.m_stockService.FindAdjustments(mmat.Key.Value, place.Key.Value, reportTo, DateTime.Now);
                            balanceOH -= (decimal)prevAdjustments.Sum(o => o.Participations.FirstOrDefault(p => p.ParticipationRoleKey == ActParticipationKey.Consumable)?.Quantity);
                        }

                        var cvx = ApplicationContext.Current.GetService<IConceptRepositoryService>().GetConceptReferenceTerm(mat.TypeConceptKey.Value, "CVX");

                        var typeItemCode = new ItemTypeCodeType()
                        {
                            Value = cvx?.Mnemonic ?? mmat.TypeConcept?.Mnemonic ?? mat.Key.Value.ToString(),
                            codeListVersion = cvx?.LoadProperty<CodeSystem>("CodeSystem")?.Authority ?? "OpenIZ-MaterialType"
                        };

                        // First we need the GTIN for on-hand balance
                        tradeItemStatuses.Add(new TradeItemInventoryStatusType()
                        {
                            gtin = mmat.Identifiers.FirstOrDefault(o => o.Authority.Oid == gtin.Oid)?.Value,
                            tradeItemDescription = mmat.Names.Select(o => new Description200Type() { Value = o.Component.FirstOrDefault()?.Value }).FirstOrDefault(),
                            itemTypeCode = typeItemCode,
                            additionalTradeItemIdentification = mmat.Identifiers.Where(o => o.Authority.Oid != gtin.Oid).Select(o => new AdditionalTradeItemIdentificationType()
                            {
                                additionalTradeItemIdentificationTypeCode = o.Authority.DomainName,
                                Value = o.Value
                            }).ToArray(),
                            inventoryDateTime = DateTime.Now,
                            inventoryDispositionCode = new InventoryDispositionCodeType() { Value = "ON_HAND" },
                            transactionalItemData = new TransactionalItemDataType[]
                            {
                            new TransactionalItemDataType()
                            {
                                tradeItemQuantity = new QuantityType()
                                {
                                    measurementUnitCode = (mmat.QuantityConcept ?? mat?.QuantityConcept)?.ReferenceTerms.Select(o => new AdditionalLogisticUnitIdentificationType()
                                    {
                                        additionalLogisticUnitIdentificationTypeCode = o.ReferenceTerm.CodeSystem.Name,
                                        Value = o.ReferenceTerm.Mnemonic
                                    }).FirstOrDefault()?.Value,
                                    Value = balanceOH
                                },
                                batchNumber = mmat.LotNumber,
                                itemExpirationDate = mmat.ExpiryDate.Value,
                                itemExpirationDateSpecified = true
                            }
                            }
                        });

                        // Broken vials?
                        tradeItemStatuses.Add(new TradeItemInventoryStatusType()
                        {
                            gtin = mmat.Identifiers.FirstOrDefault(o => o.Authority.DomainName == "GTIN")?.Value,
                            itemTypeCode = typeItemCode,
                            additionalTradeItemIdentification = mmat.Identifiers.Where(o => o.Authority.DomainName != "GTIN").Select(o => new AdditionalTradeItemIdentificationType()
                            {
                                additionalTradeItemIdentificationTypeCode = o.Authority.DomainName,
                                Value = o.Value
                            }).ToArray(),
                            tradeItemDescription = mmat.Names.Select(o => new Description200Type() { Value = o.Component.FirstOrDefault()?.Value }).FirstOrDefault(),
                            inventoryDateTime = DateTime.Now,
                            inventoryDispositionCode = new InventoryDispositionCodeType() { Value = "DAMAGED" },
                            transactionalItemData = new TransactionalItemDataType[]
                            {
                            new TransactionalItemDataType()
                            {
                                tradeItemQuantity = new QuantityType()
                                {
                                    measurementUnitCode = (mmat.QuantityConcept ?? mat?.QuantityConcept)?.ReferenceTerms.Select(o => new AdditionalLogisticUnitIdentificationType()
                                    {
                                        additionalLogisticUnitIdentificationTypeCode = o.ReferenceTerm.CodeSystem.Name,
                                        Value = o.ReferenceTerm.Mnemonic
                                    }).FirstOrDefault()?.Value,
                                    Value = Math.Abs(adjustments.Where(a => a.ReasonConceptKey.Value == ActReasonKeys.Broken).Sum(o => o.Participations.First(p => p.ParticipationRoleKey == ActParticipationKey.Consumable).Quantity.Value))
                                },
                                batchNumber = mmat.LotNumber,
                                itemExpirationDate = mmat.ExpiryDate.Value,
                                itemExpirationDateSpecified = true
                            }
                            }
                        });

                        // Cold storage failure?
                        tradeItemStatuses.Add(new TradeItemInventoryStatusType()
                        {
                            gtin = mmat.Identifiers.FirstOrDefault(o => o.Authority.DomainName == "GTIN")?.Value,
                            itemTypeCode = typeItemCode,
                            additionalTradeItemIdentification = mmat.Identifiers.Where(o => o.Authority.DomainName != "GTIN").Select(o => new AdditionalTradeItemIdentificationType()
                            {
                                additionalTradeItemIdentificationTypeCode = o.Authority.DomainName,
                                Value = o.Value
                            }).ToArray(),
                            tradeItemDescription = mmat.Names.Select(o => new Description200Type() { Value = o.Component.FirstOrDefault()?.Value }).FirstOrDefault(),
                            inventoryDateTime = DateTime.Now,
                            inventoryDispositionCode = new InventoryDispositionCodeType() { Value = "COLDCHAIN_FAILED" },
                            transactionalItemData = new TransactionalItemDataType[]
                            {
                            new TransactionalItemDataType()
                            {
                                tradeItemQuantity = new QuantityType()
                                {
                                    measurementUnitCode = (mmat.QuantityConcept ?? mat?.QuantityConcept)?.ReferenceTerms.Select(o => new AdditionalLogisticUnitIdentificationType()
                                    {
                                        additionalLogisticUnitIdentificationTypeCode = o.ReferenceTerm.CodeSystem.Name,
                                        Value = o.ReferenceTerm.Mnemonic
                                    }).FirstOrDefault()?.Value,
                                    Value = Math.Abs(adjustments.Where(a => a.ReasonConceptKey.Value == ActReasonKeys.ColdStorageFailure).Sum(o => o.Participations.First(p => p.ParticipationRoleKey == ActParticipationKey.Consumable).Quantity.Value))
                                },
                                batchNumber = mmat.LotNumber,
                                itemExpirationDate = mmat.ExpiryDate.Value,
                                itemExpirationDateSpecified = true,

                            }
                            }
                        });

                        // Cold storage failure?
                        tradeItemStatuses.Add(new TradeItemInventoryStatusType()
                        {
                            gtin = mmat.Identifiers.FirstOrDefault(o => o.Authority.DomainName == "GTIN")?.Value,
                            tradeItemDescription = mmat.Names.Select(o => new Description200Type() { Value = o.Component.FirstOrDefault()?.Value }).FirstOrDefault(),
                            itemTypeCode = typeItemCode,
                            additionalTradeItemIdentification = mmat.Identifiers.Where(o => o.Authority.DomainName != "GTIN").Select(o => new AdditionalTradeItemIdentificationType()
                            {
                                additionalTradeItemIdentificationTypeCode = o.Authority.DomainName,
                                Value = o.Value
                            }).ToArray(),
                            inventoryDateTime = DateTime.Now,
                            inventoryDispositionCode = new InventoryDispositionCodeType() { Value = "EXPIRED" },
                            transactionalItemData = new TransactionalItemDataType[]
                            {
                            new TransactionalItemDataType()
                            {
                                tradeItemQuantity = new QuantityType()
                                {
                                    measurementUnitCode = (mmat.QuantityConcept ?? mat?.QuantityConcept)?.ReferenceTerms.Select(o => new AdditionalLogisticUnitIdentificationType()
                                    {
                                        additionalLogisticUnitIdentificationTypeCode = o.ReferenceTerm.CodeSystem.Name,
                                        Value = o.ReferenceTerm.Mnemonic
                                    }).FirstOrDefault()?.Value,
                                    Value = Math.Abs(adjustments.Where(a => a.ReasonConceptKey.Value == ActReasonKeys.Expired).Sum(o => o.Participations.First(p => p.ParticipationRoleKey == ActParticipationKey.Consumable).Quantity.Value))
                                },
                                batchNumber = mmat.LotNumber,
                                itemExpirationDate = mmat.ExpiryDate.Value,
                                itemExpirationDateSpecified = true
                            }
                            }
                        });

                        // Other / Thrown away?
                        tradeItemStatuses.Add(new TradeItemInventoryStatusType()
                        {
                            gtin = mmat.Identifiers.FirstOrDefault(o => o.Authority.DomainName == "GTIN")?.Value,
                            itemTypeCode = typeItemCode,
                            tradeItemDescription = mmat.Names.Select(o => new Description200Type() { Value = o.Component.FirstOrDefault()?.Value }).FirstOrDefault(),
                            additionalTradeItemIdentification = mmat.Identifiers.Where(o => o.Authority.DomainName != "GTIN").Select(o => new AdditionalTradeItemIdentificationType()
                            {
                                additionalTradeItemIdentificationTypeCode = o.Authority.DomainName,
                                Value = o.Value
                            }).ToArray(),
                            inventoryDateTime = DateTime.Now,
                            inventoryDispositionCode = new InventoryDispositionCodeType() { Value = "WASTED" },
                            transactionalItemData = new TransactionalItemDataType[]
                            {
                            new TransactionalItemDataType()
                            {
                                tradeItemQuantity = new QuantityType()
                                {
                                    measurementUnitCode = (mmat.QuantityConcept ?? mat?.QuantityConcept)?.ReferenceTerms.Select(o => new AdditionalLogisticUnitIdentificationType()
                                    {
                                        additionalLogisticUnitIdentificationTypeCode = o.ReferenceTerm.CodeSystem.Name,
                                        Value = o.ReferenceTerm.Mnemonic
                                    }).FirstOrDefault()?.Value,
                                    Value = Math.Abs(adjustments.Where(a => a.ReasonConceptKey.Value == NullReasonKeys.Other).Sum(o => o.Participations.First(p => p.ParticipationRoleKey == ActParticipationKey.Consumable).Quantity.Value))
                                },
                                batchNumber = mmat.LotNumber,
                                itemExpirationDate = mmat.ExpiryDate.Value,
                                itemExpirationDateSpecified = true
                            }
                            }
                        });

                    }

                    // Reduce
                    locationStockStatus.tradeItemInventoryStatus = tradeItemStatuses.ToArray();
                }
                catch (Exception e)
                {
                    traceSource.TraceError("Error fetching stock data : {0}", e);
                }
                // TODO: Reduce and Group by GTIN
            });

            report.logisticsInventoryReportInventoryLocation = locationStockStatuses.ToArray();
            retVal.logisticsInventoryReport = new LogisticsInventoryReportType[] { report };
            return retVal;
        }

        /// <summary>
        /// Issues the order response message which will mark the requested order as underway
        /// </summary>
        public void IssueOrderResponse(OrderResponseMessageType orderResponse)
        {
            // TODO: Validate the standard header
            // Loop 
            Bundle orderTransaction = new Bundle();

            foreach (var resp in orderResponse.orderResponse)
            {

                // Find the original order which this despatch advice is fulfilling
                Act orderRequestAct = this.m_gs1Util.GetOrder(resp.originalOrder, ActMoodKeys.Request);
                if (orderRequestAct == null)
                    throw new KeyNotFoundException("Could not find originalOrder");

                // If the original order request is not comlete, then complete it
                ApplicationContext.Current.GetService<ITagPersistenceService>().Save(orderRequestAct.Key.Value, new ActTag("orderStatus", "accepted"));
            }

            // insert transaction
            try
            {
                ApplicationContext.Current.GetService<IBatchRepositoryService>().Insert(orderTransaction);
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error issuing order response : {0}", e);
                throw new Exception($"Error issuing order response: {e.Message}", e);
            }

        }
    }
}