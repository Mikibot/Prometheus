using Prometheus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Miki.Data.Prometheus
{
    public class Timer : IDisposable
    {
        private Stopwatch _stopwatch;
        private Gauge _gauge;

        internal Timer(Gauge g)
        {
            _stopwatch = Stopwatch.StartNew();
            _gauge = g;
        }

        public void Dispose()
        {
            _gauge.Set(_stopwatch.ElapsedMilliseconds);
        }
    }

    public static class Metrics
    {
        public static class Configure
        {
            public static IMetricServer AsServer(
                int port, 
                string url = "metrics/", 
                global::Prometheus.Advanced.ICollectorRegistry registry = null, 
                bool useHttps = false)
            {
                var metricServer = new MetricServer(port, url, registry, useHttps);
                metricServer.Start();
                return metricServer;
            }

            public static IMetricServer AsPusher(
                string endPoint, 
                string job, 
                string instance = null, 
                int intervalMilliseconds = 1000, 
                IDictionary<string, object> additionalLabels = null,
                global::Prometheus.Advanced.ICollectorRegistry registry = null)
            {
                var metricPusher = new MetricPusher(
                    endPoint,
                    job,
                    instance,
                    intervalMilliseconds,
                    additionalLabels.Select(x => new Tuple<string, string>(x.Key, x.Value.ToString())),
                    registry);
                metricPusher.Start();
                return metricPusher;
            }
        }

        /// <summary>
        /// Creates and records a counter metric.
        /// </summary>
        /// <param name="metricName">Name of the published metric</param>
        /// <param name="increment">Amount to increment</param>
        /// <param name="description">Help text</param>
        /// <param name="labels">Labels</param>
        public static void Counter(
            string metricName,
            double increment = 1,
            string description = "", 
            IDictionary<string, object> labels = null)
        {
            var counter = global::Prometheus.Metrics.CreateCounter(
                metricName, 
                description, 
                new CounterConfiguration
            {
                LabelNames = labels?.Select(x => x.Key).ToArray(),
                SuppressInitialValue = true
            });

            if (labels != null)
            {
                counter.WithLabels(labels.Select(x => x.Value.ToString()).ToArray());
            }

            counter.Inc(increment);
        }

        /// <summary>
        /// Creates and records a gauge metric.
        /// </summary>
        /// <param name="metricName">Name of the published metric</param>
        /// <param name="value">Increase or deduce values.</param>
        /// <param name="description">Help text</param>
        /// <param name="labels">Labels</param>
        public static void Gauge(
            string metricName,
            double valueDelta,
            string description = "",
            IDictionary<string, object> labels = null)
        {
            var gauge = global::Prometheus.Metrics.CreateGauge(
                    metricName,
                    description,
                    new GaugeConfiguration
                    {
                        LabelNames = labels?.Select(x => x.Key).ToArray(),
                        SuppressInitialValue = true
                    }
                );

            if (labels != null)
            {
                gauge.WithLabels(labels.Select(x => x.Value.ToString()).ToArray());
            }

            if (valueDelta > 0)
            {
                gauge.Inc(valueDelta);
            }
            else
            {
                gauge.Dec(Math.Abs(valueDelta));
            }
        }

        /// <summary>
        /// Creates and records a gauge metric.
        /// </summary>
        /// <param name="metricName">Name of the published metric</param>
        /// <param name="value">Set values.</param>
        /// <param name="description">Help text</param>
        /// <param name="labels">Labels</param>
        public static void GaugeSet(
            string metricName,
            double value,
            string description = "",
            IDictionary<string, object> labels = null)
        {
            var gauge = global::Prometheus.Metrics.CreateGauge(
                    metricName,
                    description,
                    new GaugeConfiguration
                    {
                        LabelNames = labels.Select(x => x.Key).ToArray(),
                        SuppressInitialValue = true
                    }
                );

            if (labels != null)
            {
                gauge.WithLabels(labels.Select(x => x.Value.ToString()).ToArray());
            }

            gauge.Set(value);
        }

        /// <summary>
        /// Creates and records a histogram metric.
        /// </summary>
        /// <param name="metricName">Name of the published metric</param>
        /// <param name="value">Current value</param>
        /// <param name="description">Help text</param>
        /// <param name="buckets">Buckets to set for the histogram</param>
        /// <param name="labels">Labels</param>
        public static void Histogram(
            string metricName,
            double value,
            string description = "",
            IEnumerable<double> buckets = null,
            IDictionary<string, object> labels = null)
        {
            var histogram = global::Prometheus.Metrics.CreateHistogram(
                    metricName,
                    description,
                    new HistogramConfiguration
                    {
                        LabelNames = labels.Select(x => x.Key).ToArray(),
                        SuppressInitialValue = true
                    }
                );

            if (labels != null)
            {
                histogram.WithLabels(labels.Select(x => x.Value.ToString()).ToArray());
            }

            histogram.Observe(value);
        }

        /// <summary>
        /// Creates and records a gauge metric based on the time it took to dispose the object.
        /// </summary>
        /// <param name="metricName">Name of the published metric</param>
        /// <param name="description">Help text</param>
        /// <param name="labels">Labels</param>
        public static Timer Timing(
           string metricName,
           string description = "",
           IDictionary<string, object> labels = null)
        {
            var gauge = global::Prometheus.Metrics.CreateGauge(
                    metricName,
                    description,
                    new GaugeConfiguration
                    {
                        LabelNames = labels.Select(x => x.Key).ToArray(),
                        SuppressInitialValue = true
                    }
                );

            if (labels != null)
            {
                gauge.WithLabels(labels.Select(x => x.Value.ToString()).ToArray());
            }

            return new Timer(gauge);
        }
    }
}
