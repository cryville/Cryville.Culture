using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Cryville.Culture {
	/// <summary>
	/// Provides a set of methods to check the validity of an ID, based on the ID validity data.
	/// </summary>
	/// <remarks>
	/// <para>This class parses XML documents in the <see href="https://github.com/unicode-org/cldr/tree/main/common/validity"><c>common/validity</c></see> directory in CLDR.</para>
	/// </remarks>
	public static class IdValidity {
		static readonly char[] _separators = new char[] { '\n', '\t', ' ' };
		static readonly Dictionary<string, Dictionary<string, HashSet<string>>> _list = new();
		/// <summary>
		/// Loads the validity data from an XML document.
		/// </summary>
		/// <param name="xml">The XML document to be loaded.</param>
		/// <exception cref="ArgumentNullException"><paramref name="xml" /> is <see langword="null" />.</exception>
		/// <exception cref="InvalidOperationException">An ID list in the document has already been loaded.</exception>
		/// <remarks>
		/// <para>This method parses XML documents in the <see href="https://github.com/unicode-org/cldr/tree/main/common/validity"><c>common/validity</c></see> directory in CLDR.</para>
		/// </remarks>
		public static void Load(XDocument xml) {
			if (xml == null) throw new ArgumentNullException(nameof(xml));
			foreach (var ids in xml.Descendants("id")) {
				var type = ids.Attribute("type").Value;
				if (!_list.TryGetValue(type, out var statusLists)) {
					_list.Add(type, statusLists = new());
				}
				var status = ids.Attribute("idStatus").Value;
				if (statusLists.ContainsKey(status)) throw new InvalidOperationException("Duplicate ID list.");
				var list = new HashSet<string>();
				statusLists.Add(status, list);
				foreach (var id in ids.Value.Split(_separators, StringSplitOptions.RemoveEmptyEntries)) {
					if (id[id.Length - 2] == '~') {
						var prefix = id.Substring(0, id.Length - 3);
						var start = id[id.Length - 3];
						var end = id[id.Length - 1];
						for (var c = start; c <= end; c++) {
							list.Add(prefix + c);
						}
					}
					else list.Add(id);
				}
			}
		}
		/// <summary>
		/// Gets the ID status of an ID.
		/// </summary>
		/// <param name="id">The ID to be checked.</param>
		/// <param name="type">The type of the ID.</param>
		/// <returns>The ID status of the ID, or <see langword="null" /> if the ID is not found.</returns>
		/// <exception cref="InvalidOperationException">The ID list of the given type is not found or not loaded.</exception>
		public static string? Check(string id, string type) {
			if (!_list.TryGetValue(type, out var lists))
				throw new InvalidOperationException($"ID list of type {type} not found.");
			foreach (var list in lists) {
				if (list.Value.Contains(id)) return list.Key;
			}
			return null;
		}
		/// <summary>
		/// Determines whether an ID is in the given status.
		/// </summary>
		/// <param name="id">The ID to be checked.</param>
		/// <param name="type">The type of the ID.</param>
		/// <param name="status">The status to be checked.</param>
		/// <returns>Whether the given ID is in the given status.</returns>
		/// <exception cref="InvalidOperationException">The ID list of the given type and the given status is not found or not loaded.</exception>
		public static bool Check(string id, string type, string status) {
			if (!_list.TryGetValue(type, out var lists) || !lists.TryGetValue(status, out var list))
				throw new InvalidOperationException($"ID list of type {type} and status {status} not found.");
			return list.Contains(id);
		}
		/// <summary>
		/// Gets a list of all IDs of a given type.
		/// </summary>
		/// <param name="type">The type of the IDs.</param>
		/// <returns>The list of all IDs of the given type.</returns>
		/// <exception cref="InvalidOperationException">The ID list of the given type is not found or not loaded.</exception>
		public static IEnumerable<string> Enumerate(string type) {
			if (!_list.TryGetValue(type, out var lists))
				throw new InvalidOperationException($"ID list of type {type} not found.");
			foreach (var list in lists) {
				foreach (var item in list.Value) yield return item;
			}
		}
		/// <summary>
		/// Gets a list of all IDs of a given type and given status.
		/// </summary>
		/// <param name="type">The type of the IDs.</param>
		/// <param name="status">The status of the IDs.</param>
		/// <returns>The list of all IDs of the given type and given status.</returns>
		/// <exception cref="InvalidOperationException">The ID list of the given type and the given status is not found or not loaded.</exception>
		public static IEnumerable<string> Enumerate(string type, string status) {
			if (!_list.TryGetValue(type, out var lists) || !lists.TryGetValue(status, out var list))
				throw new InvalidOperationException($"ID list of type {type} and status {status} not found.");
			return list;
		}
	}
}
