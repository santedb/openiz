﻿using System;
using System.Linq;
using OpenIZ.Core.Model;
using System.Collections.Generic;
using OpenIZ.Persistence.Data.MSSQL.Data;
using System.Security.Principal;
using OpenIZ.Persistence.Data.MSSQL.Exceptions;

namespace OpenIZ.Persistence.Data.MSSQL.Services.Persistence
{
	/// <summary>
	/// Base data persistence service
	/// </summary>
	public abstract class BaseDataPersistenceService<TModel, TDomain> : IdentifiedPersistenceService<TModel, TDomain>
		where TModel : BaseEntityData, new()
		where TDomain : class, IDbBaseData, new()
	{
        /// <summary>
        /// Performthe actual insert.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public override TModel Insert (ModelDataContext context, TModel data, IPrincipal principal)
		{
			var domainObject = this.FromModelInstance (data, context, principal) as TDomain;

            if (domainObject.Id == Guid.Empty)
                data.Key = domainObject.Id = Guid.NewGuid();

            // Ensure created by exists
            data.CreatedBy?.EnsureExists(context, principal);
			data.CreatedByKey = domainObject.CreatedBy = domainObject.CreatedBy == Guid.Empty ? principal.GetUser (context).UserId : domainObject.CreatedBy;
			context.GetTable<TDomain>().InsertOnSubmit (domainObject);

            context.SubmitChanges();
            data.CreationTime = (DateTimeOffset)domainObject.CreationTime;

            return data;
		}

        /// <summary>
        /// Perform the actual update.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public override TModel Update (ModelDataContext context, TModel data, IPrincipal principal)
		{
            // Check for key
            if (data.Key == Guid.Empty)
                throw new SqlFormalConstraintException(SqlFormalConstraintType.NonIdentityUpdate);

            // Get current object
			var domainObject = this.FromModelInstance (data, context, principal) as TDomain;
            var currentObject = context.GetTable<TDomain>().FirstOrDefault(ExpressionRewriter.Rewrite<TDomain>(o => o.Id == data.Key));
            // Not found
            if (currentObject == null)
                throw new KeyNotFoundException(data.Key.ToString());

            // VObject
            var vobject = domainObject as IDbNonVersionedBaseData;
            if(vobject != null)
            {
                vobject.UpdatedBy = principal.GetUser(context).UserId;
                vobject.UpdatedTime = DateTimeOffset.Now;
            }

            currentObject.CopyObjectData(domainObject);
            context.SubmitChanges();

			return data;
		}

        /// <summary>
        /// Performs the actual obsoletion
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public override TModel Obsolete (ModelDataContext context, TModel data, IPrincipal principal)
		{
            if (data.Key == Guid.Empty)
                throw new SqlFormalConstraintException(SqlFormalConstraintType.NonIdentityUpdate);

            // Current object
            var currentObject = context.GetTable<TDomain>().FirstOrDefault(ExpressionRewriter.Rewrite<TDomain>(o => o.Id == data.Key));
            if (currentObject == null)
                throw new KeyNotFoundException(data.Key.ToString());

            data.ObsoletedBy?.EnsureExists(context, principal);
            data.ObsoletedByKey = currentObject.ObsoletedBy = data.ObsoletedBy?.Key ?? principal.GetUser(context).UserId;
            data.ObsoletionTime = currentObject.ObsoletionTime = currentObject.ObsoletionTime ?? DateTimeOffset.Now;
            context.SubmitChanges();
			return data;
		}

       
    }
}

