﻿using System.Collections.Generic;

namespace Raven.Abstractions.Data
{
    public class CompactStatus
    {
        public CompactStatusState State;

		public List<string> Messages { get; set; }

        public static string RavenDatabaseCompactStatusDocumentKey(string databaseName)
        {
            return "Raven/Database/Compact/Status/" + databaseName;
        }

        public static string RavenFilesystemCompactStatusDocumentKey(string filesystemName)
        {
            return "Raven/FileSystem/Compact/Status/" + filesystemName;
        }
    }

    public enum CompactStatusState
    {
        Running, 
        Completed,
        Faulted
    }
}