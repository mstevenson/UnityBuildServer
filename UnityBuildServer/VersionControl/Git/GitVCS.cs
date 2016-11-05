﻿using System;
using System.IO;
using System.Collections.Generic;
using RunProcessAsTask;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace UnityBuildServer
{
    public class GitVCS : VCS
    {
        string workingDirectory;
        GitVCSConfig config;

        public static UsernamePasswordCredentials credentials;
        FilterRegistration lfsFilter;
        Signature signature;
        CredentialsHandler credentialsHandler;

        public GitVCS(string workingDirectory, GitVCSConfig config)
        {
            this.workingDirectory = workingDirectory;
            this.config = config;

            credentials = new UsernamePasswordCredentials
            {
                Username = config.Username,
                Password = config.Password
            };
            credentialsHandler = (url, usernameFromUrl, types) => new UsernamePasswordCredentials { Username = config.Username, Password = config.Password };
            signature = new Signature(new Identity("UnityBuildServer", "info@basenjigames.com"), DateTimeOffset.UtcNow);

            // Set up LFS filter
            var filter = new LFSFilter("lfs", workingDirectory, new List<FilterAttributeEntry> { new FilterAttributeEntry("lfs") });
            lfsFilter = GlobalSettings.RegisterFilter(filter);
        }

        public override string TypeName
        {
            get
            {
                return "git";
            }
        }

        public override bool IsWorkingCopyInitialized
        {
            get
            {
                return Directory.Exists($"{workingDirectory}/.git");
            }
        }

        // Clone
        public override void Download()
        {
            // TODO shallow clone?

            BuildConsole.WriteLine($"Cloning repository:  {config.RepositoryURL}");
            var cloneOptions = new CloneOptions
            {
                CredentialsProvider = credentialsHandler,
                BranchName = config.RepositoryBranchName,
                Checkout = true,
                RecurseSubmodules = true
            };
            Repository.Clone(config.RepositoryURL, workingDirectory, cloneOptions);

            if (config.IsLFS)
            {
                PullLFS();
            }

            // TODO Handle sub-module credentials
        }

        // Pull
        public override void Update()
        {
            BuildConsole.WriteLine("Cleaning working copy");

            // TODO Emulate git clean -d -x -f

            //using (var repo = new Repository(workingDirectory)) {
            //    Commit currentCommit = repo.Head.Tip.Tree;
            //    currentCommit.cl
            //}


            if (config.IsLFS)
            {
                PullLFS();
            }

            // Equivalent of 'git pull'
            using (var repo = new Repository(workingDirectory))
            {
                string remoteName = "origin";

                BuildConsole.WriteLine($"Fetching from {remoteName}:  ({config.RepositoryURL})");
                var fetchOptions = new FetchOptions
                {
                    CredentialsProvider = credentialsHandler
                };
                repo.Fetch(remoteName, fetchOptions);

                BuildConsole.WriteLine($"Merging changes into {config.RepositoryBranchName}");
                var mergeOptions = new MergeOptions
                {
                    FastForwardStrategy = FastForwardStrategy.FastForwardOnly,
                    FileConflictStrategy = CheckoutFileConflictStrategy.Theirs,
                    MergeFileFavor = MergeFileFavor.Theirs,
                    FailOnConflict = false
                };
                repo.Merge(config.RepositoryBranchName, signature, mergeOptions);
            }
        }

        void PullLFS()
        {
            BuildConsole.WriteLine($"Pulling LFS files: {config.IsLFS}");

            // TODO
        }
    }
}