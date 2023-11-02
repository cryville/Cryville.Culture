using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Cryville.Culture {
	/// <summary>
	/// Provides methods to convert an alias ID to its canonical form, based on the alias data.
	/// </summary>
	/// <remarks>
	/// <para>This class parses <see href="https://github.com/unicode-org/cldr/tree/main/common/supplemental/supplementalMetadata.xml"><c>common/supplemental/supplementalMetadata.xml</c></see> in CLDR.</para>
	/// </remarks>
	public class SupplementalMetadata {
		readonly List<AliasRule> _aliasRules = new();
		struct AliasRule {
			public LanguageId type;
			public LanguageId[] replacement;
			public string[] subtags;
			public AliasRule(LanguageId type, IEnumerable<LanguageId> replacement) {
				this.type = type;
				this.replacement = replacement.ToArray();
				if (this.type.Language == "und") this.type.Language = null;
				for (int i = 0; i < this.replacement.Length; i++) {
					var item = this.replacement[i];
					if (item.Language == "und") {
						item.Language = null;
						this.replacement[i] = item;
					}
				}
				var subtags = new List<string>();
				if (type.Language != null) subtags.Add(type.Language);
				if (type.Script != null) subtags.Add(type.Script);
				if (type.Region != null) subtags.Add(type.Region);
				subtags.AddRange(type.Variant);
				this.subtags = subtags.ToArray();
			}
		}
		/// <summary>
		/// Creates an instance of the <see cref="SupplementalMetadata" /> class.
		/// </summary>
		/// <param name="xml">The <c>supplementalMetadata</c> XML document to be loaded.</param>
		/// <exception cref="ArgumentNullException"><paramref name="xml" /> is <see langword="null" />.</exception>
		public SupplementalMetadata(XDocument xml) {
			if (xml == null) throw new ArgumentNullException(nameof(xml));
			foreach (var alias in xml.Descendants("alias").Single().Elements()) {
				string type = alias.Attribute("type").Value;
				string[] replacement = alias.Attribute("replacement").Value.Split(' ');
				if (alias.Name != "languageAlias") {
					type = "und-" + type;
					for (int i = 0; i < replacement.Length; i++) {
						replacement[i] = "und-" + replacement[i];
					}
				}
				try {
					_aliasRules.Add(new(
						new LanguageId(type).SyntaxCanonicalized,
						replacement.Select(i => new LanguageId(i).SyntaxCanonicalized)
					));
				}
				catch (FormatException) {
					// Discard
				}
			}
			_aliasRules.Sort((a, b) => {
				var c = -a.subtags.Length.CompareTo(b.subtags.Length);
				if (c != 0) return c;
				for (int i = 0; i < a.subtags.Length; i++) {
					c = StringComparer.OrdinalIgnoreCase.Compare(a.subtags[i], b.subtags[i]);
					if (c != 0) return c;
				}
				return 0;
			});
		}
		/// <summary>
		/// Converts all aliased subtags in a Unicode language identifier to their canonical forms.
		/// </summary>
		/// <param name="source">The Unicode language identifier to be converted.</param>
		/// <returns>The canonicalized Unicode language identifier.</returns>
		public LanguageId Canonicalize(LanguageId source) {
			source = source.SyntaxCanonicalized;
			while (Match(source, out var match)) {
				var type = match.type;
				var replacement = match.replacement[0]; // TODO Territory Exception
				if (type.Language != null) source.Language = replacement.Language;
				if (type.Script != null) source.Script = replacement.Script;
				if (type.Region != null) source.Region = replacement.Region;
				if (type.Variant.Count > 0) {
					foreach (var t in type.Variant) source.Variant.Remove(t);
					foreach (var t in replacement.Variant) source.Variant.Add(t);
				}
				else if (source.Variant.Count == 0 && replacement.Variant.Count > 0) {
					foreach (var t in replacement.Variant) source.Variant.Add(t);
				}
			}
			return source;
		}
		bool Match(LanguageId source, out AliasRule match) {
			foreach (var rule in _aliasRules) {
				var type = rule.type;
				if (type.Language != null && source.Language != type.Language) continue;
				if (type.Script != null && source.Script != type.Script) continue;
				if (type.Region != null && source.Region != type.Region) continue;
				if (type.Variant.Count > 0) {
					bool flag = false;
					foreach (var t in type.Variant) {
						if (!source.Variant.Contains(t)) {
							flag = true;
							break;
						}
					}
					if (flag) continue;
				}
				match = rule;
				return true;
			}
			match = default;
			return false;
		}
	}
}
