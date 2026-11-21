// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text;
using System.IO;
using System.Text.Json;

namespace Steamfitter.Api.Services
{
    /// <summary>
    /// The action parameters that carry a guest credential: the password of a guest operation and the private key
    /// of an SSH one. They are the VM Credentials' secrets copied into a Task, so they are shown only to a caller
    /// who may edit the Task's Scenario or Scenario Template, as its VM Credentials are. Anyone else, and every
    /// SignalR message, gets <see cref="Redacted"/> in place of a value that is not empty.
    /// </summary>
    public static class TaskSecretParameters
    {
        /// <summary>What a secret reads as to a caller who may not see it. A Task saved with it keeps the secret it has.</summary>
        public const string Redacted = "********";

        /// <summary>
        /// The parameter an input string that is not a JSON object of strings is read into, whole. It can hold any
        /// secret the string does, so it is redacted as one.
        /// </summary>
        public const string BadInputString = "BadInputString";

        private static readonly string[] Keys = ["Password", "PrivateKey"];

        /// <summary>Whether the parameter carries a secret. Keys are matched without case, as a repeated key can differ in it.</summary>
        public static bool IsSecret(string key) => Keys.Contains(key, StringComparer.OrdinalIgnoreCase);

        /// <summary>Replaces each secret that is not empty, and an unread input string, with <see cref="Redacted"/>.</summary>
        public static void Redact(IDictionary<string, string> actionParameters)
        {
            if (actionParameters == null)
                return;

            foreach (var key in actionParameters.Keys.Where(x => IsSecret(x) || x == BadInputString).ToList())
            {
                if (!string.IsNullOrEmpty(actionParameters[key]))
                    actionParameters[key] = Redacted;
            }
        }

        /// <summary>
        /// Empties each secret, and an unread input string a Task was saved with, for a file that leaves them out. An
        /// import must not take the placeholder for a password.
        /// </summary>
        public static void Strip(IDictionary<string, string> actionParameters)
        {
            if (actionParameters == null)
                return;

            foreach (var key in actionParameters.Keys.Where(x => IsSecret(x) || x == BadInputString).ToList())
            {
                actionParameters[key] = string.Empty;
            }
        }

        /// <summary>
        /// Empties each secret that is <see cref="Redacted"/>, for a file whose Tasks have no stored secret for the
        /// placeholder to stand for. Returns whether there was one.
        /// </summary>
        public static bool ClearRedacted(IDictionary<string, string> actionParameters)
        {
            if (!AnyRedacted(actionParameters))
                return false;

            foreach (var key in actionParameters.Keys.Where(x => (IsSecret(x) || x == BadInputString) && actionParameters[x] == Redacted).ToList())
            {
                actionParameters[key] = string.Empty;
            }

            return true;
        }

        /// <summary>
        /// The input string with each secret that is <see cref="Redacted"/> emptied, keeping every other property,
        /// repeated ones included, with its value and type, and whether there was one. Values are compared as
        /// decoded, so an escaped placeholder is one. Input that is not a JSON object is returned as it is.
        /// </summary>
        public static (string InputString, bool Cleared) ClearRedacted(string inputString)
        {
            if (string.IsNullOrEmpty(inputString))
                return (inputString, false);

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(inputString);
            }
            catch (JsonException)
            {
                return (inputString, false);
            }

            using (document)
            {
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                    return (inputString, false);

                static bool IsRedactedSecret(JsonProperty property) =>
                    (IsSecret(property.Name) || property.Name == BadInputString)
                    && property.Value.ValueKind == JsonValueKind.String
                    && property.Value.GetString() == Redacted;

                // enumerated, not read into a dictionary, so a repeated key is kept as the file has it
                if (!document.RootElement.EnumerateObject().Any(IsRedactedSecret))
                    return (inputString, false);

                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
                {
                    writer.WriteStartObject();
                    foreach (var property in document.RootElement.EnumerateObject())
                    {
                        if (IsRedactedSecret(property))
                            writer.WriteString(property.Name, string.Empty);
                        else
                            property.WriteTo(writer);
                    }
                    writer.WriteEndObject();
                }

                return (Encoding.UTF8.GetString(stream.ToArray()), true);
            }
        }

        /// <summary>Whether any secret is <see cref="Redacted"/>, which only a copy of a redacted Task carries.</summary>
        public static bool AnyRedacted(IDictionary<string, string> actionParameters) =>
            actionParameters != null && actionParameters.Any(x => (IsSecret(x.Key) || x.Key == BadInputString) && x.Value == Redacted);

        /// <summary>
        /// Puts back the stored secrets that an edit sends as <see cref="Redacted"/>, as a caller sends back the Task
        /// it was given. A redacted secret the stored Task does not have is emptied rather than saved as the placeholder.
        /// </summary>
        public static void KeepStored(IDictionary<string, string> actionParameters, string storedInputString)
        {
            if (!AnyRedacted(actionParameters))
                return;

            var stored = TryRead(storedInputString);
            foreach (var key in actionParameters.Keys.ToList())
            {
                if (actionParameters[key] != Redacted)
                    continue;

                if (key == BadInputString)
                {
                    // the whole stored string the parameter was read from, or, once a save has wrapped it, the value
                    // stored under the key
                    actionParameters[key] = stored == null
                        ? storedInputString ?? string.Empty
                        : stored.TryGetValue(BadInputString, out var wrapped) ? wrapped ?? string.Empty : string.Empty;
                }
                else if (IsSecret(key))
                {
                    // the stored value under the same key, or under one that differs only in case
                    var storedKey = stored == null ? null
                        : stored.ContainsKey(key) ? key
                        : stored.Keys.FirstOrDefault(x => string.Equals(x, key, StringComparison.OrdinalIgnoreCase));
                    actionParameters[key] = storedKey != null ? stored[storedKey] ?? string.Empty : string.Empty;
                }
            }
        }

        private static Dictionary<string, string> TryRead(string inputString)
        {
            if (string.IsNullOrEmpty(inputString))
                return null;

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(inputString);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
