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
        private IGauge _gauge;

        internal Timer(IGauge g)
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
            public static IMetricServer AsServer(
                string hostname,
                int port,
                string url = "metrics/",
                global::Prometheus.Advanced.ICollectorRegistry registry = null,
                bool useHttps = false)
            {
                var metricServer = new MetricServer(hostname, port, url, registry, useHttps);
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
                var child = counter.WithLabels(labels.Select(x => x.Value.ToString()).ToArray());
                child.Inc(increment);
            }
            else
            {
                counter.Inc(increment);
            }
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
                var child = gauge.WithLabels(labels.Select(x => x.Value.ToString()).ToArray());
                child.Inc(valueDelta);
            }
            else
            {
                gauge.Inc(valueDelta);
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
                var child = gauge.WithLabels(labels.Select(x => x.Value.ToString()).ToArray());
                child.Set(value);
            }
            else
            {
                gauge.Set(value);
            }
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
                var child = histogram.Labels(labels.Select(x => x.Value.ToString()).ToArray());
                child.Observe(value);
            }
            else
            {
                histogram.Observe(value);
            }
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
                var child = gauge.WithLabels(labels.Select(x => x.Value.ToString()).ToArray());
                return new Timer(child);
            }
            else
            {
                return new Timer(gauge);
            }
        }
    }
}
