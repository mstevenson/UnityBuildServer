﻿using System;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.ZipArchive
{
    public class ZipArchiveModule : IArchiveModule
    {
        public Type StepType { get; } = typeof(ZipArchiveStep);

        public JsonConverter ConfigConverter { get; } = new ZipArchiveConfigConverter();

        public bool MatchesConfigType(StepConfig config)
        {
            return config is ZipArchiveConfig;
        }
    }
}
