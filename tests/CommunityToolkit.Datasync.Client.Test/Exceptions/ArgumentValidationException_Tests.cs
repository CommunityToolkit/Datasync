// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;

namespace CommunityToolkit.Datasync.Client.Test.Exceptions;

[ExcludeFromCodeCoverage]
public class ArgumentValidationException_Tests
{
    [Fact]
    public void Validation_Pass()
    {
        TestObject obj = new() { Value = 1 };
        Action act = () => ArgumentValidationException.ThrowIfNotValid(obj, nameof(obj));
        act.Should().NotThrow();
    }

    [Fact]
    public void Validation_Throws()
    {
        TestObject obj = new() { Value = 0 };
        Action act = () => ArgumentValidationException.ThrowIfNotValid(obj, nameof(obj));
        ArgumentValidationException ex = act.Should().Throw<ArgumentValidationException>().Subject.First();
        ex.Message.Should().Be("Object is not valid (Parameter 'obj')");
        ex.ValidationErrors.Should().HaveCount(1);
        ex.ValidationErrors[0].ErrorMessage.Should().Be("Foo");
    }

    [Fact]
    public void Validation_Throws_CustomMessage()
    {
        TestObject obj = new() { Value = 0 };
        Action act = () => ArgumentValidationException.ThrowIfNotValid(obj, nameof(obj), "custom message");
        ArgumentValidationException ex = act.Should().Throw<ArgumentValidationException>().Subject.First();
        ex.Message.Should().Be("custom message (Parameter 'obj')");
        ex.ValidationErrors.Should().HaveCount(1);
        ex.ValidationErrors[0].ErrorMessage.Should().Be("Foo");
    }

    [Fact]
    public void Validation_Throws_Null()
    {
        TestObject obj = null;
        Action act = () => ArgumentValidationException.ThrowIfNotValid(obj, nameof(obj));
        act.Should().Throw<ArgumentNullException>();
    }

    internal class TestObject
    {
        [Range(1, 8, ErrorMessage = "Foo")]
        public int Value { get; set; } = 1;
    }
}
