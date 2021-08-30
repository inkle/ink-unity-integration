#pragma warning disable IDE1006

using System.Collections.Generic;
using System.Linq;

namespace Ink
{
    /// <summary>
    /// A class representing a character range. Allows for lazy-loading a corresponding <see cref="CharacterSet">character set</see>.
    /// </summary>
    public sealed class CharacterRange
    {
        public static CharacterRange Define(char start, char end, IEnumerable<char> excludes = null)
        {
            return new CharacterRange (start, end, excludes);
        }

        /// <summary>
        /// Returns a <see cref="CharacterSet">character set</see> instance corresponding to the character range
        /// represented by the current instance.
        /// </summary>
        /// <remarks>
        /// The internal character set is created once and cached in memory.
        /// </remarks>
        /// <returns>The char set.</returns>
        public CharacterSet ToCharacterSet ()
        {
            if (_correspondingCharSet.Count == 0) 
            {
                for (char c = _start; c <= _end; c++)
                {
                    if (!_excludes.Contains (c)) 
                    {
                        _correspondingCharSet.Add (c);
                    }
                }
            }
            return _correspondingCharSet;
        }

        public char start { get { return _start; } }
        public char end { get { return _end; } }

        CharacterRange (char start, char end, IEnumerable<char> excludes)
        {
        	_start = start;
        	_end = end;
            _excludes = excludes == null ? new HashSet<char>() : new HashSet<char> (excludes);
        }

#pragma warning disable IDE0044 // Add readonly modifier
        char _start;
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning disable IDE0044 // Add readonly modifier
        char _end;
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning disable IDE0044 // Add readonly modifier
        ICollection<char> _excludes;
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0090 // Use 'new(...)'
        CharacterSet _correspondingCharSet = new CharacterSet();
#pragma warning restore IDE0090 // Use 'new(...)'
#pragma warning restore IDE0044 // Add readonly modifier
    }    
}
