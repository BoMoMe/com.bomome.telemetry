using System;
using System.Collections.Generic;

namespace Bomome.Telemetry
{
    public enum TelemetryConsent { Unknown, Denied, Granted }

    public interface ITelemetrySink
    {
        string Id { get; }
        void Track(TelemetryEvent data);
        void Flush();
    }

    public interface ITelemetryService
    {
        bool Enabled { get; set; }
        TelemetryConsent Consent { get; set; }
        TelemetryCategory Categories { get; set; }
        void Track(TelemetryEvent data);
        void Flush();
    }

    public sealed class TelemetryRoute
    {
        public ITelemetrySink Sink { get; }
        public TelemetryCategory Categories { get; }
        public TelemetryRoute(ITelemetrySink sink, TelemetryCategory categories = TelemetryCategory.All)
        {
            Sink = sink ?? throw new ArgumentNullException(nameof(sink));
            if (string.IsNullOrWhiteSpace(sink.Id)) throw new ArgumentException("A sink needs a stable ID.");
            Categories = categories;
        }
    }

    /// <summary>
    /// Main-thread router; no queue, polling or hidden network traffic. Sinks own SDK
    /// initialization, bounded buffering and vendor-specific event mapping.
    /// </summary>
    public sealed class TelemetryService : ITelemetryService
    {
        private readonly TelemetryRoute[] routes;
        private readonly Action<string, Exception> onError;
        public bool Enabled { get; set; } = true;
        public TelemetryConsent Consent { get; set; } = TelemetryConsent.Unknown;
        public TelemetryCategory Categories { get; set; } = TelemetryCategory.All;
        public long SuppressedEvents { get; private set; }
        public long FailedDeliveries { get; private set; }

        public TelemetryService(IEnumerable<TelemetryRoute> routes, Action<string, Exception> onError = null)
        {
            if (routes == null) throw new ArgumentNullException(nameof(routes));
            var copy = new List<TelemetryRoute>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var route in routes)
            {
                if (route == null || !ids.Add(route.Sink.Id))
                    throw new ArgumentException("Routes must be non-null with unique sink IDs.");
                copy.Add(route);
            }
            this.routes = copy.ToArray();
            this.onError = onError;
        }

        public void Track(TelemetryEvent data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (!Enabled || Consent != TelemetryConsent.Granted || (Categories & data.Category) == 0)
            {
                SuppressedEvents++;
                return;
            }
            foreach (var route in routes)
                if ((route.Categories & data.Category) != 0)
                    try { route.Sink.Track(data); }
                    catch (Exception e) { FailedDeliveries++; Report(route.Sink.Id, e); }
        }

        public void Flush()
        {
            if (!Enabled || Consent != TelemetryConsent.Granted) return;
            foreach (var route in routes)
                try { route.Sink.Flush(); }
                catch (Exception e) { FailedDeliveries++; Report(route.Sink.Id, e); }
        }

        private void Report(string id, Exception error)
        {
            try { onError?.Invoke(id, error); }
            catch { /* Diagnostics cannot change game state. */ }
        }
    }
}
