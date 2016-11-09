﻿using System.Collections.Generic;

namespace SeudoBuild
{
    public class SteamDistributeStep : DistributeStep
    {
        SteamDistributeConfig config;

        public SteamDistributeStep(SteamDistributeConfig config)
        {
            this.config = config;
        }

        public override string Type { get; } = "Steam Upload";

        public override void Distribute(List<ArchiveInfo> archiveInfos, Workspace workspace)
        {
            // TODO
        }
    }
}