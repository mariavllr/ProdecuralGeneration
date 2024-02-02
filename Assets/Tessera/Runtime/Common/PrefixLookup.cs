using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Stores key-value pairs, with efficient searching for
    /// the longest key that is a prefix of a given string.
    /// </summary>
    public class PrefixLookup<T>
    {
        class Trie
        {
            // Either contains a single entry, with a length >= 2
            // or contains any number of entries with length == 1.
            public Dictionary<string, object> children;
        }

        Trie trie;

        public PrefixLookup()
        {
            trie = new Trie { };
        }

        public void Add(string name, object value)
        {
            if (name.Contains('\0'))
                throw new Exception("Character \\0 not supported in PrefixLookup");
            Add(name, name + '\0', value, trie);
        }

        private void Add(string fullName, string name, object value, Trie trie)
        {
            if (trie.children == null)
            {
                trie.children = new Dictionary<string, object> { { name, value } };
                return;
            }
            // Tries with a single child may have multiple characters in the keys
            if (trie.children.Count == 1)
            {
                var kv = trie.children.First();
                // Is it really is one of these special long tries
                if (kv.Key.Length > 1)
                {
                    var i = FindCommonPrefix(kv.Key, name);
                    if (i <= 1)
                    {
                        // Convert to a trie with a one char entru
                        trie.children.Clear();

                        var newTrie = new Trie { };
                        trie.children[kv.Key.Substring(0, 1)] = newTrie;
                        Add(null, kv.Key.Substring(1), kv.Value, newTrie);
                        // Then continue as normal
                    }
                    else
                    {
                        trie.children.Clear();

                        // Make a new long trie for the shared prefix
                        var newTrie = new Trie { };
                        trie.children[kv.Key.Substring(0, i)] = newTrie;
                        Add(null, kv.Key.Substring(i), kv.Value, newTrie);
                        Add(fullName, name.Substring(i), value, newTrie);
                        return;
                    }
                }
            }
            // We have a multi way trie based on one character
            var firstChar = name.Substring(0, 1);
            if (trie.children.TryGetValue(firstChar, out var subTrie))
            {
                // Already an existing child
                if (firstChar == "\0")
                {
                    // Two keys are exactly the same
                    throw new Exception($"Duplicate: {fullName}");
                }
                else
                {
                    // Recurse to the subtree
                    Add(fullName, name.Substring(1), value, (Trie)subTrie);
                }
            }
            else
            {
                // No existing child
                if (firstChar == "\0")
                {
                    // Create terminal child
                    trie.children[firstChar] = value;
                }
                else
                {
                    // Create non-terminal child
                    var newTrie = new Trie { };
                    trie.children[firstChar] = newTrie;
                    Add(fullName, name.Substring(1), value, newTrie);
                }
            }
        }

        public bool TryFindLongestPrefix(string name, out T value)
        {
            if (name.Contains('\0'))
                throw new Exception("Character \\0 not supported in PrefixLookup");
            return TryFindLongestPrefix(name + "\0", out value, trie);
        }

        private bool TryFindLongestPrefix(string name, out T value, Trie trie)
        {
            if (trie.children.Count == 1)
            {
                var kv = trie.children.First();
                if (kv.Key.Length > 1)
                {
                    // Handle the case of a long subTrie
                    if (name.StartsWith(kv.Key))
                    {
                        // Is this a leaf?
                        if (kv.Key[kv.Key.Length - 1] == '\0')
                        {
                            value = (T)kv.Value;
                            return true;
                        }
                        else
                        {
                            return TryFindLongestPrefix(name.Substring(kv.Key.Length), out value, (Trie)kv.Value);
                        }
                    }
                    else
                    {
                        value = default;
                        return false;
                    }

                }
            }
            var firstChar = name.Substring(0, 1);
            if (trie.children.TryGetValue(firstChar, out var subTrie))
            {
                if (firstChar == "\0")
                {
                    value = (T)subTrie;
                    return true;
                }
                else
                {
                    if (TryFindLongestPrefix(name.Substring(1), out value, (Trie)subTrie))
                    {
                        return true;
                    }
                }
            }
            // Nothing longer, check if there's a terminal value here we can use
            if (trie.children.TryGetValue("\0", out var terminal))
            {
                value = (T)terminal;
                return true;
            }
            value = default;
            return false;
        }

        // Finds chars in common, excluding the trailing \0 char.
        private static int FindCommonPrefix(string a, string b)
        {
            int i = 0;
            while (i < a.Length - 1 && i < b.Length - 1 && a[i] == b[i]) i++;
            return i;
        }
    }

}
