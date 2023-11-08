﻿using Mackiloha.App;
using NAudio.Midi;
using P9SongTool.Json;
using P9SongTool.Models;
using P9SongTool.Options;
using System.Text.Json;

namespace P9SongTool.Apps;

public class NewProjectApp
{
    protected readonly string[] DefaultMIDITracks = new[] { "PAUL", "JOHN", "GEORGE", "RINGO", "VENUE" };

    public void Parse(NewProjectOptions op)
    {
        var outputDir = Path.GetFullPath(op.OutputPath);
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        var songMetaPath = Path.Combine(outputDir, "song.json");
        var midPath = Path.Combine(outputDir, "venue.mid");
        var lipsyncDir = Path.Combine(outputDir, "lipsync");
        var extraDir = Path.Combine(outputDir, "extra");

        if (!Directory.Exists(lipsyncDir))
        {
            Directory.CreateDirectory(lipsyncDir);
            File.WriteAllText(Path.Combine(lipsyncDir, "LIPSYNC_HERE"), "");
            Log.Information("Created lipsync directory");
        }

        if (!Directory.Exists(extraDir))
        {
            Directory.CreateDirectory(extraDir);
            File.WriteAllText(Path.Combine(extraDir, "EXTRA_MILO_RELATED_FILES_HERE"), "");
            Log.Information("Created extras directory");
        }

        // Write venue mid
        CreateDefaultMid(midPath);

        // Create song preferences file
        var appState = new AppState(outputDir);

        var song = CreateP9Song(op.ProjectName);
        var songJson = SerializeSong(song);
        var songJsonPath = songMetaPath;

        File.WriteAllText(songJsonPath, songJson);
        Log.Information("Wrote \"{SongJsonName}\"", "song.json");

        Log.Information("Successfully created project in \"{OutputDir}\"", outputDir);
    }

    protected P9Song CreateP9Song(string name)
        => new P9Song()
        {
            Name = name,
            Preferences = new SongPreferences()
            {
                Venue = "dreamscape",
                MiniVenues = new List<string>()
                {
                    "abbeyroad01default"
                },
                Scenes = new List<string>(),
                DreamscapeOutfit = "sixtyeight",
                StudioOutfit = "sixtyeight_hdp",
                GeorgeInstruments = new List<string>()
                {
                    "guitar_rh_gibson_lespaul_red"
                },
                JohnInstruments = new List<string>()
                {
                    "guitar_rh_epi65casino_strip"
                },
                PaulInstruments = new List<string>()
                {
                    "bass_lh_ricken_4001s_stripped"
                },
                RingoInstruments = new List<string>()
                {
                    "drum_dream04"
                },
                Tempo = "medium",
                SongClips = "none",
                DreamscapeFont = "none",
                GeorgeAmp = "none",
                JohnAmp = "none",
                PaulAmp = "none",
                Mixer = "none",
                DreamscapeCamera = "kP9DreamSlow",
                LyricPart = "PART HARM1"
            }
        };

    protected void CreateDefaultMid(string midPath)
    {
        var mid = new MidiEventCollection(1, 480);

        // Create basic tempo track
        var tempoTrack = new List<MidiEvent>();
        tempoTrack.Add(new TextEvent("animTempo", MetaEventType.SequenceTrackName, 0));
        tempoTrack.Add(new MetaEvent(MetaEventType.EndTrack, 0, 0));
        mid.AddTrack(tempoTrack);

        // Add other tracks
        foreach (var trackName in DefaultMIDITracks)
        {
            var track = new List<MidiEvent>();
            track.Add(new TextEvent(trackName, MetaEventType.SequenceTrackName, 0));
            track.Add(new MetaEvent(MetaEventType.EndTrack, 0, 0));
            mid.AddTrack(track);
        }

        MidiFile.Export(midPath, mid);
        Log.Information("Wrote \"{VenueMidName}\"", "venue.mid");
    }

    protected string SerializeSong(P9Song song)
    {
        return JsonSerializer.Serialize(song, P9SongToolJsonContext.Default.P9Song);
    }
}
