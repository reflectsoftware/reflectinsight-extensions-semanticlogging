using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;

using ReflectSoftware.Insight.Common;

namespace ReflectSoftware.Insight.Extensions.SemanticLogging
{
    public sealed class RIEventSink: IObserver<EventEntry>, IDisposable
    {
        private static readonly String DetailsLine;

        private Boolean Disposed { get; set; }
        private IReflectInsight RIInstance { get; set; }
        private String InstanceName { get; set; }
        private String MessagePattern { get; set; }
        private String TimeFormat { get; set; }

        /// <summary>
        /// Initializes the <see cref="RIEventSink"/> class.
        /// </summary>
        static RIEventSink()
        {
            DetailsLine = String.Format("{0,40}", String.Empty).Replace(" ", "-");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RIEventSink"/> class.
        /// </summary>
        /// <param name="instanceName">Name of the instance.</param>
        /// <param name="messagePattern">The message pattern.</param>
        /// <param name="timeFormat">The time format.</param>
        public RIEventSink(String instanceName, String messagePattern, String timeFormat)
        {
            Disposed = false;
            InstanceName = instanceName ?? String.Empty;
            MessagePattern = String.IsNullOrWhiteSpace(messagePattern) ? "%message%" : messagePattern;
            TimeFormat = String.IsNullOrWhiteSpace(timeFormat) ? "yyyy-MM-ddTHH:mm:ss.fffffffZ" : timeFormat;
                                    
            RIEventManager.OnServiceConfigChange += OnConfigChange;
            OnConfigChange();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RIEventSink"/> class.
        /// </summary>
        /// <param name="instanceName">Name of the instance.</param>
        /// <param name="messagePattern">The message pattern.</param>
        public RIEventSink(String instanceName, String messagePattern): this(instanceName, messagePattern, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RIEventSink"/> class.
        /// </summary>
        /// <param name="instanceName">Name of the instance.</param>
        public RIEventSink(String instanceName): this(instanceName, null, null)
        {
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="RIEventSink"/> class.
        /// </summary>
        ~RIEventSink()
        {
            Dispose();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            lock (this)
            {
                if (!Disposed)
                {
                    Disposed = true;
                    GC.SuppressFinalize(this);
                    RIEventManager.OnServiceConfigChange -= OnConfigChange;                    
                }
            }
        }

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// </summary>
        public void OnCompleted()
        {
            Dispose();
        }

        /// <summary>
        /// Called when [configuration change].
        /// </summary>
        private void OnConfigChange()
        {
            try
            {
                lock (this)
                {
                    RIInstance = RILogManager.Get(InstanceName) ?? RILogManager.Default;
                }
            }
            catch (Exception ex)
            {                
                OnError(ex);
            }
        }

        /// <summary>
        /// Payloads the format.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <returns></returns>
        private static String PayloadFormat(EventEntry entry)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < entry.Payload.Count; i++)
            {
                sb.AppendFormat("[{0} : {1}] ", entry.Schema.Payload[i], entry.Payload[i]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Messages the format.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <returns></returns>
        private String MessageFormat(EventEntry entry)
        {
            StringBuilder sb = new StringBuilder(MessagePattern);
            sb.Replace("%providerid%", entry.ProviderId.ToString());
            sb.Replace("%eventid%", entry.EventId.ToString());
            sb.Replace("%keywords%", entry.Schema.Keywords.ToString());
            sb.Replace("%level%", entry.Schema.Level.ToString());
            sb.Replace("%message%", entry.FormattedMessage);
            sb.Replace("%opcode%", entry.Schema.Opcode.ToString());
            sb.Replace("%task%", entry.Schema.Task.ToString());
            sb.Replace("%version%", entry.Schema.Version.ToString());
            sb.Replace("%payload%", PayloadFormat(entry));
            sb.Replace("%eventname%", entry.Schema.EventName);
            sb.Replace("%timestamp%", entry.GetFormattedTimestamp(TimeFormat));

            return sb.ToString();
        }

        /// <summary>
        /// Detailses the format.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <returns></returns>
        private String DetailsFormat(EventEntry entry)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Details:");
            sb.AppendLine(DetailsLine);
            sb.AppendFormat("ProviderId: {0}{1}", entry.ProviderId, Environment.NewLine);
            sb.AppendFormat("EventId: {0}{1}", entry.EventId, Environment.NewLine);
            sb.AppendFormat("Keywords: {0}{1}", entry.Schema.Keywords, Environment.NewLine);
            sb.AppendFormat("Level: {0}{1}", entry.Schema.Level, Environment.NewLine);
            sb.AppendFormat("Message: {0}{1}", entry.FormattedMessage, Environment.NewLine);
            sb.AppendFormat("Opcode: {0}{1}", entry.Schema.Opcode, Environment.NewLine);
            sb.AppendFormat("Task: {0}{1}", entry.Schema.Task, Environment.NewLine);
            sb.AppendFormat("Version: {0}{1}", entry.Schema.Version, Environment.NewLine);
            sb.AppendFormat("Payload: {0}{1}", PayloadFormat(entry), Environment.NewLine);
            sb.AppendFormat("EventName: {0}{1}", entry.Schema.EventName, Environment.NewLine);
            sb.AppendFormat("Timestamp: {0}{1}", entry.GetFormattedTimestamp(TimeFormat), Environment.NewLine);

            return sb.ToString();
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="mType">Type of the m.</param>
        /// <param name="eventEntry">The event entry.</param>
        /// <param name="details">The details.</param>
        private void SendMessage(MessageType mType, EventEntry eventEntry, String details)
        {
            RIInstance.Send(mType, MessageFormat(eventEntry), details);
        }

        /// <summary>
        /// Called when [next].
        /// </summary>
        /// <param name="entry">The entry.</param>
        public void OnNext(EventEntry entry)
        {
            if (entry == null)
                return;

            try
            {                    
                if (entry.Schema.Level == EventLevel.Informational)
                {
                    SendMessage(MessageType.SendInformation, entry, null);
                }
                else if (entry.Schema.Level == EventLevel.Warning)
                {
                    SendMessage(MessageType.SendWarning, entry, null);
                }
                else if (entry.Schema.Level == EventLevel.Error)
                {
                    SendMessage(MessageType.SendError, entry, DetailsFormat(entry));
                }
                else if (entry.Schema.Level == EventLevel.Critical)
                {
                    SendMessage(MessageType.SendFatal, entry, DetailsFormat(entry));
                }
                else if (entry.Schema.Level == EventLevel.Verbose)
                {
                    SendMessage(MessageType.SendVerbose, entry, DetailsFormat(entry));
                }
                else // safetynet
                {
                    SendMessage(MessageType.SendMessage, entry, null);
                }
            }
            catch(Exception e)
            {
                OnError(e);
            }            
        }

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public void OnError(Exception error)
        {            
            SemanticLoggingEventSource.Log.CustomSinkUnhandledFault(error.ToString());
        }
    }

    public static class RIEventLog
    {
        /// <summary>
        /// Logs to reflect insight.
        /// </summary>
        /// <param name="eventStream">The event stream.</param>
        /// <param name="instanceName">Name of the instance.</param>
        /// <param name="messagePattern">The message pattern.</param>
        /// <param name="timeFormat">The time format.</param>
        /// <returns></returns>
        public static SinkSubscription<RIEventSink> LogToReflectInsight(this IObservable<EventEntry> eventStream, String instanceName = null, String messagePattern = null, String timeFormat = null)
        {
            var sink = new RIEventSink(instanceName, messagePattern, timeFormat);
            var subscription = eventStream.Subscribe(sink);
            return new SinkSubscription<RIEventSink>(subscription, sink);
        }

        /// <summary>
        /// Logs to reflect insight.
        /// </summary>
        /// <param name="eventStream">The event stream.</param>
        /// <param name="instanceName">Name of the instance.</param>
        /// <param name="messagePattern">The message pattern.</param>
        /// <returns></returns>
        public static SinkSubscription<RIEventSink> LogToReflectInsight(this IObservable<EventEntry> eventStream, String instanceName, String messagePattern)
        {
            return LogToReflectInsight(eventStream, instanceName, messagePattern, null);
        }

        /// <summary>
        /// Logs to reflect insight.
        /// </summary>
        /// <param name="eventStream">The event stream.</param>
        /// <param name="instanceName">Name of the instance.</param>
        /// <returns></returns>
        public static SinkSubscription<RIEventSink> LogToReflectInsight(this IObservable<EventEntry> eventStream, String instanceName)
        {
            return LogToReflectInsight(eventStream, instanceName, null, null);
        }

        /// <summary>
        /// Creates the listener.
        /// </summary>
        /// <param name="instanceName">Name of the instance.</param>
        /// <param name="messagePattern">The message pattern.</param>
        /// <param name="timeFormat">The time format.</param>
        /// <returns></returns>
        public static EventListener CreateListener(String instanceName, String messagePattern = null, String timeFormat = null)
        {
            var listener = new ObservableEventListener();
            listener.LogToReflectInsight(instanceName, messagePattern, timeFormat);
            return listener;
        }

        /// <summary>
        /// Creates the listener.
        /// </summary>
        /// <param name="instanceName">Name of the instance.</param>
        /// <returns></returns>
        public static EventListener CreateListener(String instanceName)
        {
            return CreateListener(instanceName, null, null);
        }

        /// <summary>
        /// Creates the listener.
        /// </summary>
        /// <returns></returns>
        public static EventListener CreateListener()
        {
            return CreateListener(null, null, null);
        }
    }
}
