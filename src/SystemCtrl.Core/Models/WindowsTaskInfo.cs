using System;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace SystemCtrl.Core.Models;

public partial class WindowsTaskInfo : ReactiveObject
{
    [Reactive] public partial string TaskName { get; set; } = string.Empty;

    [Reactive] public partial string TaskPath { get; set; } = string.Empty;

    [Reactive] public partial string Description { get; set; } = string.Empty;

    [Reactive] public partial string Author { get; set; } = string.Empty;

    [Reactive] public partial TaskState Status { get; set; }

    [Reactive] public partial string TriggerSummary { get; set; } = string.Empty;

    [Reactive] public partial DateTime? NextRunTime { get; set; }

    [Reactive] public partial DateTime? LastRunTime { get; set; }

    [Reactive] public partial int LastRunResult { get; set; }

    [Reactive] public partial string RunAsUser { get; set; } = string.Empty;
}
