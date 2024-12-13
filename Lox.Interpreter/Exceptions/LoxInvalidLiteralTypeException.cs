﻿using JetBrains.Annotations;
using Lox.Common.Exceptions;

namespace Lox.Interpreter.Exceptions;

/// <summary>
/// Invalid literal type exception
/// </summary>
[PublicAPI]
public sealed class LoxInvalidLiteralTypeException : LoxException
{
    /// <inheritdoc />
    public LoxInvalidLiteralTypeException() { }

    /// <inheritdoc />
    public LoxInvalidLiteralTypeException(string? message) : base(message) { }

    /// <inheritdoc />
    public LoxInvalidLiteralTypeException(string? message, Exception? innerException) : base(message, innerException) { }
}
