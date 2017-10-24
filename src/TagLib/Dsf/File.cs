//
// File.cs: Provides tagging and properties support for Apple's AIFF 
// files.
//
// Author:
//   Helmut Wahrmann
//
// Copyright (C) 2009 Helmut Wahrmann
//
// This library is free software; you can redistribute it and/or modify
// it  under the terms of the GNU Lesser General Public License version
// 2.1 as published by the Free Software Foundation.
//
// This library is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307
// USA
//

using System;
using System.Collections.Generic;
using TagLib.Id3v2;
using System.Linq;

namespace TagLib.Dsf
{
    /// <summary>
    ///    This class extends <see cref="TagLib.File" /> to provide
    ///    support for reading and writing tags and properties for files
    ///    using the AIFF file format.
    /// </summary>
    [SupportedMimeType("taglib/dsf", "dsf")]
    [SupportedMimeType("audio/x-dsf")]
    [SupportedMimeType("audio/dsf")]
    [SupportedMimeType("sound/dsf")]
    [SupportedMimeType("application/x-dsf")]
    public class File : TagLib.File
    {
        #region Private Fields

        /// <summary>
        ///    Contains the address of the DSF header block.
        /// </summary>
        private ByteVector header_block = null;

        /// <summary>
        ///  Contains the Id3v2 tag.
        /// </summary>
        private Id3v2.Tag tag = null;

        /// <summary>
        ///  Contains the media properties.
        /// </summary>
        private Properties properties = null;

        #endregion
        #region Public Static Fields

        /// <summary>
        ///    The identifier used to recognize a AIFF files.
        /// </summary>
        /// <value>
        ///    "FORM"
        /// </value>
        public static readonly ReadOnlyByteVector FileIdentifier = "DSD ";
        public static readonly ReadOnlyByteVector FmtHeader = "fmt ";
        #endregion

        #region Public Constructors

        /// <summary>
        ///    Constructs and initializes a new instance of <see
        ///    cref="File" /> for a specified path in the local file
        ///    system and specified read style.
        /// </summary>
        /// <param name="path">
        ///    A <see cref="string" /> object containing the path of the
        ///    file to use in the new instance.
        /// </param>
        /// <param name="propertiesStyle">
        ///    A <see cref="ReadStyle" /> value specifying at what level
        ///    of accuracy to read the media properties, or <see
        ///    cref="ReadStyle.None" /> to ignore the properties.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///    <paramref name="path" /> is <see langword="null" />.
        /// </exception>
        public File(string path, ReadStyle propertiesStyle)
            : this(new File.LocalFileAbstraction(path),
                   propertiesStyle)
        {
        }

        /// <summary>
        ///    Constructs and initializes a new instance of <see
        ///    cref="File" /> for a specified path in the local file
        ///    system with an average read style.
        /// </summary>
        /// <param name="path">
        ///    A <see cref="string" /> object containing the path of the
        ///    file to use in the new instance.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///    <paramref name="path" /> is <see langword="null" />.
        /// </exception>
        public File(string path)
            : this(path, ReadStyle.Average)
        {
        }

