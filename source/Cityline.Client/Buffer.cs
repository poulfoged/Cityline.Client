using System.Collections.Generic;
using System.Linq;

namespace Cityline.Client 
{
    internal class Buffer 
    {
        private List<string> _buffer = new List<string>();

        public void Add(string chunk) 
        {
            _buffer.AddRange(chunk.Split('\n'));
        }

        public bool HasTerminator()
        {
            return _buffer.IndexOf("") != -1;
        }

        public IEnumerable<string> Take() 
        {
            var position = _buffer.IndexOf("");
            var chunk = _buffer.Take(position);
            _buffer = _buffer.Skip(position+1).ToList();  
            return chunk;
        }

        public void Clear() 
        {
            this._buffer.Clear();
        }
    }
}