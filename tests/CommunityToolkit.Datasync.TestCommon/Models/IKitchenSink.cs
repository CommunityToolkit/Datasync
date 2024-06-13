// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Spatial;

#nullable enable

namespace CommunityToolkit.Datasync.TestCommon.Models;

public enum KitchenSinkState
{
    None,
    Completed,
    Failed
}

public interface IKitchenSink
{
    // Boolean types
    bool BooleanValue { get; set; }

    // Number types
    int IntValue { get; set; }
    long LongValue { get; set; }
    decimal DecimalValue { get; set; }
    double DoubleValue { get; set; }
    float FloatValue { get; set; }
    double? NullableDouble { get; set; }

    // String types
    char CharValue { get; set; }
    string? StringValue { get; set; }
    byte ByteValue { get; set; }
    byte[]? ByteArrayValue { get; set; }

    // Enum types
    KitchenSinkState EnumValue { get; set; }
    KitchenSinkState? NullableEnumValue { get; set; }

    // Complex types
    Guid? GuidValue { get; set; }

    // Date/time types
    DateTime DateTimeValue { get; set; }
    DateTimeOffset DateTimeOffsetValue { get; set; }
    DateOnly DateOnlyValue { get; set; }
    TimeOnly TimeOnlyValue { get; set; }

    // Geospatial types
    GeographyPoint? PointValue { get; set; }
}