        /// <summary>
        ///    Constructs and initializes a new instance of <see
        ///    cref="File" /> for a specified file abstraction and
        ///    specified read style.
        /// </summary>
        /// <param name="abstraction">
        ///    A <see cref="IFileAbstraction" /> object to use when
        ///    reading from and writing to the file.
        /// </param>
        /// <param name="propertiesStyle">
        ///    A <see cref="ReadStyle" /> value specifying at what level
        ///    of accuracy to read the media properties, or <see
        ///    cref="ReadStyle.None" /> to ignore the properties.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///    <paramref name="abstraction" /> is <see langword="null"
        ///    />.
        /// </exception>
        public File(File.IFileAbstraction abstraction,
                    ReadStyle propertiesStyle)
            : base(abstraction)
        {
            Mode = AccessMode.Read;
            try
            {
                Seek(0);
                if (ReadBlock(4) != FileIdentifier)
                    throw new CorruptFileException(
                        "File does not begin with DSF identifier");

                var chunkSize = ReadBlock(8).ToULong(false); // discard chunk size
                var fileSize = ReadBlock(8).ToULong(false);
                if ((long)fileSize != this.Length)
                {
                    Console.WriteLine("warning, dsf file mismatch with reported length");
                }
                var id3_chunk_pos = ReadBlock(8).ToULong(false);
                if (ReadBlock(4) != FmtHeader)
                    throw new CorruptFileException(
                        "Fmt chunk does not begin with fmt identifier");
                var fmtSize = ReadBlock(8).ToUInt(false);
                var fmtVersion = ReadBlock(4).ToUInt(false);
                var fmtId = ReadBlock(4).ToUInt(false);
                var channelType = ReadBlock(4).ToUInt(false);
                var channelNum = ReadBlock(4).ToUInt(false);
                var sampleFrequency = ReadBlock(4).ToUInt(false);
                var bitsPerSample = ReadBlock(4).ToUInt(false);
                var sampleCount = ReadBlock(8).ToULong(false);


                long tag_start = -1;
                long tag_end = -1;

                if (id3_chunk_pos > 0 && id3_chunk_pos < fileSize)
                {
                    tag = new Id3v2.Tag(this, (uint)id3_chunk_pos);

                    // Get the length of the tag out of the ID3 chunk
                    Seek(tag_start + 4);
                    uint tag_size = ReadBlock(4).ToUInt(true) + 8;

                    tag_start = InvariantStartPosition = (uint)id3_chunk_pos;
                    tag_end = InvariantEndPosition = tag_start + tag_size;
                }

                var duration = TimeSpan.FromSeconds((double)(sampleCount / sampleFrequency));
                var bitdepth = bitsPerSample;
                var bitrate = Math.Round((sampleFrequency * channelNum * bitdepth) / 1000.0);
                //var bitrate = (int)((stream_length * 8L) / duration.TotalSeconds) / 1000;

                properties = new Properties(duration, new DsdAudioCodec()
                {
                    AudioBitrate = (int)bitrate,
                    AudioChannels = (int)channelNum,
                    AudioSampleRate = (int)sampleFrequency,
                    Description = "Dsd Audio",
                    Duration = duration,
                    MediaTypes = MediaTypes.Audio
                });

            }
            catch(CorruptFileException)
            {
                throw;
            }
            catch(Exception e)
            {
                throw new CorruptFileException("Failed to parse dsf file", e);
            }
            finally
            {
                Mode = AccessMode.Closed;
            }

            TagTypesOnDisk = TagTypes;

            GetTag(TagTypes.Id3v2, true);
        }

        /// <summary>
        ///    Constructs and initializes a new instance of <see
        ///    cref="File" /> for a specified file abstraction with an
        ///    average read style.
        /// </summary>
        /// <param name="abstraction">
        ///    A <see cref="IFileAbstraction" /> object to use when
        ///    reading from and writing to the file.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///    <paramref name="abstraction" /> is <see langword="null"
        ///    />.
        /// </exception>
        public File(File.IFileAbstraction abstraction)
            : this(abstraction, ReadStyle.Average)
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///    Gets a abstract representation of all tags stored in the
        ///    current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="TagLib.Tag" /> object representing all tags
        ///    stored in the current instance.
        /// </value>
        public override Tag Tag
        {
            get { return tag; }
        }

        /// <summary>
        ///    Gets the media properties of the file represented by the
        ///    current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="TagLib.Properties" /> object containing the
        ///    media properties of the file represented by the current
        ///    instance.
        /// </value>
        public override TagLib.Properties Properties
        {
            get { return properties; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///    Saves the changes made in the current instance to the
        ///    file it represents.
        /// </summary>
        public override void Save()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///    Removes a set of tag types from the current instance.
        /// </summary>
        /// <param name="types">
        ///    A bitwise combined <see cref="TagLib.TagTypes" /> value
        ///    containing tag types to be removed from the file.
        /// </param>
        /// <remarks>
        ///    In order to remove all tags from a file, pass <see
        ///    cref="TagTypes.AllTags" /> as <paramref name="types" />.
        /// </remarks>
        public override void RemoveTags(TagTypes types)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///    Gets a tag of a specified type from the current instance,
        ///    optionally creating a new tag if possible.
        /// </summary>
        /// <param name="type">
        ///    A <see cref="TagLib.TagTypes" /> value indicating the
        ///    type of tag to read.
        /// </param>
        /// <param name="create">
        ///    A <see cref="bool" /> value specifying whether or not to
        ///    try and create the tag if one is not found.
        /// </param>
        /// <returns>
        ///    A <see cref="Tag" /> object containing the tag that was
        ///    found in or added to the current instance. If no
        ///    matching tag was found and none was created, <see
        ///    langword="null" /> is returned.
        /// </returns>
        public override TagLib.Tag GetTag(TagTypes type, bool create)
        {
            TagLib.Tag id32_tag = null;

            switch (type)
            {
                case TagTypes.Id3v2:
                    if (tag == null && create)
                    {
                        tag = new Id3v2.Tag();
                        tag.Version = 2;
                    }

                    id32_tag = tag;
                    break;
            }

            return id32_tag;
        }

        #endregion
        
    }

    class DsdAudioCodec : IAudioCodec
    {
        public int AudioBitrate { get; set; }

        public int AudioChannels { get; set; }

        public int AudioSampleRate { get; set; }

        public string Description { get; set; }

        public TimeSpan Duration { get; set; }
        public MediaTypes MediaTypes { get; set; }
    }
}
