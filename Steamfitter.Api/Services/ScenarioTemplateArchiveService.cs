// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using STT = System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Infrastructure.Extensions;
using Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Services
{
    public interface IScenarioTemplateArchiveService
    {
        STT.Task<(MemoryStream Stream, string FileName)> ExportAsync(IEnumerable<Guid> ids, CancellationToken ct);
        STT.Task<ImportScenarioTemplatesResult> ImportAsync(IFormFile file, CancellationToken ct);
    }

    /// <summary>
    /// Exports Scenario Templates, with their VM Credentials and Task trees, to a json file,
    /// and imports them back as new copies with new ids. Nothing in a Scenario Template is a
    /// file of its own, so the export is plain json; a zip holding the json is also accepted
    /// on import. Each Scenario Template that cannot be imported is reported, the rest are created.
    /// </summary>
    public class ScenarioTemplateArchiveService : IScenarioTemplateArchiveService
    {
        // guard against zip bombs and oversized uploads; a scenario template json is a few hundred KB at most.
        // Counted on the bytes actually read, not on the size a zip entry declares.
        private const long MaxJsonBytes = 10 * 1024 * 1024;

        // what Windows refuses in a file name; Path.GetInvalidFileNameChars() on Linux is only '/' and '\0'
        private static readonly char[] InvalidFileNameChars = "<>:\"/\\|?*".ToCharArray();

        private static readonly Regex GuidPattern = new(
            @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
            RegexOptions.Compiled);

        internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly SteamfitterContext _context;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;

        public ScenarioTemplateArchiveService(SteamfitterContext context, IPrincipal user, IMapper mapper)
        {
            _context = context;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
        }

        #region Export

        public async STT.Task<(MemoryStream Stream, string FileName)> ExportAsync(IEnumerable<Guid> ids, CancellationToken ct)
        {
            var idList = ids?.Distinct().ToList() ?? new List<Guid>();
            IQueryable<ScenarioTemplateEntity> query = _context.ScenarioTemplates
                .AsNoTracking()
                .Include(st => st.VmCredentials)
                .Include(st => st.Tasks);

            if (idList.Any())
            {
                query = query.Where(st => idList.Contains(st.Id));
            }

            var templates = await query.AsSplitQuery().OrderBy(st => st.Name).ToListAsync(ct);
            var missing = idList.Except(templates.Select(t => t.Id)).ToList();
            if (missing.Any())
                throw new Infrastructure.Exceptions.EntityNotFoundException<ScenarioTemplate>(
                    $"ScenarioTemplate {string.Join(", ", missing)} was not found.");

            // one requested template is written as an object, anything else as a list
            var json = idList.Count == 1
                ? JsonSerializer.SerializeToUtf8Bytes(ToExport(templates[0]), JsonOptions)
                : JsonSerializer.SerializeToUtf8Bytes(templates.Select(ToExport).ToList(), JsonOptions);

            var fileName = idList.Count == 1
                ? SanitizeFileName(templates[0].Name)
                : ScenarioTemplateExportConstants.DefaultFileName;

            return (new MemoryStream(json), $"{fileName}.json");
        }

        private static ScenarioTemplateExport ToExport(ScenarioTemplateEntity template)
        {
            return new ScenarioTemplateExport
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                DurationHours = template.DurationHours,
                DefaultVmCredentialId = template.DefaultVmCredentialId,
                VmCredentials = template.VmCredentials
                    .OrderBy(c => c.Username)
                    .Select(c => new VmCredentialExport
                    {
                        Id = c.Id,
                        Username = c.Username,
                        Password = c.Password,
                        Description = c.Description
                    })
                    .ToList(),
                Tasks = OrderParentsFirst(template.Tasks)
                    .Select(ToExport)
                    .ToList()
            };
        }

        private static TaskExport ToExport(TaskEntity task)
        {
            var export = new TaskExport
            {
                Id = task.Id,
                Name = task.Name,
                Description = task.Description,
                Action = task.Action,
                VmMask = task.VmMask,
                ApiUrl = task.ApiUrl,
                ExpectedOutput = task.ExpectedOutput,
                ExpirationSeconds = task.ExpirationSeconds,
                DelaySeconds = task.DelaySeconds,
                IntervalSeconds = task.IntervalSeconds,
                Iterations = task.Iterations,
                IterationTermination = task.IterationTermination,
                TriggerTaskId = task.TriggerTaskId,
                TriggerCondition = task.TriggerCondition,
                Score = task.Score,
                UserExecutable = task.UserExecutable,
                Repeatable = task.Repeatable
            };

            if (!string.IsNullOrEmpty(task.InputString))
            {
                try
                {
                    export.ActionParameters = JsonSerializer.Deserialize<Dictionary<string, string>>(task.InputString);
                }
                catch (JsonException)
                {
                    export.InputString = task.InputString;
                }
            }

            return export;
        }

        /// <summary>
        /// Orders a template's tasks breadth first from the root tasks, so a parent always
        /// appears before its children in the file. Tasks not reachable from a root are appended.
        /// </summary>
        private static IEnumerable<TaskEntity> OrderParentsFirst(IEnumerable<TaskEntity> tasks)
        {
            var list = tasks.ToList();
            var ids = list.Select(t => t.Id).ToHashSet();
            var byParent = list
                .Where(t => t.TriggerTaskId.HasValue && ids.Contains(t.TriggerTaskId.Value))
                .ToLookup(t => t.TriggerTaskId.Value);
            var ordered = new List<TaskEntity>();
            var seen = new HashSet<Guid>();
            var queue = new Queue<TaskEntity>(list
                .Where(t => !t.TriggerTaskId.HasValue || !ids.Contains(t.TriggerTaskId.Value))
                .OrderBy(t => t.Name));

            while (queue.Count > 0)
            {
                var task = queue.Dequeue();
                if (!seen.Add(task.Id))
                    continue;
                ordered.Add(task);
                foreach (var child in byParent[task.Id].OrderBy(t => t.Name))
                {
                    queue.Enqueue(child);
                }
            }

            ordered.AddRange(list.Where(t => !seen.Contains(t.Id)));
            return ordered;
        }

        private static string SanitizeFileName(string name)
        {
            var sanitized = new string((name ?? string.Empty)
                .Select(c => InvalidFileNameChars.Contains(c) || char.IsControl(c) || char.IsWhiteSpace(c) ? '-' : c)
                .ToArray())
                .Trim('-', '.');

            return string.IsNullOrEmpty(sanitized) ? ScenarioTemplateExportConstants.DefaultFileName : sanitized;
        }

        #endregion

        #region Import

        public async STT.Task<ImportScenarioTemplatesResult> ImportAsync(IFormFile file, CancellationToken ct)
        {
            var result = new ImportScenarioTemplatesResult();

            List<(ScenarioTemplateExport Template, string Error)> templates;
            try
            {
                templates = await ReadFile(file, ct);
            }
            catch (Exception ex) when (ex is InvalidDataException or JsonException or NotSupportedException)
            {
                result.Failures.Add(new ImportScenarioTemplateFailure
                {
                    Name = file?.FileName,
                    Reason = $"Invalid file: {ex.Message}"
                });
                return result;
            }

            var userId = _user.GetId();
            var userName = _user.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
            var now = DateTime.UtcNow;
            var entities = new List<ScenarioTemplateEntity>();
            var namesInUse = (await _context.ScenarioTemplates.Select(st => st.Name).ToListAsync(ct)).ToHashSet();

            foreach (var (template, error, index) in templates.Select((x, i) => (x.Template, x.Error, i)))
            {
                if (error != null)
                {
                    result.Failures.Add(new ImportScenarioTemplateFailure
                    {
                        Name = $"Item {index + 1}",
                        Reason = error
                    });
                    continue;
                }

                var failures = Validate(template).ToList();
                if (failures.Any())
                {
                    result.Failures.AddRange(failures.Select(reason => new ImportScenarioTemplateFailure
                    {
                        Id = template.Id,
                        Name = template.Name,
                        Reason = reason
                    }));
                    continue;
                }

                var entity = BuildEntity(template, userId, now, result.Warnings);

                // the same naming as a copy, so an imported template can be told apart from the one it shares a name with
                if (namesInUse.Contains(entity.Name) && !string.IsNullOrEmpty(userName))
                {
                    entity.Name = $"{template.Name} - {userName}";
                    result.Warnings.Add($"Scenario Template {template.Name} already exists and was imported as {entity.Name}");
                }
                namesInUse.Add(entity.Name);

                _context.ScenarioTemplates.Add(entity);
                entities.Add(entity);
            }

            if (entities.Any())
            {
                // A single SaveChanges is atomic on its own, so no explicit transaction is
                // needed (and none is opened, which keeps this safe under a retrying
                // execution strategy).
                await _context.SaveChangesAsync(ct);
            }

            result.ScenarioTemplates = _mapper.Map<List<ScenarioTemplate>>(entities);
            return result;
        }

        /// <summary>
        /// Reads the Scenario Templates from an uploaded json file, or from the json file inside an uploaded zip.
        /// The json is either a list of Scenario Templates or a single one. An item that cannot be read
        /// is returned with the reason, so it fails on its own.
        /// </summary>
        private static async STT.Task<List<(ScenarioTemplateExport Template, string Error)>> ReadFile(IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                throw new InvalidDataException("No file was uploaded");

            // a zip is never larger than the json it holds, so this bounds both
            if (file.Length > MaxJsonBytes)
                throw new InvalidDataException($"{file.FileName} is too large");

            await using var upload = file.OpenReadStream();
            var buffer = await ReadCapped(upload, file.FileName, ct);

            if (IsZip(buffer))
            {
                using var zip = new ZipArchive(buffer, ZipArchiveMode.Read);
                var jsonEntries = zip.Entries
                    .Where(e => e.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var entry = jsonEntries.FirstOrDefault(e => e.FullName == ScenarioTemplateExportConstants.ExportFileName)
                    ?? (jsonEntries.Count == 1 ? jsonEntries[0] : null);

                if (entry == null)
                    throw new InvalidDataException($"The zip does not contain {ScenarioTemplateExportConstants.ExportFileName}");

                await using var entryStream = entry.Open();
                return Deserialize(await ReadCapped(entryStream, entry.FullName, ct));
            }

            return Deserialize(buffer);
        }

        private static async STT.Task<MemoryStream> ReadCapped(Stream stream, string name, CancellationToken ct)
        {
            var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(chunk, ct)) > 0)
            {
                if (buffer.Length + read > MaxJsonBytes)
                    throw new InvalidDataException($"{name} is larger than {MaxJsonBytes / (1024 * 1024)} MB");
                buffer.Write(chunk, 0, read);
            }
            buffer.Position = 0;
            return buffer;
        }

        private static bool IsZip(MemoryStream stream)
        {
            var bytes = stream.GetBuffer();
            return stream.Length >= 4 && bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04;
        }

        private static List<(ScenarioTemplateExport Template, string Error)> Deserialize(MemoryStream stream)
        {
            // a UTF-8 byte order mark, as some editors write, is skipped
            var bytes = stream.GetBuffer().AsMemory(0, (int)stream.Length);
            if (bytes.Span.StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }))
                bytes = bytes[3..];

            using var document = JsonDocument.Parse(bytes);
            return document.RootElement.ValueKind switch
            {
                JsonValueKind.Array => document.RootElement.EnumerateArray().Select(DeserializeItem).ToList(),
                JsonValueKind.Object => [DeserializeItem(document.RootElement)],
                _ => throw new InvalidDataException("The file is not a Scenario Template export")
            };
        }

        private static (ScenarioTemplateExport Template, string Error) DeserializeItem(JsonElement item)
        {
            if (item.ValueKind != JsonValueKind.Object)
                return (null, $"Not a Scenario Template ({item.ValueKind})");

            try
            {
                return (item.Deserialize<ScenarioTemplateExport>(JsonOptions), null);
            }
            catch (JsonException ex)
            {
                return (null, $"Not a valid Scenario Template: {ex.Message}");
            }
        }

        private static IEnumerable<string> Validate(ScenarioTemplateExport template)
        {
            if (string.IsNullOrWhiteSpace(template.Name))
                yield return "Scenario Template has no Name";

            template.Tasks ??= new List<TaskExport>();
            template.VmCredentials ??= new List<VmCredentialExport>();
            var tasks = template.Tasks;
            var credentials = template.VmCredentials;

            if (tasks.Any(t => t == null) || credentials.Any(c => c == null))
            {
                yield return "The file contains an empty Task or VM Credential";
                yield break;
            }

            foreach (var duplicate in tasks.GroupBy(t => t.Id).Where(g => g.Key == Guid.Empty || g.Count() > 1))
            {
                yield return $"Task id {duplicate.Key} is missing or not unique";
            }

            foreach (var duplicate in credentials.GroupBy(c => c.Id).Where(g => g.Key == Guid.Empty || g.Count() > 1))
            {
                yield return $"VM Credential id {duplicate.Key} is missing or not unique";
            }

            var taskIds = tasks.Select(t => t.Id).ToHashSet();
            foreach (var task in tasks)
            {
                if (task.TriggerTaskId.HasValue && !taskIds.Contains(task.TriggerTaskId.Value))
                    yield return $"Task {task.Name} ({task.Id}): Trigger Task {task.TriggerTaskId} is not part of the imported Scenario Template";

                if (!Enum.IsDefined(task.Action))
                    yield return $"Task {task.Name} ({task.Id}): Unknown Action {task.Action}";

                if (!Enum.IsDefined(task.TriggerCondition))
                    yield return $"Task {task.Name} ({task.Id}): Unknown Trigger Condition {task.TriggerCondition}";

                if (!Enum.IsDefined(task.IterationTermination))
                    yield return $"Task {task.Name} ({task.Id}): Unknown Iteration Termination {task.IterationTermination}";
            }

            // every chain of Trigger Tasks must end at a root task
            var parentOf = tasks
                .GroupBy(t => t.Id)
                .ToDictionary(g => g.Key, g => g.First().TriggerTaskId);
            foreach (var task in tasks)
            {
                var visited = new HashSet<Guid> { task.Id };
                var parentId = task.TriggerTaskId;
                while (parentId.HasValue && parentOf.ContainsKey(parentId.Value))
                {
                    if (!visited.Add(parentId.Value))
                    {
                        yield return $"Task {task.Name} ({task.Id}): Trigger Tasks form a cycle";
                        break;
                    }
                    parentId = parentOf[parentId.Value];
                }
            }
        }

        private static ScenarioTemplateEntity BuildEntity(
            ScenarioTemplateExport template,
            Guid userId,
            DateTime now,
            List<string> warnings)
        {
            var entity = new ScenarioTemplateEntity
            {
                Id = Guid.NewGuid(),
                Name = template.Name,
                Description = template.Description,
                DurationHours = template.DurationHours,
                CreatedBy = userId,
                DateCreated = now
            };

            var credentialIdMap = new Dictionary<Guid, Guid>();
            foreach (var credential in template.VmCredentials)
            {
                var credentialEntity = new VmCredentialEntity
                {
                    Id = Guid.NewGuid(),
                    Username = credential.Username,
                    Password = credential.Password,
                    Description = credential.Description,
                    CreatedBy = userId,
                    DateCreated = now
                };
                credentialIdMap[credential.Id] = credentialEntity.Id;
                entity.VmCredentials.Add(credentialEntity);
            }

            if (template.DefaultVmCredentialId.HasValue)
            {
                if (credentialIdMap.TryGetValue(template.DefaultVmCredentialId.Value, out var defaultId))
                {
                    entity.DefaultVmCredentialId = defaultId;
                }
                else
                {
                    warnings.Add($"Scenario Template {template.Name}: Default VM Credential {template.DefaultVmCredentialId} is not in the file and was cleared");
                }
            }

            var taskEntities = new Dictionary<Guid, TaskEntity>();
            foreach (var task in template.Tasks)
            {
                var taskEntity = new TaskEntity
                {
                    Id = Guid.NewGuid(),
                    Name = task.Name,
                    Description = task.Description,
                    Action = task.Action,
                    VmMask = task.VmMask,
                    ApiUrl = task.ApiUrl,
                    InputString = task.InputString ??
                        (task.ActionParameters != null ? JsonSerializer.Serialize(task.ActionParameters) : null),
                    ExpectedOutput = task.ExpectedOutput,
                    ExpirationSeconds = task.ExpirationSeconds,
                    DelaySeconds = task.DelaySeconds,
                    IntervalSeconds = task.IntervalSeconds,
                    Iterations = task.Iterations > 0 ? task.Iterations : 1,
                    IterationTermination = task.IterationTermination,
                    CurrentIteration = 0,
                    TriggerCondition = task.TriggerCondition,
                    Score = task.Score,
                    UserExecutable = task.UserExecutable,
                    Repeatable = task.Repeatable,
                    Status = Data.TaskStatus.none,
                    UserId = userId,
                    CreatedBy = userId,
                    DateCreated = now,
                    ScenarioTemplate = entity
                };
                taskEntities[task.Id] = taskEntity;
                entity.Tasks.Add(taskEntity);

                if (!string.IsNullOrEmpty(task.VmMask) && GuidPattern.IsMatch(task.VmMask))
                {
                    warnings.Add($"Scenario Template {template.Name}: Task {task.Name} targets VMs by id ({task.VmMask}). " +
                        "VM ids are specific to the environment the file came from; check the Task's VMs before running it.");
                }
            }

            // rebuild the Trigger Task tree. The navigations make EF insert parents before children;
            // the id is set too, because CalculateScores below treats a task with no TriggerTaskId as a root,
            // and EF only fills it in from the navigation once the entity is tracked.
            foreach (var task in template.Tasks.Where(t => t.TriggerTaskId.HasValue))
            {
                var child = taskEntities[task.Id];
                var parent = taskEntities[task.TriggerTaskId.Value];
                child.TriggerTaskId = parent.Id;
                child.TriggerTask = parent;
                parent.Children.Add(child);
            }

            entity.Memberships.Add(new ScenarioTemplateMembershipEntity
            {
                UserId = userId,
                ScenarioTemplate = entity,
                RoleId = ScenarioTemplateRoleEntityDefaults.ScenarioTemplateCreatorRoleId
            });

            entity.CalculateScores();

            return entity;
        }

        #endregion
    }
}
