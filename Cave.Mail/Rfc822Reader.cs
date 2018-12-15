using System;

namespace Cave.Mail
{
    /// <summary>
    /// Provides a mail reader used to read rfc 822
    /// </summary>
    public class Rfc822Reader
    {
        byte[] m_Data;
        int m_Position = 0;

        /// <summary>
        /// Extracts a byte buffer with the specified length starting at the specified position
        /// </summary>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public byte[] Extract(int start, int count)
        {
            byte[] result = new byte[count];
            Buffer.BlockCopy(m_Data, start, result, 0, count);
            return result;
        }

        /// <summary>
        /// Creates a new <see cref="Rfc822Reader"/> from the specified data
        /// </summary>
        /// <param name="data"></param>
        public Rfc822Reader(byte[] data)
        {
            m_Data = data;
        }

        /// <summary>
        /// Peeks at the next byte. Result is -1 if no more bytes available.
        /// </summary>
        /// <returns></returns>
        public virtual int Peek()
        {
            if (m_Position >= m_Data.Length)
            {
                return -1;
            }

            return m_Data[m_Position];
        }

        /// <summary>
        /// Reads the next byte. Result is -1 if no more bytes available.
        /// </summary>
        /// <returns></returns>
        public virtual int Read()
        {
            if (m_Position >= m_Data.Length)
            {
                return -1;
            }

            return m_Data[m_Position++];
        }

        /// <summary>
        /// Reads a string block with the specified length.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public string ReadBlock(int count)
        {
            string result = ASCII.GetString(m_Data, m_Position, count);
            m_Position += count;
            return result;
        }

        /// <summary>
        /// Readss a byte block with the specified length.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public byte[] ReadBlockData(int count)
        {
            byte[] result = Extract(m_Position, count);
            m_Position += count;
            return result;
        }

        /// <summary>
        /// Reads a string with all bytes until the end is reached.
        /// </summary>
        /// <returns></returns>
        public virtual string ReadToEnd()
        {
            string result = ASCII.GetString(m_Data, m_Position, m_Data.Length - m_Position);
            m_Position = m_Data.Length;
            return result;
        }

        /// <summary>
        /// Reads a byte buffer with all bytes until the end is reached.
        /// </summary>
        /// <returns></returns>
        public byte[] ReadToEndData()
        {
            int start = m_Position;
            m_Position = m_Data.Length;
            return Extract(start, m_Position - start);
        }

        /// <summary>
        /// Reads a line with the specified encoding without the trailing CR, LF, CRLF
        /// </summary>
        /// <returns></returns>
        public virtual string ReadLine()
        {
            int start = m_Position;
            while (m_Position < m_Data.Length)
            {
                switch (m_Data[m_Position])
                {
                    case 0x0D:
                    {
                        int l_End = m_Position;
                        m_Position += 1;
                        if (m_Data[m_Position] == 0x0A) { m_Position += 1; }
                        return Rfc2047.DefaultEncoding.GetString(m_Data, start, l_End - start);
                    }
                    case 0x0A:
                    {
                        int l_End = m_Position;
                        m_Position += 1;
                        return Rfc2047.DefaultEncoding.GetString(m_Data, start, l_End - start);
                    }
                }
                m_Position++;
            }
            int size = m_Data.Length - start;
            if (size == 0)
            {
                return null;
            }

            return ASCII.GetString(m_Data, start, size);
        }

        /// <summary>
        /// Provides the current position in the reader.
        /// </summary>
        public int Position { get => m_Position; set { if ((value >= m_Data.Length) || (value < 0)) { throw new ArgumentOutOfRangeException(nameof(value)); } m_Position = value; } }

        /// <summary>
        /// Provides the overall length of data this reader works on.
        /// </summary>
        public int Length => m_Data.Length;
    }
}
