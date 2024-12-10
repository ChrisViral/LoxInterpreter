﻿namespace Lox.Runtime.Types;

/// <summary>
/// Lox custom class object
/// </summary>
public abstract class LoxObject
{
    /// <inheritdoc cref="object.ToString"/>
    public override string ToString() => "[obj]";
}