using System;
using System.Collections.Generic;

namespace YallaFinance.Infrastructure.Database.Generated.Entities;

public partial class DatabaseVersion
{
    public long Id { get; set; }

    public string Version { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string ScriptName { get; set; } = null!;

    public string ChecksumSha256 { get; set; } = null!;

    public DateTime AppliedAtUtc { get; set; }

    public string AppliedBy { get; set; } = null!;
}
