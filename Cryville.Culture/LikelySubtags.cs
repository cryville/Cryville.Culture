using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Cryville.Culture {
	/// <summary>
	/// Provides methods to maximize and minimize a Unicode language identifier, based on the likely subtag data.
	/// </summary>
	/// <remarks>
	/// <para>This class parses <see href="https://github.com/unicode-org/cldr/tree/main/common/supplemental/likelySubtags.xml"><c>common/supplemental/likelySubtags.xml</c></see> in CLDR.</para>
	/// </remarks>
	public class LikelySubtags {
		readonly SupplementalMetadata _metadata;
		readonly Dictionary<LanguageId, LanguageId> _tags;
		/// <summary>
		/// Creates an instance of the <see cref="LikelySubtags" /> class.
		/// </summary>
		/// <param name="xml">The <c>likelySubtags</c> XML document to be loaded.</param>
		/// <param name="metadata">The alias data.</param>
		/// <exception cref="ArgumentNullException"><paramref name="xml" /> or <paramref name="metadata" /> is <see langword="null" />.</exception>
		public LikelySubtags(XDocument xml, SupplementalMetadata metadata) {
			if (xml == null) throw new ArgumentNullException(nameof(xml));
			_metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
			_tags = xml.Descendants("likelySubtag").ToDictionary(
				i => new LanguageId(i.Attribute("from").Value).SyntaxCanonicalized,
				i => new LanguageId(i.Attribute("to").Value).SyntaxCanonicalized
			);
		}
		/// <summary>
		/// Maximizes a Unicode language identifier.
		/// </summary>
		/// <param name="source">The Unicode language identifier to be maximized.</param>
		/// <returns>The maximized Unicode language identifier.</returns>
		/// <exception cref="KeyNotFoundException">No matching likely subtag is found.</exception>
		public LanguageId AddLikelySubtags(LanguageId source) {
			source = _metadata.Canonicalize(source);
			if (source.IsGrandfathered) return source;
			if (source.Script == "Zzzz") source.Script = null;
			if (source.Region == "ZZ") source.Region = null;
			if (source.Language != "und" && source.Script != null && source.Region != null) return source;

			var tempSource = source.Clone(true);
			if (!_tags.TryGetValue(tempSource, out var match)) {
				var region = tempSource.Region;
				tempSource.Region = null;
				if (tempSource.Script == null || !_tags.TryGetValue(tempSource, out match)) {
					tempSource.Script = null;
					tempSource.Region = region;
					if (tempSource.Region == null || !_tags.TryGetValue(tempSource, out match)) {
						tempSource.Region = null;
						if (!_tags.TryGetValue(tempSource, out match)) {
							throw new KeyNotFoundException("No matching likely subtags.");
						}
					}
				}
			}

			if (source.Language == "und") source.Language = match.Language;
			source.Script ??= match.Script;
			if (source.Region == null || IdValidity.Check(source.Region, "region", "macroregion")) source.Region = match.Region;

			return source;
		}
		/// <summary>
		/// Minimizes a Unicode language identifier.
		/// </summary>
		/// <param name="source">The Unicode language identifier to be minimized.</param>
		/// <param name="favoringScript">Whether to favor preserving the script subtag instead of the region subtag.</param>
		/// <returns>The minimized Unicode language identifier.</returns>
		/// <exception cref="KeyNotFoundException">No matching likely subtag is found.</exception>
		public LanguageId RemoveLikelySubtags(LanguageId source, bool favoringScript = false) {
			var max = AddLikelySubtags(source);
			var maxNoVariant = max.Clone(true);
			var candidate1 = new LanguageId { Language = maxNoVariant.Language };
			if (AddLikelySubtags(candidate1) == maxNoVariant) return candidate1;
			var candidate2 = new LanguageId { Language = maxNoVariant.Language, Region = maxNoVariant.Region };
			var candidate3 = new LanguageId { Language = maxNoVariant.Language, Script = maxNoVariant.Script };
			if (favoringScript) {
				candidate1 = candidate2;
				candidate2 = candidate3;
				candidate3 = candidate1;
			}
			if (AddLikelySubtags(candidate2) == maxNoVariant) return candidate2;
			if (AddLikelySubtags(candidate3) == maxNoVariant) return candidate3;
			return max;
		}
	}
}
