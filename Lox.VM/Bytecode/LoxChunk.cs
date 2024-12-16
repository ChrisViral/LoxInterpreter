﻿using System.Collections;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Lox.VM.Runtime;

namespace Lox.VM.Bytecode;

/// <summary>
/// Constant value type
/// </summary>
public enum ConstantType : byte
{
    CONSTANT   = LoxOpcode.CONSTANT_8,
    NDF_GLOBAL = LoxOpcode.NDF_GLOBAL_8,
    DEF_GLOBAL = LoxOpcode.DEF_GLOBAL_8,
    GET_GLOBAL = LoxOpcode.GET_GLOBAL_8,
    SET_GLOBAL = LoxOpcode.SET_GLOBAL_8
}

/// <summary>
/// Lox bytecode chunk
/// </summary>
[PublicAPI]
public partial class LoxChunk : IList<byte>, IReadOnlyList<byte>, IDisposable
{
    #region Constants
    /// <summary>
    /// Maximum constant index value (24bits)
    /// </summary>
    public const int MAX_CONSTANT = 1 << 24;
    #endregion

    #region Fields
    private int version;
    private readonly List<byte> code       = [];
    private readonly List<LoxValue> values = [];
    private readonly List<int> lines       = [];
    #endregion

    #region Properties
    /// <inheritdoc cref="List{T}.Count" />
    public int Count => this.code.Count;

    /// <summary>
    /// If this object has been disposed or not
    /// </summary>
    public bool IsDisposed { get; private set; }
    #endregion

    #region Indexer
    /// <inheritdoc cref="List{T}.this[int]" />
    public byte this[int index]
    {
        get => this.code[index];
        set => this.code[index] = value;
    }

    /// <inheritdoc cref="List{T}.this" />
    public byte this[Index index]
    {
        get => this.code[index];
        set => this.code[index] = value;
    }

    /// <summary>
    /// Gets a readonly span over a specified range of the bytecode
    /// </summary>
    /// <param name="range">Range to get</param>
    public ReadOnlySpan<byte> this[Range range] => CollectionsMarshal.AsSpan(this.code)[range];
    #endregion

    #region Constructors
    /// <summary>
    /// Chunk finalizer
    /// </summary>
    ~LoxChunk() => Dispose();
    #endregion

    #region Methods
    /// <summary>
    /// Span of the bytecode
    /// </summary>
    public ReadOnlySpan<byte> AsSpan() => CollectionsMarshal.AsSpan(this.code);

    /// <summary>
    /// Adds the given opcode to the chunk
    /// </summary>
    /// <param name="opcode">Opcode to add</param>
    /// <param name="line">Line for this opcode</param>
    public void AddOpcode(LoxOpcode opcode, int line)
    {
        this.version++;
        this.code.Add((byte)opcode);
        AddLine(line);
    }

    /// <summary>
    /// Adds a constant to the chunk
    /// </summary>
    /// <param name="value">Constant to add</param>
    /// <param name="line">Line for this constant</param>
    /// <param name="index">Index the constant was added at</param>
    /// <param name="type">Constant value type</param>
    /// <returns><see langword="true"/> if the constant was successfully added, otherwise <see langword="false"/> if the constant limit has been reached</returns>
    public bool AddConstant(in LoxValue value, int line, out int index, ConstantType type)
    {
        index = this.values.Count;
        if (index >= MAX_CONSTANT) return false;

        this.values.Add(value);
        AddConstantOpcode(index, line, type);
        return true;
    }

    /// <summary>
    /// Adds a constant opcode for the constant at the given index
    /// </summary>
    /// <param name="index">Index of the constant to add the opcode for</param>
    /// <param name="line">Opcode line</param>
    /// <param name="type">Constant value type</param>
    public void AddIndexedConstant(int index, int line, ConstantType type)
    {
        if (index is < 0 or >= MAX_CONSTANT) throw new ArgumentOutOfRangeException(nameof(index), index, "Constant index outside of range [0, 2^24[");

        AddConstantOpcode(index, line, type);
    }

    /// <summary>
    /// Adds a constant opcode for the constant at the given index
    /// </summary>
    /// <param name="index">Index of the constant to add the opcode for</param>
    /// <param name="line">Opcode line</param>
    /// <param name="type">Constant value type</param>
    public void AddConstantOpcode(int index, int line, ConstantType type)
    {
        this.version++;
        byte opcode = (byte)type;
        switch (index)
        {
            case <= byte.MaxValue:
                this.code.AddRange((byte)opcode, (byte)index);
                AddLine(line, 2);
                break;

            case <= ushort.MaxValue:
            {
                Span<byte> bytes = stackalloc byte[sizeof(ushort)];
                BitConverter.TryWriteBytes(bytes, (ushort)index);
                this.code.AddRange((byte)(opcode + 1), bytes[0], bytes[1]);
                AddLine(line, 3);
                break;
            }

            default:
            {
                Span<byte> bytes = stackalloc byte[sizeof(int)];
                BitConverter.TryWriteBytes(bytes, index);
                this.code.AddRange((byte)(opcode + 2), bytes[0], bytes[1], bytes[2]);
                AddLine(line, 4);
                break;
            }
        }
    }

