using System;
using System.Collections.Generic;
using System.Linq;
using PocketSharp.Models;

namespace PocketX.Converter
{
    public class ArrayToStringConverter
    {
        public static string ConvertTagsToString(IEnumerable<PocketTag> tags)
            => tags == null ? "" : "#" + string.Join(" #", tags.Select(_ => _.Name).ToArray());
    }
}
