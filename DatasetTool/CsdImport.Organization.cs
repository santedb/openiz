﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Data;
using MARC.HI.EHRS.SVC.Core.Services;
using OpenIZ.Core.Model.Constants;
using OpenIZ.Core.Model.DataTypes;
using OpenIZ.Core.Model.Entities;
using OpenIZ.Core.Security;

namespace OizDevTool
{
	/// <summary>
	/// Represents a CSD import utility.
	/// </summary>
	public partial class CsdImport
    {
		/// <summary>
		/// Maps the organizations.
		/// </summary>
		/// <param name="csdOrganizations">The CSD organizations.</param>
		/// <returns>Returns a list of organizations.</returns>
		private static IEnumerable<Organization> MapOrganizations(IEnumerable<organization> csdOrganizations)
	    {
		    var organizationService = ApplicationContext.Current.GetService<IDataPersistenceService<Organization>>();
		    var organizations = new List<Organization>();

		    foreach (var csdOrganization in csdOrganizations)
		    {
			    var organization = new Organization();

			    Guid key;

			    // attempt to map the key value
			    if (!TryMapKey(csdOrganization.entityID, out key))
			    {
				    key = Guid.NewGuid();
				    organization.Tags.Add(new EntityTag(CsdEntityIdTag, csdOrganization.entityID));
			    }

			    organization.Key = key;

			    // try get existing
			    organization = organizationService.Get(new Identifier<Guid>(key), AuthenticationContext.SystemPrincipal, false);

				if (organization == null)
				{
					Console.ForegroundColor = ConsoleColor.Cyan;
					Console.WriteLine($"Organization not found using key: {key}, will create one {Environment.NewLine}");
					Console.ResetColor();

					organization = new Organization
					{
						Addresses = csdOrganization.address?.Select(a => MapEntityAddress(a, new Uri(AddressTypeCodeSystem))).ToList() ?? new List<EntityAddress>(),
						CreationTime = csdOrganization.record?.created ?? DateTimeOffset.Now,
						Extensions = csdOrganization.extension?.Select(e => MapEntityExtension(e.urn, e.type)).ToList() ?? new List<EntityExtension>(),
						Identifiers = csdOrganization.otherID?.Select(MapEntityIdentifier).ToList() ?? new List<EntityIdentifier>(),
						Key = key,
						StatusConceptKey = csdOrganization.record?.status != null ? MapStatusCode(csdOrganization.record.status, "http://openiz.org/csd/CSD-OrganizationStatusCodes") : StatusKeys.Active,
						Tags = new List<EntityTag>
						{
							new EntityTag(ImportedDataTag, "true")
						}
					};
				}
				else
				{
					Console.ForegroundColor = ConsoleColor.Cyan;
					Console.WriteLine($"Organization found using key: {key}, will reset basic properties {Environment.NewLine}");
					Console.ResetColor();

					// reset basic properties
					organization.CreationTime = DateTimeOffset.Now;
					organization.PreviousVersion = null;
					organization.VersionKey = null;
					organization.VersionSequence = null;
				}

			    // map type concept
			    if (csdOrganization.codedType?.Any() == true)
			    {
				    // we don't support multiple specialties for a organization at the moment, so we only take the first one
				    // TODO: cleanup
				    organization.TypeConceptKey = MapCodedType(csdOrganization.codedType[0].code, csdOrganization.codedType[0].codingScheme)?.Key;
			    }

			    // map specializations
			    if (csdOrganization.specialization?.Any() == true)
			    {
				    // we don't support multiple industry values for a organization at the moment, so we only take the first one
				    // TODO: cleanup
				    organization.IndustryConceptKey = MapCodedType(csdOrganization.specialization[0].code, csdOrganization.specialization[0].codingScheme)?.Key;
			    }

			    // map parent relationships
			    if (csdOrganization.parent?.entityID != null)
			    {
				    Guid parentKey;

				    if (TryMapKey(csdOrganization.parent?.entityID, out parentKey))
				    {
					    // if I'm not related to myself... CSD fix
					    if (parentKey != organization.Key)
					    {
						    organization.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.Parent, LookupByEntityIdOrTag<Organization>(csdOrganization.parent.entityID)));
					    }
				    }
			    }

			    // map primary name
			    if (csdOrganization.primaryName != null)
			    {
				    organization.Names.RemoveAll(c => c.NameUseKey == NameUseKeys.OfficialRecord);
				    organization.Names.Add(new EntityName(NameUseKeys.OfficialRecord, csdOrganization.primaryName));
			    }

			    // map tags
			    if (csdOrganization.record?.sourceDirectory != null)
			    {
				    organization.Tags.Clear();
					organization.Tags.Add(new EntityTag("sourceDirectory", csdOrganization.record.sourceDirectory));
			    }

			    // map names
			    if (csdOrganization.otherName?.Any() == true)
			    {
				    organization.Names.RemoveAll(c => c.NameUseKey == NameUseKeys.Assigned);
				    organization.Names.AddRange(csdOrganization.otherName.Select(c => MapEntityNameOrganization(NameUseKeys.Assigned, c)));
			    }

			    // map contacts
			    if (csdOrganization.contact?.Any() == true)
			    {
					// HACK
				    organization.Relationships.RemoveAll(r => r.RelationshipTypeKey == EntityRelationshipTypeKeys.Contact);
					organization.Relationships.AddRange(csdOrganization.contact.Select(MapEntityRelationshipOrganizationContact));
			    }

			    // map contact points
			    if (csdOrganization.contactPoint?.Any() == true)
			    {
				    organization.Telecoms.AddRange(csdOrganization.contactPoint.Select(c => MapContactPoint(TelecomAddressUseKeys.Public, c)));
			    }

			    organizations.Add(organization);

				Console.WriteLine($"Mapped organization: {organization.Key.Value} {string.Join(" ", organization.Names.SelectMany(n => n.Component).Select(c => c.Value))}");
		    }

		    return organizations;
	    }
	}
}