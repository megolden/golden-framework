namespace Golden
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Linq;
	using System.ComponentModel;
	using System.Globalization;
	using System.Diagnostics;

    /// <summary>
    /// A class for manipulation and conversion binary, octal or hexadecimal numeric values to clr primitive numeric types.
    /// </summary>
	[DebuggerDisplay("{ToDecimal()}")]
	[TypeConverter(typeof(BinaryExpressionConverter))]
	public class BinaryExpression : IComparable, IComparable<BinaryExpression>, IEquatable<BinaryExpression>, System.Collections.IEnumerable, IEnumerable<bool>, ICloneable, IEqualityComparer<BinaryExpression>, IConvertible
	{
		#region Fields

		private byte[] _Bytes;
		private int _Length;

        #endregion
        #region Properties

        /// <summary>
        /// Gets or sets number of bits.
        /// </summary>
        public int Length
		{
			get { return _Length; }
			set { SetLength(value); }
		}
        /// <summary>
        /// Gets or sets boolean value of bit with specified index.
        /// </summary>
        /// <param name="index">Index of bit</param>
        /// <returns>boolean value of bit</returns>
		public bool this[int index]
		{
			get { return Get(index); }
			set { Set(index, value); }
		}

		#endregion
		#region Methods

		public BinaryExpression(string value, int fromBase)
		{
			BinaryExpression exp;
			if (IsValid(value, fromBase, out exp) == false) throw new ArgumentOutOfRangeException(nameof(value));
			Update(exp._Bytes, exp._Length, true);
		}
		public BinaryExpression(bool[] value)
		{
			var bytes = new byte[(int)Math.Ceiling(value.Length / 8F)];
			for (int i = 0; i < value.Length; i++)
			{
				if (value[i]) bytes[(i / 8)] |= (byte)Math.Pow(2D, (double)(i % 8));
			}
			Update(bytes, value.Length, true);
		}
		public BinaryExpression(string value) : this(value, 2) { }
		public BinaryExpression(byte[] value, int bitCount) : this(value, bitCount, false) { }
		public BinaryExpression(byte[] value) : this(value, value.Length * 8) { }
		public BinaryExpression(int length) : this(new byte[(int)Math.Ceiling(length / 8F)], length) { }
		private BinaryExpression(byte[] value, int bitCount, bool setDirect)
		{
			Update(value, bitCount, setDirect);
		}
		private void Update(byte[] value, int bitCount, bool setDirect = false)
		{
			if (setDirect)
			{
				_Bytes = value;
			}
			else
			{
				var byteLen = (int)Math.Ceiling(bitCount / 8F);
				_Bytes = new byte[byteLen];
				Array.Copy(value, 0, _Bytes, 0, byteLen);
			}
			_Length = bitCount;
		}
		private void SetLength(int value)
		{
			if (value == _Length) return;
			_Length = value;
			int rem;
			var newBLen = Math.DivRem(value, 8, out rem);
			if (rem != 0) newBLen++;
			if (_Bytes.Length != newBLen) Array.Resize<byte>(ref _Bytes, newBLen);
			if (rem != 0) _Bytes[newBLen - 1] &= MakeByte(rem);
		}
		private static byte MakeByte(int trueBits)
		{
			switch (trueBits)
			{
				case 0:
					return (byte)0;
				case 1:
					return (byte)1;
				case 2:
					return (byte)3;
				case 3:
					return (byte)7;
				case 4:
					return (byte)15;
				case 5:
					return (byte)31;
				case 6:
					return (byte)63;
				case 7:
					return (byte)127;
				case 8:
					return (byte)255;
			}
			throw new InvalidOperationException();
		}
		private static byte MakeByte(params bool[] bits)
		{
			if (bits.Length > 8) throw new OverflowException();
			byte result = (byte)0;
			for (int i = 0; i < bits.Length; i++)
			{
				if (bits[i]) result += (byte)Math.Pow(2D, (double)i);
			}
			return result;
		}
		private static char MakeHexByte(byte value)
		{
			if (value >= (byte)10) return (char)('a' + (value - (byte)10));
			return (char)((byte)'0' + value);
		}
		private static string GetOctalBinary(byte value)
		{
			switch (value)
			{
				case 0:
					return "000";
				case 1:
					return "001";
				case 2:
					return "010";
				case 3:
					return "011";
				case 4:
					return "100";
				case 5:
					return "101";
				case 6:
					return "110";
				case 7:
					return "111";
			}
			throw new InvalidCastException();
		}
		private static string GetHexBinary(byte value)
		{
			switch (value)
			{
				case 0:
					return "0000";
				case 1:
					return "0001";
				case 2:
					return "0010";
				case 3:
					return "0011";
				case 4:
					return "0100";
				case 5:
					return "0101";
				case 6:
					return "0110";
				case 7:
					return "0111";
				case 8:
					return "1000";
				case 9:
					return "1001";
				case 10:
					return "1010";
				case 11:
					return "1011";
				case 12:
					return "1100";
				case 13:
					return "1101";
				case 14:
					return "1110";
				case 15:
					return "1111";
			}
			throw new InvalidCastException();
		}
		private int GetEffectiveLength()
		{
			if (_Length == 0) return 0;
			int lastTrueIndex = _Length - 1;
			while (lastTrueIndex >= 0 && !Get(lastTrueIndex)) lastTrueIndex--;
			return (lastTrueIndex + 1);
		}
		public void Set(int index, bool value)
		{
			if (index < 0 || index >= _Length) throw new IndexOutOfRangeException();
			int rem;
			var div = Math.DivRem(index, 8, out rem);
			var bit = (byte)Math.Pow(2D, (double)rem);
			if (value)
				_Bytes[div] |= bit;
			else
				_Bytes[div] &= (byte)(~bit);
		}
		public bool Get(int index)
		{
			if (index < 0 || index >= _Length) throw new IndexOutOfRangeException();
			int rem;
			var div = Math.DivRem(index, 8, out rem);
			return ((_Bytes[div] & (byte)Math.Pow(2D, (double)rem)) != 0);
		}
		/// <summary>
		/// Performs the bitwise AND operation on the elements in the current <see cref="BinaryExpression"/> against the corresponding elements in the specified <see cref="BinaryExpression"/>.
		/// </summary>
		/// <param name="exp">The <see cref="BinaryExpression"/> with which to perform the bitwise AND operation.</param>
		/// <returns>
		/// The current instance containing the result of the bitwise AND operation on the elements in the current <see cref="BinaryExpression"/> against the corresponding elements in the specified <see cref="BinaryExpression"/>
		/// </returns>
		public BinaryExpression And(BinaryExpression exp)
		{
			SetLength(Math.Max(_Length, exp._Length));
			for (int i = 0; i < _Length; i++)
			{
				if (i < exp._Length)
				{
					var flag1 = Get(i);
					if (flag1) Set(i, exp.Get(i));
				}
				else
				{
					Set(i, false);
				}
			}
			return this;
		}
		/// <summary>
		/// Performs the bitwise OR operation on the elements in the current <see cref="BinaryExpression"/> against the corresponding elements in the specified <see cref="BinaryExpression"/>.
		/// </summary>
		/// <param name="exp">The <see cref="BinaryExpression"/> with which to perform the bitwise OR operation.</param>
		/// <returns>
		/// The current instance containing the result of the bitwise OR operation on the elements in the current <see cref="BinaryExpression"/> against the corresponding elements in the specified <see cref="BinaryExpression"/>
		/// </returns>
		public BinaryExpression Or(BinaryExpression exp)
		{
			SetLength(Math.Max(_Length, exp._Length));
			for (int i = 0; i < _Length; i++)
			{
				if (i < exp._Length)
				{
					var flag1 = Get(i);
					if (!flag1) Set(i, exp.Get(i));
				}
			}
			return this;
		}
		/// <summary>
		/// Performs the bitwise exclusive OR operation on the elements in the current <see cref="BinaryExpression"/> against the corresponding elements in the specified <see cref="BinaryExpression"/>.
		/// </summary>
		/// <param name="exp">The <see cref="BinaryExpression"/> with which to perform the bitwise exclusive OR operation.</param>
		/// <returns>
		/// The current instance containing the result of the bitwise exclusive OR operation on the elements in the current <see cref="BinaryExpression"/> against the corresponding elements in the specified <see cref="BinaryExpression"/>
		/// </returns>
		public BinaryExpression Xor(BinaryExpression exp)
		{
			SetLength(Math.Max(_Length, exp._Length));
			for (int i = 0; i < _Length; i++)
			{
				if (i < exp._Length)
				{
					Set(i, Get(i) ^ exp.Get(i));
				}
				else
				{
					Set(i, Get(i) ^ false);
				}
			}
			return this;
		}
		/// <summary>
		/// Inverts all the bit values in the current <see cref="BinaryExpression"/> instance.
		/// </summary>
		/// <returns>The current instance with inverted bit values.</returns>
		public BinaryExpression Not()
		{
			if (_Length == 0) return this;
			for (int i = 0; i < _Length; i++)
			{
				Set(i, !Get(i));
			}
			return this;
		}
		/// <summary>
		/// Trims ineffective zero bits from the current <see cref="BinaryExpression"/> instance.
		/// </summary>
		/// <returns>The current instance with trimmed ineffective zero bits.</returns>
		public BinaryExpression Trim()
		{
			SetLength(GetEffectiveLength());
			return this;
		}
		/// <summary>
		/// Shifts bit values of this <see cref="BinaryExpression"/> instance by the number of bits specified by <paramref name="count"/> parameter.
		/// </summary>
		/// <param name="count">The number of bits to shift.</param>
		/// <returns>The current instance with shifted bits.</returns>
		public BinaryExpression ShiftLeft(int count)
		{
			return MoveBitsLeft(count, false);
		}
		/// <summary>
		/// Performs circular shift operation on bit values of this <see cref="BinaryExpression"/> instance by the number of bits specified by <paramref name="count"/> parameter.
		/// </summary>
		/// <param name="count">The number of bits to shift.</param>
		/// <returns>The current instance with shifted bits.</returns>
		public BinaryExpression RotateLeft(int count)
		{
			return MoveBitsLeft(count, true);
		}
		private BinaryExpression MoveBitsLeft(int count, bool circular)
		{
			if (count < 0) throw new InvalidOperationException();

			if (count == 0) return this;

			var srcBin = this.ToString(2);
			var retBin = new StringBuilder();
			if (count >= srcBin.Length && !circular)
			{
				//Reset all bits of this instance to zero.
				Update(new byte[_Bytes.Length], _Length, true);
			}
			else
			{
				count %= srcBin.Length;
				if (count == 0 && circular) return this;
				retBin.Append(srcBin.Substring(count));
				for (int i = 0; i < count; i++)
				{
					retBin.Append((circular ? srcBin[i] : '0'));
				}
				var temp = new BinaryExpression(retBin.ToString(), 2);
				Update(temp._Bytes, retBin.Length, true);
			}
			return this;
		}
		/// <summary>
		/// Shifts bit values of this <see cref="BinaryExpression"/> instance by the number of bits specified by <paramref name="count"/> parameter.
		/// </summary>
		/// <param name="count">The number of bits to shift.</param>
		/// <returns>The current instance with shifted bits.</returns>
		public BinaryExpression ShiftRight(int count)
		{
			return MoveBitsRight(count, false);
		}
		/// <summary>
		/// Performs circular shift operation on bit values of this <see cref="BinaryExpression"/> instance by the number of bits specified by <paramref name="count"/> parameter.
		/// </summary>
		/// <param name="count">The number of bits to shift.</param>
		/// <returns>The current instance with shifted bits.</returns>
		public BinaryExpression RotateRight(int count)
		{
			return MoveBitsRight(count, true);
		}
		private BinaryExpression MoveBitsRight(int count, bool circular)
		{
			if (count < 0) throw new InvalidOperationException();
			if (count == 0) return this;
			var srcBin = this.ToString(2);
			var retBin = new StringBuilder();
			if (count >= srcBin.Length && !circular)
			{
				//Reset all bits of this instance to zero.
				Update(new byte[_Bytes.Length], _Length, true);
			}
			else
			{
				count %= srcBin.Length;
				if (count == 0 && circular) return this;
				retBin.Append(srcBin.Substring(0, srcBin.Length - count));
				for (int i = 1; i <= count; i++)
				{
					retBin.Insert(0, (circular ? srcBin[srcBin.Length - i] : '0'));
				}
				var temp = new BinaryExpression(retBin.ToString(), 2);
				Update(temp._Bytes, retBin.Length, true);
			}
			return this;
		}
		public byte ToByte()
		{
			if (_Length > 8) throw new OverflowException();
			if (_Length == 0) return (byte)0;
			return _Bytes[0];
		}
		public ushort ToUInt16()
		{
			if (_Length > 16) throw new OverflowException();
			var rb = new[]
			{
				(_Bytes.Length > 0 ? _Bytes[0] : (byte)0),
				(_Bytes.Length > 1 ? _Bytes[1] : (byte)0),
			};
			return BitConverter.ToUInt16(rb, 0);
		}
		public uint ToUInt32()
		{
			if (_Length > 32) throw new OverflowException();
			var rb = new[]
			{
				(_Bytes.Length > 0 ? _Bytes[0] : (byte)0),
				(_Bytes.Length > 1 ? _Bytes[1] : (byte)0),
				(_Bytes.Length > 2 ? _Bytes[2] : (byte)0),
				(_Bytes.Length > 3 ? _Bytes[3] : (byte)0),
			};
			return BitConverter.ToUInt32(rb, 0);
		}
		public ulong ToUInt64()
		{
			if (_Length > 64) throw new OverflowException();
			var rb = new[]
			{
				(_Bytes.Length > 0 ? _Bytes[0] : (byte)0),
				(_Bytes.Length > 1 ? _Bytes[1] : (byte)0),
				(_Bytes.Length > 2 ? _Bytes[2] : (byte)0),
				(_Bytes.Length > 3 ? _Bytes[3] : (byte)0),
				(_Bytes.Length > 4 ? _Bytes[4] : (byte)0),
				(_Bytes.Length > 5 ? _Bytes[5] : (byte)0),
				(_Bytes.Length > 6 ? _Bytes[6] : (byte)0),
				(_Bytes.Length > 7 ? _Bytes[7] : (byte)0),
			};
			return BitConverter.ToUInt64(rb, 0);
		}
		public short ToInt16()
		{
			if (_Length > 16) throw new OverflowException();
			var rb = new[]
			{
				(_Bytes.Length > 0 ? _Bytes[0] : (byte)0),
				(_Bytes.Length > 1 ? _Bytes[1] : (byte)0),
			};
			return BitConverter.ToInt16(rb, 0);
		}
		public int ToInt32()
		{
			if (_Length > 32) throw new OverflowException();
			var rb = new[]
			{
				(_Bytes.Length > 0 ? _Bytes[0] : (byte)0),
				(_Bytes.Length > 1 ? _Bytes[1] : (byte)0),
				(_Bytes.Length > 2 ? _Bytes[2] : (byte)0),
				(_Bytes.Length > 3 ? _Bytes[3] : (byte)0),
			};
			return BitConverter.ToInt32(rb, 0);
		}
		public long ToInt64()
		{
			if (_Length > 64) throw new OverflowException();
			var rb = new[]
			{
				(_Bytes.Length > 0 ? _Bytes[0] : (byte)0),
				(_Bytes.Length > 1 ? _Bytes[1] : (byte)0),
				(_Bytes.Length > 2 ? _Bytes[2] : (byte)0),
				(_Bytes.Length > 3 ? _Bytes[3] : (byte)0),
				(_Bytes.Length > 4 ? _Bytes[4] : (byte)0),
				(_Bytes.Length > 5 ? _Bytes[5] : (byte)0),
				(_Bytes.Length > 6 ? _Bytes[6] : (byte)0),
				(_Bytes.Length > 7 ? _Bytes[7] : (byte)0),
			};
			return BitConverter.ToInt64(rb, 0);
		}
		public decimal ToDecimal()
		{
			if (_Length > 96) throw new OverflowException();
			var rb = new[]
			{
				(_Bytes.Length > 0 ? _Bytes[0] : (byte)0),
				(_Bytes.Length > 1 ? _Bytes[1] : (byte)0),
				(_Bytes.Length > 2 ? _Bytes[2] : (byte)0),
				(_Bytes.Length > 3 ? _Bytes[3] : (byte)0),
				(_Bytes.Length > 4 ? _Bytes[4] : (byte)0),
				(_Bytes.Length > 5 ? _Bytes[5] : (byte)0),
				(_Bytes.Length > 6 ? _Bytes[6] : (byte)0),
				(_Bytes.Length > 7 ? _Bytes[7] : (byte)0),
				(_Bytes.Length > 8 ? _Bytes[8] : (byte)0),
				(_Bytes.Length > 9 ? _Bytes[9] : (byte)0),
				(_Bytes.Length > 10 ? _Bytes[10] : (byte)0),
				(_Bytes.Length > 11 ? _Bytes[11] : (byte)0),
			};
			return new decimal(
				BitConverter.ToInt32(rb, 0),
				BitConverter.ToInt32(rb, 4),
				BitConverter.ToInt32(rb, 8),
				false,
				0);
		}
		public override string ToString()
		{
			return ToString(2);
		}
		public string ToString(int toBase)
		{
			var result = new StringBuilder();
			int i;
			switch (toBase)
			{
				case 2:
					#region Binary
					for (i = 0; i < _Length; i++)
					{
						result.Append((Get(i) ? '1' : '0'));
					}
					result = new StringBuilder(result.ToString().Reverse());
					#endregion
					break;
				case 8:
					#region Octal
					for (i = 0; i < _Length; i += 3)
					{
						if ((i + 2) < _Length)
							result.Append(MakeByte(Get(i), (Get(i + 1)), Get(i + 2)));
						else if ((i + 1) < _Length)
							result.Append(MakeByte(Get(i), (Get(i + 1))));
						else
							result.Append((Get(i) ? '1' : '0'));
					}
					result = new StringBuilder(result.ToString().Reverse());
					#endregion
					break;
				case 10:
					#region Decimal
					result.Append(this.ToDecimal());
					#endregion
					break;
				case 16:
					#region Hexadecimal
					for (i = 0; i < _Length; i += 4)
					{
						if ((i + 3) < _Length)
							result.Append(MakeHexByte(MakeByte(Get(i), (Get(i + 1)), Get(i + 2), Get(i + 3))));
						else if ((i + 2) < _Length)
							result.Append(MakeHexByte(MakeByte(Get(i), (Get(i + 1)), Get(i + 2))));
						else if ((i + 1) < _Length)
							result.Append(MakeHexByte(MakeByte(Get(i), (Get(i + 1)))));
						else
							result.Append((Get(i) ? '1' : '0'));
					}
					result = new StringBuilder(result.ToString().Reverse());
					#endregion
					break;
				default:
					throw new NotSupportedException();
			}
			return (result.Length == 0 ? "0" : result.ToString());
		}
		public byte[] ToByteArray()
		{
            return ((byte[])_Bytes.Clone());
		}
		public bool[] ToArray()
		{
			return (this as IEnumerable<bool>).ToArray();
		}
		public bool IsAll(bool value)
		{
			if (_Length == 0) return (!value);

			for (int i = 0; i < _Length; i++)
			{
				if (Get(i) != value) return false;
			}
			return true;
		}
		public void SetAll(bool value)
		{
			if (_Length == 0) return;
			for (int i = 0; i < _Length; i++)
			{
				Set(i, value);
			}
		}
		public static BinaryExpression Parse(bool value)
		{
			return new BinaryExpression(BitConverter.GetBytes(value));
		}
		public static BinaryExpression Parse(char value)
		{
			return new BinaryExpression(BitConverter.GetBytes(value));
		}
		public static BinaryExpression Parse(float value)
		{
			return new BinaryExpression(BitConverter.GetBytes(value));
		}
		public static BinaryExpression Parse(double value)
		{
			return new BinaryExpression(BitConverter.GetBytes(value));
		}
		public static BinaryExpression Parse(byte value)
		{
			return new BinaryExpression(new[] { value });
		}
		public static BinaryExpression Parse(sbyte value)
		{
			return new BinaryExpression(new[] { (byte)value });
		}
		public static BinaryExpression Parse(short value)
		{
			return new BinaryExpression(BitConverter.GetBytes(value));
		}
		public static BinaryExpression Parse(int value)
		{
			return new BinaryExpression(BitConverter.GetBytes(value));
		}
		public static BinaryExpression Parse(long value)
		{
			return new BinaryExpression(BitConverter.GetBytes(value));
		}
		public static BinaryExpression Parse(ushort value)
		{
			return new BinaryExpression(BitConverter.GetBytes(value));
		}
		public static BinaryExpression Parse(uint value)
		{
			return new BinaryExpression(BitConverter.GetBytes(value));
		}
		public static BinaryExpression Parse(ulong value)
		{
			return new BinaryExpression(BitConverter.GetBytes(value));
		}
		public static BinaryExpression Parse(decimal value)
		{
			var decimalBits = decimal.GetBits(value);
			var bytes = new List<byte>();
			for (int i = 0; i < 3; i++) bytes.AddRange(BitConverter.GetBytes(decimalBits[i]));
			return new BinaryExpression(bytes.ToArray());
		}
		public static bool TryParse(string s, out BinaryExpression exp)
		{
			return TryParse(s, 2, out exp);
		}
		public static bool TryParse(string s, int fromBase, out BinaryExpression exp)
		{
			return IsValid(s, fromBase, out exp);
		}
		public static bool IsValid(string s)
		{
			BinaryExpression exp;
			return IsValid(s, out exp);
		}
		private static bool IsValid(string s, out BinaryExpression exp)
		{
			return IsValid(s, 2, out exp);
		}
		private static bool IsValid(string s, int fromBase, out BinaryExpression exp)
		{
			exp = default(BinaryExpression);
			if (s == null) return false;
			if (s.Length == 0)
			{
				exp = new BinaryExpression(1);
				return true;
			}
			char ch;
			int div, rem;
			switch (fromBase)
			{
				case 2:
					#region Binary
					byte[] bytes = new byte[(int)Math.Ceiling(s.Length / 8F)];
					for (int ci = s.Length - 1, bi = 0; ci >= 0; ci--, bi++)
					{
						ch = s[ci];
						if (ch != '0' && ch != '1') return false;
						div = Math.DivRem(bi, 8, out rem);
						if (ch != '0') bytes[div] |= (byte)Math.Pow(2D, (double)rem);
					}
					exp = new BinaryExpression(bytes, s.Length, true);
					#endregion
					break;
				case 8:
					#region Octal
					var strOctal = new StringBuilder();
					foreach (var chr in s)
					{
						if (chr < '0' || chr > '7') return false;
						strOctal.Append(GetOctalBinary((byte)(chr - 48)));
					}
					return IsValid(strOctal.ToString(), 2, out exp);
					#endregion
				case 10:
					#region Decimal
					decimal decValue;
					if (decimal.TryParse(s, NumberStyles.None, null, out decValue) == false || decValue < 0M) return false;
					if (decValue <= 255M)
						exp = BinaryExpression.Parse(decimal.ToByte(decValue));
					else if (decValue <= new decimal(ushort.MaxValue))
						exp = BinaryExpression.Parse(decimal.ToUInt16(decValue));
					else if (decValue <= new decimal(uint.MaxValue))
						exp = BinaryExpression.Parse(decimal.ToUInt32(decValue));
					else if (decValue <= new decimal(ulong.MaxValue))
						exp = BinaryExpression.Parse(decimal.ToUInt64(decValue));
					else
						exp = BinaryExpression.Parse(decValue);
					#endregion
					break;
				case 16:
					#region Hexadecimal
					var strHex = new StringBuilder();
					foreach (var chr in s)
					{
						if (chr >= '0' && chr <= '9')
							strHex.Append(GetHexBinary((byte)(chr - '0')));
						else if (chr >= 'a' && chr <= 'f')
							strHex.Append(GetHexBinary((byte)(chr - 'a' + 10)));
						else if (chr >= 'A' && chr <= 'F')
							strHex.Append(GetHexBinary((byte)(chr - 'A' + 10)));
						else
							return false;
					}
					return IsValid(strHex.ToString(), 2, out exp);
					#endregion
				default:
					throw new ArgumentOutOfRangeException(nameof(fromBase), "Invalid Base.");
			}
			return true;
		}
		public int CompareTo(object obj)
		{
			if (object.ReferenceEquals(obj, null)) return 1;
			if (!(obj is BinaryExpression)) throw new InvalidOperationException();
			return CompareTo((BinaryExpression)obj);
		}
		public int CompareTo(BinaryExpression exp)
		{
			return InternalCompareTo(exp, false);
		}
		public int CompareToExactly(BinaryExpression exp)
		{
			return InternalCompareTo(exp, true);
		}
		private int InternalCompareTo(BinaryExpression exp, bool exactly)
		{
			if (object.ReferenceEquals(exp, null)) return 1;

			var effLen1 = (exactly ? 0 : this.GetEffectiveLength());
			var effLen2 = (exactly ? 0 : exp.GetEffectiveLength());

			int result = effLen1.CompareTo(effLen2);
			if (result != 0) return result;
			for (int i = 0; i < _Length; i++)
			{
				result = Get(i).CompareTo(exp.Get(i));
				if (result != 0) return result;
			}
			return 0;
		}
		public override int GetHashCode()
		{
			return this.ToString(2).GetHashCode();
		}
		public override bool Equals(object obj)
		{
			return (obj is BinaryExpression && Equals((BinaryExpression)obj));
		}
		public bool Equals(BinaryExpression exp)
		{
			return (CompareTo(exp) == 0);
		}
		public bool EqualsExactly(BinaryExpression exp)
		{
			return (InternalCompareTo(exp, true) == 0);
		}
		public static bool Equals(BinaryExpression x, BinaryExpression y)
		{
			if (object.ReferenceEquals(x, null)) return object.ReferenceEquals(y, null);
			return (x.CompareTo(y) == 0);
		}
		public static bool EqualsExactly(BinaryExpression x, BinaryExpression y)
		{
			if (object.ReferenceEquals(x, null)) return object.ReferenceEquals(y, null);
			return (x.InternalCompareTo(y, true) == 0);
		}
		#region IEnumerable
		public IEnumerator<bool> GetEnumerator()
		{
			if (_Length == 0) yield break;
			for (int i = 0; i < _Length; i++)
			{
				yield return Get(i);
			}
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		#endregion
		#region ICloneable
		public BinaryExpression Clone()
		{
			return new BinaryExpression(_Bytes, _Length);
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}
		#endregion
		#region IEqualityComparer
		int IEqualityComparer<BinaryExpression>.GetHashCode(BinaryExpression obj)
		{
			if (object.ReferenceEquals(obj, null)) return 0;
			return obj.GetHashCode();
		}
		bool IEqualityComparer<BinaryExpression>.Equals(BinaryExpression x, BinaryExpression y)
		{
			return Equals(x, y);
		}
		#endregion
		#region IConvertible
		TypeCode IConvertible.GetTypeCode()
		{
			return TypeCode.Object;
		}
		bool IConvertible.ToBoolean(IFormatProvider provider)
		{
			return (GetEffectiveLength() > 0);
		}
		byte IConvertible.ToByte(IFormatProvider provider)
		{
			return this.ToByte();
		}
		char IConvertible.ToChar(IFormatProvider provider)
		{
			return ((char)this.ToInt16());
		}
		decimal IConvertible.ToDecimal(IFormatProvider provider)
		{
			return this.ToDecimal();
		}
		double IConvertible.ToDouble(IFormatProvider provider)
		{
			return (double)this.ToUInt64();
		}
		short IConvertible.ToInt16(IFormatProvider provider)
		{
			return this.ToInt16();
		}
		int IConvertible.ToInt32(IFormatProvider provider)
		{
			return this.ToInt32();
		}
		long IConvertible.ToInt64(IFormatProvider provider)
		{
			return this.ToInt64();
		}
		sbyte IConvertible.ToSByte(IFormatProvider provider)
		{
			return ((sbyte)this.ToInt16());
		}
		float IConvertible.ToSingle(IFormatProvider provider)
		{
			return (float)this.ToInt32();
		}
		string IConvertible.ToString(IFormatProvider provider)
		{
			return this.ToString();
		}
		ushort IConvertible.ToUInt16(IFormatProvider provider)
		{
			return this.ToUInt16();
		}
		uint IConvertible.ToUInt32(IFormatProvider provider)
		{
			return this.ToUInt32();
		}
		ulong IConvertible.ToUInt64(IFormatProvider provider)
		{
			return this.ToUInt64();
		}
		DateTime IConvertible.ToDateTime(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}
		object IConvertible.ToType(Type conversionType, IFormatProvider provider)
		{
			if (conversionType == typeof(byte[]))
				return this.ToByteArray();
			else if (conversionType == typeof(bool[]))
				return this.ToArray();
			return Utility.Utilities.ConvertibleToType(this, conversionType, provider);
		}
		#endregion

		#endregion
		#region Operators
		public static BinaryExpression operator |(BinaryExpression expr1, BinaryExpression expr2)
		{
			return expr1.Clone().Or(expr2);
		}
		public static BinaryExpression operator &(BinaryExpression expr1, BinaryExpression expr2)
		{
			return expr1.Clone().And(expr2);
		}
		public static BinaryExpression operator ^(BinaryExpression expr1, BinaryExpression expr2)
		{
			return expr1.Clone().Xor(expr2);
		}
		public static BinaryExpression operator ~(BinaryExpression expr1)
		{
			return expr1.Clone().Not();
		}
		public static BinaryExpression operator <<(BinaryExpression expr, int count)
		{
			return expr.Clone().ShiftLeft(count);
		}
		public static BinaryExpression operator >>(BinaryExpression expr, int count)
		{
			return expr.Clone().ShiftRight(count);
		}
		public static bool operator !=(BinaryExpression expr1, BinaryExpression expr2)
		{
            if (object.ReferenceEquals(expr1, null)) return (!object.ReferenceEquals(expr2, null));
			return (!expr1.Equals(expr2));
		}
		public static bool operator ==(BinaryExpression expr1, BinaryExpression expr2)
		{
            if (object.ReferenceEquals(expr1, null)) return object.ReferenceEquals(expr2, null);
			return expr1.Equals(expr2);
        }
		public static bool operator >(BinaryExpression expr1, BinaryExpression expr2)
		{
			return (expr1.CompareTo(expr2) > 0);
		}
		public static bool operator >=(BinaryExpression expr1, BinaryExpression expr2)
		{
			return (expr1.CompareTo(expr2) >= 0);
		}
		public static bool operator <(BinaryExpression expr1, BinaryExpression expr2)
		{
			return (expr1.CompareTo(expr2) < 0);
		}
		public static bool operator <=(BinaryExpression expr1, BinaryExpression expr2)
		{
			return (expr1.CompareTo(expr2) <= 0);
		}
		public static implicit operator BinaryExpression(byte value)
		{
			return BinaryExpression.Parse(value);
		}
		public static implicit operator BinaryExpression(short value)
		{
			return BinaryExpression.Parse(value);
		}
		public static implicit operator BinaryExpression(ushort value)
		{
			return BinaryExpression.Parse(value);
		}
		public static implicit operator BinaryExpression(int value)
		{
			return BinaryExpression.Parse(value);
		}
		public static implicit operator BinaryExpression(uint value)
		{
			return BinaryExpression.Parse(value);
		}
		public static implicit operator BinaryExpression(long value)
		{
			return BinaryExpression.Parse(value);
		}
		public static implicit operator BinaryExpression(ulong value)
		{
			return BinaryExpression.Parse(value);
		}
		public static implicit operator BinaryExpression(decimal value)
		{
			return BinaryExpression.Parse(value);
		}
		#endregion
	}
	public class BinaryExpressionConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			if (sourceType == typeof(BinaryExpression)) return true;
			switch (Type.GetTypeCode(sourceType))
			{
				case TypeCode.Boolean:
				case TypeCode.Byte:
				case TypeCode.Char:
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.SByte:
				case TypeCode.Single:
				case TypeCode.String:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
					return true;
			}
			if (sourceType == typeof(byte[])) return true;
			if (sourceType == typeof(bool[])) return true;
			return false;
		}
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			if (destinationType == typeof(BinaryExpression)) return true;
			switch (Type.GetTypeCode(destinationType))
			{
				case TypeCode.Boolean:
				case TypeCode.Byte:
				case TypeCode.Char:
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.SByte:
				case TypeCode.Single:
				case TypeCode.String:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
					return true;
			}
			if (destinationType == typeof(byte[])) return true;
			if (destinationType == typeof(bool[])) return true;
			return base.CanConvertTo(context, destinationType);
		}
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value is BinaryExpression) return value;
			if (value == null) return null;
			var valueType = value.GetType();
			switch (Type.GetTypeCode(valueType))
			{
				case TypeCode.Boolean:
					return BinaryExpression.Parse((bool)value);
				case TypeCode.Byte:
					return BinaryExpression.Parse((byte)value);
				case TypeCode.Char:
					return BinaryExpression.Parse((char)value);
				case TypeCode.Decimal:
					return BinaryExpression.Parse((decimal)value);
				case TypeCode.Double:
					return BinaryExpression.Parse((double)value);
				case TypeCode.Int16:
					return BinaryExpression.Parse((short)value);
				case TypeCode.Int32:
					return BinaryExpression.Parse((int)value);
				case TypeCode.Int64:
					return BinaryExpression.Parse((long)value);
				case TypeCode.SByte:
					return BinaryExpression.Parse((sbyte)value);
				case TypeCode.Single:
					return BinaryExpression.Parse((float)value);
				case TypeCode.String:
					return new BinaryExpression((string)value);
				case TypeCode.UInt16:
					return BinaryExpression.Parse((ushort)value);
				case TypeCode.UInt32:
					return BinaryExpression.Parse((uint)value);
				case TypeCode.UInt64:
					return BinaryExpression.Parse((ulong)value);
			}
			if (valueType == typeof(byte[])) return new BinaryExpression((byte[])value);
			if (valueType == typeof(bool[])) return new BinaryExpression((bool[])value);
			return base.ConvertFrom(context, culture, value);
		}
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (value == null) return base.ConvertTo(context, culture, value, destinationType);
			var ic = value as IConvertible;
			return ic.ToType(destinationType, culture);
		}
		public override bool IsValid(ITypeDescriptorContext context, object value)
		{
			if (value is BinaryExpression)
				return true;
			else if (value is string)
				return BinaryExpression.IsValid((string)value);
			return false;
		}
	}
}
