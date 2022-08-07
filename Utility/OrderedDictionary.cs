using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Godot.Utility
{
    /// <summary>
    /// Represents a generic collection of key-value pairs that can be accessed by the key or by the index, and preserves insertion order.
    /// </summary>
    /// <typeparam name="TKey">The <see cref="System.Type"/> of key.</typeparam>
    /// <typeparam name="TValue">The <see cref="System.Type"/> of value.</typeparam>
    public class OrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue> where TKey : notnull
    {
        /// <summary>
        /// Initializes a new <see cref="OrderedDictionary{TKey,TValue}"/> with the default initial capacity.
        /// </summary>
        public OrderedDictionary() : this(10)
        {
        }
        
        /// <summary>
        /// Initializes a new <see cref="OrderedDictionary{TKey,TValue}"/> with the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The initial number of key-value pairs the <see cref="OrderedDictionary{TKey,TValue}"/> can contain.</param>
        public OrderedDictionary(int capacity)
        {
            this.dictionary = new(capacity);
            this.list = new(capacity);
        }
        
        private readonly Dictionary<TKey, TValue> dictionary;
        
        private readonly List<KeyValuePair<TKey, TValue>> list;
        
        /// <summary>
        /// Returns the number of key-value pairs in the <see cref="OrderedDictionary{TKey,TValue}"/>.
        /// </summary>
        public int Count
        {
            get
            {
                return this.dictionary.Count;
            }
        }
        
        /// <summary>
        /// Returns an <see cref="IEnumerable{T}"/> of keys in the <see cref="OrderedDictionary{TKey,TValue}"/>.
        /// </summary>
        public IEnumerable<TKey> Keys
        {
            get
            {
                return this.list.Select(pair => pair.Key);
            }
        }
        
        /// <summary>
        /// Returns an <see cref="IEnumerable{T}"/> of values in the <see cref="OrderedDictionary{TKey,TValue}"/>.
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                return this.list.Select(pair => pair.Value);
            }
        }
        
        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get
            {
                return this.Keys.ToArray();
            }
        }
        
        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get
            {
                return this.Values.ToArray();
            }
        }
        
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get
            {
                return false;
            }
        }
        
        /// <summary>
        /// Gets or sets the value associated with <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        public TValue this[TKey key]
        {
            get
            {
                return this.dictionary[key];
            }
            set
            {
                this[this.IndexOfKey(key)] = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the value of the key at the specified index.
        /// </summary>
        /// <param name="index">The index of the key-value pair.</param>
        public TValue this[int index]
        {
            get
            {
                return this.dictionary[this.list[index].Key];
            }
            set
            {
                TKey key = this.list[index].Key;
                this.dictionary[key] = value;
                this.list[index] = new(key, value);
            }
        }
        
        /// <summary>
        /// Determines if the <see cref="OrderedDictionary{TKey,TValue}"/> contains <paramref name="key"/> as a key.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <returns><see langword="true"/> if <paramref name="key"/> is contained as a key in the <see cref="OrderedDictionary{TKey,TValue}"/>, else <see langword="false"/>.</returns>
        public bool ContainsKey(TKey key)
        {
            return this.dictionary.TryGetValue(key, out _);
        }
        
        /// <summary>
        /// Determines if the <see cref="OrderedDictionary{TKey,TValue}"/> contains <paramref name="value"/> as a value.
        /// </summary>
        /// <param name="value">The value to search for.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is contained as a value in the <see cref="OrderedDictionary{TKey,TValue}"/>, else <see langword="false"/>.</returns>
        public bool ContainsValue(TValue value)
        {
            return this.dictionary.ContainsValue(value);
        }
        
        /// <summary>
        /// Gets the value associated with <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <see cref="OrderedDictionary{TKey,TValue}"/> contains an element with the specified key, else <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="key"/> is <see langword="null"/></exception>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return key is null ? throw new ArgumentNullException(nameof(key)) : this.dictionary.TryGetValue(key, out value);
        }
        
        /// <summary>
        /// Returns the index of <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The zero-based index of <paramref name="key"/>, or -1 if the <see cref="OrderedDictionary{TKey,TValue}"/> does not contain it.</returns>
        public int IndexOfKey(TKey key)
        {
            return this.Keys.IndexOf(key);
        }
        
        /// <summary>
        /// Returns the index of <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The zero-based index of <paramref name="value"/>, or -1 if the <see cref="OrderedDictionary{TKey,TValue}"/> does not contain it.</returns>
        public int IndexOfValue(TValue value)
        {
            return this.Values.IndexOf(value);
        }
        
        /// <summary>
        /// Adds <paramref name="key"/> and <paramref name="value"/> to the <see cref="OrderedDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>The index of the newly added key-value pair.</returns>
        public int Add(TKey key, TValue value)
        {
            this.dictionary.Add(key, value);
            this.list.Add(new(key, value));
            return this.Count - 1;
        }
        
        /// <summary>
        /// Inserts a key-value pair into the <see cref="OrderedDictionary{TKey,TValue}"/> at the specified index.
        /// </summary>
        /// <param name="index">The index of the key-value pair.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void InsertAt(int index, TKey key, TValue value)
        {
            this.dictionary.Add(key, value);
            this.list.Insert(index, new(key, value));
        }
        
        /// <summary>
        /// Removes the value with the specified key from the <see cref="OrderedDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns><see langword="true"/> if the element is found and removed, else <see langword="false"/>.</returns>
        public bool Remove(TKey key)
        {
            bool removed = this.dictionary.Remove(key);
            if (removed)
            {
                this.list.RemoveAt(this.IndexOfKey(key));
            }
            return removed;
        }
        
        /// <summary>
        /// Removes the key-value pair at the specified index in the <see cref="OrderedDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the key-value pair (according to insertion order).</param>
        public void RemoveAt(int index)
        {
            TKey key = this.list[index].Key;
            this.list.RemoveAt(index);
            this.dictionary.Remove(key);
        }
        
        /// <summary>
        /// Removes all key-value pairs from the <see cref="OrderedDictionary{TKey,TValue}"/>.
        /// </summary>
        public void Clear()
        {
            this.dictionary.Clear();
            this.list.Clear();
        }
        
        /// <summary>
        /// Returns an <see cref="IEnumerator{T}"/> that iterates through the <see cref="OrderedDictionary{TKey,TValue}"/> in the order of insertion of items.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> for the <see cref="OrderedDictionary{TKey,TValue}"/>.</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return this.list.GetEnumerator();
        }
        
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> pair)
        {
            return this.list.Contains(pair);
        }
        
        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            this.Add(key, value);
        }
        
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> pair)
        {
            (TKey key, TValue value) = pair;
            this.Add(key, value);
        }
        
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> pair)
        {
            return this.Remove(pair.Key);
        }
        
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            this.list.CopyTo(array, index);
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}