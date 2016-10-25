using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TagLib.Aiff
{
    class AiffDataChunkError : Exception
    {
        public AiffDataChunkError(string aMessage)
            : base(aMessage)
        { }
    }

    class AiffDataChunks
    {
        private List<AiffDataChunk> iChunks;

        public AiffDataChunks(File aFile)
        {
            iChunks = new List<AiffDataChunk>();


            var firstChunkLength = 12;
            var chunkHeaderSize = 8;

            // read the initial aiff header chunk
            var initialChunk = new AiffDataChunk();
            aFile.Seek(0);
            initialChunk.Offset = 0;
            initialChunk.Pattern = aFile.ReadBlock(4);
            aFile.Seek(4);
            var fileLengthBlock = aFile.ReadBlock(4);
            var fileLength = (long)fileLengthBlock.ToULong(true);
            initialChunk.Length = fileLength;
            iChunks.Add(initialChunk);
            long currentIndex = firstChunkLength; // length of initial data chunk

            while (currentIndex < fileLength + chunkHeaderSize)
            {
                var chunk = new AiffDataChunk();
                aFile.Seek(currentIndex);
                chunk.Offset = currentIndex;
                chunk.Pattern = aFile.ReadBlock(4);
                aFile.Seek(currentIndex + 4);
                var lengthBlock = aFile.ReadBlock(4);
                var length = (long)lengthBlock.ToULong(true);
                if (length % 2 == 1)
                {
                    length += 1; // aiff pads odd length blocks
                }
                chunk.Length = length;
                iChunks.Add(chunk);
                currentIndex += chunk.Length + chunkHeaderSize;
            }

            // reset the stream
            aFile.Seek(0);
        }

        public long Find(ByteVector pattern, long startPosition, ByteVector before)
        {
            if (before != null)
            {
                var chunkBefore = iChunks.FirstOrDefault(c => c.Offset >= startPosition && c.Pattern.Find(before, 0, 1) != -1);
                if (chunkBefore != null)
                {
                    var idx = iChunks.IndexOf(chunkBefore);
                    // only search chunks up to the before chunk
                    var chunk = iChunks.Take(idx).FirstOrDefault(c => c.Offset >= startPosition && c.Pattern.Find(pattern, 0, 1) != -1);
                    if (chunk == null)
                    {
                        return -1;
                    }
                    return chunk.Offset;
                }
                else
                {
                    // before not found, just do a normal search
                    return Find(pattern, startPosition, null);
                }
            }
            else
            {
                var chunk = iChunks.FirstOrDefault(c => c.Offset >= startPosition && c.Pattern.Find(pattern, 0, 1) != -1);
                if (chunk == null)
                {
                    return -1;
                }
                return chunk.Offset;
            }
        }

        public long Find(ByteVector pattern, long startPosition)
        {
            return Find(pattern, startPosition, null);
        }

        private class AiffDataChunk
        {
            private ByteVector iPattern;
            private long iLength;
            private long iOffset;

            public long Offset
            {
                get
                {
                    return iOffset;
                }
                set
                {
                    if (value < 0)
                    {
                        throw new AiffDataChunkError(string.Format("Invalid chunk offset: {0}", value));
                    }
                    iOffset = value;
                }
            }

            public long Length
            {
                get
                {
                    return iLength;
                }
                set
                {
                    if (value <= 0)
                    {
                        throw new AiffDataChunkError(string.Format("Invalid chunk length: {0}", value));
                    }
                    iLength = value;
                }
            }

            public ByteVector Pattern
            {
                get
                {
                    return iPattern;
                }
                set
                {
                    if (!IsValidPattern(value))
                    {
                        throw new AiffDataChunkError(string.Format("Invalid chunk header: {0}", value.ToString()));
                    }
                    iPattern = value;
                }
            }

            private bool IsValidPattern(ByteVector aPattern)
            {
                var validPatterns = new List<string>()
                {
                    "FORM", "COMM", "INST", "MARK", "SKIP", "SSND", "NAME", "FVER", "MIDI", "AESD", "APPL", "COMT", "AUTH", "(c) ", "ANNO", "ID3 ", "FLLR"
                };
                return (validPatterns.Contains(aPattern.ToString()));
            }
        }
    }
}
