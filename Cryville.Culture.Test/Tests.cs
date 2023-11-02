using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace Cryville.Culture.Test {
	public class Tests {
		SupplementalMetadata _metadata;
		LikelySubtags _likelySubtags;
		LanguageMatching _languageMatching;

		static readonly XmlReaderSettings _xmlReaderSettings = new() {
			DtdProcessing = DtdProcessing.Ignore,
		};

		static XDocument LoadXml(string path) {
			using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
			using var reader = XmlReader.Create(stream, _xmlReaderSettings);
			return XDocument.Load(path);
		}
		static string GetResourcePath(params string[] paths) => Path.Combine(Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources"), Path.Combine(paths));

		[OneTimeSetUp]
		public void SetUp() {
			foreach (var i in new DirectoryInfo(GetResourcePath("cldr", "common", "validity")).EnumerateFiles()) {
				IdValidity.Load(LoadXml(i.FullName));
			}
			_metadata = new SupplementalMetadata(LoadXml(GetResourcePath("cldr", "common", "supplemental", "supplementalMetadata.xml")));
			_likelySubtags = new LikelySubtags(LoadXml(GetResourcePath("cldr", "common", "supplemental", "likelySubtags.xml")), _metadata);
			_languageMatching = new LanguageMatching(LoadXml(GetResourcePath("cldr", "common", "supplemental", "languageInfo.xml")), _likelySubtags);
			Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
		}

		[Test]
		public void LanguageId() {
			Assert.Multiple(() => {
				Assert.That(new LanguageId("root").ToString(), Is.EqualTo("root"));
				Assert.That(new LanguageId("root").SyntaxCanonicalized.ToString(), Is.EqualTo("und"));
				Assert.That(new LanguageId("en-latn-us").SyntaxCanonicalized.ToString(), Is.EqualTo("en-Latn-US"));
				Assert.That(new LanguageId("latn-us").SyntaxCanonicalized.ToString(), Is.EqualTo("und-Latn-US"));
			});
		}

		[Test]
		public void Validity() {
			Assert.Multiple(() => {
				Assert.That(new LanguageId("root").IsValid);
				Assert.That(new LanguageId("root").SyntaxCanonicalized.IsValid);
				Assert.That(new LanguageId("en-latn-us").SyntaxCanonicalized.IsValid);
				Assert.That(new LanguageId("latn-us").SyntaxCanonicalized.IsValid);
				Assert.That(!new LanguageId("zzz").SyntaxCanonicalized.IsValid);
			});
		}

		[Test]
		public void AddLikelySubtags() {
			TestContext.WriteLine(_likelySubtags.AddLikelySubtags(new("en")));
			TestContext.WriteLine(_likelySubtags.AddLikelySubtags(new("en-us")));
			TestContext.WriteLine(_likelySubtags.AddLikelySubtags(new("en-gb")));
			TestContext.WriteLine(_likelySubtags.AddLikelySubtags(new("en-latn")));
			TestContext.WriteLine(_likelySubtags.AddLikelySubtags(new("en-latn-us")));

			TestContext.WriteLine(_likelySubtags.AddLikelySubtags(new("zh")));
			TestContext.WriteLine(_likelySubtags.AddLikelySubtags(new("zh-cn")));
			TestContext.WriteLine(_likelySubtags.AddLikelySubtags(new("zh-hk")));
			TestContext.WriteLine(_likelySubtags.AddLikelySubtags(new("zh-tw")));
			TestContext.WriteLine(_likelySubtags.AddLikelySubtags(new("zh-hans")));
			TestContext.WriteLine(_likelySubtags.AddLikelySubtags(new("zh-hant")));

			TestContext.WriteLine(_likelySubtags.AddLikelySubtags(new("hira")));
			TestContext.WriteLine(_likelySubtags.AddLikelySubtags(new("jpan")));
			TestContext.WriteLine(_likelySubtags.AddLikelySubtags(new("latn")));
			TestContext.WriteLine(_likelySubtags.AddLikelySubtags(new("latn-ro")));

			TestContext.WriteLine(_likelySubtags.AddLikelySubtags(new("hani")));
			TestContext.WriteLine(_likelySubtags.AddLikelySubtags(new("hans")));
			TestContext.WriteLine(_likelySubtags.AddLikelySubtags(new("hant")));

			TestContext.WriteLine(_likelySubtags.AddLikelySubtags(new("i_hak")));
			TestContext.WriteLine(_likelySubtags.AddLikelySubtags(new("zh_xiang")));
			TestContext.WriteLine(_likelySubtags.AddLikelySubtags(new("en_GB_oed")));
		}

		[Test]
		public void RemoveLikelySubtags() {
			TestContext.WriteLine(_likelySubtags.RemoveLikelySubtags(new("en-Latn-US")));
			TestContext.WriteLine(_likelySubtags.RemoveLikelySubtags(new("cmn-Hans-CN")));
			TestContext.WriteLine(_likelySubtags.RemoveLikelySubtags(new("zh-Hant-HK")));
			TestContext.WriteLine(_likelySubtags.RemoveLikelySubtags(new("zh-Hant-TW")));
			TestContext.WriteLine(_likelySubtags.RemoveLikelySubtags(new("zh-Hant-TW"), true));
		}

		[Test]
		public void LanguageMatching() {
			TestContext.WriteLine(_languageMatching.GetDistance(new("yue"), new("zh")));
			TestContext.WriteLine(_languageMatching.GetDistance(new("en"), new("zh")));
			TestContext.WriteLine(_languageMatching.GetDistance(new("zh-TW"), new("zh-HK")));
			TestContext.WriteLine(_languageMatching.GetDistance(new("en-US"), new("en-GB")));
			TestContext.WriteLine(_languageMatching.GetDistance(new("en-US"), new("en-CA")));
			TestContext.WriteLine(_languageMatching.GetDistance(new("zh-latn"), new("Hans")));

			TestContext.WriteLine(_languageMatching.GetDistance(new("en-US"), new("zzzz")));
			TestContext.WriteLine(_languageMatching.GetDistance(new("zh-CN"), new("zzzz")));
			TestContext.WriteLine(_languageMatching.GetDistance(new("en-US"), new("dsrt")));
		}
	}
}
