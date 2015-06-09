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
        private ReflectInsight RIInstance { get; set; }
        private String InstanceName { get; set; }
        private String MessagePattern { get; set; }
        private String TimeFormat { get; set; }
                
        //---------------------------------------------------------------------
        static RIEventSink()
        {
            DetailsLine = String.Format("{0,40}", String.Empty).Replace(" ", "-");
        }
        //---------------------------------------------------------------------
        public RIEventSink(String instanceName, String messagePattern, String timeFormat)
        {
            Disposed = false;
            InstanceName = instanceName ?? String.Empty;
            MessagePattern = String.IsNullOrWhiteSpace(messagePattern) ? "%message%" : messagePattern;
            TimeFormat = String.IsNullOrWhiteSpace(timeFormat) ? "yyyy-MM-ddTHH:mm:ss.fffffffZ" : timeFormat;
                                    
            RIEventManager.OnServiceConfigChange += OnConfigChange;
            OnConfigChange();
        }
        //---------------------------------------------------------------------
        public RIEventSink(String instanceName, String messagePattern): this(instanceName, messagePattern, null)
        {
        }
        //---------------------------------------------------------------------
        public RIEventSink(String instanceName): this(instanceName, null, null)
        {
        }
        //---------------------------------------------------------------------
        ~RIEventSink()
        {
            Dispose();
        }
        //---------------------------------------------------------------------
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
        //---------------------------------------------------------------------
        public void OnCompleted()
        {
            Dispose();
        }
        //---------------------------------------------------------------------
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
        //---------------------------------------------------------------------
        private static String PayloadFormat(EventEntry entry)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < entry.Payload.Count; i++)
            {
                sb.AppendFormat("[{0} : {1}] ", entry.Schema.Payload[i], entry.Payload[i]);
            }

            return sb.ToString();
        }
        //---------------------------------------------------------------------
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
        //---------------------------------------------------------------------
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
        //---------------------------------------------------------------------
        private void SendMessage(MessageType mType, EventEntry eventEntry, String details)
        {
            RIInstance.Send(mType, MessageFormat(eventEntry), details);
        }
        //---------------------------------------------------------------------
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
        //---------------------------------------------------------------------
        public void OnError(Exception error)
        {            
            SemanticLoggingEventSource.Log.CustomSinkUnhandledFault(error.ToString());
        }
    }

    public static class RIEventLog
    {
        //---------------------------------------------------------------------
        public static SinkSubscription<RIEventSink> LogToReflectInsight(this IObservable<EventEntry> eventStream, String instanceName = null, String messagePattern = null, String timeFormat = null)
        {
            var sink = new RIEventSink(instanceName, messagePattern, timeFormat);
            var subscription = eventStream.Subscribe(sink);
            return new SinkSubscription<RIEventSink>(subscription, sink);
        }
        //---------------------------------------------------------------------
        public static SinkSubscription<RIEventSink> LogToReflectInsight(this IObservable<EventEntry> eventStream, String instanceName, String messagePattern)
        {
            return LogToReflectInsight(eventStream, instanceName, messagePattern, null);
        }
        //---------------------------------------------------------------------
        public static SinkSubscription<RIEventSink> LogToReflectInsight(this IObservable<EventEntry> eventStream, String instanceName)
        {
            return LogToReflectInsight(eventStream, instanceName, null, null);
        }
        //---------------------------------------------------------------------
        public static EventListener CreateListener(String instanceName, String messagePattern = null, String timeFormat = null)
        {
            var listener = new ObservableEventListener();
            listener.LogToReflectInsight(instanceName, messagePattern, timeFormat);
            return listener;
        }
        //---------------------------------------------------------------------
        public static EventListener CreateListener(String instanceName)
        {
            return CreateListener(instanceName, null, null);
        }
        //---------------------------------------------------------------------
        public static EventListener CreateListener()
        {
            return CreateListener(null, null, null);
        }
    }
}
