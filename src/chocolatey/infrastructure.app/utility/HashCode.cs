// The MIT License (MIT)
//
// Copyright (c) Muhammad Rehan Saeed
//
// Taken from this blog post: https://rehansaeed.com/gethashcode-made-easy/
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.Linq;

public struct HashCode
{
    private readonly int value;

    private HashCode(int value)
    {
        this.value = value;
    }

    public static implicit operator int(HashCode hashCode)
    {
        return hashCode.value;
    }

    public static HashCode Of<T>(T item)
    {
        return new HashCode(GetHashCode(item));
    }

    public HashCode And<T>(T item)
    {
        return new HashCode(CombineHashCodes(this.value, GetHashCode(item)));
    }

    public HashCode AndEach<T>(IEnumerable<T> items)
    {
        if (items == null)
        {
            return new HashCode(this.value);
        }

        var hashCode = items.Any() ? items.Select(GetHashCode).Aggregate(CombineHashCodes) : 0;
        return new HashCode(CombineHashCodes(this.value, hashCode));
    }

    private static int CombineHashCodes(int h1, int h2)
    {
        unchecked
        {
            // Code copied from System.Tuple so it must be the best way to combine hash codes or at least a good one.
            return ((h1 << 5) + h1) ^ h2;
        }
    }

    private static int GetHashCode<T>(T item)
    {
        return item == null ? 0 : item.GetHashCode();
    }
}