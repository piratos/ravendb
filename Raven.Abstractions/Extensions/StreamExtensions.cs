//-----------------------------------------------------------------------
// <copyright file="StreamExtension.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Raven.Abstractions.Logging;
using System.Runtime.InteropServices;
using System.Text;
using Raven.Abstractions.Data;

namespace Raven.Abstractions.Extensions
{
	/// <summary>
	/// Extensions for working with streams
	/// </summary>
	public static class StreamExtensions
	{
		public static void CopyTo(this Stream stream, Stream other)
		{
			var buffer = new byte[0x1000];
			while (true)
			{
				int read = stream.Read(buffer, 0, buffer.Length);
				if (read == 0)
					return;
				other.Write(buffer, 0, read);
			}
		}

		public static void Write(this Stream stream, int value)
		{
			var buffer = BitConverter.GetBytes(value);
			stream.Write(buffer, 0, 4);
		}

		public static int ReadInt32(this Stream stream)
		{
			var buffer = new byte[4];
			stream.Read(buffer, 0, 4);

			return BitConverter.ToInt32(buffer, 0);
		}

#if !SILVERLIGHT
		public static string ReadString(this Stream stream,Encoding encoding)
		{
			var stringLength = stream.ReadInt32();
			var buffer = new byte[stringLength];
			stream.Read(buffer, 0, stringLength);

			return encoding.GetString(buffer);
		}
#endif

		public static void Write(this Stream stream,string value,Encoding encoding)
		{
			stream.Write(value.Length);
			var buffer = encoding.GetBytes(value);
			stream.Write(buffer, 0, value.Length);
		}

		public static void Write(this Stream stream, Etag etag)
		{
			var buffer = etag.ToByteArray();
			stream.Write(buffer, 0, 16);
		}

		public static Etag ReadEtag(this Stream stream)
		{
			var buffer = new byte[16]; //etag size is 16 bytes
			stream.Read(buffer, 0, 16);
			return Etag.Parse(buffer);
		}

		/// <summary>
		/// Reads the entire request buffer to memory and return it as a byte array.
		/// </summary>
		/// <param name="stream">The stream to read.</param>
		/// <returns>The returned byte array.</returns>
		public static byte[] ReadData(this Stream stream)
		{
			var list = new List<byte[]>();
			const int defaultBufferSize = 1024 * 16;
			var buffer = new byte[defaultBufferSize];
			var currentOffset = 0;
			int read;
			while ((read = stream.Read(buffer, currentOffset, buffer.Length - currentOffset)) != 0)
			{
				currentOffset += read;
				if (currentOffset == buffer.Length)
				{
					list.Add(buffer);
					buffer = new byte[defaultBufferSize];
					currentOffset = 0;
				}
			}
			var totalSize = list.Sum(x => x.Length) + currentOffset;
			var result = new byte[totalSize];
			var resultOffset = 0;
			foreach (var partial in list)
			{
				Buffer.BlockCopy(partial, 0, result, resultOffset, partial.Length);
				resultOffset += partial.Length;
			}
			Buffer.BlockCopy(buffer, 0, result, resultOffset, currentOffset);
			return result;
		}

		/// <summary>
		/// Asynchronously reads the entire request buffer to memory and return it as a byte array.
		/// </summary>
		/// <param name="stream">The stream to read.</param>
		/// <returns>A task that, when completed, contains the returned byte array.</returns>
		public static async Task<byte[]> ReadDataAsync(this Stream stream)
		{
			var list = new List<byte[]>();
			const int defaultBufferSize = 1024 * 16;
			var buffer = new byte[defaultBufferSize];
			var currentOffset = 0;
			int read;
			while ((read = await stream.ReadAsync(buffer, currentOffset, buffer.Length - currentOffset)) != 0)
			{
				currentOffset += read;
				if (currentOffset == buffer.Length)
				{
					list.Add(buffer);
					buffer = new byte[defaultBufferSize];
					currentOffset = 0;
				}
			}
			var totalSize = list.Sum(x => x.Length) + currentOffset;
			var result = new byte[totalSize];
			var resultOffset = 0;
			foreach (var partial in list)
			{
				Buffer.BlockCopy(partial, 0, result, resultOffset, partial.Length);
				resultOffset += partial.Length;
			}
			Buffer.BlockCopy(buffer, 0, result, resultOffset, currentOffset);
			return result;
		}

		/// <summary>
		/// Allocates a byte array and reads an entire block from the stream
		/// </summary>
		public static byte[] ReadEntireBlock(this Stream stream, int count)
		{
			byte[] buffer = new byte[count];
			stream.ReadEntireBlock(buffer, 0, count);
			return buffer;
		}

		/// <summary>
		/// Reads an entire block from the stream
		/// </summary>
		public static void ReadEntireBlock(this Stream stream, byte[] buffer, int start, int count)
		{
			int totalRead = 0;
			while (totalRead < count)
			{
				int read = stream.Read(buffer, start + totalRead, count - totalRead);
				if (read == 0)
					throw new EndOfStreamException();
				totalRead += read;
			}
		}

		public static Stream DisposeTogetherWith(this Stream stream, params IDisposable[] disposables)
		{
			return new DisposingStream(stream, disposables);
		}

		private class DisposingStream : Stream
		{
			private Stream stream;
			private IDisposable[] disposables;

			public DisposingStream(Stream stream, IDisposable[] disposables)
			{
				this.stream = stream;
				this.disposables = disposables;
			}

			public override bool CanRead
			{
				get { return stream.CanRead; }
			}

			public override bool CanSeek
			{
				get { return stream.CanSeek; }
			}

			public override bool CanWrite
			{
				get { return stream.CanWrite; }
			}

			public override void Flush()
			{
				stream.Flush();
			}

			public override long Length
			{
				get { return stream.Length; }
			}

			public override long Position
			{
				get { return stream.Position; }
				set { stream.Position = value; }
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				return stream.Read(buffer, offset, count);
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				return stream.Seek(offset, origin);
			}

			public override void SetLength(long value)
			{
				stream.SetLength(value);
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				stream.Write(buffer, offset, count);
			}

			protected override void Dispose(bool disposing)
			{
				stream.Dispose();
				if (disposing)
				{
					foreach (var d in disposables)
					{
						try
						{
							d.Dispose();
						}
						catch (Exception ex)
						{
							LogManager.GetCurrentClassLogger().ErrorException("Error when disposing a DisposingStream: " + ex.Message, ex);
						}
					}
				}
			}
		}
	}
}
