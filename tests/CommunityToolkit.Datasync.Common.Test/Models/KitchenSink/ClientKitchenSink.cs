// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Spatial;

#nullable enable

namespace CommunityToolkit.Datasync.Common.Test.Models;

[ExcludeFromCodeCoverage]
public class ClientKitchenSink : ClientTableData, IKitchenSink
{
    public bool BooleanValue { get; set; }
    public int IntValue { get; set; }
    public long LongValue { get; set; }
    public decimal DecimalValue { get; set; }
    public double DoubleValue { get; set; }
    public float FloatValue { get; set; }
    public double? NullableDouble { get; set; }
    public char CharValue { get; set; }
    public string? StringValue { get; set; }
    public byte ByteValue { get; set; }
    public byte[]? ByteArrayValue { get; set; }
    public KitchenSinkState EnumValue { get; set; }
    public KitchenSinkState? NullableEnumValue { get; set; }
    public Guid? GuidValue { get; set; }
    public DateTime DateTimeValue { get; set; }
    public DateTimeOffset DateTimeOffsetValue { get; set; }
    public DateOnly DateOnlyValue { get; set; }
    public TimeOnly TimeOnlyValue { get; set; }
    public GeographyPoint? PointValue { get; set; }
}
