// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using NJsonSchema;
using NJsonSchema.Generation;

namespace CommunityToolkit.Datasync.Server.NSwag;

/// <summary>
/// NSwag Schema processor for the Community Datasync Toolkit.
/// </summary>
public class DatasyncSchemaProcessor : ISchemaProcessor
{
    /// <summary>
    /// List of the system properties within the <see cref="ITableData"/> interface.
    /// </summary>
    private static readonly string[] systemProperties = ["deleted", "updatedAt", "version"];

    /// <summary>
    /// Processes each schema in turn, doing required modifications.
    /// </summary>
    /// <param name="context">The schema processor context.</param>
    public void Process(SchemaProcessorContext context)
    {
        if (context.ContextualType.Type.GetInterfaces().Contains(typeof(ITableData)))
        {
            foreach (KeyValuePair<string, JsonSchemaProperty> prop in context.Schema.Properties)
            {
                if (systemProperties.Contains(prop.Key))
                {
                    prop.Value.IsReadOnly = true;
                }
            }
        }
    }
}
