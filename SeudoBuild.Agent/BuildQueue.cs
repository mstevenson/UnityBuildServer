using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using SeudoBuild.Pipeline;

namespace SeudoBuild.Agent
{
    /// <summary>
    /// Queues projects and feeds them sequentially to a builder.
    /// </summary>
    public class BuildQueue : IBuildQueue
    {
        IBuilder builder;
        IModuleLoader moduleLoader;
        ILogger logger;

        CancellationTokenSource tokenSource;
        int buildIndex;
        bool isQueueRunning;

        public BuildResult ActiveBuild { get; private set; }
        ConcurrentQueue<BuildResult> QueuedBuilds { get; } = new ConcurrentQueue<BuildResult>();
        ConcurrentDictionary<int, BuildResult> Builds { get; } = new ConcurrentDictionary<int, BuildResult>();

        public BuildQueue(IBuilder builder, IModuleLoader moduleLoader, ILogger logger)
        {
            this.builder = builder;
            this.moduleLoader = moduleLoader;
            this.logger = logger;
        }

        /// <summary>
        /// Begin executing builds in the queue. Builds will continue until the queue has been exhaused.
        /// </summary>
        public void StartQueue()
        {
            if (isQueueRunning)
            {
                return;
            }
            isQueueRunning = true;

            tokenSource = new CancellationTokenSource();

            // Create output folder in user's documents folder
            string outputPath = CreateOutputFolder();

            Task.Factory.StartNew(() => TaskQueuePump(outputPath, moduleLoader), tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        string CreateOutputFolder()
        {
            string directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (Workspace.RunningPlatform == Platform.Mac)
            {
                directory = Path.Combine(directory, "Documents");
            }
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException("User documents folder not found");
            }
            directory = Path.Combine(directory, "SeudoBuild");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            return directory;
        }

        void TaskQueuePump(string outputPath, IModuleLoader moduleLoader)
        {
            while (true)
            {
                // Clean up and bail out
                if (tokenSource.IsCancellationRequested)
                {
                    ActiveBuild = null;
                    return;
                }

                if (!builder.IsRunning && QueuedBuilds.Count > 0)
                {
                    BuildResult build = null;
                    if (QueuedBuilds.TryDequeue(out build))
                    {
                        // Ignore builds that have been cancelled
                        if (build.BuildStatus != BuildResult.Status.Queued)
                        {
                            continue;
                        }

                        string printableTarget = string.IsNullOrEmpty(build.TargetName) ? "default target" : $"target '{build.TargetName}'";
                        logger.QueueNotification($"Building project '{build.ProjectConfiguration.ProjectName}', {printableTarget}");

                        ActiveBuild = build;
                        builder.Build(ActiveBuild.ProjectConfiguration, ActiveBuild.TargetName, outputPath);
                    }
                }
                Thread.Sleep(200);
            }
        }

        /// <summary>
        /// Queues a project for building.
        /// If target is null, the default target in the given ProjectConfig will be used.
        /// </summary>
        public BuildResult EnqueueBuild(ProjectConfig config, string target = null)
        {
            buildIndex++;
            var result = new BuildResult
            {
                Id = buildIndex,
                BuildStatus = BuildResult.Status.Queued,
                ProjectConfiguration = config,
                TargetName = target
            };

            QueuedBuilds.Enqueue(result);
            Builds.TryAdd(result.Id, result);
            return result;
        }

        /// <summary>
        /// Returns results for all known builds, including successful, failed, in-progress, and queued builds.
        /// </summary>
        public IEnumerable<BuildResult> GetAllBuildResults()
        {
            return Builds.Values;
        }

        /// <summary>
        /// Return the result for a specific build.
        /// </summary>
        public BuildResult GetBuildResult(int buildId)
        {
            BuildResult result = null;
            Builds.TryGetValue(buildId, out result);
            return result;
        }

        /// <summary>
        /// Stop a given build. If the build is in progress it will be halted, otherwise it will be removed from the queue.
        /// </summary>
        public BuildResult CancelBuild(int buildId)
        {
            if (ActiveBuild != null && ActiveBuild.Id == buildId)
            {
                ActiveBuild = null;
                // TODO signal build process to stop
            }

            BuildResult result = null;
            if (Builds.TryGetValue(buildId, out result))
            {
                result.BuildStatus = BuildResult.Status.Cancelled;
            }
            return result;
        }
    }
}