    /// <summary>
    /// Gets a constant's value from the chunk
    /// </summary>
    /// <param name="index">Constant index to get</param>
    /// <returns>The constants value</returns>
    public ref LoxValue GetConstant(int index) => ref CollectionsMarshal.AsSpan(this.values)[index];

    /// <summary>
    /// Adds a given line entry
    /// </summary>
    /// <param name="line">Line to add</param>
    /// <param name="repeats">How many time the line appears</param>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="line"/> is smaller than zero</exception>
    private void AddLine(int line, int repeats = 1)
    {
        if (line < 0) throw new ArgumentOutOfRangeException(nameof(line), line, "Line number cannot be negative");

        // No lines stored
        if (this.lines.Count is 0)
        {
            if (repeats > 1)
            {
                this.lines.Add(-repeats);
            }
            this.lines.Add(line);
            return;
        }

        // Get the lines as a span for easier manipulation
        Span<int> linesSpan = CollectionsMarshal.AsSpan(this.lines);
        ref int lastLine = ref linesSpan[^1];
        // Previous line is different from current line
        if (lastLine != line)
        {
            if (repeats > 1)
            {
                this.lines.Add(-repeats);
            }

            this.lines.Add(line);
            return;
        }

        // Only one value stored
        if (linesSpan.Length is 1)
        {
            // Set the last line to an encoding value and push the line to the end
            lastLine = -repeats - 1;
            this.lines.Add(line);
            return;
        }

        // Get encoding value
        ref int currentEncoding = ref linesSpan[^2];
        // Value before is not an encoding value
        if (currentEncoding >= 0)
        {
            lastLine = -repeats - 1;
            this.lines.Add(line);
            return;
        }

        // Decrease encoding value
        currentEncoding -= repeats;
    }

    /// <summary>
    /// Get the line number of the specified bytecode
    /// </summary>
    /// <param name="index">Bytecode index</param>
    /// <returns>The line number for this bytecode index</returns>
    public int GetLine(int index)
    {
        int offset = 0;
        int currentLine;
        do
        {
            currentLine = this.lines[offset++];
            int currentEncoding = -1;
            if (currentLine < 0)
            {
                currentEncoding = currentLine;
                currentLine     = this.lines[offset++];
            }

            index += currentEncoding;
        }
        while (index >= 0);

        return currentLine;
    }

    /// <summary>
    /// Get bytecode info at the given index
    /// </summary>
    /// <param name="index">Bytecode index</param>
    /// <returns>A tuple containing the bytecode and line for the given index</returns>
    public (byte bytecode, int line) GetBytecodeInfo(int index) => (this.code[index], GetLine(index));

    /// <inheritdoc cref="List{T}.Clear" />
    public void Clear()
    {
        this.version = 0;
        this.code.Clear();
        this.lines.Clear();
    }

    /// <summary>
    /// Grabs an array of the bytecode
    /// </summary>
    /// <returns>Bytecode array</returns>
    public byte[] ToBytecodeArray() => this.code.ToArray();

    /// <inheritdoc cref="List{T}.GetEnumerator" />
    public List<byte>.Enumerator GetEnumerator() => this.code.GetEnumerator();

    /// <summary>
    /// Gets a bytecode enumerator over this chunk
    /// </summary>
    /// <returns>Bytecode enumerator</returns>
    public BytecodeEnumerator GetBytecodeEnumerator() => new(this);

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (LoxValue value in this.values)
        {
            value.FreeResources();
        }
        this.values.Clear();
        GC.SuppressFinalize(this);
    }
    #endregion

    #region Explicit interface implementations
    /// <inheritdoc />
    bool ICollection<byte>.IsReadOnly => false;

    /// <inheritdoc />
    void ICollection<byte>.Add(byte item) => this.code.Add(item);

    /// <inheritdoc />
    bool ICollection<byte>.Contains(byte item) => this.code.Contains(item);

    /// <inheritdoc />
    void ICollection<byte>.CopyTo(byte[] array, int arrayIndex) => this.code.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    bool ICollection<byte>.Remove(byte item) => this.code.Remove(item);

    /// <inheritdoc />
    int IList<byte>.IndexOf(byte item) => this.code.IndexOf(item);

    /// <inheritdoc />
    void IList<byte>.Insert(int index, byte item) => this.code.Insert(index, item);

    /// <inheritdoc />
    void IList<byte>.RemoveAt(int index) => this.code.RemoveAt(index);

    /// <inheritdoc />
    IEnumerator<byte> IEnumerable<byte>.GetEnumerator() => this.code.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.code).GetEnumerator();
    #endregion
}
