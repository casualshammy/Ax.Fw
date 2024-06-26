﻿using System.Collections.Immutable;

namespace Ax.Fw.Storage.Data;

public record StorageRetentionOptions(
  TimeSpan? DocumentMaxAgeFromCreation,
  TimeSpan? DocumentMaxAgeFromLastChange,
  TimeSpan? ScanInterval,
  Action<IReadOnlySet<DocumentEntryMeta>>? OnDocsDeleteCallback = null);
