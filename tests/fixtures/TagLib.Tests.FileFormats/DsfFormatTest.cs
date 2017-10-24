using System;
using NUnit.Framework;
using TagLib;

namespace TagLib.Tests.FileFormats
{
	[TestFixture]
	public class DsfFormatTest : IFormatTest
	{
		private static string sampleF = "samples/sample.dsf";
        private static string emptyF = "samples/empty.dsf";
        private static string corruptF = "samples/corrupt.dsf";

        private File sample;
        private File empty;

        [TestFixtureSetUp]
		public void Init()
		{
            empty = File.Create(emptyF);
            sample = File.Create(sampleF);
        }

        [Test]
		public void ReadAudioProperties()
		{
            Assert.AreEqual(2822400, sample.Properties.AudioSampleRate);
            Assert.AreEqual(11, sample.Properties.Duration.TotalSeconds);
            Assert.AreEqual(5645, sample.Properties.AudioBitrate);
            Assert.AreEqual(2822400, empty.Properties.AudioSampleRate);
            Assert.AreEqual(11, empty.Properties.Duration.TotalSeconds);
            Assert.AreEqual(5645, empty.Properties.AudioBitrate);
        }

        [Test]
		public void ReadTags()
		{
            Assert.IsNotNull(empty.Tag);
            Assert.IsNull(empty.Tag.Album);
            Assert.IsNull(empty.Tag.FirstPerformer);
            Assert.IsNull(empty.Tag.Comment);
            Assert.IsNull(empty.Tag.FirstGenre);
            Assert.IsNull(empty.Tag.Title);
            Assert.AreEqual(0, empty.Tag.Track);
            Assert.AreEqual(0, empty.Tag.Year);

            Assert.AreEqual("Album", sample.Tag.Album);
			Assert.AreEqual("Artist", sample.Tag.FirstPerformer);
			Assert.AreEqual("Comment", sample.Tag.Comment);
			Assert.AreEqual("Genre", sample.Tag.FirstGenre);
			Assert.AreEqual("Title", sample.Tag.Title);
			Assert.AreEqual(5, sample.Tag.Track);
			Assert.AreEqual(2009, sample.Tag.Year);
		}

		[Test]
		public void TestCorruptionResistance()
		{
			StandardTests.TestCorruptionResistance(corruptF);
		}
	}
}
