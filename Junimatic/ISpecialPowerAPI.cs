using System;
using Microsoft.Xna.Framework;

namespace SpecialPowerUtilities;

public interface ISpecialPowerAPI
{
    /// <summary>Register the data for a power category for a specific mod to be used in the Powers menu display.</summary>
    /// <param name="uniqueID">The unique ID of the mod (typically yours) from the manifest.</param>
    /// <param name="displayName">A function that returns a string to be used as the display text when hovering over the power category icon.</param>
    /// <param name="iconTexture">An asset name for the icon you wish to use for the power category e.g. "Mods/UniqueId/PowersTab"</param>
    /// <returns>
    /// <para>A bool describing whether the power category was successfully registered.</para>
    /// <para>True if the power category was successfully registered, false if the power category was not successfully registered for some reason.</para>
    /// </returns>
    bool RegisterPowerCategory(string uniqueID, Func<string> displayName, string iconTexture);



    /// <summary>Register the data for a power category for a specific mod to be used in the Powers menu display.</summary>
    /// <param name="uniqueID">The unique ID of the mod (typically yours) from the manifest.</param>
    /// <param name="displayName">A function that returns a string to be used as the display text when hovering over the power category icon.</param>
    /// <param name="iconTexture">An asset name for the icon you wish to use for the power category e.g. "Mods/UniqueId/PowersTab"</param>
    /// <param name="sourceRectPosition">The X and Y coordinates of the top left of your icon as found in the asset specified by iconTexture.</param>
    /// <param name="sourceRectSize">The width and height of the icon as found in the asset specified by iconTexture.</param>
    /// <returns>
    /// <para>A bool describing whether the power category was successfully registered.</para>
    /// <para>True if the power category was successfully registered, false if the power category was not successfully registered for some reason.</para>
    /// </returns>
    bool RegisterPowerCategory(string uniqueID, Func<string> displayName, string iconTexture, Point sourceRectPosition, Point sourceRectSize);
}
