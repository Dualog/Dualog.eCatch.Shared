﻿using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Dualog.eCatch.Shared.Enums;
using Dualog.eCatch.Shared.Extensions;
using Dualog.eCatch.Shared.Utilities;

namespace Dualog.eCatch.Shared.Services
{
    public static class KeyValueReferenceTableLoader
    {
        public static Dictionary<string, string> Load(string filename, EcatchLangauge langauge)
        {
            var result = new Dictionary<string, string>();

            using (var stream = ResourceLoader.GetEmbeddedResourceStream(typeof(KeyValueReferenceTableLoader).GetTypeInfo().Assembly, filename))
            using (var streamReader = new StreamReader(stream))
            {
                while (!streamReader.EndOfStream)
                {
                    var line = streamReader.ReadLine();
                    var items = line.Split('\t');
                    result.Add(items[0], items[langauge.ToReferenceTableIndex(filename)]);
                }
            }

            return result;
        }
    }
}
