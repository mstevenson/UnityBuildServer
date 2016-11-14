﻿using System;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.UnityBuild
{
    public class UnityStandardBuildModule : IBuildModule
    {
        public string Name { get; } = "Unity (Standard)";

        public Type StepType { get; } = typeof(UnityStandardBuildStep);

        public JsonConverter ConfigConverter { get; } = new UnityBuildConverter();

        public bool MatchesConfigType(StepConfig config)
        {
            return config is UnityStandardBuildConfig;
        }
    }
}