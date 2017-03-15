using System.Linq;
using NLog;
using NzbDrone.Common.Reflection;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.Search
{
    public class TorrentSeedingSpecification : IDecisionEngineSpecification
    {
        private readonly IndexerFactory _indexerFactory;
        private readonly Logger _logger;

        public TorrentSeedingSpecification(IndexerFactory indexerFactory, Logger logger)
        {
            _indexerFactory = indexerFactory;
            _logger = logger;
        }

        public RejectionType Type => RejectionType.Permanent;


        public Decision IsSatisfiedBy(RemoteEpisode remoteEpisode, SearchCriteriaBase searchCriteria)
        {
            var torrentInfo = remoteEpisode.Release as TorrentInfo;

            if (torrentInfo == null)
            {
                return Decision.Accept();
            }

            var indexer = _indexerFactory.Get(torrentInfo.IndexerId);
            var minimumSeedersProperty = indexer.Settings.GetType().GetSimpleProperties().SingleOrDefault(s => s.Name == "MinimumSeeders");
            var minimumSeeders = minimumSeedersProperty == null ? 1 : (int)minimumSeedersProperty.GetValue(indexer.Settings, null);

            if (torrentInfo.Seeders.HasValue && torrentInfo.Seeders.Value < minimumSeeders)
            {
                _logger.Debug("Not enough seeders: {0}. Minimum seeders: {1}", torrentInfo.Seeders, minimumSeeders);
                return Decision.Reject("Not enough seeders: {0}. Minimum seeders: {1}", torrentInfo.Seeders, minimumSeeders);
            }

            return Decision.Accept();
        }
    }
}