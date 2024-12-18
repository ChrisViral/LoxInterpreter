﻿using JetBrains.Annotations;
using Lox.Common.Exceptions;

namespace Lox.Interpreter.Exceptions;

/// <summary>
/// Lox invalid native definition exception
/// </summary>
[PublicAPI]
public sealed class LoxInvalidNativeDefinitionException : LoxException
{
    /// <inheritdoc />
    public LoxInvalidNativeDefinitionException() { }

    /// <inheritdoc />
    public LoxInvalidNativeDefinitionException(string? message) : base(message) { }

    /// <inheritdoc />
    public LoxInvalidNativeDefinitionException(string? message, Exception? innerException) : base(message, innerException) { }
}
