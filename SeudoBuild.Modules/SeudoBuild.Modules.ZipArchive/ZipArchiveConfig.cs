﻿namespace SeudoBuild.Modules.ZipArchive
{
    public class ZipArchiveConfig : ArchiveStepConfig
    {
        public override string Type { get; } = "Zip File";
        public string Filename { get; set; }
    }
}