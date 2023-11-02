using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;

namespace Cryville.Culture {
	/// <summary>
	/// Provides methods to match a language supported by the application against desired languages, based on the language matching data.
	/// </summary>
	/// <remarks>
	/// <para>This class parses <see href="https://github.com/unicode-org/cldr/tree/main/common/supplemental/languageInfo.xml"><c>common/supplemental/languageInfo.xml</c></see> in CLDR.</para>
	/// </remarks>
	public class LanguageMatching {
		readonly List<IMatchingGroup> _groups = new();

		#region Matching sub-logic
		interface IMatchingGroup {
			bool Add(string desired, string supported, int distance);
			bool Match(LanguageId desired, LanguageId supported, out LanguageId match, out int distance);
		}
		sealed class InstancedMatchingGroup : IMatchingGroup {
			readonly bool _matchLanguage;
			readonly bool _matchScript;
			readonly bool _matchRegion;
			readonly Dictionary<Matching, int> _map = new();
			public InstancedMatchingGroup(LanguageId desiredInstance) {
				desiredInstance = desiredInstance.SyntaxCanonicalized;
				_matchLanguage = desiredInstance.Language != "und";
				_matchScript = desiredInstance.Script != null;
				_matchRegion = desiredInstance.Region != null;
			}
			public bool Add(string desired, string supported, int distance) {
				try {
					var ldesired = new LanguageId(desired).SyntaxCanonicalized;
					var lsupported = new LanguageId(supported).SyntaxCanonicalized;
					if (ldesired.Language != "und" != _matchLanguage) return false;
					if (ldesired.Script != null != _matchScript) return false;
					if (ldesired.Region != null != _matchRegion) return false;
					_map.Add(new(ldesired, lsupported), distance);
				}
				catch (FormatException) {
					return false;
				}
				return true;
			}
			public bool Match(LanguageId desired, LanguageId supported, out LanguageId match, out int distance) {
				if (!_matchLanguage) desired.Language = supported.Language = "und";
				if (!_matchScript) desired.Script = supported.Script = null;
				if (!_matchRegion) desired.Region = supported.Region = null;
				match = supported;
				return _map.TryGetValue(new(desired, supported), out distance);
			}
		}
		sealed class GenericMatchingGroup : IMatchingGroup {
			delegate bool Matcher(string str);
			readonly LanguageMatching _sys;
			Matcher? _dl, _sl, _ds, _ss, _dr, _sr;
			int _distance;
			public GenericMatchingGroup(LanguageMatching sys) { _sys = sys; }
			public bool Add(string desired, string supported, int distance) {
				if (_distance > 0) return false;
				GenerateMatchers(desired, out _dl, out _ds, out _dr);
				GenerateMatchers(supported, out _sl, out _ss, out _sr);
				_distance = distance;
				return true;
			}
			void GenerateMatchers(string pattern, out Matcher? l, out Matcher? s, out Matcher? r) {
				var subpatterns = pattern.Split('-', '_');
				l = GenerateMatcher(subpatterns[0]);
				s = subpatterns.Length >= 2 ? GenerateMatcher(subpatterns[1]) : null;
				r = subpatterns.Length >= 3 ? GenerateMatcher(subpatterns[2]) : null;
			}
			Matcher GenerateMatcher(string pattern) {
				if (pattern == "*") return i => true;
				if (pattern.Contains("$")) {
					bool negFlag = false;
					if (pattern[1] == '!') {
						pattern = "$" + pattern.Substring(2);
						negFlag = true;
					}
					var set = _sys._matchVariables[pattern];
					return negFlag ? (i => !set.Contains(i)) : set.Contains;
				}
				return i => i == pattern;
			}
			public bool Match(LanguageId desired, LanguageId supported, out LanguageId match, out int distance) {
				match = supported;
				distance = _distance;
				bool flag = false;
				if (!_dl!(desired.Language!)) return false;
				if (!_sl!(supported.Language!)) return false;
				if (desired.Language != supported.Language) {
					flag = true;
				}
				if (_ds != null && !_ds(desired.Script!)) return false;
				if (_ss != null && !_ss(supported.Script!)) return false;
				if (desired.Script != supported.Script && (_ds != null || _ss != null)) {
					flag = true;
				}
				if (_dr != null && !_dr(desired.Region!)) return false;
				if (_sr != null && !_sr(supported.Region!)) return false;
				if (desired.Region != supported.Region && (_dr != null || _sr != null)) {
					flag = true;
				}
				return flag;
			}
		}
		struct Matching : IEquatable<Matching> {
			public LanguageId desired;
			public LanguageId supported;

