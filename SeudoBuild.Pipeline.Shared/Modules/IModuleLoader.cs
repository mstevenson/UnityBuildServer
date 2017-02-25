﻿namespace SeudoBuild.Pipeline
{
    /// <summary>
    /// Loads pipeline module DLLs and makes them available to build scripts
    /// that include the pipeline steps that the modules define.
    /// </summary>
    public interface IModuleLoader
    {
        IModuleRegistry Registry { get; }
        T CreatePipelineStep<T>(StepConfig config, IWorkspace workspace) where T : IPipelineStep;
    }
}
