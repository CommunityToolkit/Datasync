// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace CommunityToolkit.Datasync.Client.Query.Linq;

/// <summary>
/// A version of <see cref="MemberInfo"/> that can be used as a key in
/// an <see cref="IDictionary{TKey, TValue}"/> object.
/// </summary>
internal class MemberInfoKey : IEquatable<MemberInfoKey>
{
    private static readonly Type[] emptyTypeParameters = [];

    // Information about the class member
    private readonly Type type;
    private readonly string memberName;
    private readonly bool isMethod;
    private readonly bool isInstance;
    private readonly Type[] parameters;

    /// <summary>
    /// Construct a <see cref="MemberInfoKey"/> based on a <see cref="MemberInfo"/> object
    /// </summary>
    /// <param name="memberInfo">The <see cref="MemberInfo"/> object to use.</param>
    public MemberInfoKey(MemberInfo memberInfo)
    {
        this.memberName = memberInfo.Name;
        this.type = memberInfo.DeclaringType;

        if (memberInfo is MethodInfo asMethod)
        {
            this.isMethod = true;
            this.isInstance = !asMethod.IsStatic;
            this.parameters = asMethod.GetParameters().Select(p => p.ParameterType).ToArray();
        }
        else if (memberInfo is PropertyInfo asProperty)
        {
            this.isMethod = false;
            this.isInstance = true;
            this.parameters = emptyTypeParameters;
        }
        else
        {
            throw new ArgumentException("All MemberInfoKey instances must be either methods or properties", nameof(memberInfo));
        }
    }

    /// <summary>
    /// Construct a <see cref="MemberInfoKey"/> explicitly.
    /// </summary>
    /// <param name="type">The type of the class that contains the member.</param>
    /// <param name="memberName"> The name of the class member.</param>
    /// <param name="isMethod">true if the member is a method</param>
    /// <param name="isInstance">true if the member is an instance member</param>
    /// <param name="parameters">Types of the member for parameters</param>
    public MemberInfoKey(Type type, string memberName, bool isMethod, bool isInstance, params Type[] parameters)
    {
        this.type = type;
        this.memberName = memberName;
        this.isInstance = isInstance;
        this.isMethod = isMethod;
        this.parameters = parameters;
    }

    #region IEquatable<T>
    /// <summary>
    /// Compares two <see cref="MemberInfoKey"/> objects for equality
    /// </summary>
    /// <param name="other">The object for comparison</param>
    /// <returns>True if the object matches</returns>
    public bool Equals(MemberInfoKey other)
        => other.type == this.type && other.isMethod == this.isMethod && other.isInstance == this.isInstance
            && string.Equals(other.memberName, this.memberName, StringComparison.Ordinal)
            && this.parameters.SequenceEqual(other.parameters);
    #endregion

    #region IEquality Overrides
    /// <summary>
    /// We need to override <see cref="Equals(object)"/> because we are implementing <see cref="IEquatable{T}"/>
    /// which adds the type-specific <c>Equals()</c> method.
    /// </summary>
    /// <param name="obj">The object for comparison</param>
    /// <returns>True if the object matches</returns>
    public override bool Equals(object obj) => obj is MemberInfoKey other && Equals(other);

    /// <summary>
    /// We need to override <see cref="GetHashCode"/> because we are implementing <see cref="IEquatable{T}"/>
    /// which adds the type-specific <c>Equals()</c> method.
    /// </summary>
    /// <returns>The hash code for the object</returns>
    public override int GetHashCode() => this.memberName.GetHashCode() | this.type.GetHashCode();
    #endregion
}
