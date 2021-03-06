#region License
/*
 * NReco library (http://nreco.googlecode.com/)
 * Copyright 2008-2014 Vitaliy Fedorchenko
 * Distributed under the LGPL licence
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion

using System;
using System.Data;
using System.Data.Common;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Transactions;
using NReco.Logging;

namespace NReco.Application
{
	/// <summary>
	/// Transaction-aware implementation of event broker used for NReco application events.
	/// </summary>
	public class EventBroker
	{
		static ILog log = LogManager.GetLogger(typeof(EventBroker));

		/// <summary>
		/// Occurs each time when event is published, but before executing subscribed handlers
		/// </summary>
		public event EventHandler<EventArgs> Publishing;

		/// <summary>
		/// Occurs each time when event is published, but after executing subscribed handlers
		/// </summary>
		public event EventHandler<EventArgs> Published;

		private IDictionary<Type, IList<HandlerWrapper>> eventHandlers = new Dictionary<Type, IList<HandlerWrapper>>();

		/// <summary>
		/// Get or set DB connections that should be opened for events published in transaction.
		/// </summary>
		public IEnumerable<IDbConnection> TransactionConnections { get; set; }

		/// <summary>
		/// Initializes a new instance of DataEventBroker
		/// </summary>
		public EventBroker() {
		}

		/// <summary>
		/// Publish specified event in the transaction scope
		/// </summary>
		/// <param name="sender">event source</param>
		/// <param name="eventArgs">event arguments</param>
		public virtual void PublishInTransaction(object sender, EventArgs eventArgs) {
			using (var scope = new TransactionScope()) {
				var openedConnections = new List<IDbConnection>();
				try {
					if (TransactionConnections!=null)
						foreach (var conn in TransactionConnections) {
							if (conn.State != ConnectionState.Open) {
								conn.Open();
								openedConnections.Add(conn);
							}
						}
					Publish(sender, eventArgs);
				} finally {
					foreach (var conn in openedConnections)
						try {
							if (conn.State==ConnectionState.Open)
								conn.Close();
						} catch (Exception ex) {
							log.Write(LogEvent.Error, "Cannot close DB connection: {0}", ex);
						}
				}
				scope.Complete();
			}
		}

		/// <summary>
		/// Publish specified event for registered subscribers
		/// </summary>
		/// <param name="sender">event source</param>
		/// <param name="eventArgs">event arguments</param>
		public virtual void Publish(object sender, EventArgs eventArgs) {
			if (eventArgs==null)
				throw new ArgumentNullException("eventData");
			if (Publishing != null)
				Publishing(sender, eventArgs);

			var eventDataType = eventArgs.GetType();
			while (eventDataType != null) {
				if (eventHandlers.ContainsKey(eventDataType)) {
					var matchedHandlers = new HandlerWrapper[eventHandlers[eventDataType].Count];
					eventHandlers[eventDataType].CopyTo(matchedHandlers,0);
					foreach (var h in matchedHandlers)
						h.Handler.DynamicInvoke(sender, eventArgs);
				}
				eventDataType = eventDataType.BaseType;
			}

			if (Published != null)
				Published(sender, eventArgs);
		}

		/// <summary>
		/// Subscribe a handler for specified event type
		/// </summary>
		/// <typeparam name="T">event type to match</typeparam>
		/// <param name="handler">event handler delegate</param>
		public void Subscribe<T>(EventHandler<T> handler) where T : EventArgs {
			var eventType = typeof(T);
			SubscribeInternal(eventType, null, handler);
		}

		/// <summary>
		/// Subscribe a handler for specified event type
		/// </summary>
		/// <typeparam name="T">event type to match</typeparam>
		/// <param name="match">match condition delegate</param>
		/// <param name="handler">event handler delegate</param>
		public void Subscribe<T>(Func<EventArgs, bool> match, EventHandler<T> handler) where T : EventArgs {
			var eventType = typeof(T);
			SubscribeInternal(eventType, match, handler);
		}


		/// <summary>
		/// Subscribe a handler for specified event type
		/// </summary>
		/// <param name="eventType">event type to match</param>
		/// <param name="handler">event handler delegate</param>
		public void Subscribe(Type eventType, EventHandler<EventArgs> handler) {
			SubscribeInternal(eventType, null, handler);
		}

		/// <summary>
		/// Subscribe a handler for specified event type and match condition
		/// </summary>
		/// <param name="eventType">event type to match</param>
		/// <param name="match">match condition delegate</param>
		/// <param name="handler">event handler delegate</param>
		public void Subscribe(Type eventType, Func<EventArgs, bool> match, EventHandler<EventArgs> handler) {
			SubscribeInternal(eventType, match, handler);
		}

		protected virtual void SubscribeInternal(Type eventType, Func<EventArgs, bool> match, Delegate handler) {
			if (!eventHandlers.ContainsKey(eventType))
				eventHandlers[eventType] = new List<HandlerWrapper>();
			eventHandlers[eventType].Add(new HandlerWrapper(handler, match));
		}

		/// <summary>
		/// Unsubscribes specified delegate from all events
		/// </summary>
		/// <remarks>This method unsubscribes specified delegate from ALL event types</remarks>
		/// <param name="handler">delegate to unsubscribe</param>
		/// <returns>true if item is successfully removed; otherwise, false.</returns>
		public virtual bool Unsubscribe(Delegate handler) {
			var removed = false;
			var h = new HandlerWrapper(handler, null);
			foreach (var entry in eventHandlers) {
				if (entry.Value.Remove(h)) {
					removed = true;
				}
			}
			return removed;
		}

		internal class HandlerWrapper {
			internal Delegate Handler;
			internal Func<EventArgs,bool> Match;

			public HandlerWrapper(Delegate d, Func<EventArgs,bool> match) {
				Handler = d;
				Match = match;
			}
		
			public bool IsMatch(EventArgs e) {
				if (Match!=null)
					return Match(e);
				return true;
			}

			public override bool Equals(object o)
			{
 				if (!(o is HandlerWrapper)) return false;
				var other = (HandlerWrapper)o;

				if (Handler.Target == other.Handler.Target && Handler.Method==other.Handler.Method) {
					return true;
				}
				return Handler==((HandlerWrapper)o).Handler;
			}

			public override int GetHashCode() {
				return Handler.GetHashCode();
			}

		}


	}
}
