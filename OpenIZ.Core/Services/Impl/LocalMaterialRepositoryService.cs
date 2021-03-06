﻿/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
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
 * Date: 2017-9-1
 */
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using OpenIZ.Core.Model.Entities;
using OpenIZ.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MARC.HI.EHRS.SVC.Core.Data;
using OpenIZ.Core.Exceptions;

namespace OpenIZ.Core.Services.Impl
{
	/// <summary>
	/// Local material persistence service
	/// </summary>
	public class LocalMaterialRepositoryService : LocalEntityRepositoryServiceBase, IMaterialRepositoryService, IRepositoryService<Material>, IRepositoryService<ManufacturedMaterial>
    {
		/// <summary>
		/// Find manufactured material
		/// </summary>
		public IEnumerable<ManufacturedMaterial> FindManufacturedMaterial(Expression<Func<ManufacturedMaterial, bool>> expression)
		{
            int t = 0;
            return this.FindManufacturedMaterial(expression, 0, null, out t);
		}

		/// <summary>
		/// Find manufactured material
		/// </summary>
		public IEnumerable<ManufacturedMaterial> FindManufacturedMaterial(Expression<Func<ManufacturedMaterial, bool>> expression, int offset, int? count, out int totalCount)
		{
			return base.Find(expression, offset, count, out totalCount, Guid.Empty);
		}

		/// <summary>
		/// Finds the specified material
		/// </summary>
		public IEnumerable<Material> FindMaterial(Expression<Func<Material, bool>> expression)
		{
            int t = 0;
            return this.FindMaterial(expression, 0, null, out t);
		}

		/// <summary>
		/// Find the specified material
		/// </summary>
		public IEnumerable<Material> FindMaterial(Expression<Func<Material, bool>> expression, int offset, int? count, out int totalCount)
		{
            return base.Find(expression, offset, count, out totalCount, Guid.Empty);
		}

		/// <summary>
		/// Get manufactured material
		/// </summary>
		public ManufacturedMaterial GetManufacturedMaterial(Guid id, Guid versionId)
		{
            return base.Get<ManufacturedMaterial>(id, versionId);
		}

		/// <summary>
		/// Gets the specified identified material
		/// </summary>
		public Material GetMaterial(Guid id, Guid versionId)
		{
            return base.Get<Material>(id, versionId);
		}

		/// <summary>
		/// Insert manufactured material
		/// </summary>
		public ManufacturedMaterial Insert(ManufacturedMaterial material)
		{
            return base.Insert(material);
		}

		/// <summary>
		/// Inserts the specified material
		/// </summary>
		public Material Insert(Material material)
		{
            return base.Insert(material);
		}

		/// <summary>
		/// Obsolete the specified material
		/// </summary>
		public ManufacturedMaterial ObsoleteManufacturedMaterial(Guid key)
		{
            return base.Obsolete<ManufacturedMaterial>(key);
		}

		/// <summary>
		/// Obsoletes the speciied material
		/// </summary>
		public Material ObsoleteMaterial(Guid key)
		{
            return base.Obsolete<Material>(key);
		}

		/// <summary>
		/// Save the specified manufactured material
		/// </summary>
		public ManufacturedMaterial Save(ManufacturedMaterial material)
		{
            return base.Save(material);
		}

		/// <summary>
		/// Save the specified material
		/// </summary>
		public Material Save(Material material)
		{
            return base.Save(material);
		}

        /// <summary>
        /// Find material
        /// </summary>
        IEnumerable<Material> IRepositoryService<Material>.Find(Expression<Func<Material, bool>> query)
        {
            return this.FindMaterial(query);
        }

        /// <summary>
        /// Find manufactured material
        /// </summary>
        IEnumerable<ManufacturedMaterial> IRepositoryService<ManufacturedMaterial>.Find(Expression<Func<ManufacturedMaterial, bool>> query)
        {
            return this.FindManufacturedMaterial(query);
        }

        /// <summary>
        /// Find material
        /// </summary>
        IEnumerable<Material> IRepositoryService<Material>.Find(Expression<Func<Material, bool>> query, int offset, int? count, out int totalResults)
        {
            return this.FindMaterial(query, offset, count, out totalResults);
        }

        /// <summary>
        /// Find manufactured material
        /// </summary>
        IEnumerable<ManufacturedMaterial> IRepositoryService<ManufacturedMaterial>.Find(Expression<Func<ManufacturedMaterial, bool>> query, int offset, int? count, out int totalResults)
        {
            return this.FindManufacturedMaterial(query, offset, count, out totalResults);
        }

        /// <summary>
        /// Get material
        /// </summary>
        Material IRepositoryService<Material>.Get(Guid key)
        {
            return this.GetMaterial(key, Guid.Empty);
        }

        /// <summary>
        /// Get manufactured material
        /// </summary>
        ManufacturedMaterial IRepositoryService<ManufacturedMaterial>.Get(Guid key)
        {
            return this.GetManufacturedMaterial(key, Guid.Empty);
        }

        /// <summary>
        /// Get material
        /// </summary>
        Material IRepositoryService<Material>.Get(Guid key, Guid versionKey)
        {
            return this.GetMaterial(key, versionKey);
        }

        /// <summary>
        /// Get manufactured material
        /// </summary>
        ManufacturedMaterial IRepositoryService<ManufacturedMaterial>.Get(Guid key, Guid versionKey)
        {
            return this.GetManufacturedMaterial(key, versionKey);
        }
        
        /// <summary>
        /// Obsoletes material
        /// </summary>
        Material IRepositoryService<Material>.Obsolete(Guid key)
        {
            return this.ObsoleteMaterial(key);
        }

        /// <summary>
        /// Obsoletes manufactured material
        /// </summary>
        ManufacturedMaterial IRepositoryService<ManufacturedMaterial>.Obsolete(Guid key)
        {
            return this.ObsoleteManufacturedMaterial(key);
        }


    }
}