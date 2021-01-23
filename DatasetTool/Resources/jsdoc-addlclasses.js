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

// Empty guid
EmptyGuid : "00000000-0000-0000-0000-000000000000",

/**
     * @class
     * @summary Represents a simple exception class
     * @constructor
     * @memberof OpenIZModel
     * @property {String} message Informational message about the exception
     * @property {Object} details Any detail / diagnostic information
     * @property {OpenIZModel#Exception} caused_by The cause of the exception
     * @param {String} message Informational message about the exception
     * @param {Object} detail Any detail / diagnostic information
     * @param {OpenIZModel#Exception} cause The cause of the exception
     */
Exception : function (type, message, detail, cause) {
    _self = this;

    this.type = type;
    this.message = message;
    this.details = detail;
    this.caused_by = cause;

}