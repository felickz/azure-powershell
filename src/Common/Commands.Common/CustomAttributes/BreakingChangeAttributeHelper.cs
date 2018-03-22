﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

#undef DEBUG

using Microsoft.WindowsAzure.Commands.Common.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Commands.Common.CustomAttributes
{
    public class BreakingChangeAttributeHelper
    {
        public const string SUPPRESS_ERROR_OR_WARNING_MESSAGE_ENV_VARIABLE_NAME = "SuppressAzurePowerShellBreakingChangeWarnings";

        public const string BREAKING_CHANGE_DOCUMENTATION_HEADER = @"<!--
    Please leave this section at the top of the breaking change documentation.

    New breaking changes should go under the section titled 'Current Breaking Changes', and should adhere to the following format:
    ## Current Breaking Changes

    The following cmdlets were affected this release:

    **Cmdlet 1**
    - Description of what has changed

    ```powershell
    # Old
    # Sample of how the cmdlet was previously called

    # New
    # Sample of how the cmdlet should now be called
    ```

    ## Release X.0.0

    The following cmdlets were affected this release:

    **Cmdlet 1**
    - Description of what has changed

    ```powershell

    # Old\
    # Sample of how the cmdlet was previously called

    # New
    # Sample of how the cmdlet should now be called
    ```

    Note: the above sections follow the template found in the link below:

    https://github.com/Azure/azure-powershell/blob/dev/documentation/breaking-changes/breaking-change-template.md
    -->";

        /**
         * This functiontakesn ina Type (expected to be a derevative of the AzurePSCmdlet)
         * And reads all the deprecation attributes attached to it
         * Prints a message on the cmdline For each of the attribute found
         * 
         * the boundParameterNames is a list of parameters bound to the cmdlet at runtime, 
         * We only process the Parameter beaking change attributes attached only params listed in this list (if present)
         * */
        public static void ProcessCustomAttributesAtRuntime(Type type, List<string> boundParameterNames)
        {
            bool emitWarningOrError = true;

            try
            {
                emitWarningOrError = Boolean.Parse(System.Environment.GetEnvironmentVariable(SUPPRESS_ERROR_OR_WARNING_MESSAGE_ENV_VARIABLE_NAME));
            }
            catch (Exception)
            {
                //no action
            }

            if (!emitWarningOrError)
            {
                //Do not process the attributes at runtime... The env variable to override the warning messages is set
                return;
            }

            List<GenericBreakingChangeAttribute> attributes = GetAllBreakingChangeAttributesInType(type, boundParameterNames);

            if (attributes != null && attributes.Count > 0)
            {
                Console.WriteLine(string.Format(Resources.BreakingChangesAttributesHeaderMessage, Utilities.GetNameFromCmdletType(type)));
            }

            foreach (GenericBreakingChangeAttribute attribute in attributes)
            {
                attribute.PrintCustomAttributeInfo(type, false);
            }
        }

        /**
         * Takes in atype which is expected to be derived from AzurePScmdlet and gathers all the breaking change attributes on it
         * Runs through the list of attributes and returns the deprecation messages for each of the attributes. 
         * This function is sued from the static analysis tools to generate the breaking change guide 
         **/
        public static List<string> GetBreakingChangeMessagesForType(Type type)
        {
            List<string> messages = new List<string>();

            //This is used as a migration guide, we need to process all properties/fields, moreover at this point of time we do not have a list of all the
            //bound params anyways
            foreach (GenericBreakingChangeAttribute attribute in GetAllBreakingChangeAttributesInType(type, null))
            {
                messages.Add(attribute.GetBreakingChangeTextFromAttribute(type, true));
            }

            return messages;
        }

        /**
         * This functiontakesn in a Type (expected to be a derevative of the AzurePSCmdlet)
         * And returns all the deprecation attributes attached to it
         * 
         * the boundParameterNames is a list of parameters bound to the cmdlet at runtime, 
         * We only process the Parameter beaking change attributes attached only params listed in this list (if present)
         * */
        **/
        public static List<GenericBreakingChangeAttribute> GetAllBreakingChangeAttributesInType(Type type, List<string> boundParameterNames)
        {
            List<GenericBreakingChangeAttribute> attributeList = new List<GenericBreakingChangeAttribute>();

            attributeList.AddRange(type.GetCustomAttributes(typeof(GenericBreakingChangeAttribute), false).Cast<GenericBreakingChangeAttribute>());

            foreach (MethodInfo m in type.GetRuntimeMethods())
            {
                attributeList.AddRange(m.GetCustomAttributes(typeof(GenericBreakingChangeAttribute), false).Cast<GenericBreakingChangeAttribute>());
            }

            //Only process the fields if the bound params have the field Name in them. If the bound params is null then
            //we do no filtering
            foreach (FieldInfo f in type.GetRuntimeFields())
            {
                if ((boundParameterNames != null && boundParameterNames.Contains(f.Name)) || (boundParameterNames == null))
                {
                    attributeList.AddRange(f.GetCustomAttributes(typeof(GenericBreakingChangeAttribute), false).Cast<GenericBreakingChangeAttribute>());
                }
            }

            //Only process the property if the bound params have the prop Name in them. If the bound params is null then
            //we do no filtering
            foreach (PropertyInfo p in type.GetRuntimeProperties())
            {
                if ((boundParameterNames != null && boundParameterNames.Contains(p.Name)) || (boundParameterNames == null))
                {
                    attributeList.AddRange(p.GetCustomAttributes(typeof(GenericBreakingChangeAttribute), false).Cast<GenericBreakingChangeAttribute>());
                }
            }

            return attributeList;
        }
    }
}
