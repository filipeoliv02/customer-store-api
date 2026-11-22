using System;
using System.Diagnostics.CodeAnalysis;

namespace CustomerStoreApi.Managers;

/// <summary>
/// Describes the result of an operation.
/// </summary>
/// <typeparam name="T">The type of the result value.</typeparam>
public sealed class Result<T>
{
    private Result(T? value, string? errorCode, string? errorDescription)
    {
        this.Value = value;
        this.ErrorCode = errorCode;
        this.ErrorDescription = errorDescription;
    }

    /// <summary>
    /// Gets a value indicating whether the result represents a failure.
    /// </summary>
    [MemberNotNullWhen(true, nameof(ErrorCode), nameof(ErrorDescription))]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool Failed => this.ErrorCode is not null;

    /// <summary>
    /// Gets the value. Only meaningful when <see cref="Failed"/> is <c>false</c>.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the error code. Only set when <see cref="Failed"/> is <c>true</c>.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Gets the error description. Only set when <see cref="Failed"/> is <c>true</c>.
    /// </summary>
    public string? ErrorDescription { get; }

    /// <summary>
    /// Creates a new instance of <see cref="Result{T}" /> that represents success.
    /// </summary>
    /// <param name="value">The result value.</param>
    /// <returns>The <see cref="Result{T}" /> instance.</returns>
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "By design.")]
    public static Result<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new Result<T>(value, null, null);
    }

    /// <summary>
    /// Creates a new instance of <see cref="Result{T}" /> that represents a failure.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorDescription">The error description.</param>
    /// <returns>The <see cref="Result{T}" /> instance.</returns>
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "By design.")]
    public static Result<T> Failure(string errorCode, string errorDescription)
    {
        ArgumentException.ThrowIfNullOrEmpty(errorCode);
        ArgumentNullException.ThrowIfNull(errorDescription);

        return new Result<T>(default, errorCode, errorDescription);
    }

    /// <summary>
    /// Gets a value indicating whether the result represents a failure with the specified error code.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <returns>A value indicating whether the result represents a failure with the specified error code.</returns>
    public bool FailedWith(string errorCode) =>
        string.Equals(this.ErrorCode, errorCode, StringComparison.OrdinalIgnoreCase);
}
