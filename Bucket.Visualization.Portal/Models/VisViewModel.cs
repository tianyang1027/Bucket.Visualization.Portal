namespace Bucket.Visualization.Portal.Models
{
    public class VisViewModel
    {
        public StatsViewModel? Stats  { get; set; }

        public List<SamplesViewModel>? Samples { get; set; }
    }

    public class SamplesViewModel
    {
        public string? BucketName { get; set; }
        public string? Key { get; set; }
        public string? PKey { get; set; }
        public string? MUrl { get; set; }
        public string? PUrl { get; set; }
        public string? MDomain { get; set; }
        public string? PDomain { get; set; }
        public string? ProdThumbnailKey { get; set; }
        public string? PrismyV3Rank { get; set; }
        public string? Title { get; set; }

    }

    public class StatsViewModel
    {
        public string? BucketName { get; set; }

        public string? Count { get; set; }

        public string? totalCount { get; set; }

        public string? Percentage { get; set; }
    }
}
