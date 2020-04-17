// Purpose: Implement a simple TextReader which can peek one character ahead
// Author : Scott Bakker
// Created: 04/17/2020

using System;
using System.IO;

namespace JsonLibrary
{
    public class CharReader : IDisposable
    {
        private readonly TextReader _baseReader;

        // character storage to allow PeekNext()
        int _currChar = -1;
        int _nextChar = -1;
        bool _eof = false;

        public CharReader(string value)
        {
            _baseReader = new StringReader(value);
        }

        public CharReader(TextReader reader)
        {
            _baseReader = reader;
        }

        public void Close()
        {
            _baseReader.Close();
        }

        public int Read()
        {
            int returnChar;
            if (_nextChar != -1)
            {
                returnChar = _currChar;
            }
            else
            {
                returnChar = _baseReader.Read();
            }
            _currChar = -1;
            _nextChar = -1;
            return returnChar;
        }

        public int Peek()
        {
            if (_currChar != -1)
            {
                return _currChar;
            }
            return _baseReader.Peek();
        }

        public int PeekNext()
        {
            if (_eof)
            {
                return -1;
            }
            if (_nextChar != -1)
            {
                return _nextChar;
            }
            _currChar = _baseReader.Read();
            _nextChar = _baseReader.Peek();
            _eof = (_nextChar == -1);
            return _nextChar;
        }

        #region dispose

        // Track whether Dispose has been called.
        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _baseReader.Dispose();
                }
                disposed = true;
            }
        }

        #endregion
    }
}