			public Matching(LanguageId desired, LanguageId supported) {
				this.desired = desired;
				this.supported = supported;
			}

			public readonly bool Equals(Matching other) => desired == other.desired && supported == other.supported;
			public override readonly bool Equals(object obj) => obj is Matching other && Equals(other);
			public override readonly int GetHashCode() {
				var ret = desired.GetHashCode();
				var h1 = supported.GetHashCode();
				return ret ^ (h1 << 16 | h1 >> 16);
			}
		}
		#endregion

		readonly LikelySubtags _subtags;
		readonly Dictionary<string, HashSet<string>> _matchVariables = new();
		/// <summary>
		/// Creates an instance of the <see cref="LanguageMatching" /> class.
		/// </summary>
		/// <param name="xml">The <c>languageInfo</c> XML document to be loaded.</param>
		/// <param name="subtags">The likely subtag data.</param>
		/// <exception cref="ArgumentNullException"><paramref name="xml" /> or <paramref name="subtags" /> is <see langword="null" />.</exception>
		/// <exception cref="FormatException">The given <c>languageInfo</c> XML document is invalid.</exception>
		public LanguageMatching(XDocument xml, LikelySubtags subtags) {
			if (xml == null) throw new ArgumentNullException(nameof(xml));
			_subtags = subtags ?? throw new ArgumentNullException(nameof(subtags));
			IMatchingGroup? group = null;
			foreach (var i in xml.Descendants("matchVariable")) {
				_matchVariables.Add(i.Attribute("id").Value, new(i.Attribute("value").Value.Split('+')));
			}
			foreach (var i in xml.Descendants("languageMatch")) {
				var desired = i.Attribute("desired").Value;
				var supported = i.Attribute("supported").Value;
				var distance = int.Parse(i.Attribute("distance").Value, CultureInfo.InvariantCulture);
				var oneway = i.Attribute("oneway");
				group ??= CreateMatchingGroup(desired);
				while (!group.Add(desired, supported, distance)) {
					_groups.Add(group);
					group = CreateMatchingGroup(desired);
				}
				if ((oneway == null || oneway.Value == "false") && supported != desired) {
					while (!group.Add(supported, desired, distance)) {
						_groups.Add(group);
						group = CreateMatchingGroup(supported);
					}
				}
			}
			if (group == null) throw new FormatException("Invalid languageInfo XML document.");
			_groups.Add(group);
		}
		IMatchingGroup CreateMatchingGroup(string instance) {
			if (instance.Contains("$") || instance.Contains("*"))
				return new GenericMatchingGroup(this);
			return new InstancedMatchingGroup(new(instance));
		}

		/// <summary>
		/// Computes the distance between two languages.
		/// </summary>
		/// <param name="start">The starting language.</param>
		/// <param name="end">The destination language.</param>
		/// <returns>The distance from the <paramref name="start" /> language to the <paramref name="end" /> language.</returns>
		/// <exception cref="InvalidOperationException">The distance cannot be computed.</exception>
		public int GetDistance(LanguageId start, LanguageId end) {
			var distance = 0;
			start = _subtags.AddLikelySubtags(start);
			end = _subtags.AddLikelySubtags(end);
			while (start != end) {
				bool flag = false;
				foreach (var group in _groups) {
					if (group.Match(start, end, out var match, out var dist)) {
						if (match.Language != "und") start.Language = match.Language;
						if (match.Script != null) start.Script = match.Script;
						if (match.Region != null) start.Region = match.Region;
						distance += dist;
						flag = true;
						break;
					}
				}
				if (!flag) throw new InvalidOperationException("Failed to find distance.");
			}
			return distance;
		}

		/// <summary>
		/// Matches a language among a list of supported languages against a desired language.
		/// </summary>
		/// <param name="desired">The desired language.</param>
		/// <param name="supported">The list of supported languages.</param>
		/// <param name="match">The matched language, or the default value of <see cref="LanguageId" /> if there is no match.</param>
		/// <param name="distance">The distance from <paramref name="desired" /> to <paramref name="match" />, or <see cref="int.MaxValue" /> if there is no match.</param>
		/// <returns>Whether there is a match.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="supported" /> is <see langword="null" />.</exception>
		public bool Match(LanguageId desired, IEnumerable<LanguageId> supported, out LanguageId match, out int distance) {
			if (supported == null) throw new ArgumentNullException(nameof(supported));
			match = default;
			distance = int.MaxValue;
			foreach (var supportedLang in supported) {
				var dist = GetDistance(desired, supportedLang);
				if (dist < distance) {
					match = supportedLang;
					distance = dist;
				}
			}
			return distance != int.MaxValue;
		}
	}
}
