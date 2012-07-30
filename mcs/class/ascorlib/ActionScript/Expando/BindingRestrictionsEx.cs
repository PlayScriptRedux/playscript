/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Apache License, Version 2.0, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System.Linq.Expressions;

using System;
using System.Dynamic;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Runtime.CompilerServices;

namespace ActionScript.Expando {

    /// <summary>
    /// Represents a set of binding restrictions on the <see cref="DynamicMetaObject"/>under which the dynamic binding is valid.
    /// </summary>
    public abstract class BindingRestrictionsEx {

        /// <summary>
        /// The method takes a DynamicMetaObject, and returns an instance restriction for testing null if the object
        /// holds a null value, otherwise returns a type restriction.
        /// </summary>
        internal static BindingRestrictions GetTypeRestriction(DynamicMetaObject obj) {
            if (obj.Value == null && obj.HasValue) {
                return BindingRestrictions.GetInstanceRestriction(obj.Expression, null);
            } else {
                return BindingRestrictions.GetTypeRestriction(obj.Expression, obj.LimitType);
            }
        }


    }
}
