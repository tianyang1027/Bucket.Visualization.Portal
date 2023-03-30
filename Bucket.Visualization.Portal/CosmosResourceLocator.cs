namespace Bucket.Visualization.Portal
{
    using System;

    public class CosmosResourceLocator
    {
        private readonly string _vc;
        private readonly string _path;

        public CosmosResourceLocator(string url, DateTime dt)
            : this(url, dt, null)
        {
        }

        public CosmosResourceLocator(string url, DateTime dt, int? number)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("Stream URL is null or empty");

            var uri = new Uri(url);

            if (uri.Scheme.ToLowerInvariant() != "https" || !uri.Host.ToLowerInvariant().Contains("cosmos") || !uri.AbsolutePath.ToLowerInvariant().StartsWith("/cosmos/"))
                throw new ArgumentException("Stream URL does not look like a valid Cosmos URL: " + url);

            var vcNameIdx = url.IndexOf('/', uri.Scheme.Length + 3 + uri.Host.Length + 8);
            if (vcNameIdx == -1)
                throw new ArgumentException("Cannot extract virtual cluster name from URL: " + url);

            _vc = url.Substring(0, vcNameIdx);
            _path = url.Substring(vcNameIdx);

            _path = _path.Replace("(today)", dt.ToString("(yyyy-MM-dd)"));

            if (number != null)
            {
                _path = _path.Replace("%n", ((int)number).ToString());
            }
        }

        public string VirtualCluster
        {
            get { return _vc; }
        }

        public string StreamPath
        {
            get { return _path; }
        }

        public string Url
        {
            get { return _vc + _path; }
        }
    }
}
