using RoyTheunissen.FMODSyntax;
using UnityEngine;
using VertexSnapper.Managers;

namespace VertexSnapper.Helper;

public static class AudioExtensions
{
    /// <summary>
    ///     Plays the sound only if cool sounds are enabled in the configuration.
    /// </summary>
    public static T PlayIfEnabled<T>(this FmodAudioConfig<T> audioEvent, Transform source = null)
        where T : FmodAudioPlayback => VertexSnapperConfigManager.SoundEnabled.Value ? audioEvent.Play(source) : null;
}