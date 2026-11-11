namespace WinFormsMudBlazor.Models.Entities;

public enum MixerType
{
    MIXER100 = 100,
    MIXER200 = 200,
    MIXER300 = 300,
    MIXER400 = 400
}

public static class MixerTypeExtensions
{
    public static string GetDisplayName(this MixerType mixer) => mixer switch
    {
        MixerType.MIXER100 => "MIXER 100",
        MixerType.MIXER200 => "MIXER 200",
        MixerType.MIXER300 => "MIXER 300",
        MixerType.MIXER400 => "MIXER 400",
        _ => mixer.ToString()
    };
}
