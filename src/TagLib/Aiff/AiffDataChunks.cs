using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TagLib.Aiff
{
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

            while (currentIndex < fileLength + firstChunkLength)
            {
                var chunk = new AiffDataChunk();
                aFile.Seek(currentIndex);
                chunk.Offset = currentIndex;
                chunk.Pattern = aFile.ReadBlock(4);
                aFile.Seek(currentIndex + 4);
                var lengthBlock = aFile.ReadBlock(4);
                chunk.Length = (long)lengthBlock.ToULong(true);
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
            public long Offset;
            public long Length;
            public ByteVector Pattern;
        }
    }
}
