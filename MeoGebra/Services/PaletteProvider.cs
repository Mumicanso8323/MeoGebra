using System.Collections.Generic;
using OxyPlot;

namespace MeoGebra.Services;

public static class PaletteProvider {
    public static IReadOnlyList<OxyColor> Colors { get; } = new List<OxyColor> {
        OxyColors.SkyBlue,
        OxyColors.OrangeRed,
        OxyColors.MediumSeaGreen,
        OxyColors.MediumPurple,
        OxyColors.Goldenrod,
        OxyColors.Crimson,
        OxyColors.DeepPink,
        OxyColors.Teal
    };
}
