﻿using CommandLine;
using Mackiloha.App;
using Mackiloha.App.Extensions;

namespace SuperFreq.Options;

[Verb("png2tex", HelpText = "Converts HMX texture to png (beta feature)")]
internal class PngToTextureOptions : GameOptions
{
    [Value(0, Required = true, MetaName = "pngPath", HelpText = "Path to input png")]
    public string InputPath { get; set; }

    [Value(1, Required = true, MetaName = "texPath", HelpText = "Path to output texture")]
    public string OutputPath { get; set; }

    public static void Parse(PngToTextureOptions op)
    {
        op.UpdateOptions();

        var appState = AppState.FromFile(op.InputPath);
        appState.UpdateSystemInfo(op.GetSystemInfo());

        var bitmap = TextureExtensions.BitmapFromImage(op.InputPath, appState.SystemInfo);
        var serializer = appState.GetSerializer();
        serializer.WriteToFile(op.OutputPath, bitmap);

        Log.Information("Wrote image to \"{outputPath}\"", op.OutputPath);
    }
}
