using System.Text;

namespace TeamDecided.RaftConsensus.NamedPipeLogWriter
{
    internal class LoggingBuffer
    {
        private readonly StringBuilder _buffer;
        private int _bufferCount;
        public bool HasEverReceivedMessages { get; private set; }

        public LoggingBuffer()
        {
            _buffer = new StringBuilder();
            _bufferCount = 0;
        }

        public void AddToBuffer(string line, bool isHeader = false)
        {
            lock (_buffer)
            {
                HasEverReceivedMessages = !isHeader;

                _bufferCount += 1;
                _buffer.Append(line);
            }
        }

        public override string ToString()
        {
            lock (_buffer)
            {
                string temp = _buffer.ToString();
                ClearInternal();
                return temp;
            }
        }

        public void Clear()
        {
            lock (_buffer)
            {
                ClearInternal();
            }
        }

        private void ClearInternal()
        {
            _bufferCount = 0;
            _buffer.Clear();
        }

        public int Count
        {
            get
            {
                lock (_buffer)
                {
                    return _bufferCount;
                }
            }
        }
    }
}
