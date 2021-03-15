/*
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
using OpenIZ.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenIZ.Core.Services.Impl
{
    /// <summary>
    /// Represents a business rules service with no behavior, but intended to help in the implementation of another
    /// business rules service
    /// </summary>
    public abstract class BaseBusinessRulesService<TModel> : IBusinessRulesService<TModel> where TModel : IdentifiedData
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => $"Business rules for {typeof(TModel).FullName}";

        /// <summary>
        /// Gets the next behavior
        /// </summary>
        public IBusinessRulesService<TModel> Next { get; set; }

        /// <summary>
        /// Next
        /// </summary>
        IBusinessRulesService IBusinessRulesService.Next
        {
            get => this.Next;
            set => this.Next = value as IBusinessRulesService<TModel>;
        }

        /// <summary>
        /// After insert
        /// </summary>
        public virtual TModel AfterInsert(TModel data)
        {
            return this.Next?.AfterInsert(data) ?? data;
        }

        /// <summary>
        /// After insert
        /// </summary>
        public object AfterInsert(object data)
        {
            return this.AfterInsert((TModel)data);
        }

        /// <summary>
        /// After obsolete
        /// </summary>
        public virtual TModel AfterObsolete(TModel data)
        {
            return this.Next?.AfterObsolete(data) ?? data;
        }

        /// <summary>
        /// After obsoletion
        /// </summary>
        public object AfterObsolete(object data)
        {
            return this.AfterObsolete((TModel)data);
        }

        /// <summary>
        /// After query
        /// </summary>
        /// <param name="results"></param>
        /// <returns></returns>
        public virtual IEnumerable<TModel> AfterQuery(IEnumerable<TModel> results)
        {
            return this.Next?.AfterQuery(results) ?? results;
        }

        /// <summary>
        /// After query
        /// </summary>
        public IEnumerable<object> AfterQuery(IEnumerable<object> results)
        {
            return this.AfterQuery(results.OfType<TModel>());
        }

        /// <summary>
        /// Fired after retrieve
        /// </summary>
        public virtual TModel AfterRetrieve(TModel result)
        {
            return this.Next?.AfterRetrieve(result) ?? result;
        }

        /// <summary>
        /// After the data has been retrieved
        /// </summary>
        public object AfterRetrieve(object result)
        {
            return this.AfterRetrieve((TModel)result);
        }

        /// <summary>
        /// After update
        /// </summary>
        public virtual TModel AfterUpdate(TModel data)
        {
            return this.Next?.AfterUpdate(data) ?? data;
        }

        /// <summary>
        /// After update
        /// </summary>
        public object AfterUpdate(object data)
        {
            return this.AfterUpdate((TModel)data);
        }

        /// <summary>
        /// Before insert complete
        /// </summary>
        public virtual TModel BeforeInsert(TModel data)
        {
            return this.Next?.BeforeInsert(data) ?? data;
        }

        /// <summary>
        /// Before insert
        /// </summary>
        public object BeforeInsert(object data)
        {
            return this.BeforeInsert((TModel)data);
        }

        /// <summary>
        /// Before obselete
        /// </summary>
        public virtual TModel BeforeObsolete(TModel data)
        {
            return this.Next?.BeforeObsolete(data) ?? data;
        }

        /// <summary>
        /// Before obsoletion occurs
        /// </summary>
        public object BeforeObsolete(object data)
        {
            return this.BeforeObsolete((TModel)data);

        }

        /// <summary>
        /// Before update
        /// </summary>
        public virtual TModel BeforeUpdate(TModel data)
        {
            return this.Next?.BeforeUpdate(data) ?? data;
        }

        /// <summary>
        /// Before update occurs
        /// </summary>
        public object BeforeUpdate(object data)
        {
            return this.BeforeUpdate((TModel)data);
        }

        /// <summary>
        /// Validate the specified object
        /// </summary>
        public virtual List<DetectedIssue> Validate(TModel data)
        {
            return this.Next?.Validate(data) ?? new List<DetectedIssue>();
        }

        /// <summary>
        /// Validate the specified object
        /// </summary>
        public List<DetectedIssue> Validate(object data)
        {
            return this.Validate((TModel)data);
        }
    }
}