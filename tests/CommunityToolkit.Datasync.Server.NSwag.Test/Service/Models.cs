// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using CommunityToolkit.Datasync.TestCommon.Models;
using Microsoft.Spatial;

namespace CommunityToolkit.Datasync.Server.NSwag.Test.Service;

[ExcludeFromCodeCoverage]
public class TodoItem : EntityTableData
{
    public string Title { get; set; }
}

[ExcludeFromCodeCoverage]
public class KitchenSink : EntityTableData
{
    public bool BooleanValue { get; set; }
    public byte ByteValue { get; set; }
    public byte[] ByteArrayValue { get; set; }
    public char CharValue { get; set; }
    public DateOnly DateOnlyValue { get; set; }
    public DateTime DateTimeValue { get; set; }
    public DateTimeOffset DateTimeOffsetValue { get; set; }
    public decimal DecimalValue { get; set; }
    public double DoubleValue { get; set; }
    public KitchenSinkState EnumValue { get; set; }
    public float FloatValue { get; set; }
    public Guid? GuidValue { get; set; }
    public int IntValue { get; set; }
    public long LongValue { get; set; }
    public double? NullableDouble { get; set; }
    public KitchenSinkState? NullableEnumValue { get; set; }
    public string StringValue { get; set; }
    public TimeOnly TimeOnlyValue { get; set; }
}
