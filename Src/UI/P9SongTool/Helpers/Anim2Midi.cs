﻿using Mackiloha.Song;
using NAudio.Midi;
using System.Text.RegularExpressions;

namespace P9SongTool.Helpers;

public class Anim2Midi
{
    protected readonly string[] TBRBCharacters = new[] { "paul", "john", "george", "ringo" };

    protected readonly PropAnim Anim;
    protected readonly MidiHelper MidiHelper;

    public Anim2Midi(PropAnim anim, string midPath)
    {
        Anim = anim;
        MidiHelper = new MidiHelper(midPath);
    }

    public void ExportMidi(string exportMidPath)
    {
        // Create directory if it doesn't exist
        var dirPath = Path.GetDirectoryName(exportMidPath);
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        // Create dictionary of tracks to filter events in
        var midFilteredTracks = TBRBCharacters
            .Select(x => x.ToUpper())
            .Concat(new[] { "VENUE" })
            .ToDictionary(x => x, y => new List<MidiEvent>());

        var nameFilterRegex = new Regex($"(?i)([_]?)({string.Join("|", TBRBCharacters)})$");

        foreach (var group in Anim.DirectorGroups)
        {
            // Default event name + track
            var eventName = group.PropName;
            var track = midFilteredTracks["VENUE"];

            var match = nameFilterRegex.Match(group.PropName);
            if (match.Success)
            {
                // Get last group match (ex: "_ringo" -> "ringo" and "spot_paul" -> "paul")
                var key = match
                    .Groups
                    .Values
                    .Last()
                    .Value
                    .ToUpper();

                // Look for character track
                if (midFilteredTracks.ContainsKey(key))
                {
                    // Remove appended name + assign character track
                    eventName = nameFilterRegex.Replace(eventName, "");
                    track = midFilteredTracks[key];
                }
            }

            foreach (var ev in group.Events.OrderBy(x => x.Position))
            {
                var tickPos = MidiHelper.FramePosToTicks((decimal)ev.Position);

                var evValue = ev switch
                {
                    DirectedEventFloat evFloat => $"{evFloat.Value}",
                    DirectedEventTextFloat { Value: 0.0f } evTextFloat => $"{evTextFloat.Text}", // postproc events
                    DirectedEventTextFloat evTextFloat => $"{evTextFloat.Text} {evTextFloat.Value}",
                    DirectedEventBoolean evBool => $"{(evBool.Enabled ? "TRUE" : "FALSE")}",
                    DirectedEventVector4 evVector4 => $"{evVector4.Value.X} {evVector4.Value.Y} {evVector4.Value.Z} {evVector4.Value.W}",
                    DirectedEventVector3 evVector3 => $"{evVector3.Value.X} {evVector3.Value.Y} {evVector3.Value.Z}",
                    DirectedEventText evTextFloat => $"{evTextFloat.Text}",
                    _ => throw new NotSupportedException()
                };

                var evText = eventName switch
                {
                    "body" => $"[{evValue}]",
                    "shot" => $"[{evValue}]",
                    "configuration" => $"[config ({evValue})]", // Use alias
                    "postproc" when evValue.EndsWith(".pp") && !evValue.Contains(' ') => $"[{evValue}]",
                    "lyric_transition" when evValue.EndsWith(".anim") && !evValue.StartsWith('(') => $"[{evValue}]",
                    _ => $"[{eventName} ({evValue})]",
                };
                track.Add(new TextEvent(evText, MetaEventType.TextEvent, tickPos));
            }
        }

        foreach (var key in midFilteredTracks.Keys)
        {
            if (midFilteredTracks[key].Count <= 0)
            {
                // Remove empty track
                midFilteredTracks.Remove(key);
            }
        }

        // Get tracks from mid
        var midiTracks = MidiHelper.CreateMidiTracksFromBase();

        foreach (var kv in midFilteredTracks)
        {
            var trackName = kv.Key;
            var track = kv.Value;

            var existingTrack = midiTracks
                .FirstOrDefault(x =>
                    x.Any(y => (y is TextEvent te)
                        && te.MetaEventType == MetaEventType.SequenceTrackName
                        && te.Text == trackName));

            if ((existingTrack is null))
            {
                // Sort events
                track.Sort((x, y) => x.AbsoluteTime.CompareTo(y.AbsoluteTime));

                // Create new track
                // Insert track name and add end event
                track.Insert(0, new TextEvent(trackName, MetaEventType.SequenceTrackName, 0));
                track.Add(new MetaEvent(MetaEventType.EndTrack, 0, track.Max(x => x.AbsoluteTime)));

                midiTracks.Add(track);
                continue;
            }

            // Use existing track

            // Remove track name event
            var trackNameEvent = existingTrack
                .First(x => x is TextEvent me
                    && me.MetaEventType == MetaEventType.SequenceTrackName);

            existingTrack.Remove(trackNameEvent);

            // Remove end event if it exists (it probably should)
            var endEvent = existingTrack
                .FirstOrDefault(x => x is MetaEvent me
                    && me.MetaEventType == MetaEventType.EndTrack);

            if (!(endEvent is null))
                existingTrack.Remove(endEvent);

            existingTrack.AddRange(track);

            // Sort events
            existingTrack.Sort((x, y) => x.AbsoluteTime.CompareTo(y.AbsoluteTime));

            // Insert track name
            existingTrack.Insert(0, new TextEvent(trackName, MetaEventType.SequenceTrackName, 0));

            // Add end event
            existingTrack.Add(new MetaEvent(MetaEventType.EndTrack, 0, existingTrack.Max(x => x.AbsoluteTime)));
        }

        // Copy tracks to mid
        var mid = new MidiEventCollection(1, MidiHelper.GetTicksPerQuarter());
        midiTracks.ForEach(x => mid.AddTrack(x));

        MidiFile.Export(exportMidPath, mid);
    }
}
