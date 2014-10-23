﻿using System;
using System.Threading.Tasks;
using Hermes.Messages;
using Hermes.Properties;

namespace Hermes.Formatters
{
	public abstract class Formatter<T> : IFormatter
		where T : class, IMessage
	{
		readonly IChannel<IMessage> reader;
		readonly IChannel<byte[]> writer;

		public Formatter (IChannel<IMessage> reader, IChannel<byte[]> writer)
		{
			this.reader = reader;
			this.writer = writer;
		}
		
		public abstract MessageType MessageType { get; }

		protected abstract T Read (byte[] packet);

		protected abstract byte[] Write (T message);

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public async Task ReadAsync (byte[] packet)
		{
			var actualType = (MessageType)packet.Byte (0).Bits (4);

			if (MessageType != actualType) {
				var error = string.Format(Resources.Formatter_InvalidPacket, typeof(T).Name);

				throw new ProtocolException (error);
			}

			var message = this.Read (packet);

			await this.reader.SendAsync (message);
		}

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public async Task WriteAsync (IMessage message)
		{
			if (message.Type != MessageType) {
				var error = string.Format(Resources.Formatter_InvalidMessage, typeof(T).Name);

				throw new ProtocolException (error);
			}

			var packet = this.Write (message as T);

			await this.writer.SendAsync (packet);
		}

		protected void ValidateHeaderFlag (byte[] packet, Func<MessageType, bool> messageTypePredicate, int expectedFlag)
		{
			var headerFlag = packet.Byte (0).Bits (5, 4);

			if (messageTypePredicate(this.MessageType) && headerFlag != expectedFlag) {
				var error = string.Format (Resources.Formatter_InvalidHeaderFlag, headerFlag, typeof(T).Name, expectedFlag);

				throw new ProtocolException (error);
			}
		}
	}
}
