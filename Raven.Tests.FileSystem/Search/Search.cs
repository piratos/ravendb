﻿using Raven.Json.Linq;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Xunit;

namespace Raven.Tests.FileSystem
{
    public class Search : RavenFilesTestWithLogs
    {
        [Fact]
        public void CanSearchForFilesBySize()
        {
            var client = NewAsyncClient();

            client.UploadAsync("1", StreamOfLength(1)).Wait();
            client.UploadAsync("2", StreamOfLength(2)).Wait();
            client.UploadAsync("3", StreamOfLength(3)).Wait();
            client.UploadAsync("4", StreamOfLength(4)).Wait();
            client.UploadAsync("5", StreamOfLength(5)).Wait();

            var files = client.SearchAsync("__size_numeric:[2 TO 4]").Result.Files;
            var fileNames = files.Select(f => f.Name).ToArray();

            Assert.Equal(new[] {"2", "3", "4"}, fileNames);
        }

        [Fact]
        public void CanSearchForFilesBySizeWithWildcardMin()
        {
            var client = NewAsyncClient();

            client.UploadAsync("1", StreamOfLength(1)).Wait();
            client.UploadAsync("2", StreamOfLength(2)).Wait();
            client.UploadAsync("3", StreamOfLength(3)).Wait();
            client.UploadAsync("4", StreamOfLength(4)).Wait();
            client.UploadAsync("5", StreamOfLength(5)).Wait();

            var files = client.SearchAsync("__size_numeric:[* TO 4]").Result.Files;
            var fileNames = files.Select(f => f.Name).ToArray();

            Assert.Equal(new[] { "1", "2", "3", "4" }, fileNames);
        }

        [Fact]
        public void CanSearchForFilesBySizeWithWildcardMax()
        {
            var client = NewAsyncClient();

            client.UploadAsync("1", StreamOfLength(1)).Wait();
            client.UploadAsync("2", StreamOfLength(2)).Wait();
            client.UploadAsync("3", StreamOfLength(3)).Wait();
            client.UploadAsync("4", StreamOfLength(4)).Wait();
            client.UploadAsync("5", StreamOfLength(5)).Wait();

            var files = client.SearchAsync("__size_numeric:[3 TO *]").Result.Files;
            var fileNames = files.Select(f => f.Name).ToArray();

            Assert.Equal(new[] { "3", "4", "5" }, fileNames);
        }

        [Fact]
        public void CanGetSearchTerms()
        {
            var client = NewAsyncClient();

            var ms = new MemoryStream();
            client.UploadAsync("Test", ms, new RavenJObject() { { "TestKey", "TestValue" } }).Wait();
            client.UploadAsync("Test2", ms, new RavenJObject() { { "Another", "TestValue" } }).Wait();

            var terms = client.GetSearchFieldsAsync(0, pageSize: 1024).Result;

            Assert.Contains("__key", terms);
            Assert.Contains("TestKey", terms);
            Assert.Contains("Another", terms);
        }

        private Stream StreamOfLength(int length)
        {
            var memoryStream = new MemoryStream(Enumerable.Range(0, length).Select(i => (byte)i).ToArray());

            return memoryStream;
        }
    }
}
