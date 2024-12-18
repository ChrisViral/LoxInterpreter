﻿using Lox.Interpreter.Scanner;

namespace Lox.Interpreter.Syntax.Expressions;

/// <summary>
/// Variable expression
/// </summary>
/// <param name="Identifier">Variable identifier</param>
/// <param name="Value">Value to assign</param>
public sealed record AssignmentExpression(in Token Identifier, LoxExpression Value) : LoxExpression
{
    /// <inheritdoc />
    public override void Accept(IExpressionVisitor visitor) => visitor.VisitAssignmentExpression(this);

    /// <inheritdoc />
    public override T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitAssignmentExpression(this);
}
