using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Cryville.Culture {
	/// <summary>
	/// Represents a <see href="https://unicode.org/reports/tr35/#Unicode_language_identifier">Unicode language identifier</see>.
	/// </summary>
	public struct LanguageId : IEquatable<LanguageId> {
		static readonly HashSet<string> _grandfathered = new(StringComparer.OrdinalIgnoreCase) {
			// Irregular
			"en-GB-oed",
			"i-ami", "i-bnn", "i-default", "i-enochian", "i-hak",
			"i-klingon", "i-lux", "i-mingo", "i-navajo",
			"i-pwn", "i-tao", "i-tay", "i-tsu",
			"sgn-BE-FR", "sgn-BE-NL", "sgn-CH-DE",
			// Regular
			"art-lojban", "cel-gaulish", "no-bok", "no-nyn",
			"zh-guoyu", "zh-hakka", "zh-min", "zh-min-nan", "zh-xiang",
		};

		/// <summary>
		/// The language subtag.
		/// </summary>
		public string? Language { get; set; }
		/// <summary>
		/// The script subtag.
		/// </summary>
		public string? Script { get; set; }
		/// <summary>
		/// The region subtag.
		/// </summary>
		public string? Region { get; set; }

		ICollection<string>? m_variant;
		/// <summary>
		/// The variant subtags.
		/// </summary>
		public ICollection<string> Variant => m_variant ??= new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Creates an instance of the <see cref="LanguageId" /> struct.
		/// </summary>
		/// <param name="str">The string representation of the identifier.</param>
		/// <exception cref="ArgumentNullException"><paramref name="str" /> is <see langword="null" />.</exception>
		/// <exception cref="FormatException">The input string is in an incorrect format.</exception>
		public LanguageId(string str) {
			if (str == null) throw new ArgumentNullException(nameof(str));
			if (str == "root") {
				Language = "root";
				Script = null;
				Region = null;
				m_variant = null;
				return;
			}
			str = str.Replace('_', '-');
			if (_grandfathered.Contains(str)) {
				Language = str;
				Script = null;
				Region = null;
				m_variant = null;
				return;
			}
			var comps = str.Split('-');
			string? language = null;
			Script = null; Region = null;
			List<string> variant = new();
			bool flag = false, flag2 = false;
			foreach (var comp in comps) {
				if (language == null) {
					if (!comp.All(IsAlpha))
						throw new FormatException("Invalid locale format.");
					if (comp.Length == 4) {
						language = "und";
						Script = comp;
						flag = true;
					}
					else if (comp.Length >= 2 && comp.Length <= 8) {
						language = comp;
					}
					else {
						throw new FormatException("Invalid locale format.");
					}
					continue;
				}
				if (!flag) {
					flag = true;
					if (comp.Length == 4 && comp.All(IsAlpha)) {
						Script = comp;
						continue;
					}
				}
				if (!flag2) {
					flag2 = true;
					if ((comp.Length == 2 && comp.All(IsAlpha)) || (comp.Length == 3 && comp.All(IsDigit))) {
						Region = comp;
						continue;
					}
				}
				if ((comp.Length >= 5 && comp.Length <= 8 && comp.All(IsAlphaNum)) || (comp.Length == 4 && IsDigit(comp[0]) && comp.Skip(1).All(IsAlphaNum))) {
					variant.Add(comp);
				}
				else throw new FormatException("Invalid locale format.");
			}
			Language = language;
			m_variant = new SortedSet<string>(variant, StringComparer.OrdinalIgnoreCase);
		}
		static bool IsDigit(char c) => c >= '0' && c <= '9';
		static bool IsAlpha(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
		static bool IsAlphaNum(char c) => IsAlpha(c) || IsDigit(c);

		/// <summary>
		/// Whether the current Unicode language identifier represents a <see href="https://www.rfc-editor.org/rfc/rfc5646.html#section-2.1">grandfathered tag</see>.
		/// </summary>
		public readonly bool IsGrandfathered => Language != null && _grandfathered.Contains(Language);

		/// <summary>
		/// Whether the current Unicode language identifier is valid.
		/// </summary>
		/// <remarks>
		/// <para>This property calls <see cref="IdValidity.Check(string, string)" /> to determine whether all the subtags are valid. Call <see cref="IdValidity.Load(System.Xml.Linq.XDocument)" /> to load the validity data before getting this property.</para>
		/// </remarks>
		public readonly bool IsValid {
			get {
				if (Language == "root" || IsGrandfathered) return true;
				if (Language != null && IdValidity.Check(Language, "language") == null) return false;
				if (Script != null && IdValidity.Check(Script, "script") == null) return false;
				if (Region != null && IdValidity.Check(Region, "region") == null) return false;
				if (m_variant != null) {
					foreach (var t in m_variant) {
						if (IdValidity.Check(t, "variant") == null) return false;
					}
				}
				return true;
			}
		}

		/// <summary>
		/// Gets a syntax-canonicalized version of the current Unicode language identifier.
		/// </summary>
		[SuppressMessage("Globalization", "CA1308")]
		public readonly LanguageId SyntaxCanonicalized {
			get {
				if (Language == "root") return new("und");
				var ret = new LanguageId();
				if (Language != null) ret.Language = Language.ToLowerInvariant();
				else ret.Language = "und";
				if (Script != null) ret.Script = Script.Substring(0, 1).ToUpperInvariant() + Script.Substring(1).ToLowerInvariant();
				if (Region != null) ret.Region = Region.ToUpperInvariant();
				if (m_variant != null) {
					foreach (var t in m_variant) ret.Variant.Add(t.ToLowerInvariant());
				}
				return ret;
			}
		}

		/// <summary>
		/// Gets a clone of the current Unicode language identifier.
		/// </summary>
		/// <param name="excludeVariant">Whether to exclude variant subtags.</param>
		/// <returns>A clone of the current Unicode language identifier.</returns>
		public readonly LanguageId Clone(bool excludeVariant = false) {
			var ret = (LanguageId)MemberwiseClone();
			if (excludeVariant)
				ret.m_variant = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
			else
				ret.m_variant = new SortedSet<string>(m_variant, StringComparer.OrdinalIgnoreCase);
			return ret;
		}

		/// <inheritdoc />
		public readonly bool Equals(LanguageId other) =>
			Language == other.Language &&
			Script == other.Script &&
			Region == other.Region &&
			(((m_variant == null || m_variant.Count == 0) && (other.m_variant == null || other.m_variant.Count == 0)) || m_variant.SequenceEqual(other.m_variant));
		/// <inheritdoc />
		public override readonly bool Equals(object obj) => obj is LanguageId other && Equals(other);
		/// <inheritdoc />
		public override readonly int GetHashCode() {
			var ret = 0;
			if (Language != null) ret ^= Language.GetHashCode();
			if (Script != null) ret ^= Script.GetHashCode();
			if (Region != null) ret ^= Region.GetHashCode();
			if (m_variant != null) {
				foreach (var i in m_variant) ret ^= i.GetHashCode();
			}
			return ret;
		}
		/// <inheritdoc />
		public static bool operator ==(LanguageId a, LanguageId b) => a.Equals(b);
		/// <inheritdoc />
		public static bool operator !=(LanguageId a, LanguageId b) => !(a == b);

		/// <inheritdoc />
		public override readonly string ToString() {
			var ret = Language ?? "und";
			if (Script != null) ret += "-" + Script;
			if (Region != null) ret += "-" + Region;
			if (m_variant != null) {
				foreach (var i in m_variant) ret += "-" + i;
			}
			return ret;
		}
	}
}
